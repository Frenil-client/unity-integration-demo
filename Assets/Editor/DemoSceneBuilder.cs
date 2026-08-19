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

        /// <summary>위쪽 헤더/버튼 영역이 시작되는 높이 비율. 위 30%가 조작부, 아래 70%가 목록이다.</summary>
        private const float TopSectionRatio = 0.7f;

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

            // 화면을 비율로 자른다. 위 30%는 헤더/버튼/로그, 아래 70%는 선수 목록.
            // 픽셀 높이로 쌓으면 해상도나 종횡비가 바뀔 때 비율이 무너지므로 앵커로 나눈다.
            var topSection = Section("TopSection", root, bottom: TopSectionRatio, top: 1f);
            var listSection = Section("ListSection", root, bottom: 0f, top: TopSectionRatio);

            // 위 영역은 남는 높이를 가중치로 나눠 갖는다. 고정 픽셀 높이를 주면
            // 30%가 그보다 작아지는 종횡비에서 내용이 영역 밖으로 넘친다.
            var topLayout = VerticalLayout(topSection, 12, 24);
            topLayout.childForceExpandHeight = true;

            // 헤더 - 제목 + 레드닷 배지
            var header = Empty("Header", topSection);
            Weight(header, 1f, minHeight: 56f);
            HorizontalLayout(header, 12, 0);
            var title = Text("Title", header, "SQUAD", 56f);
            Flexible(title.rectTransform, width: 1f);
            BuildHeaderBadge(header);

            // 조작 버튼
            var buttonRow = Empty("Buttons", topSection);
            Weight(buttonRow, 1f, minHeight: 64f);
            HorizontalLayout(buttonRow, 12, 0);
            var trainButton = Button("TrainButton", buttonRow, "훈련");
            var signButton = Button("SignButton", buttonRow, "영입");
            var claimButton = Button("ClaimButton", buttonRow, "리포트 확인");

            // 리프 노드의 알림은 해당 동작을 하는 버튼에 직접 붙인다.
            AttachButtonBadge(signButton.GetComponent<RectTransform>(), SquadRedDotBridge.SigningsNode);
            AttachButtonBadge(claimButton.GetComponent<RectTransform>(), SquadRedDotBridge.TrainingReportsNode);

            // 로그 - 버튼 바로 아래에 둔다. 피드백은 그 동작을 한 자리 가까이 있는 편이 낫고,
            // 아래 영역은 통째로 목록에 내주기 위해서이기도 하다.
            var logText = Text("Log", topSection, string.Empty, 30f);
            Weight(logText.rectTransform, 0.6f, minHeight: 36f);

            // 카드 목록 - 아래 70%를 통째로 차지하고, 넘치면 스크롤된다
            var cardScroll = BuildScrollView("CardScrollView", listSection, out var cardRoot);
            Stretch(cardScroll);
            cardScroll.offsetMin = new Vector2(24f, 24f);
            cardScroll.offsetMax = new Vector2(-24f, 0f);

            // 팝업은 Root보다 뒤에 만들어야 위에 그려진다 (UGUI는 형제 순서가 곧 그리기 순서).
            var reportPopup = BuildReportPopup(canvas.transform);

            var view = canvas.gameObject.AddComponent<SquadView>();
            var so = new SerializedObject(view);
            so.FindProperty("_cardRoot").objectReferenceValue = cardRoot;
            so.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("_trainButton").objectReferenceValue = trainButton;
            so.FindProperty("_signButton").objectReferenceValue = signButton;
            so.FindProperty("_claimButton").objectReferenceValue = claimButton;
            so.FindProperty("_logText").objectReferenceValue = logText;
            so.FindProperty("_reportPopup").objectReferenceValue = reportPopup;
            so.ApplyModifiedPropertiesWithoutUndo();

            var bootstrapGo = new GameObject("SquadDemoBootstrap");
            var bootstrap = bootstrapGo.AddComponent<SquadDemoBootstrap>();
            var bso = new SerializedObject(bootstrap);
            bso.FindProperty("_squadView").objectReferenceValue = view;
            bso.ApplyModifiedPropertiesWithoutUndo();

        }

        // 훈련 리포트 팝업. 컴포넌트는 항상 활성인 바깥 오브젝트에 두고, 실제로 켜고 끄는 것은
        // 그 안의 Container다. 컴포넌트가 붙은 오브젝트를 직접 끄면 바인딩 시점이 꼬이기 쉽다.
        private static ReportPopupView BuildReportPopup(Transform parent)
        {
            var popupRoot = Empty("ReportPopup", parent);
            Stretch(popupRoot);
            var popup = popupRoot.gameObject.AddComponent<ReportPopupView>();

            var container = Empty("Container", popupRoot);
            Stretch(container);

            // 딤 - 화면 전체를 덮고, 클릭하면 닫힌다.
            // 패널이 딤보다 뒤(위)에 그려지고 자체 Image로 레이캐스트를 막으므로
            // 패널 위를 눌렀을 때는 딤의 클릭이 발생하지 않는다.
            var dimmer = Panel("Dimmer", container, new Color(0f, 0f, 0f, 0.65f));
            Stretch(dimmer);
            var dimmerButton = dimmer.gameObject.AddComponent<Button>();
            dimmerButton.targetGraphic = dimmer.GetComponent<Image>();
            dimmerButton.transition = Selectable.Transition.None;

            var panel = Panel("Panel", container, CardColor);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(880f, 1000f);
            panel.anchoredPosition = Vector2.zero;
            VerticalLayout(panel, 16, 24);

            var titleBar = Empty("TitleBar", panel);
            FixedHeight(titleBar, 88f);
            HorizontalLayout(titleBar, 12, 0);
            var popupTitle = Text("Title", titleBar, "훈련 리포트", 44f);
            Flexible(popupTitle.rectTransform, width: 1f);

            var closeButton = Button("CloseButton", titleBar, "X");
            var closeRect = (RectTransform)closeButton.transform;
            Element(closeRect).flexibleWidth = 0f;
            Preferred(closeRect, width: 88f);

            var reportScroll = BuildScrollView("ReportScrollView", panel, out var listRoot);
            FlexibleHeight(reportScroll, 1f);

            // 행 템플릿 - 목록 바깥에 비활성으로 두고 복제해서 쓴다.
            var rowTemplate = Text("RowTemplate", container, "-", 30f);
            PreferredHeight(rowTemplate, 46f);
            rowTemplate.gameObject.SetActive(false);

            var so = new SerializedObject(popup);
            so.FindProperty("_container").objectReferenceValue = container.gameObject;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.FindProperty("_dimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("_listRoot").objectReferenceValue = listRoot;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();

            container.gameObject.SetActive(false);
            return popup;
        }

        // 선수를 계속 영입하면 목록이 화면을 넘어간다. 스크롤 뷰가 없으면 넘친 카드를
        // 볼 수 없을 뿐 아니라, 목록이 아래쪽 로그까지 화면 밖으로 밀어내 버린다.
        // 그래서 목록은 스크롤 뷰 안에 넣고, 남는 세로 공간을 이 뷰가 흡수하게 한다.
        // 스크롤 뷰 오브젝트를 돌려주고, 항목이 들어갈 콘텐츠는 out으로 넘긴다.
        // 바깥 크기를 정하는 방식이 호출처마다 다르기 때문에(앵커 고정 / 레이아웃 배분)
        // 여기서는 크기를 건드리지 않는다.
        private static RectTransform BuildScrollView(string name, RectTransform parent, out RectTransform content)
        {
            var scrollView = Empty(name, parent);

            var scroll = scrollView.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var viewport = Empty("Viewport", scrollView);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            // 스크롤 콘텐츠는 부모 레이아웃이 크기를 잡아주지 않으므로
            // 여기서는 ContentSizeFitter가 자기 높이를 스스로 결정해야 한다.
            content = Empty("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayout(content, 10, 0);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            return scrollView;
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
            // 너비 기준(0)으로 맞춘다. 가로 폭이 항상 1080 단위로 고정되므로 버튼과 카드의
            // 크기가 해상도와 무관하게 일정하고, 세로는 화면 비율만큼 늘거나 줄어든다.
            // 그래서 세로 배치는 픽셀이 아니라 비율(TopSectionRatio)로 나눈다.
            scaler.matchWidthOrHeight = 0f;

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

        private static void FlexibleHeight(RectTransform rect, float value) =>
            Element(rect).flexibleHeight = value;

        /// <summary>부모 높이의 [bottom, top] 구간을 차지하는 빈 영역을 만든다. 비율이 곧 레이아웃이다.</summary>
        private static RectTransform Section(string name, RectTransform parent, float bottom, float top)
        {
            var rect = Empty(name, parent);
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// 남는 높이를 가중치로 나눠 갖게 한다. preferredHeight를 -1로 두어야
        /// 레이아웃 그룹이 고정 높이 대신 flexible 배분을 쓴다.
        /// </summary>
        private static void Weight(RectTransform rect, float flexibleHeight, float minHeight)
        {
            var element = Element(rect);
            element.minHeight = minHeight;
            element.preferredHeight = -1f;
            element.flexibleHeight = flexibleHeight;
        }

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
