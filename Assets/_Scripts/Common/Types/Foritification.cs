using Common.Constants;
using System.Xml.Serialization;
using UnityEngine;

namespace Common.Types
{
    [XmlRoot("Fortification")]
    public class Fortification
    {
        public ushort Offense { get { return (ushort)(IsPlaceHolderFort ? 0 : FortificationConstants.GetFortificationOffense(FortificationLevel)); } }
        public ushort Defense { get { return (ushort)(IsPlaceHolderFort ? 0 : FortificationConstants.GetFortificationDefense(FortificationLevel)); } }
        [XmlAttribute("Position", typeof(int))]
        public int Position { get; private set; }
        [XmlAttribute("IsPlaceHolderFort", typeof(bool))]
        public bool IsPlaceHolderFort { get; private set; }
        [XmlAttribute("FortificationId", typeof(int))]
        public int FortificationId { get; private set; }
        public int FortificationLevel { get; private set; }

        private int mCachedNationSize = -1;
        private Vector2 mCachedPosition;

        public Fortification()
        {
            IsPlaceHolderFort = true;
            FortificationId = -1;
        }

        public Fortification(Fortification copy)
        {
            Position = copy.Position;
            IsPlaceHolderFort = copy.IsPlaceHolderFort;
            FortificationId = copy.FortificationId;
            FortificationLevel = copy.FortificationLevel;
        }

        public Fortification(int position, int id)
        {
            FortificationId = id;
            IsPlaceHolderFort = false;
            Position = position;
        }

        public Vector2 PositionInNation(int nationSize)
        {
            if (mCachedNationSize != nationSize)
            {
                mCachedNationSize = nationSize;
                float row = Position / nationSize - nationSize / 2;
                float column = Position % nationSize - nationSize / 2;
                mCachedPosition = new Vector2(column, row);
            }

            return mCachedPosition;
        }

        public void UpdgredFortifications(int byNumLevels)
        {
            FortificationLevel += byNumLevels;
        }
    }
}
