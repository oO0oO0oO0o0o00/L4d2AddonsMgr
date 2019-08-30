using System.Runtime.InteropServices;
using System.Windows.Media;

namespace L4d2AddonsMgr {
    internal static class Windowsifier {

        // Get accent color for win10 or glass color for win 7.
        // https://stackoverflow.com/questions/13660976/get-the-active-color-of-windows-8-automatic-color-theme
        [DllImport("dwmapi.dll", EntryPoint = "#127")]
        private static extern void DwmGetColorizationParameters(ref DwmColorizationParams parameters);

        public static Color GetAccentColor() {
            var parameters = new DwmColorizationParams();
            DwmGetColorizationParameters(ref parameters);
            return Color.FromArgb((byte)(parameters.ColorizationColor >> 24), (byte)(parameters.ColorizationColor >> 16),
                (byte)(parameters.ColorizationColor >> 8), (byte)(parameters.ColorizationColor));
        }

        public static string GetColorizationDebugString() {
            var parameters = new DwmColorizationParams();
            DwmGetColorizationParameters(ref parameters);
            return string.Format("{0}, {1}, {2}, {3}\n{4}, {5}, {6}",
                parameters.ColorizationColor, parameters.ColorizationAfterglow,
                parameters.ColorizationColorBalance, parameters.ColorizationAfterglowBalance,
                parameters.ColorizationBlurBalance, parameters.ColorizationGlassReflectionIntensity,
                parameters.ColorizationOpaqueBlend);
        }

        private struct DwmColorizationParams {
            public uint ColorizationColor,
                ColorizationAfterglow,
                ColorizationColorBalance,
                ColorizationAfterglowBalance,
                ColorizationBlurBalance,
                ColorizationGlassReflectionIntensity,
                ColorizationOpaqueBlend;
        }
    }
}
