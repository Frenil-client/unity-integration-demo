using System;
using System.Collections.Generic;
using Frenil.MVVM;
using SquadDemo.Domain;
using StatSystem;
using TMPro;
using UnityEngine;

namespace SquadDemo.UI
{
    /// <summary>
    /// 선수 카드 하나의 View. 프리팹으로 만들어 두고 SquadView가 목록 항목마다 하나씩 찍는다.
    ///
    /// ViewBase&lt;T&gt;를 쓰지 않은 이유: 제약이 <c>where TViewModel : ViewModelBase, new()</c>이라
    /// 생성자 인자가 필요한 ViewModel(여기서는 SquadPlayer를 받는다)은 아예 타입 인자로
    /// 넣을 수 없다. 목록의 각 항목처럼 "바깥에서 만들어 주입받는" ViewModel이 프레임워크의
    /// 기본 경로에서 빠져 있는 셈이라, unity-mvvm 쪽에서 주입을 1급으로 다루도록 고칠
    /// 지점으로 기록해 둔다. 그 전까지 이 View는 Bind/Unbind를 직접 관리한다.
    /// </summary>
    public sealed class PlayerCardView : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _overallText;

        [Header("Ratings")]
        [SerializeField] private TextMeshProUGUI _shootingText;
        [SerializeField] private TextMeshProUGUI _passingText;
        [SerializeField] private TextMeshProUGUI _paceText;
        [SerializeField] private TextMeshProUGUI _defendingText;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _maxedColor = new Color(0.20f, 0.55f, 0.95f);

        private readonly List<Action> _unbindActions = new List<Action>();

        /// <summary>현재 이 카드가 표시 중인 ViewModel. 바인딩 전이면 null.</summary>
        public PlayerCardViewModel ViewModel { get; private set; }

        public void Bind(PlayerCardViewModel viewModel)
        {
            Unbind();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            _nameText.text = $"{viewModel.Name}  <size=70%>{viewModel.PositionLabel}</size>";

            Subscribe(viewModel.Overall, overall => _overallText.text = $"OVR {overall}");

            Subscribe(viewModel.MaxedOut, maxed =>
                _overallText.color = maxed ? _maxedColor : _normalColor);

            BindRating(viewModel, SquadPlayer.Shooting, _shootingText);
            BindRating(viewModel, SquadPlayer.Passing, _passingText);
            BindRating(viewModel, SquadPlayer.Pace, _paceText);
            BindRating(viewModel, SquadPlayer.Defending, _defendingText);
        }

        public void Unbind()
        {
            foreach (var unbind in _unbindActions)
                unbind?.Invoke();

            _unbindActions.Clear();
            ViewModel = null;
        }

        private void BindRating(PlayerCardViewModel viewModel, StatId id, TextMeshProUGUI target)
        {
            if (target == null) return;

            string label = SquadPlayer.DisplayName(id);
            Subscribe(viewModel.RatingOf(id), rating => target.text = $"{label} {rating}");
        }

        // ViewBase.Subscribe와 같은 규칙 - 구독 즉시 현재 값으로 1회 동기화하고,
        // 해제 콜백을 모아 두었다가 Unbind/OnDestroy에서 일괄 해제한다.
        private void Subscribe<T>(IReadOnlyObservable<T> observable, Action<T> handler)
        {
            observable.OnChanged += handler;
            _unbindActions.Add(() => observable.OnChanged -= handler);
            handler(observable.Value);
        }

        private void OnDestroy() => Unbind();
    }
}
