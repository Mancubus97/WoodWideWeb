using UnityEngine;
/// <summary>
/// Attach this script to your player's Camera.
/// It renders a small text label in the bottom-right corner of the screen
/// using Unity's legacy OnGUI system — no Canvas or prefab required.
/// </summary>
/// 
namespace WoodWideWeb
{
    public class ScreenUI : MonoBehaviour
    {
        [Header("Display Text")]
        [Tooltip("The text shown in the bottom-right corner.")]
        string displayText = "Score: " + Constants.score;

        [Header("Style")]
        public int fontSize = 45;
        public Color textColor = Color.softGreen;

        [Header("Position")]
        [Tooltip("Padding from the top-right edge of the screen, in pixels.")]
        public int paddingRight = 12;
        public int paddingTop = 12;

        private GUIStyle _style;



        private void Update()
        {
            // Update the display text with the current score
            displayText = "Score: " + Constants.score;
        }
        private void OnGUI()
        {
            // Build the style once and cache it
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.LowerRight
                };
                _style.normal.textColor = textColor;
            }

            // Measure how wide/tall the text will be
            GUIContent content = new GUIContent(displayText);
            Vector2 size = _style.CalcSize(content);

            // Place the rect in the top-right corner with padding
            Rect rect = new Rect(
                Screen.width - size.x - paddingRight,
                paddingTop,
                size.x,
                size.y
            );

            GUI.Label(rect, content, _style);
        }
    }
}

