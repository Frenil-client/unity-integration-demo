using SquadDemo.Glue;
using SquadDemo.UI;
using UnityEngine;

namespace SquadDemo.Bootstrap
{
    /// <summary>
    /// 씬에 배치된 조각들을 이어 붙이는 진입점.
    ///
    /// UI는 프리팹으로 미리 만들어 두므로 이 컴포넌트는 화면을 조립하지 않는다.
    /// 남은 일은 하나다 — ViewModel의 "미확인 개수"를 레드닷 트리로 흘려보내는 브리지를
    /// 만들고, 씬이 내려갈 때 정리하는 것.
    ///
    /// Start에서 만드는 이유: SquadView가 Awake에서 ViewModel을 만들기 때문에
    /// 모든 Awake가 끝난 뒤인 Start 시점이어야 Model이 준비되어 있다.
    /// </summary>
    public sealed class SquadDemoBootstrap : MonoBehaviour
    {
        [Tooltip("레드닷과 연결할 스쿼드 화면")]
        [SerializeField] private SquadView _squadView;

        private SquadRedDotBridge _redDotBridge;

        private void Start()
        {
            if (_squadView == null)
            {
                Debug.LogError("[SquadDemo] SquadView가 연결되지 않았습니다.", this);
                return;
            }

            _redDotBridge = new SquadRedDotBridge(_squadView.Model);
        }

        private void OnDestroy()
        {
            _redDotBridge?.Dispose();
            _redDotBridge = null;
        }
    }
}
