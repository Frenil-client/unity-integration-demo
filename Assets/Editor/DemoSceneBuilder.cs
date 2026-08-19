using System.IO;
using RedDotSystem;
using SquadDemo.Bootstrap;
using SquadDemo.Glue;
using SquadDemo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SquadDemo.EditorTools
{
    /// <summary>
    /// 데모 씬과 프리팹을 한 번에 만들어 주는 에디터 도구입니다.
    /// (Tools ▸ Squad Demo ▸ 씬과 프리팹 생성)
    ///
    /// UI 조립 코드가 런타임에 남아 있으면 화면 구조를 바꿀 때마다 코드를 고쳐야 하고,
    /// Inspector에서 확인할 수도 없다. 그래서 조립은 에디터 시점에 한 번만 수행해
    /// 프리팹과 씬으로 굳히고, 런타임에는 프리팹을 찍어 쓰기만 한다.
    ///
    /// 이 스크립트는 결과물을 만드는 도구일 뿐이라 데모 실행에는 관여하지 않는다.
    /// 레이아웃을 바꾸고 싶으면 생성된 프리팹을 직접 편집하는 편이 빠르다.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string PrefabFolder = "Assets/Prefabs";
        private const string SceneFolder = "Assets/Scenes";
        private const string CardPrefabPath = PrefabFolder + "/PlayerCard.prefab";
        private const string ScenePath = SceneFolder + "/SquadDemo.unity";

        private static readonly Color Background = new Color(0.11f, 0.13f, 0.16f);
        private static readonly Color CardColor = new Color(0.17f, 0.20f, 0.25f);
        private static readonly Color Accent = new Color(0.20f, 0.55f, 0.95f);
        private static readonly Color DotColor = new Color(0.90f, 0.24f, 0.24f);

        [MenuItem("Tools/Squad Demo/씬과 프리팹 생성")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);

            var cardPrefab = BuildCardPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            BuildScene(cardPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SquadDemo] 생성 완료 — {ScenePath} / {CardPrefabPath}");
            EditorUtility.DisplayDialog("Squad Demo",
                $"씬과 프리팹을 만들었습니다.\n\n{ScenePath}\n{CardPrefabPath}\n\n씬을 열고 재생하세요.", "확인");
        }

        // 선수 카드 프리팹 ---------------------------------------------------

        private static PlayerCardView BuildCardPrefab()
        {
            var root = Panel("PlayerCard", null, CardColor);
            FixedHeight(root, 150f);

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(20, 20, 14, 14);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var view = root.gameObject.AddComponent<PlayerCardView>();

            var identity = Empty("Identity", root);
            VerticalLayout(identity, 4, 0);
            Flexible(identity, width: 1f);
            var nameText = Text("Name", identity, "-", 36f);
            var overallText = Text("Overall", identity, "OVR -", 30f);
            PreferredHeight(nameText, 46f);
            PreferredHeight(overallText, 38f);

            var ratings = Empty("Ratings", root);
            VerticalLayout(ratings, 2, 0);
            Preferred(ratings, width: 200f);
            var shooting = RatingText("Shooting", ratings);
            var passing = RatingText("Passing", ratings);
            var pace = RatingText("Pace", ratings);
            var defending = RatingText("Defending", ratings);

            var so = new SerializedObject(view);
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_overallText").objectReferenceValue = overallText;
            so.FindProperty("_shootingText").objectReferenceValue = shooting;
            so.FindProperty("_passingText").objectReferenceValue = passing;
            so.FindProperty("_paceText").objectReferenceValue = pace;
            so.FindProperty("_defendingText").objectReferenceValue = defending;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, CardPrefabPath);
            Object.DestroyImmediate(root.gameObject);

            return prefab.GetComponent<PlayerCardView>();
        }

        private static TextMeshProUGUI RatingText(string name, Transform parent)
        {
            var text = Text(name, parent, "-", 26f, TextAlignmentOptions.Right);
            PreferredHeight(text, 28f);
            return text;
        }

        // 씬 ---------------------------------------------------------------

        private static void BuildScene(PlayerCardView cardPrefab)
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // 아이콘이 OnEnable에서 노드를 찾으므로 매니저가 씬에 있어야 한다.
            new GameObject("RedDotManager", typeof(RedDotManager));

            var canvas = CreateCanvas("DemoCanvas");

            var root = Panel("Root", canvas.transform, Background);
            Stretch(root);
            VerticalLayout(root, 16, 24);

            // 헤더 - 제목 + 레드닷 배지
            var header = Empty("Header", root);
            FixedHeight(header, 96f);
            HorizontalLayout(header, 12, 0);
            var title = Text("Title", header, "SQUAD", 56f);
            Flexible(title.rectTransform, width: 1f);
            BuildHeaderBadge(header);

            // 조작 버튼
            var buttonRow = Empty("Buttons", root);
            FixedHeight(buttonRow, 104f);
            HorizontalLayout(buttonRow, 12, 0);
            var trainButton = Button("TrainButton", buttonRow, "훈련");
            var signButton = Button("SignButton", buttonRow, "영입");
            var claimButton = Button("ClaimButton", buttonRow, "리포트 확인");

            // 리프 노드의 알림은 해당 동작을 하는 버튼에 직접 붙인다.
            AttachButtonBadge(signButton.GetComponent<RectTransform>(), SquadRedDotBridge.SigningsNode);
            AttachButtonBadge(claimButton.GetComponent<RectTransform>(), SquadRedDotBridge.TrainingReportsNode);

            // 카드 목록 - 자식 높이만큼 스스로 커진다
            var cardRoot = Empty("Cards", root);
            VerticalLayout(cardRoot, 10, 0);

            // 로그
            var logText = Text("Log", root, string.Empty, 30f);
            PreferredHeight(logText, 64f);

            var view = canvas.gameObject.AddComponent<SquadView>();
            var so = new SerializedObject(view);
            so.FindProperty("_cardRoot").objectReferenceValue = cardRoot;
            so.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("_trainButton").objectReferenceValue = trainButton;
            so.FindProperty("_signButton").objectReferenceValue = signButton;
            so.FindProperty("_claimButton").objectReferenceValue = claimButton;
            so.FindProperty("_logText").objectReferenceValue = logText;
            so.ApplyModifiedPropertiesWithoutUndo();

            var bootstrapGo = new GameObject("SquadDemoBootstrap");
            var bootstrap = bootstrapGo.AddComponent<SquadDemoBootstrap>();
            var bso = new SerializedObject(bootstrap);
            bso.FindProperty("_squadView").objectReferenceValue = view;
            bso.ApplyModifiedPropertiesWithoutUndo();

        }

        // 상단 합계 배지 - Character 노드는 CharacterLevelUp(훈련 리포트)과
        // CharacterEquipment(영입)의 부모라, 두 알림의 합계가 트리에서 자동으로 올라온다.
        private static void BuildHeaderBadge(Transform parent)
        {
            var holder = Empty("Badge", parent);
            Preferred(holder, width: 96f);
            CreateBadge(holder, SquadRedDotBridge.SummaryNode, new Vector2(1f, 0.5f), Vector2.zero, 72f);
        }

        // 버튼 우상단에 붙는 알림 점. 어떤 버튼의 알림인지 한눈에 보이게 하는 것이 목적이다.
        private static void AttachButtonBadge(RectTransform host, RedDotType type) =>
            CreateBadge(host, type, new Vector2(1f, 1f), new Vector2(-8f, -8f), 44f);

        private static RedDotCountIcon CreateBadge(RectTransform host, RedDotType type,
                                                   Vector2 anchor, Vector2 offset, float size)
        {
            var dot = Panel("RedDot", host, DotColor);
            dot.sizeDelta = new Vector2(size, size);
            dot.anchorMin = anchor;
            dot.anchorMax = anchor;
            dot.pivot = anchor;
            dot.anchoredPosition = offset;

            var countText = Text("Count", dot, "0", size * 0.45f, TextAlignmentOptions.Center);
            Stretch(countText.rectTransform);

            var icon = host.gameObject.AddComponent<RedDotCountIcon>();
            var so = new SerializedObject(icon);
            so.FindProperty("_redDotType").intValue = (int)type;
            so.FindProperty("_icon").objectReferenceValue = dot.gameObject;
            so.FindProperty("_countText").objectReferenceValue = countText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return icon;
        }

        // UI 조립 헬퍼 -------------------------------------------------------

        private static Canvas CreateCanvas(string name)
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

        private static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static RectTransform Empty(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        // childControlHeight를 켜는 것이 핵심이다. 꺼두면 자식이 자기 기본 높이를 유지해
        // 부모 영역 밖으로 넘치고, 카드끼리 겹쳐 보인다.
        private static VerticalLayoutGroup VerticalLayout(RectTransform rect, int spacing, int padding)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static HorizontalLayoutGroup HorizontalLayout(RectTransform rect, int spacing, int padding)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return layout;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static LayoutElement Element(RectTransform rect) =>
            rect.gameObject.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();

        private static void FixedHeight(RectTransform rect, float height)
        {
            var element = Element(rect);
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private static void PreferredHeight(TMP_Text text, float height) =>
            Element(text.rectTransform).preferredHeight = height;

        private static void Preferred(RectTransform rect, float width) =>
            Element(rect).preferredWidth = width;

        private static void Flexible(RectTransform rect, float width) =>
            Element(rect).flexibleWidth = width;

        private static TextMeshProUGUI Text(string name, Transform parent, string content, float size,
                                            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static Button Button(string name, Transform parent, string label)
        {
            var rect = Panel(name, parent, Accent);
            Flexible(rect, width: 1f);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var text = Text("Label", rect, label, 34f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);

            return button;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
