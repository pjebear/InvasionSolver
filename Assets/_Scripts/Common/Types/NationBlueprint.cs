﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace Common.Types
{
    [XmlRoot("NationBlueprint")]
    public class NationBlueprint
    {
        public bool IsDefeated { get { return mNumCapturedCities == Fortifications.Count; } }
        public int NumCitiesRemaining { get { return Fortifications.Count - mNumCapturedCities; } }

        [XmlArray("Fortifications"), XmlArrayItem("Fortification", typeof(Fortification))]
        public List<Fortification> Fortifications { get; private set; }

        [XmlAttribute("NationSize", typeof(int))]
        public int NationSize { get; private set; }

        private Dictionary<int, HashSet<int>> mInvasionOrderingsMap;
        private Dictionary<int, int> mNumProtectorsMap;
        [XmlIgnore()]
        public Dictionary<int, bool> IsCapturedMap { get; private set; }
        private int mNumCapturedCities;
        private List<Fortification> mCachedBorderCities;
        private List<Fortification> mCachedInternalCities; // REMOVE LATER
        private bool mLoadedFromFile = true;

        public NationBlueprint()
        {
            Fortifications = new List<Fortification>();
            mInvasionOrderingsMap = new Dictionary<int, HashSet<int>>();
            mNumProtectorsMap = new Dictionary<int, int>();
            IsCapturedMap = new Dictionary<int, bool>();
        }

        public NationBlueprint(int numFortifications)
        {
            mLoadedFromFile = false;
            Fortifications = new List<Fortification>();
            IsCapturedMap = new Dictionary<int, bool>();
            mNumCapturedCities = 0;
            mInvasionOrderingsMap = new Dictionary<int, HashSet<int>>();
            NationSize = 3;
            mNumProtectorsMap = new Dictionary<int, int>();
            PopulateFortifications(numFortifications);
        }

        public NationBlueprint(bool[,] fortificationGrid)
        {
            mLoadedFromFile = false;
            Fortifications = new List<Fortification>();
            IsCapturedMap = new Dictionary<int, bool>();
            mNumCapturedCities = 0;
            mInvasionOrderingsMap = new Dictionary<int, HashSet<int>>();
            NationSize = fortificationGrid.GetLength(0);
            mNumProtectorsMap = new Dictionary<int, int>();
            PopulateFortifications(fortificationGrid);
        }

        public NationBlueprint(NationBlueprint previousNation, List<Fortification> captured)
        {
            mLoadedFromFile = false;
            if (previousNation.mLoadedFromFile)
            {
                previousNation.BeginInvasion(Vector2.right);
            }
            Fortifications = new List<Fortification>(); // keep a seperate copy if we want to change forts 
            foreach (Fortification fort in previousNation.Fortifications)
            {
                Fortifications.Add(new Fortification(fort));
            }
            IsCapturedMap = new Dictionary<int, bool>();
            foreach (var capturedPair in previousNation.IsCapturedMap)
            {
                IsCapturedMap.Add(capturedPair.Key, capturedPair.Value);
            }

            mInvasionOrderingsMap = previousNation.mInvasionOrderingsMap;

            mNumProtectorsMap = new Dictionary<int, int>();
            foreach (var protectorsPair in previousNation.mNumProtectorsMap)
            {
                mNumProtectorsMap.Add(protectorsPair.Key, protectorsPair.Value);
            }

            // update with captured cities
            mNumCapturedCities = previousNation.mNumCapturedCities + captured.Count;
            foreach (Fortification fort in captured)
            {
                foreach (int wasProtected in mInvasionOrderingsMap[fort.FortificationId])
                {
                    mNumProtectorsMap[wasProtected]--;
                }
                IsCapturedMap[fort.FortificationId] = true;
            }

            UpgradeUpgradeForts();
        }

        private void UpgradeUpgradeForts()
        {
            foreach (Fortification fort in Fortifications)
            {
                int numLevelsIncreased = mInvasionOrderingsMap[fort.FortificationId].Count > 0 ? 2 : 1;
                fort.UpdgredFortifications(numLevelsIncreased);
            }
        }

        public void BeginInvasion(Vector2 invasionDirection) // north south east west
        {
            Vector2 invasionPoint = -invasionDirection * NationSize / 2;
            if (mLoadedFromFile)
            {
                foreach (Fortification fort in Fortifications)
                {
                    IsCapturedMap.Add(fort.FortificationId, false); // all forts start off not captured
                    mNumProtectorsMap.Add(fort.FortificationId, 0);
                    mInvasionOrderingsMap.Add(fort.FortificationId, new HashSet<int>());
                }
            }
            

            var sortedFortifications = new List<Fortification>(Fortifications);
            sortedFortifications.Sort(delegate (Fortification lhs, Fortification rhs)
            {
                return (invasionPoint - rhs.PositionInNation(NationSize)).sqrMagnitude.CompareTo((invasionPoint - lhs.PositionInNation(NationSize)).sqrMagnitude);
            });
            for (int closestToInvasion = sortedFortifications.Count - 1; closestToInvasion > 0; --closestToInvasion)
            {
                Fortification protectingFortification = sortedFortifications[closestToInvasion];
                for (int protectedCity = closestToInvasion - 1; protectedCity >= 0; --protectedCity)
                {
                    Fortification protectedFortification = sortedFortifications[protectedCity];
                    Vector2 deltaProtector = protectingFortification.PositionInNation(NationSize) - invasionPoint;
                    Vector2 deltaProtected = protectedFortification.PositionInNation(NationSize) - invasionPoint;
                    Vector2 protectionLine = protectedFortification.PositionInNation(NationSize) - protectingFortification.PositionInNation(NationSize);
                    float rowsBehind = Vector2.Dot(invasionDirection, protectionLine);
                    float columnsBehind = 0;
                    if (invasionDirection.x == 0)
                    {
                        columnsBehind = Mathf.Abs(protectionLine.x);
                    }
                    else
                    {
                        columnsBehind = Mathf.Abs(protectionLine.y);
                    }

                    if (rowsBehind > 0 && columnsBehind <= rowsBehind)
                    {
                        mInvasionOrderingsMap[protectingFortification.FortificationId].Add(protectedFortification.FortificationId);
                        mNumProtectorsMap[protectedFortification.FortificationId]++;
                    }
                }
            }
        }

        public List<Fortification> GetBorderCities()
        {
            if (mCachedBorderCities == null)
            {
                mCachedBorderCities = new List<Fortification>();
                foreach (var protectionPair in mNumProtectorsMap)
                {
                    if (protectionPair.Value == 0 // no protectors
                        && !IsCapturedMap[protectionPair.Key] // hasn't been captured yet
                        )
                    {
                        mCachedBorderCities.Add(Fortifications[protectionPair.Key]);
                    }
                }
            }
            return mCachedBorderCities;
        }

        public List<Fortification> GetInternalCities()
        {
            if (mCachedInternalCities == null)
            {
                mCachedInternalCities = new List<Fortification>();
                foreach (var protectionPair in mNumProtectorsMap)
                {
                    if (protectionPair.Value > 0 // no protectors
                        && !IsCapturedMap[protectionPair.Key] // hasn't been captured yet
                        )
                    {
                        mCachedInternalCities.Add(Fortifications[protectionPair.Key]);
                    }
                }
            }
            return mCachedInternalCities;
        }

        public float GetNationValue()
        {
            return NumCitiesRemaining * 1000f;
        }

        public new string ToString()
        {
            return "Cities to capture: " + Fortifications.Count;
        }

        private void PopulateFortifications(int numFortsToPopulate)
        {
            Debug.Assert(NationSize % 2 == 1);
            Fortification[,] fortGrid = new Fortification[NationSize, NationSize];
            int numGridElements = NationSize * NationSize;

            for (int column = 0; column < NationSize && Fortifications.Count < numFortsToPopulate; column++)
            {
                for (int row = 0; row < NationSize && Fortifications.Count < numFortsToPopulate; row++)
                {
                    int product = row * NationSize;
                    if ((numFortsToPopulate - Fortifications.Count) == (numGridElements - (product + column)) // have to place the rest of the forts
                        || UnityEngine.Random.Range(1f, 1f) >= 0.5f)
                    {
                        Fortification fort = new Fortification(product + column, Fortifications.Count);
                        Fortifications.Add(fort);
                        IsCapturedMap.Add(fort.FortificationId, false); // all forts start off not captured
                        mNumProtectorsMap.Add(fort.FortificationId, 0);
                        mInvasionOrderingsMap.Add(fort.FortificationId, new HashSet<int>());
                    }
                }
            }
        }

        private void PopulateFortifications(bool[,] fortificationGrid)
        {
            int numGridElements = NationSize * NationSize;

            for (int column = 0; column < NationSize; column++)
            {
                for (int row = 0; row < NationSize; row++)
                {
                    int product = row * NationSize;
                    if (fortificationGrid[row,column])
                    {
                        Fortification fort = new Fortification(product + column, Fortifications.Count);
                        Fortifications.Add(fort);
                        IsCapturedMap.Add(fort.FortificationId, false); // all forts start off not captured
                        mNumProtectorsMap.Add(fort.FortificationId, 0);
                        mInvasionOrderingsMap.Add(fort.FortificationId, new HashSet<int>());
                    }
                }
            }
        }

        public void Save(string filePath)
        {
            var serializer = new XmlSerializer(typeof(NationBlueprint));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        public static NationBlueprint Load(string filePath)
        {
            var serializer = new XmlSerializer(typeof(NationBlueprint));
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                return serializer.Deserialize(stream) as NationBlueprint;
            }
        }
    }
}
