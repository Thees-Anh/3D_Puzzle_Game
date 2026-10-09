using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    /// <summary>Applies a crisp, platform-safe type treatment to the complete main menu.</summary>
    public static class MainMenuTypography
    {
        private static Font interfaceFont;

        public static void Apply(Transform menuRoot)
        {
            if (menuRoot == null) return;

            Canvas canvas = menuRoot.GetComponent<Canvas>();
            if (canvas != null) canvas.pixelPerfect = true;

            if (interfaceFont == null)
            {
                interfaceFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Roboto", "Noto Sans", "Segoe UI Semibold", "Segoe UI", "Arial" },
                    72);
                if (interfaceFont == null)
                    interfaceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            Text[] texts = menuRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++) Style(texts[i]);
        }

        private static void Style(Text text)
        {
            text.font = interfaceFont;
            text.alignByGeometry = true;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            bool isButton = text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null;
            bool isHeading = text.fontSize >= 38 || text.name == "Heading";
            bool isSettingLabel = text.name.EndsWith("Label");

            if (isHeading)
            {
                text.fontStyle = FontStyle.Bold;
                AddShadow(text, new Vector2(2f, -2f), new Color(0f, 0f, 0f, .72f));
            }
            else if (isButton)
            {
                text.fontStyle = FontStyle.Bold;
                text.supportRichText = false;
                AddShadow(text, new Vector2(1f, -1f), new Color(0f, 0f, 0f, .62f));
            }
            else if (isSettingLabel)
            {
                text.fontStyle = FontStyle.Bold;
                AddShadow(text, new Vector2(1f, -1f), new Color(0f, 0f, 0f, .48f));
            }
            else
            {
                text.fontStyle = FontStyle.Normal;
                AddShadow(text, new Vector2(1f, -1f), new Color(0f, 0f, 0f, .42f));
            }
        }

        private static void AddShadow(Text text, Vector2 distance, Color color)
        {
            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = distance;
            shadow.effectColor = color;
            shadow.useGraphicAlpha = true;
        }
    }
}
