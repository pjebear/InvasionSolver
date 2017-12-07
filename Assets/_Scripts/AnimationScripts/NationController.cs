using Common.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets._Scripts.AnimationScripts
{
    public class NationController : MonoBehaviour
    {
        public FortController FortPrefab;

        private Dictionary<int, FortController> mFortLookup;
        private NationBlueprint mNation;
        private List<Fortification> mDestroyedForts;

        private void Awake()
        {
            mFortLookup = new Dictionary<int, FortController>();
            mDestroyedForts = new List<Fortification>();
        }

        public void ResetNation()
        {
            foreach (FortController fort in mFortLookup.Values)
            {
                Destroy(fort.gameObject);
            }
            mFortLookup.Clear();
        }

        public void InitializeNation(NationBlueprint nation)
        {
            mNation = nation;
            Vector2 fortDimensions = FortPrefab.GetComponent<RectTransform>().rect.size + Vector2.one * 40f; // padding
            Vector2 nationSize = fortDimensions * nation.NationSize;
            Vector2 currentSize = GetComponent<RectTransform>().rect.size;
            float scaleX = currentSize.x / nationSize.x;
            float scaleY = currentSize.y / nationSize.y;
            float scaleFactor = Mathf.Min(scaleX, scaleY, 1);
            fortDimensions *= scaleFactor;

            // create a fortController for each fort
            foreach (Fortification fort in nation.Fortifications)
            {
                FortController script = script = Instantiate(FortPrefab, transform);  
                Vector2 positionInNation = fort.PositionInNation(nation.NationSize);
                script.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
                script.transform.localPosition = new Vector2(positionInNation.x * fortDimensions.x, positionInNation.y * fortDimensions.y);
                script.Initialize(fort.Defense, fort.FortificationId, fort.FortificationLevel);
                mFortLookup.Add(fort.FortificationId, script);
            }
        }

        public void UpgradeForts(float overSeconds)
        {
            mNation = new NationBlueprint(mNation, mDestroyedForts);
            mDestroyedForts.Clear();
            foreach (Fortification fort in mNation.Fortifications)
            {
               if (!mNation.IsCapturedMap[fort.FortificationId])
               {
                    mFortLookup[fort.FortificationId].Upgrade(fort.Defense, fort.FortificationLevel, overSeconds);
               }
            }
        }

        public void DestroyFort(Fortification fort, float overSeconds)
        {
            mFortLookup[fort.FortificationId].Demolish(overSeconds);
            mDestroyedForts.Add(fort);
        }

        public void DestroyForts(List<Fortification> forts, float overSeconds)
        {
            foreach (Fortification fort in forts)
            {
                mFortLookup[fort.FortificationId].Demolish(overSeconds);
            }
            mDestroyedForts.AddRange(forts);
        }
    }
}
