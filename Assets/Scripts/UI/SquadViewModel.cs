using Frenil.MVVM;
using SquadDemo.Domain;

namespace SquadDemo.UI
{
    /// <summary>
    /// 스쿼드 화면의 ViewModel. Unity에 의존하지 않으므로 UI 없이 단위 테스트할 수 있다.
    ///
    /// 목록은 ObservableList로 노출해 항목이 추가될 때 전체가 아니라 해당 슬롯만 생기게 하고,
    /// 미확인 리포트 / 영입 가능 인원은 Observable로 노출해 레드닷 브리지가 구독한다.
    /// ViewModel 자체는 레드닷도 팝업의 생김새도 모른다 — 열려 있어야 하는지(bool)와
    /// 무엇을 보여줄지(목록)만 들고 있고, 실제 표시는 View의 몫이다.
    /// </summary>
    public sealed class SquadViewModel : ViewModelBase
    {
        private readonly SquadRoster _roster = new SquadRoster();
        private readonly ObservableList<PlayerCardViewModel> _cards = new ObservableList<PlayerCardViewModel>();
        private readonly ObservableList<string> _reports = new ObservableList<string>();

        private readonly Observable<string> _log =
            new Observable<string>("훈련하면 리포트가 쌓입니다. 버튼의 빨간 점이 각 알림, 상단 배지가 그 합계입니다.");

        private readonly Observable<int> _unreadReports = new Observable<int>(0);
        private readonly Observable<int> _availableSignings = new Observable<int>(0);
        private readonly Observable<bool> _canSign = new Observable<bool>(false);
        private readonly Observable<bool> _isReportPopupOpen = new Observable<bool>(false);

        public IReadOnlyObservableList<PlayerCardViewModel> Cards => _cards;

        /// <summary>팝업에 표시할 훈련 리포트 목록. 확인하고 닫으면 비워진다.</summary>
        public IReadOnlyObservableList<string> Reports => _reports;

        public IReadOnlyObservable<string> Log => _log;

        /// <summary>아직 확인하지 않은 훈련 리포트 수. 레드닷 카운트의 소스다.</summary>
        public IReadOnlyObservable<int> UnreadReports => _unreadReports;

        /// <summary>영입 가능한 유망주 수. 레드닷 카운트의 소스다.</summary>
        public IReadOnlyObservable<int> AvailableSignings => _availableSignings;

        public IReadOnlyObservable<bool> CanSign => _canSign;

        /// <summary>리포트 팝업이 열려 있어야 하는지.</summary>
        public IReadOnlyObservable<bool> IsReportPopupOpen => _isReportPopupOpen;

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
                _reports.Add($"{result.Player.Name} · {statName} +{result.Amount} → {result.Player.RatingOf(result.StatId)}");
                _unreadReports.Value = _reports.Count;
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

        /// <summary>
        /// 훈련 리포트 팝업을 연다. 여는 순간 "확인"으로 간주해 레드닷이 꺼진다.
        /// 볼 리포트가 없으면 열지 않는다.
        /// </summary>
        public void OpenTrainingReports()
        {
            if (_reports.Count == 0)
            {
                _log.Value = "확인할 훈련 리포트가 없습니다.";
                return;
            }

            _isReportPopupOpen.Value = true;
            _unreadReports.Value = 0;
            _log.Value = $"훈련 리포트 {_reports.Count}건을 확인하는 중입니다.";
        }

        /// <summary>
        /// 팝업을 닫는다. 확인이 끝난 리포트는 비운다.
        /// 닫기(X)와 팝업 바깥 클릭이 모두 이 경로로 들어온다 —
        /// 닫는 방법이 늘어나도 ViewModel은 하나만 알면 된다.
        /// </summary>
        public void CloseTrainingReports()
        {
            if (!_isReportPopupOpen.Value) return;

            int count = _reports.Count;

            // 목록을 비우기 전에 먼저 닫아, 팝업이 사라지는 순간에 행이 지워지는 것이 보이지 않게 한다.
            _isReportPopupOpen.Value = false;
            _reports.Clear();
            _log.Value = $"훈련 리포트 {count}건을 확인했습니다.";
        }

        private void RefreshSigningState()
        {
            _canSign.Value = _roster.CanSign;
            _availableSignings.Value = _roster.RemainingProspects;
        }

        public override void Dispose()
        {
            foreach (var card in _cards)
                card.Dispose();

            _cards.Clear();
            _reports.Clear();
            base.Dispose();
        }
    }
}
