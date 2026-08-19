using RedDotSystem;
using TMPro;
using UnityEngine;

namespace SquadDemo.UI
{
    /// <summary>
    /// 코드로 UI를 만드는 이 데모에서 RedDotCountIcon의 직렬화 참조를 채우기 위한 파생 클래스.
    ///
    /// 보통은 Inspector에서 드롭다운으로 지정하면 되므로 이런 클래스가 필요 없다.
    /// 여기서는 프리팹 없이 실행 시 UI를 조립하기 때문에 파생 클래스를 통해 값을 넣는다.
    /// </summary>
    public sealed class DemoRedDotBadge : RedDotCountIcon
    {
        /// <summary>
        /// GameObject가 비활성인 동안 호출해야 한다.
        /// RedDotIcon은 OnEnable에서 노드를 찾아 콜백을 걸기 때문에,
        /// 활성화 이후에 값을 넣으면 이미 잘못된 노드에 연결된 뒤다.
        /// </summary>
        public void Setup(RedDotType type, GameObject icon, TextMeshProUGUI countText)
        {
            _redDotType = type;
            _icon = icon;
            _countText = countText;
        }
    }
}
