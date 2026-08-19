using Frenil.MVVM;
using SquadDemo.Domain;
using SquadDemo.Glue;
using StatSystem;

namespace SquadDemo.UI
{
    /// <summary>
    /// 선수 카드 하나의 ViewModel. Unity에 의존하지 않는 순수 C#이다.
    ///
    /// 선수의 능력치는 Stat이 보관하고, 그 변경은 StatObservableBridge를 통해
    /// Observable로 흘러들어온다. 카드 UI는 Stat도 SquadPlayer도 모른 채
    /// Observable만 구독하면 된다.
    /// </summary>
    public sealed class PlayerCardViewModel : ViewModelBase
    {
        private readonly SquadPlayer _player;
        private readonly StatObservableBridge _bridge;
        private readonly Observable<int> _overall;
        private readonly Observable<bool> _maxedOut;

        public string Name => _player.Name;
        public string PositionLabel => SquadPlayer.PositionLabel(_player.Position);

        public IReadOnlyObservable<int> Overall => _overall;

        /// <summary>모든 능력치가 상한에 도달했는지. 카드에 배지를 띄우는 용도.</summary>
        public IReadOnlyObservable<bool> MaxedOut => _maxedOut;

        public PlayerCardViewModel(SquadPlayer player)
        {
            _player = player;
            _bridge = new StatObservableBridge(player.Stats);
            _overall = new Observable<int>(player.Overall);
            _maxedOut = new Observable<bool>(IsMaxedOut());

            // 개별 능력치가 바뀌면 종합 등급을 다시 계산한다.
            // 파생 값을 ViewModel이 계산하므로 View는 결과만 받는다.
            foreach (var id in SquadPlayer.RatedStatIds)
                Subscribe(_bridge.Track(id), _ => Refresh());
        }

        /// <summary>능력치 하나를 반응형 값으로 노출한다.</summary>
        public IReadOnlyObservable<int> RatingOf(StatId id) => _bridge.Track(id);

        private void Refresh()
        {
            _overall.Value = _player.Overall;
            _maxedOut.Value = IsMaxedOut();
        }

        private bool IsMaxedOut()
        {
            foreach (var id in SquadPlayer.RatedStatIds)
            {
                if (_player.RatingOf(id) < SquadPlayer.MaxRating) return false;
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
