using UnityEngine;

namespace DoubleSlit
{
    public enum DSE_LaserColor
    {
        Red = 0,
        Yellow = 1,
        Green = 2,
        Blue = 3,
        Violet = 4
    }

    public static class DSE_ColorHelper
    {
        public static Color ToUnityColor(DSE_LaserColor laserColor)
        {
            return laserColor switch
            {
                DSE_LaserColor.Red => new Color(1f, 0.05f, 0.05f, 1f),
                DSE_LaserColor.Yellow => new Color(1f, 0.92f, 0.0f, 1f),
                DSE_LaserColor.Green => new Color(0.0f, 0.95f, 0.1f, 1f),
                DSE_LaserColor.Blue => new Color(0.1f, 0.3f, 1.0f, 1f),
                DSE_LaserColor.Violet => new Color(0.6f, 0.0f, 1.0f, 1f),
                _ => Color.white
            };
        }

        public static string ToIndonesianName(DSE_LaserColor laserColor)
        {
            return laserColor switch
            {
                DSE_LaserColor.Red => "Merah",
                DSE_LaserColor.Yellow => "Kuning",
                DSE_LaserColor.Green => "Hijau",
                DSE_LaserColor.Blue => "Biru",
                DSE_LaserColor.Violet => "Ungu",
                _ => "Tidak Dikenal"
            };
        }

        public static float GetWavelengthNm(DSE_LaserColor laserColor)
        {
            return laserColor switch
            {
                DSE_LaserColor.Red => 700f,
                DSE_LaserColor.Yellow => 580f,
                DSE_LaserColor.Green => 532f,
                DSE_LaserColor.Blue => 450f,
                DSE_LaserColor.Violet => 405f,
                _ => 532f
            };
        }
    }
}