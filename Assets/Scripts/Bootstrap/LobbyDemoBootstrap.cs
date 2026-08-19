using LobbyDemo.Glue;
using LobbyDemo.UI;
using UnityEngine;

namespace LobbyDemo.Bootstrap
{
    /// <summary>
    /// 씬에 배치된 조각들을 이어 붙이는 진입점.
    ///
    /// UI는 프리팹으로 미리 만들어 두므로 이 컴포넌트는 화면을 조립하지 않는다.
    /// 남은 일은 하나다 — ViewModel의 "처리할 것이 남았다"는 수치를 레드닷 트리로
    /// 흘려보내는 브리지를 만들고, 씬이 내려갈 때 정리하는 것.
    ///
    /// Start에서 만드는 이유: LobbyView가 Awake에서 ViewModel을 만들기 때문에
    /// 모든 Awake가 끝난 뒤인 Start 시점이어야 Model이 준비되어 있다.
    /// </summary>
    public sealed class LobbyDemoBootstrap : MonoBehaviour
    {
        [Tooltip("레드닷과 연결할 로비 화면")]
        [SerializeField] private LobbyView _lobbyView;

        private LobbyRedDotBridge _redDotBridge;

        private void Start()
        {
            if (_lobbyView == null)
            {
                Debug.LogError("[LobbyDemo] LobbyView가 연결되지 않았습니다.", this);
                return;
            }

            _redDotBridge = new LobbyRedDotBridge(_lobbyView.Model);
        }

        private void OnDestroy()
        {
            _redDotBridge?.Dispose();
            _redDotBridge = null;
        }
    }
}
