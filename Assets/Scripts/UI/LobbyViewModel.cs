using Frenil.MVVM;
using LobbyDemo.Domain;

namespace LobbyDemo.UI
{
    /// <summary>
    /// 로비 화면의 ViewModel. Unity에 의존하지 않으므로 UI 없이 단위 테스트할 수 있다.
    ///
    /// 목록은 ObservableList로 노출해 항목이 추가될 때 전체가 아니라 해당 슬롯만 생기게 하고,
    /// "처리할 것이 남았다"는 세 수치는 Observable로 노출해 레드닷 브리지가 구독한다.
    /// ViewModel 자체는 레드닷도 팝업의 생김새도 모른다 — 열려 있어야 하는지(bool)와
    /// 무엇을 보여줄지(목록)만 들고 있고, 실제 표시는 View의 몫이다.
    /// </summary>
    public sealed class LobbyViewModel : ViewModelBase
    {
        private readonly HeroRoster _roster = new HeroRoster();
        private readonly ObservableList<HeroCardViewModel> _cards = new ObservableList<HeroCardViewModel>();
        private readonly ObservableList<string> _reports = new ObservableList<string>();

        private readonly Observable<string> _log =
            new Observable<string>("강화하면 리포트가 쌓입니다. 버튼의 빨간 점이 각 알림, 상단 배지가 그 합계입니다.");

        private readonly Observable<int> _unreadRewards = new Observable<int>(0);
        private readonly Observable<int> _remainingSummons = new Observable<int>(0);
        private readonly Observable<int> _completedDailyQuests = new Observable<int>(0);
        private readonly Observable<bool> _canSummon = new Observable<bool>(false);
        private readonly Observable<bool> _isRewardPopupOpen = new Observable<bool>(false);

        public IReadOnlyObservableList<HeroCardViewModel> Cards => _cards;

        /// <summary>팝업에 표시할 보상 기록(강화 리포트 + 임무 보상). 확인하고 닫으면 비워진다.</summary>
        public IReadOnlyObservableList<string> Reports => _reports;

        public IReadOnlyObservable<string> Log => _log;

        /// <summary>아직 확인하지 않은 보상 기록 수. Character 레드닷의 소스다.</summary>
        public IReadOnlyObservable<int> UnreadRewards => _unreadRewards;

        /// <summary>남은 소환권 수. Shop 레드닷의 소스다.</summary>
        public IReadOnlyObservable<int> RemainingSummons => _remainingSummons;

        /// <summary>수령하지 않은 일일 임무 보상 수. Quest 레드닷의 소스다.</summary>
        public IReadOnlyObservable<int> CompletedDailyQuests => _completedDailyQuests;

        public IReadOnlyObservable<bool> CanSummon => _canSummon;

        /// <summary>보상 팝업이 열려 있어야 하는지.</summary>
        public IReadOnlyObservable<bool> IsRewardPopupOpen => _isRewardPopupOpen;

        public LobbyViewModel()
        {
            foreach (var hero in _roster.Heroes)
                _cards.Add(new HeroCardViewModel(hero));

            RefreshSummonState();
        }

        public void EnhanceRandomHero()
        {
            var result = _roster.EnhanceRandomHero();
            if (!result.IsValid) return;

            string statName = Hero.StatLabel(result.StatId);

            if (!result.Improved)
            {
                // 상한에 걸려 값이 그대로면 Stat.Changed도 발행되지 않는다.
                _log.Value = $"{result.Hero.Name} · {statName}은 이미 최대치({Hero.MaxStat})입니다.";
                return;
            }

            _reports.Add($"{result.Hero.Name} · {statName} +{result.Amount} → {result.Hero.ValueOf(result.StatId)}");
            _unreadRewards.Value = _reports.Count;
            _log.Value = $"{result.Hero.Name} · {statName} +{result.Amount} (전투력 {result.Hero.CombatPower})";

            if (result.QuestCompleted)
            {
                _completedDailyQuests.Value++;
                _log.Value = $"{_log.Value} · 일일 임무 완료";
            }
        }

        public void SummonNextHero()
        {
            var hero = _roster.Summon();
            if (hero == null)
            {
                _log.Value = "소환할 캐릭터가 더 없습니다.";
                return;
            }

            _cards.Add(new HeroCardViewModel(hero));
            _log.Value = $"{hero.Name} ({Hero.ClassLabel(hero.Class)}) 소환 완료 · 전투력 {hero.CombatPower}";
            RefreshSummonState();
        }

        /// <summary>
        /// 보상 팝업을 연다. 여는 순간 "확인"으로 간주해 레드닷이 꺼진다.
        /// 볼 기록이 없으면 열지 않는다.
        /// </summary>
        public void OpenRewardPopup()
        {
            if (_reports.Count == 0)
            {
                _log.Value = "확인할 보상 기록이 없습니다.";
                return;
            }

            _isRewardPopupOpen.Value = true;
            _unreadRewards.Value = 0;
            _log.Value = $"보상 기록 {_reports.Count}건을 확인하는 중입니다.";
        }

        /// <summary>
        /// 팝업을 닫는다. 확인이 끝난 리포트는 비운다.
        /// 닫기(X)와 팝업 바깥 클릭이 모두 이 경로로 들어온다 —
        /// 닫는 방법이 늘어나도 ViewModel은 하나만 알면 된다.
        /// </summary>
        public void CloseRewardPopup()
        {
            if (!_isRewardPopupOpen.Value) return;

            int count = _reports.Count;

            // 목록을 비우기 전에 먼저 닫아, 팝업이 사라지는 순간에 행이 지워지는 것이 보이지 않게 한다.
            _isRewardPopupOpen.Value = false;
            _reports.Clear();
            _log.Value = $"보상 기록 {count}건을 확인했습니다.";
        }

        /// <summary>
        /// 완료된 일일 임무 보상을 모두 수령한다. Quest 레드닷이 꺼지는 지점.
        ///
        /// 보상은 전 캐릭터에 걸리는 축복(공격력 %)이다. 강화와 달리 기본값을 건드리지 않고
        /// 모디파이어로 얹히므로, 카드의 공격력과 전투력이 즉시 오르면서도 언제든 정확히 원복된다.
        /// 수령 기록은 보상 팝업 목록에 남는다.
        /// </summary>
        public void ClaimDailyQuests()
        {
            if (_completedDailyQuests.Value == 0)
            {
                _log.Value = $"완료된 일일 임무가 없습니다. (강화 {_roster.EnhancementsUntilQuest}회 남음)";
                return;
            }

            int claimed = _completedDailyQuests.Value;
            _completedDailyQuests.Value = 0;
            _roster.AddBlessing(claimed);

            string record = $"일일 임무 {claimed}개 수령 · 축복 공격력 +{_roster.BlessingPercent:P0}";
            _reports.Add(record);
            _unreadRewards.Value = _reports.Count;
            _log.Value = record;
        }

        private void RefreshSummonState()
        {
            _canSummon.Value = _roster.CanSummon;
            _remainingSummons.Value = _roster.RemainingSummons;
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
