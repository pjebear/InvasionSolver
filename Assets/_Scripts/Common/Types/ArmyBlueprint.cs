using Common.Constants;
using Common.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Common.Types
{
    [XmlRoot("ArmyBlueprint")]
    public class ArmyBlueprint
    {
        public static float InitialArmyValue { get; private set; }
        [XmlArray("Soldiers"), XmlArrayItem(typeof(ushort))]
        public List<ushort> Soldiers;
        [XmlArray("Healers"), XmlArrayItem(typeof(ushort))]
        public List<ushort> Healers;
        [XmlArray("Archers"), XmlArrayItem(typeof(ushort))]
        public List<ushort> Archers;

        public ushort Damage
        {
            get
            {
                return (ushort)(Soldiers.Count * ArmyConstants.SOLDIER_OFFENSE
                    + Healers.Count * ArmyConstants.HEALER_OFFENSE
                    + Archers.Count * (ArmyConstants.ARCHER_OFFENSE + ArmyConstants.ARCHER_STACKING_OFFENSE * (Archers.Count - 1))
                    );
            }
        }
        private ushort mCachedHealth;
        private ushort mCachedMaxHealth;
        public ushort MaxHealth
        {
            get
            {
                if (mCachedMaxHealth > 0)
                    return mCachedMaxHealth;

                mCachedMaxHealth += (ushort)(Soldiers.Count * ArmyConstants.SOLDIER_HEALTH);
                mCachedMaxHealth += (ushort)(Healers.Count * ArmyConstants.HEALER_HEALTH);
                mCachedMaxHealth += (ushort)(Archers.Count * ArmyConstants.ARCHER_HEALTH);

                return mCachedMaxHealth;
            }
        }
        public ushort Health
        {
            get
            {
                if (mCachedHealth > 0)
                    return mCachedHealth;

                foreach (ushort soldier in Soldiers)
                {
                    mCachedHealth += soldier;
                    mCachedMaxHealth += ArmyConstants.SOLDIER_HEALTH;
                }
                foreach (ushort soldier in Healers)
                {
                    mCachedHealth += soldier;
                    mCachedMaxHealth += ArmyConstants.HEALER_HEALTH;
                }
                foreach (ushort soldier in Archers)
                {
                    mCachedHealth += soldier;
                    mCachedMaxHealth += ArmyConstants.ARCHER_HEALTH;
                }
                return mCachedHealth;
            }
        }
        public bool CanHeal
        {
            get
            {
                return Healers.Count > 0;
            }
        }
        public int Size
        {
            get { return Soldiers.Count + Healers.Count + Archers.Count; }
        }
        public bool IsEmpty
        {
            get
            {
                return Soldiers.Count == 0
                    && Healers.Count == 0
                    && Archers.Count == 0;
            }
        }

        // Only called when first making the army!!
        public ArmyBlueprint(int numSoldiers, int numHealers, int numArchers)
           : this()
        {
            ushort soldierHealth = ArmyConstants.GetHealthForInvader(InvaderType.Soldier),
                healerHealth = ArmyConstants.GetHealthForInvader(InvaderType.Healer),
                archerHealth = ArmyConstants.GetHealthForInvader(InvaderType.Archer);

            for (int i = 0; i < numSoldiers; ++i)
            {
                Soldiers.Add(soldierHealth);
            }
            for (int i = 0; i < numHealers; ++i)
            {
                Healers.Add(healerHealth);
            }
            for (int i = 0; i < numArchers; ++i)
            {
                Archers.Add(archerHealth);
            }
            Debug.Assert(InitialArmyValue == 0);
            InitialArmyValue = CalculateArmyValue();
        }

        public ArmyBlueprint(List<ushort> soldiers, List<ushort> healers, List<ushort> archers)
        {
            Soldiers = soldiers;
            Healers = healers;
            Archers = archers;
        }

        public static ArmyBlueprint MergeSubArmies(List<ArmyBlueprint> subArmies)
        {
            List<ushort> soldiers = new List<ushort>();
            List<ushort> archers = new List<ushort>();
            List<ushort> healers = new List<ushort>();

            foreach (ArmyBlueprint subArmy in subArmies)
            {
                for (int i = 0; i < subArmy.Soldiers.Count; ++i)
                {
                    if (subArmy.Soldiers[i] > 0)
                    {
                        soldiers.Add(subArmy.Soldiers[i]);
                    }

                }
                for (int i = 0; i < subArmy.Healers.Count; ++i)
                {
                    if (subArmy.Healers[i] > 0)
                    {
                        healers.Add(subArmy.Healers[i]);
                    }
                }
                for (int i = 0; i < subArmy.Archers.Count; ++i)
                {
                    if (subArmy.Archers[i] > 0)
                    {
                        archers.Add(subArmy.Archers[i]);
                    }
                }
            }
            soldiers.Sort();
            healers.Sort();
            archers.Sort();
            ArmyBlueprint merged = new ArmyBlueprint(soldiers, healers, archers);

            return merged;
        }

        public ArmyBlueprint()
        {
            Soldiers = new List<ushort>();
            Healers = new List<ushort>();
            Archers = new List<ushort>();
        }

        public float CalculateArmyValue()
        {
            float armyValue = 0;
            foreach (ushort soldier in Soldiers)
            {
                armyValue += 10 + soldier / (float)ArmyConstants.SOLDIER_HEALTH;
            }
            foreach (ushort soldier in Healers)
            {
                armyValue += 10 + soldier / (float)ArmyConstants.HEALER_HEALTH;
            }
            foreach (ushort soldier in Archers)
            {
                armyValue += 10 + soldier / (float)ArmyConstants.ARCHER_HEALTH;
            }
            return armyValue;
        }

        public bool Equals(ArmyBlueprint toCompare)
        {
            if (Soldiers.Count == toCompare.Soldiers.Count
                && Healers.Count == toCompare.Healers.Count
                && Archers.Count == toCompare.Archers.Count)
            {
                for (int i = 0; i < Soldiers.Count; i++)
                {
                    if (Soldiers[i] != toCompare.Soldiers[i])
                    {
                        return false;
                    }
                }
                for (int i = 0; i < Healers.Count; i++)
                {
                    if (Healers[i] != toCompare.Healers[i])
                    {
                        return false;
                    }
                }
                for (int i = 0; i < Archers.Count; i++)
                {
                    if (Archers[i] != toCompare.Archers[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public new string ToString()
        {
            string outputString = "{";

            outputString += "S: ";
            foreach (ushort invader in Soldiers)
            {
                outputString += invader + " ";
            }
            outputString += " H: ";
            foreach (ushort invader in Healers)
            {
                outputString += invader + " ";
            }
            outputString += " A: ";
            foreach (ushort invader in Archers)
            {
                outputString += invader + " ";
            }
            return outputString + "}";
        }

        public void Heal()
        {
            ushort amountToHeal = 0;
            foreach (ushort healer in Healers)
            {
                if (healer > 0) // healer wasn't killed during attack
                    amountToHeal += (ushort)(ArmyConstants.HEALER_HEALING + ArmyConstants.HEALER_STACKING_HEALING * (Healers.Count - 1));
            }

            if (amountToHeal > 0)
            {
                for (int i = 0; i < Soldiers.Count; ++i)
                {
                    if (Soldiers[i] > 0)
                    {
                        Soldiers[i] += amountToHeal;
                        if (Soldiers[i] > ArmyConstants.SOLDIER_HEALTH)
                        {
                            Soldiers[i] = ArmyConstants.SOLDIER_HEALTH;
                        }
                    }
                }
                for (int i = 0; i < Healers.Count; ++i)
                {
                    if (Healers[i] > 0)
                    {
                        Healers[i] += amountToHeal;
                        if (Healers[i] > ArmyConstants.HEALER_HEALTH)
                        {
                            Healers[i] = ArmyConstants.HEALER_HEALTH;
                        }
                    }
                }
                for (int i = 0; i < Archers.Count; ++i)
                {
                    if (Archers[i] > 0)
                    {
                        Archers[i] += amountToHeal;
                        if (Archers[i] > ArmyConstants.ARCHER_HEALTH)
                        {
                            Archers[i] = ArmyConstants.ARCHER_HEALTH;
                        }
                    }
                }
            }
        }

        public void Save(string filePath)
        {
            var serializer = new XmlSerializer(typeof(ArmyBlueprint));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        public static ArmyBlueprint Load(string filePath)
        {
            var serializer = new XmlSerializer(typeof(ArmyBlueprint));
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                return serializer.Deserialize(stream) as ArmyBlueprint;
            }
        }
    }
}
