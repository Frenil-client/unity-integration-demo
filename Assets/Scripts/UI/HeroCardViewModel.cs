using Frenil.MVVM;
using LobbyDemo.Domain;
using LobbyDemo.Glue;
using StatSystem;

namespace LobbyDemo.UI
{
    /// <summary>
    /// 캐릭터 카드 하나의 ViewModel. Unity에 의존하지 않는 순수 C#이다.
    ///
    /// 능력치는 Stat이 보관하고, 그 변경은 StatObservableBridge를 통해 Observable로
    /// 흘러들어온다. 카드 UI는 Stat도 Hero도 모른 채 Observable만 구독하면 된다.
    /// </summary>
    public sealed class HeroCardViewModel : ViewModelBase
    {
        private readonly Hero _hero;
        private readonly StatObservableBridge _bridge;
        private readonly Observable<int> _combatPower;
        private readonly Observable<bool> _maxedOut;

        public string Name => _hero.Name;
        public string ClassLabel => Hero.ClassLabel(_hero.Class);

        /// <summary>전투력. 개별 능력치에서 파생되며 ViewModel이 계산한다.</summary>
        public IReadOnlyObservable<int> CombatPower => _combatPower;

        /// <summary>모든 능력치가 상한에 도달했는지. 카드 강조 표시용.</summary>
        public IReadOnlyObservable<bool> MaxedOut => _maxedOut;

        public HeroCardViewModel(Hero hero)
        {
            _hero = hero;
            _bridge = new StatObservableBridge(hero.Stats);
            _combatPower = new Observable<int>(hero.CombatPower);
            _maxedOut = new Observable<bool>(IsMaxedOut());

            // 개별 능력치가 바뀌면 전투력을 다시 계산한다.
            // 파생 값을 ViewModel이 계산하므로 View는 결과만 받는다.
            foreach (var id in Hero.RatedStats)
                Subscribe(_bridge.Track(id), _ => Refresh());
        }

        /// <summary>능력치 하나를 반응형 값으로 노출한다.</summary>
        public IReadOnlyObservable<int> ValueOf(StatId id) => _bridge.Track(id);

        private void Refresh()
        {
            _combatPower.Value = _hero.CombatPower;
            _maxedOut.Value = IsMaxedOut();
        }

        private bool IsMaxedOut()
        {
            foreach (var id in Hero.RatedStats)
            {
                if (_hero.ValueOf(id) < Hero.MaxStat) return false;
            }
            return true;
        }

        public override void Dispose()
        {
            // base.Dispose()가 Subscribe로 건 구독을 풀고, 브리지가 Stat.Changed 구독을 푼다.
            base.Dispose();
            _bridge.Dispose();
        }
    }
}
