using UnityEngine;

namespace JanitorsCloset
{
    /// <summary>
    /// UI scaling for IMGUI mod windows.
    /// Final scale = KSP UI Scale × mod percent (50%–150%), matching OrbitalPayloadCalculator.
    /// </summary>
    public static class UIScale
    {
        static Matrix4x4 savedMatrix;

        public static bool IsActive { get; private set; }

        public static float DefaultUiScalePercent
        {
            get
            {
                if (Screen.height >= 2160)
                    return 75f;
                if (Screen.height >= 1440)
                    return 85f;
                return 100f;
            }
        }

        public static float ModUiScalePercent
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                    return DefaultUiScalePercent;
                var settings = HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>();
                if (settings == null)
                    return DefaultUiScalePercent;
                if (settings.uiScaleAuto)
                    return DefaultUiScalePercent;
                return settings.uiScalePercent;
            }
        }

        public static float Factor
        {
            get
            {
                float ksp = GameSettings.UI_SCALE;
                if (GameSettings.UI_SCALE_APPS > 1f)
                    ksp *= GameSettings.UI_SCALE_APPS;
                if (ksp <= 1.01f && Screen.height > 1080)
                    ksp = Mathf.Max(ksp, Mathf.Min(1.5f, Mathf.Sqrt((float)Screen.height / 1080f)));

                float mod = Mathf.Clamp(ModUiScalePercent / 100f, 0.5f, 1.5f);
                return ksp * mod;
            }
        }

        public static int Scale(int value) => Mathf.Max(1, Mathf.RoundToInt(value * Factor));

        public static float Scale(float value) => value * Factor;

        public static void BeginGUI()
        {
            var factor = Factor;
            if (Mathf.Abs(factor - 1f) < 0.001f)
                return;
            savedMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(factor, factor), Vector2.zero);
            IsActive = true;
        }

        public static void EndGUI()
        {
            if (!IsActive)
                return;
            GUI.matrix = savedMatrix;
            IsActive = false;
        }

        public static Vector2 GuiMousePosition()
        {
            var pos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            var factor = Factor;
            return Mathf.Abs(factor - 1f) < 0.001f ? pos : pos / factor;
        }

        public static Vector2 ScreenToGuiPosition(Vector2 screenPosBottomLeft)
        {
            var pos = new Vector2(screenPosBottomLeft.x, Screen.height - screenPosBottomLeft.y);
            var factor = Factor;
            return Mathf.Abs(factor - 1f) < 0.001f ? pos : pos / factor;
        }

        public static Vector2 GuiScreenSize()
        {
            var factor = Factor;
            return Mathf.Abs(factor - 1f) < 0.001f
                ? new Vector2(Screen.width, Screen.height)
                : new Vector2(Screen.width / factor, Screen.height / factor);
        }

        public static Rect ClampToGuiScreen(Rect rect)
        {
            var size = GuiScreenSize();
            rect.width = Mathf.Clamp(rect.width, 0, size.x);
            rect.height = Mathf.Clamp(rect.height, 0, size.y);
            rect.x = Mathf.Clamp(rect.x, 0, size.x - rect.width);
            rect.y = Mathf.Clamp(rect.y, 0, size.y - rect.height);
            return rect;
        }
    }
}
