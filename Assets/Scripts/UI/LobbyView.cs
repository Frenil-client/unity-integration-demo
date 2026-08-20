using System.Collections.Generic;
using Frenil.MVVM;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDemo.UI
{
    /// <summary>
    /// 로비 화면의 View. unity-mvvm의 ViewBase를 그대로 쓴다
    /// (LobbyViewModel은 인자 없는 생성자를 가지므로 프레임워크의 기본 경로에 맞는다).
    ///
    /// 참조는 전부 Inspector에서 연결한다. 목록은 ListChange 델타로 받아 바뀐 슬롯만
    /// 손대므로, 캐릭터를 하나 소환해도 기존 카드는 다시 만들어지지 않는다.
    /// </summary>
    public sealed class LobbyView : ViewBase<LobbyViewModel>
    {
        [Header("Card List")]
        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private HeroCardView _cardPrefab;

        [Header("Controls")]
        [SerializeField] private Button _enhanceButton;
        [SerializeField] private Button _summonButton;
        [SerializeField] private Button _rewardButton;
        [SerializeField] private Button _questButton;

        [Header("Output")]
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private RewardPopupView _rewardPopup;

        private readonly List<HeroCardView> _cardViews = new List<HeroCardView>();

        /// <summary>레드닷 브리지가 구독할 수 있도록 ViewModel을 노출한다.</summary>
        public LobbyViewModel Model => ViewModel;

        protected override void Bind(LobbyViewModel viewModel)
        {
            Subscribe(viewModel.Log, message => _logText.text = message);
            Subscribe(viewModel.CanSummon, canSummon => _summonButton.interactable = canSummon);
            Subscribe(viewModel.Cards, OnCardsChanged);

            _enhanceButton.onClick.AddListener(viewModel.EnhanceRandomHero);
            _summonButton.onClick.AddListener(viewModel.SummonNextHero);
            _rewardButton.onClick.AddListener(viewModel.OpenRewardPopup);
            _questButton.onClick.AddListener(viewModel.ClaimDailyQuests);

            // 리스너도 구독과 같은 수명에 묶는다.
            RegisterUnbind(() =>
            {
                _enhanceButton.onClick.RemoveListener(viewModel.EnhanceRandomHero);
                _summonButton.onClick.RemoveListener(viewModel.SummonNextHero);
                _rewardButton.onClick.RemoveListener(viewModel.OpenRewardPopup);
                _questButton.onClick.RemoveListener(viewModel.ClaimDailyQuests);
            });

            // 팝업은 같은 ViewModel을 공유한다. 소유하지 않으므로 팝업이 사라져도
            // ViewModel은 이 View의 것으로 남는다.
            _rewardPopup.Initialize(viewModel);
        }

        private void OnCardsChanged(ListChange<HeroCardViewModel> change)
        {
            switch (change.Type)
            {
                case ListChangeType.Added:
                    InsertCard(change.Index, change.NewItem);
                    break;

                case ListChangeType.Removed:
                    RemoveCard(change.Index);
                    break;

                case ListChangeType.Replaced:
                    _cardViews[change.Index].Initialize(change.NewItem);
                    break;

                default:
                    RebuildAll();
                    break;
            }
        }

        private void InsertCard(int index, HeroCardViewModel cardViewModel)
        {
            var card = Instantiate(_cardPrefab, _cardRoot);
            card.Initialize(cardViewModel);
            card.transform.SetSiblingIndex(index);
            _cardViews.Insert(index, card);
        }

        private void RemoveCard(int index)
        {
            var card = _cardViews[index];
            _cardViews.RemoveAt(index);

            card.Release();
            Destroy(card.gameObject);
        }

        private void RebuildAll()
        {
            for (int i = _cardViews.Count - 1; i >= 0; i--)
                RemoveCard(i);

            var cards = ViewModel.Cards;
            for (int i = 0; i < cards.Count; i++)
                InsertCard(i, cards[i]);
        }

        protected override void OnDestroy()
        {
            // 구독 해제와 ViewModel Dispose는 base가 처리한다.
            base.OnDestroy();

            foreach (var card in _cardViews)
            {
                if (card != null) card.Release();
            }

            _cardViews.Clear();
        }
    }
}
