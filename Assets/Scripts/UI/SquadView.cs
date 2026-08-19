using System.Collections.Generic;
using Frenil.MVVM;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquadDemo.UI
{
    /// <summary>
    /// 스쿼드 화면의 View. unity-mvvm의 ViewBase를 그대로 쓴다
    /// (SquadViewModel은 인자 없는 생성자를 가지므로 프레임워크의 기본 경로에 맞는다).
    ///
    /// 참조는 전부 Inspector에서 연결한다. 목록은 ListChange 델타로 받아 바뀐 슬롯만
    /// 손대므로, 선수를 한 명 영입해도 기존 카드는 다시 만들어지지 않는다.
    /// </summary>
    public sealed class SquadView : ViewBase<SquadViewModel>
    {
        [Header("Card List")]
        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private PlayerCardView _cardPrefab;

        [Header("Controls")]
        [SerializeField] private Button _trainButton;
        [SerializeField] private Button _signButton;
        [SerializeField] private Button _claimButton;

        [Header("Output")]
        [SerializeField] private TextMeshProUGUI _logText;

        private readonly List<PlayerCardView> _cardViews = new List<PlayerCardView>();

        /// <summary>레드닷 브리지가 구독할 수 있도록 ViewModel을 노출한다.</summary>
        public SquadViewModel Model => ViewModel;

        protected override void Bind(SquadViewModel viewModel)
        {
            Subscribe(viewModel.Log, message => _logText.text = message);
            Subscribe(viewModel.CanSign, canSign => _signButton.interactable = canSign);
            Subscribe(viewModel.Cards, OnCardsChanged);

            _trainButton.onClick.AddListener(viewModel.TrainRandomPlayer);
            _signButton.onClick.AddListener(viewModel.SignNextPlayer);
            _claimButton.onClick.AddListener(viewModel.ClaimTrainingReports);
        }

        private void OnCardsChanged(ListChange<PlayerCardViewModel> change)
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
                    _cardViews[change.Index].Bind(change.NewItem);
                    break;

                default:
                    RebuildAll();
                    break;
            }
        }

        private void InsertCard(int index, PlayerCardViewModel cardViewModel)
        {
            var card = Instantiate(_cardPrefab, _cardRoot);
            card.Bind(cardViewModel);
            card.transform.SetSiblingIndex(index);
            _cardViews.Insert(index, card);
        }

        private void RemoveCard(int index)
        {
            var card = _cardViews[index];
            _cardViews.RemoveAt(index);

            card.Unbind();
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
                if (card != null) card.Unbind();
            }

            _cardViews.Clear();
        }
    }
}
