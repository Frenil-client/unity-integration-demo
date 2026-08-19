using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquadDemo.UI
{
    /// <summary>
    /// 데모 UI를 코드로 조립하기 위한 최소 헬퍼.
    ///
    /// 씬과 프리팹을 저장소에 넣지 않은 이유: 바이너리 에셋은 diff가 되지 않아
    /// 코드 리뷰로 확인할 수 없고, 이 데모에서 봐야 할 것은 UI 꾸밈새가 아니라
    /// 세 패키지가 맞물리는 방식이기 때문이다. 그래서 화면은 실행 시 코드로 만든다.
    /// 실제 프로젝트라면 당연히 프리팹을 쓴다.
    /// </summary>
    internal static class UiFactory
    {
        public static readonly Color Background = new Color(0.11f, 0.13f, 0.16f);
        public static readonly Color CardColor = new Color(0.17f, 0.20f, 0.25f);
        public static readonly Color Accent = new Color(0.20f, 0.55f, 0.95f);
        public static readonly Color DotColor = new Color(0.90f, 0.24f, 0.24f);

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return rect;
        }

        public static RectTransform Empty(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static VerticalLayoutGroup VerticalLayout(RectTransform rect, int spacing, int padding)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalLayout(RectTransform rect, int spacing, int padding)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return layout;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static LayoutElement FixedHeight(RectTransform rect, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            return element;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string content, float size,
                                           TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        public static Button Button(string name, Transform parent, string label)
        {
            var rect = Panel(name, parent, Accent);
            FixedHeight(rect, 96f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var text = Text("Label", rect, label, 34f, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform);

            return button;
        }
    }
}
