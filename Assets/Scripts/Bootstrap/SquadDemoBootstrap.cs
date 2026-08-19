using RedDotSystem;
using SquadDemo.Glue;
using SquadDemo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SquadDemo.Bootstrap
{
    /// <summary>
    /// 데모 진입점. 빈 씬의 빈 GameObject에 이 컴포넌트만 붙이고 재생하면 동작한다.
    ///
    /// 하는 일은 세 가지다.
    /// 1. RedDotManager를 세운다 (RedDotIcon보다 먼저여야 한다 - 아이콘은 OnEnable에서 노드를 찾는다)
    /// 2. UI를 조립하고 SquadView에 참조를 주입한다
    /// 3. ViewModel과 레드닷 트리를 잇는 브리지를 만든다
    ///
    /// 세 패키지가 만나는 지점이 전부 이 파일과 Glue/ 안에 있고, 패키지끼리는 서로를 모른다.
    /// </summary>
    public sealed class SquadDemoBootstrap : MonoBehaviour
    {
        private SquadRedDotBridge _redDotBridge;

        private void Start()
        {
            EnsureEventSystem();
            EnsureRedDotManager();

            var view = BuildUi();

            // ViewModel의 "미확인 개수"를 레드닷 카운트로 흘려보낸다.
            _redDotBridge = new SquadRedDotBridge(view.Model);
        }

        private void OnDestroy()
        {
            _redDotBridge?.Dispose();
            _redDotBridge = null;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private static void EnsureRedDotManager()
        {
            if (RedDotManager.Instance != null) return;

            // 활성 GameObject에 붙이면 Awake가 즉시 돌아 트리가 구성되고 Instance가 채워진다.
            new GameObject("RedDotManager").AddComponent<RedDotManager>();
        }

        private static SquadView BuildUi()
        {
            var canvas = UiFactory.CreateCanvas("DemoCanvas");

            var background = UiFactory.Panel("Background", canvas.transform, UiFactory.Background);
            UiFactory.Stretch(background);
            UiFactory.VerticalLayout(background, 16, 24);

            // 헤더 - 제목과 레드닷 배지
            var header = UiFactory.Empty("Header", background);
            UiFactory.FixedHeight(header, 90f);
            UiFactory.HorizontalLayout(header, 12, 0);

            UiFactory.Text("Title", header, "SQUAD", 52f);
            var badge = BuildBadge(header);

            // 조작 버튼
            var buttonRow = UiFactory.Empty("Buttons", background);
            UiFactory.FixedHeight(buttonRow, 96f);
            UiFactory.HorizontalLayout(buttonRow, 12, 0);

            var trainButton = UiFactory.Button("TrainButton", buttonRow, "훈련");
            var signButton = UiFactory.Button("SignButton", buttonRow, "영입");
            var claimButton = UiFactory.Button("ClaimButton", buttonRow, "리포트 확인");

            // 카드 목록
            var cardRoot = UiFactory.Empty("Cards", background);
            UiFactory.VerticalLayout(cardRoot, 10, 0);

            // 로그
            var logText = UiFactory.Text("Log", background, string.Empty, 28f);
            UiFactory.FixedHeight((RectTransform)logText.transform, 60f);

            // View는 비활성 상태에서 만들어 참조를 주입한 뒤 활성화한다.
            // AddComponent는 활성 GameObject에서 Awake를 즉시 호출하므로,
            // 그 전에 Configure가 끝나 있어야 Bind가 참조를 쓸 수 있다.
            var viewGo = new GameObject("SquadView");
            viewGo.SetActive(false);
            viewGo.transform.SetParent(canvas.transform, false);

            var view = viewGo.AddComponent<SquadView>();
            view.Configure(cardRoot, logText, trainButton, signButton, claimButton);
            viewGo.SetActive(true);

            // 배지는 트리가 채워진 뒤 활성화해 현재 카운트로 즉시 동기화되게 한다.
            badge.gameObject.SetActive(true);

            return view;
        }

        private static DemoRedDotBadge BuildBadge(Transform parent)
        {
            var holder = UiFactory.Empty("Badge", parent);
            holder.gameObject.SetActive(false);
            UiFactory.FixedHeight(holder, 64f);

            var dot = UiFactory.Panel("Dot", holder, UiFactory.DotColor);
            dot.sizeDelta = new Vector2(64f, 64f);
            dot.anchorMin = new Vector2(1f, 0.5f);
            dot.anchorMax = new Vector2(1f, 0.5f);
            dot.pivot = new Vector2(1f, 0.5f);
            dot.anchoredPosition = Vector2.zero;

            var countText = UiFactory.Text("Count", dot, "0", 30f, TextAlignmentOptions.Center);
            UiFactory.Stretch((RectTransform)countText.transform);

            var badge = holder.gameObject.AddComponent<DemoRedDotBadge>();

            // Character 노드는 CharacterLevelUp(훈련 리포트) + CharacterEquipment(영입) 의 부모라,
            // 배지 하나에 두 알림의 합계가 자동으로 집계된다. 이 합산은 트리가 해 준다.
            badge.Setup(SquadRedDotBridge.SummaryNode, dot.gameObject, countText);

            return badge;
        }
    }
}
