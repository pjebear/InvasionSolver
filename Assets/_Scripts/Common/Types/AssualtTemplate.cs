using Common.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Common.Types
{
    [XmlRoot("AssaultTemplate")]
    public class AssaultTemplate
    {
        [XmlElement("ToAssault", typeof(Fortification))]
        public Fortification ToAssault { get; private set; }

        [XmlElement("Attackers", typeof(ArmyBlueprint))]
        public ArmyBlueprint Attackers { get; private set; }

        [XmlElement("mCalculatedFortDamage", typeof(ushort))]
        public ushort CalculatedFortDamage { get; private set; }

        public AssaultTemplate()
        {
            ToAssault = null;
            Attackers = null;
            CalculatedFortDamage = 0;
        }

        public AssaultTemplate(Fortification toAssault, ArmyBlueprint invaders, ushort fortDamage)
        {
            ToAssault = toAssault;
            Attackers = invaders;
            CalculatedFortDamage = fortDamage;
        }

        public static AssaultTemplate GetAssualtTemplate(Fortification toAssault, ArmyBlueprint subArmy)
        {
            bool isPossibleAssault = true;
            ushort simulationDamage = 0;

            if (!toAssault.IsPlaceHolderFort)
            {
                float damageRatio = 1;
                if (subArmy.Damage > 0)
                {
                    damageRatio = (float)toAssault.Defense / subArmy.Damage;
                }

                // The more damage an army has the faster it will take the city
                simulationDamage = (ushort)(toAssault.Offense * damageRatio);
                ushort damageReduction =
                    (ushort)(subArmy.Soldiers.Count > 1
                    ? (subArmy.Soldiers.Count - 1 * ArmyConstants.SOLDIER_STACKING_DEFENCE) * subArmy.Soldiers.Count
                    : 0);
                simulationDamage = (ushort)(damageReduction > simulationDamage ? 0 : simulationDamage - damageReduction);

                isPossibleAssault = subArmy.Health > simulationDamage && subArmy.Damage >= toAssault.Defense || toAssault.IsPlaceHolderFort;
            }
            // else fort damage is zero so just put it through

            return isPossibleAssault ?
                new AssaultTemplate(toAssault, subArmy, simulationDamage)
                : null;
        }

        public ArmyBlueprint AssaultFortification(bool healGroupAfter = true)
        {
            // Create new copy of army so template can be stored for inspection purposes
            List<ushort> soldiers = new List<ushort>(Attackers.Soldiers);
            List<ushort> healers = new List<ushort>(Attackers.Healers);
            List<ushort> archers = new List<ushort>(Attackers.Archers);

            if (!ToAssault.IsPlaceHolderFort)
            {
                ushort remainingDamage = CalculatedFortDamage;
                remainingDamage = ApplyDamageToInvaderGroup(soldiers, remainingDamage);
                remainingDamage = ApplyDamageToInvaderGroup(healers, remainingDamage);
                remainingDamage = ApplyDamageToInvaderGroup(archers, remainingDamage);

                Debug.Assert(remainingDamage == 0, "After applying damage to an invasion group there is still " + remainingDamage + " damage to apply");
            }
            ArmyBlueprint armyPostBattle = new ArmyBlueprint(soldiers, healers, archers);
            if (healGroupAfter)
            {
                armyPostBattle.Heal();
            }
            return armyPostBattle;
        }

        private static ushort ApplyDamageToInvaderGroup(List<ushort> invaders, ushort damageToDistribute)
        {
            //ASSUMES INVADERS IS SORTED ASCENDING
            if (invaders.Count == 0 || damageToDistribute == 0)
            {
                return damageToDistribute;
            }

            ushort totalHealthOfGroup = 0;
            foreach (ushort invader in invaders)
            {
                totalHealthOfGroup += invader;
            }

            // if total damage to deal exceeds group health, kill all and return
            if (totalHealthOfGroup <= damageToDistribute)
            {
                for (int invader = 0; invader < invaders.Count; ++invader)
                {
                    invaders[invader] = 0;
                }
                return (ushort)(damageToDistribute - totalHealthOfGroup);
            }

            int lowestHealthIdx = 0;
            ushort lowestHealth = ushort.MaxValue;

            while (damageToDistribute > 0)
            {
                while ((lowestHealth = invaders[lowestHealthIdx]) == 0)
                {
                    lowestHealthIdx++;
                }

                ushort minimumDamageToDistribute = (ushort)(damageToDistribute / (invaders.Count - lowestHealthIdx));

                if (minimumDamageToDistribute >= lowestHealth)
                {
                    for (int invaderIdx = lowestHealthIdx; invaderIdx < invaders.Count; ++invaderIdx)
                    {
                        invaders[invaderIdx] -= lowestHealth;
                        damageToDistribute -= lowestHealth;
                    }
                }
                else
                {
                    if (minimumDamageToDistribute > 0)
                    {
                        for (int invaderIdx = lowestHealthIdx; invaderIdx < invaders.Count; ++invaderIdx)
                        {
                            invaders[invaderIdx] -= minimumDamageToDistribute;
                            damageToDistribute -= minimumDamageToDistribute;
                        }
                    }
                    for (int invaderIdx = lowestHealthIdx; invaderIdx < invaders.Count && damageToDistribute > 0; ++invaderIdx)
                    {
                        invaders[invaderIdx]--;
                        damageToDistribute--;
                    }
                    Debug.Assert(damageToDistribute == 0);
                }
            }
            Debug.Assert(damageToDistribute == 0);
            return damageToDistribute;
        }
    }
}
