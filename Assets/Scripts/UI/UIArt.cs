using UnityEngine;

namespace TapBlast
{
    /// <summary>Theme colour tokens for state feedback set from code. Static colours, sprites and
    /// layouts are saved Canvas assets; keep these in step with the authored palette.</summary>
    public static class UIArt
    {
        public static readonly Color Ink = new Color32(30, 34, 64, 255);          // Headings, primary text.
        public static readonly Color Subtle = new Color32(107, 113, 144, 255);    // Secondary text (4.8:1 on white).
        public static readonly Color Primary = new Color32(91, 108, 255, 255);    // Brand blue-violet.
        public static readonly Color Accent = new Color32(155, 107, 255, 255);    // Gradient partner of Primary.
        public static readonly Color Success = new Color32(34, 170, 96, 255);
        public static readonly Color Warning = new Color32(255, 128, 31, 255);
        // Unearned stars hide; a lavender star slot drawn behind each star shows instead.
        public static readonly Color Unearned = new Color(1, 1, 1, 0);
        // Legacy names kept for existing call sites.
        public static readonly Color Card = Color.white;
        public static readonly Color Cyan = Primary;
        public static readonly Color Muted = Subtle;
        public static readonly Color Gold = Warning;

        /// <summary>Fixed-width amount for compact slots (header pill, wallet cards): full digits up to
        /// 99,999, then 100K / 1.23M / 2.14B (truncated, never rounded up). Detail views show N0.</summary>
        public static string Compact(long value)
        {
            if (value < 100000) return value.ToString("N0");
            long divisor = value >= 1000000000 ? 1000000000 : value >= 1000000 ? 1000000 : 1000;
            string unit = divisor == 1000000000 ? "B" : divisor == 1000000 ? "M" : "K";
            long whole = value / divisor, places = whole >= 100 ? 1 : whole >= 10 ? 10 : 100;
            return ((decimal)(value / (divisor / places)) / places).ToString("0.##") + unit;
        }
    }
}
