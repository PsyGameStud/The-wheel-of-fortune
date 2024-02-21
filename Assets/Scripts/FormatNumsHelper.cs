using UnityEngine;

namespace PsyGameStud.Gameplay
{
    public static class FormatNumsHelper
    {
        private static string[] _names = new string[]
        {
            "",
            "k",
            "m",
            "b",
            "T"
        };

        public static string FormatNum(float value)
        {
            if (value == 0) return "0";

            int i = 0;

            while (i + 1 < _names.Length && value > 1000)
            {
                value /= 1000;
                i++;
            }

            return value.ToString(format: "#.##") + _names[i];
        }
    }
}
