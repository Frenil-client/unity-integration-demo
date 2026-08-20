using Frenil.MVVM;
using LobbyDemo.Domain;
using StatSystem;
using TMPro;
using UnityEngine;

namespace LobbyDemo.UI
{
    /// <summary>
    /// 캐릭터 카드 하나의 View. 프리팹으로 만들어 두고 LobbyView가 목록 항목마다 하나씩 찍는다.
    ///
    /// ViewModel을 바깥에서 받으므로 <see cref="InjectableViewBase{TViewModel}"/>를 상속한다.
    /// <see cref="ViewBase{TViewModel}"/>는 <c>new()</c> 제약이 있어 생성자로 Hero를 받는
    /// <see cref="HeroCardViewModel"/>을 타입 인자로 넣을 수 없다.
    ///
    /// 구독 수명은 베이스가 관리한다. <c>Initialize</c>로 다시 붙이면 이전 구독이 먼저 풀리므로,
    /// 슬롯을 재활용해도 사라진 항목의 값 변경이 이 카드를 건드리지 않는다.
    /// </summary>
    public sealed class HeroCardView : InjectableViewBase<HeroCardViewModel>
    {
        [Header("Identity")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _combatPowerText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _attackText;
        [SerializeField] private TextMeshProUGUI _magicText;
        [SerializeField] private TextMeshProUGUI _defenseText;
        [SerializeField] private TextMeshProUGUI _resistanceText;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _maxedColor = new Color(0.20f, 0.55f, 0.95f);

        protected override void Bind(HeroCardViewModel viewModel)
        {
            _nameText.text = $"{viewModel.Name}  <size=70%>{viewModel.ClassLabel}</size>";

            Subscribe(viewModel.CombatPower, power => _combatPowerText.text = $"전투력 {power}");

            Subscribe(viewModel.MaxedOut, maxed =>
                _combatPowerText.color = maxed ? _maxedColor : _normalColor);

            BindStat(viewModel, StatId.AttackPower, _attackText);
            BindStat(viewModel, StatId.MagicAttack, _magicText);
            BindStat(viewModel, StatId.Defense, _defenseText);
            BindStat(viewModel, StatId.StatusResistance, _resistanceText);
        }

        private void BindStat(HeroCardViewModel viewModel, StatId id, TextMeshProUGUI target)
        {
            if (target == null) return;

            string label = Hero.StatLabel(id);
            Subscribe(viewModel.ValueOf(id), value => target.text = $"{label} {value}");
        }
    }
}
