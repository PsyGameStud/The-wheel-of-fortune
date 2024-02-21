using UnityEngine ;

namespace PsyGameStud.PickerWheelUI 
{
   [System.Serializable]
    public class WheelPiece 
    {
        public Sprite Icon ;
        public CurrencyType CurrencyType;

        [Tooltip ("Reward amount")] public int Amount ;

        [Tooltip ("Probability in %")] 
        [Range (0f, 100f)] 
        public float Chance = 100f ;

        [HideInInspector] public int Index ;
        [HideInInspector] public double _weight = 0f ;
    }

    public enum CurrencyType : int
    {
        Gold = 0,
        Gem = 1,
        Rubin = 2,
        None = 3,
    }
}
