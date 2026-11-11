namespace ForgottenVillage {
    public static class PowerGridLevels {
        public const int Count = 3;
        public static PowerGridLevel Create(int round) {
            switch(round) {
                case 0: return new PowerGridLevel("RESTORE THE LIGHTS",new[]{6,6},new[]{2,3,4});
                case 1: return new PowerGridLevel("RESTART THE WORKSHOP",new[]{7,6},new[]{3,4,2,3},false,0);
                case 2: return new PowerGridLevel("RESCUE THE ARCHIVE",new[]{8,6},new[]{4,3,5,2,2},true);
                default: throw new System.ArgumentOutOfRangeException(nameof(round));
            }
        }
    }
}
