
namespace Common.Constants
{
    using Enums;

    static class ArmyConstants
    {
        public static ushort SOLDIER_OFFENSE = 50;
        public static ushort SOLDIER_HEALTH = 100;
        public static ushort SOLDIER_STACKING_DEFENCE = 2;
        public static InvasionPosition SOLDIER_POSITION = InvasionPosition.Front;

        public static ushort ARCHER_OFFENSE = 100;
        public static ushort ARCHER_HEALTH = 50;
        public static ushort ARCHER_STACKING_OFFENSE = 2;
        private static InvasionPosition ARCHER_POSITION = InvasionPosition.Rear;

        public static ushort HEALER_OFFENSE = 0;
        public static ushort HEALER_HEALTH = 40;
        public static ushort HEALER_HEALING = 10;
        public static ushort HEALER_STACKING_HEALING = 1;
        private static InvasionPosition HEALER_POSITION = InvasionPosition.Middle;

        public static ushort GetOffenseForInvader(InvaderType type)
        {
            switch (type)
            {
                case (InvaderType.Soldier):
                    return SOLDIER_OFFENSE;
                case (InvaderType.Archer):
                    return ARCHER_OFFENSE;
                case (InvaderType.Healer):
                    return HEALER_OFFENSE;
            }
            UnityEngine.Debug.LogError("GetOffenseForInvader(): Recieved unsupported invader type " + type.ToString());
            return 0;
        }

        public static ushort GetHealthForInvader(InvaderType type)
        {
            switch (type)
            {
                case (InvaderType.Soldier):
                    return SOLDIER_HEALTH;
                case (InvaderType.Archer):
                    return ARCHER_HEALTH;
                case (InvaderType.Healer):
                    return HEALER_HEALTH;
            }
            UnityEngine.Debug.LogError("GetHealthForInvader(): Recieved unsupported invader type " + type.ToString());
            return 0;
        }

        public static InvasionPosition GetPositionForInvader(InvaderType type)
        {
            switch (type)
            {
                case (InvaderType.Soldier):
                    return SOLDIER_POSITION;
                case (InvaderType.Archer):
                    return ARCHER_POSITION;
                case (InvaderType.Healer):
                    return HEALER_POSITION;
            }
            UnityEngine.Debug.LogError("GetPositionForInvader(): Recieved unsupported invader type " + type.ToString());
            return InvasionPosition.Front;
        }
    }

    static class FortificationConstants
    {
        private static ushort FORTIFICATION_DEFENSE = 80;
        private static ushort FORTIFICATION_OFFENSE = 100;

        public static ushort GetFortificationOffense()
        {
            return FORTIFICATION_OFFENSE;
        }

        public static ushort GetFortificationDefense()
        {
            return FORTIFICATION_DEFENSE;
        }
    }
}
