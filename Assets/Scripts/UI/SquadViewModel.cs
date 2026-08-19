using Frenil.MVVM;
using SquadDemo.Domain;

namespace SquadDemo.UI
{
    /// <summary>
    /// 스쿼드 화면의 ViewModel. Unity에 의존하지 않으므로 UI 없이 단위 테스트할 수 있다.
    ///
    /// 목록은 ObservableList로 노출해 카드가 추가될 때 전체가 아니라 해당 슬롯만 생기게 하고,
    /// 미확인 리포트 / 영입 가능 인원은 Observable로 노출해 레드닷 브리지가 구독한다.
    /// ViewModel 자체는 레드닷을 모른다.
    /// </summary>
    public sealed class SquadViewModel : ViewModelBase
    {
        private readonly SquadRoster _roster = new SquadRoster();
        private readonly ObservableList<PlayerCardViewModel> _cards = new ObservableList<PlayerCardViewModel>();

        private readonly Observable<string> _log = new Observable<string>("훈련 버튼을 눌러 스쿼드를 성장시키세요.");
        private readonly Observable<int> _unreadReports = new Observable<int>(0);
        private readonly Observable<int> _availableSignings = new Observable<int>(0);
        private readonly Observable<bool> _canSign = new Observable<bool>(false);

        public IReadOnlyObservableList<PlayerCardViewModel> Cards => _cards;
        public IReadOnlyObservable<string> Log => _log;

        /// <summary>아직 확인하지 않은 훈련 리포트 수. 레드닷 카운트의 소스다.</summary>
        public IReadOnlyObservable<int> UnreadReports => _unreadReports;

        /// <summary>영입 가능한 유망주 수. 레드닷 카운트의 소스다.</summary>
        public IReadOnlyObservable<int> AvailableSignings => _availableSignings;

        public IReadOnlyObservable<bool> CanSign => _canSign;

        public SquadViewModel()
        {
            foreach (var player in _roster.Players)
                _cards.Add(new PlayerCardViewModel(player));

            RefreshSigningState();
        }

        public void TrainRandomPlayer()
        {
            var result = _roster.TrainRandomPlayer();
            if (!result.IsValid) return;

            string statName = SquadPlayer.DisplayName(result.StatId);

            if (result.Improved)
            {
                // 리포트가 쌓이면 레드닷 카운트가 올라간다.
                _unreadReports.Value++;
                _log.Value = $"{result.Player.Name} · {statName} +{result.Amount} (종합 {result.Player.Overall})";
            }
            else
            {
                // 상한에 걸려 값이 그대로면 Stat.Changed도 발행되지 않는다.
                _log.Value = $"{result.Player.Name} · {statName}는 이미 최대치({SquadPlayer.MaxRating})입니다.";
            }
        }

        public void SignNextPlayer()
        {
            var player = _roster.Sign();
            if (player == null)
            {
                _log.Value = "영입할 유망주가 더 없습니다.";
                return;
            }

            _cards.Add(new PlayerCardViewModel(player));
            _log.Value = $"{player.Name} ({SquadPlayer.PositionLabel(player.Position)}) 영입 완료 · 종합 {player.Overall}";
            RefreshSigningState();
        }

        /// <summary>훈련 리포트를 모두 확인 처리한다. 레드닷이 꺼지는 지점.</summary>
        public void ClaimTrainingReports()
        {
            if (_unreadReports.Value == 0)
            {
                _log.Value = "확인할 훈련 리포트가 없습니다.";
                return;
            }

            _log.Value = $"훈련 리포트 {_unreadReports.Value}건을 확인했습니다.";
            _unreadReports.Value = 0;
        }

        private void RefreshSigningState()
        {
            _canSign.Value = _roster.CanSign;
            _availableSignings.Value = _roster.CanSign ? 1 : 0;
        }

        public override void Dispose()
        {
            foreach (var card in _cards)
                card.Dispose();

            _cards.Clear();
            base.Dispose();
        }
    }
}
