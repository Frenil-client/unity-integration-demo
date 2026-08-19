using System;
using System.Collections.Generic;
using Frenil.MVVM;
using StatSystem;

namespace LobbyDemo.Glue
{
    /// <summary>
    /// StatSystem의 <see cref="Stat.Changed"/>를 MVVM의 <see cref="Observable{T}"/>로 옮기는 어댑터.
    ///
    /// 이 클래스가 데모에 있는 이유가 이 데모의 설계 전부다. StatSystem이 Observable을 직접
    /// 노출하면 스탯 패키지가 UI 프레임워크에 의존하게 되고, 반대로 MVVM이 Stat을 알면
    /// UI 프레임워크가 게임플레이 패키지에 묶인다. 둘 다 "드롭인으로 쓸 수 있는 독립 패키지"라는
    /// 전제를 깨뜨린다.
    ///
    /// 그래서 StatSystem은 순수 C# 이벤트만 발행하고(어떤 UI 프레임워크와도 붙을 수 있다),
    /// 두 패키지를 실제로 잇는 코드는 그것들을 함께 쓰는 쪽 - 즉 이 데모 - 에만 존재한다.
    /// </summary>
    public sealed class StatObservableBridge : IDisposable
    {
        private readonly Stat _stat;
        private readonly Dictionary<StatId, Observable<int>> _tracked = new Dictionary<StatId, Observable<int>>();
        private bool _disposed;

        public StatObservableBridge(Stat stat)
        {
            _stat = stat ?? throw new ArgumentNullException(nameof(stat));
            _stat.Changed += OnStatChanged;
        }

        /// <summary>
        /// 해당 StatId를 반응형 값으로 노출한다. 같은 id로 여러 번 불러도 같은 인스턴스를 돌려준다.
        /// 반환 타입이 읽기 전용이라 구독자가 값을 거꾸로 쓸 수 없다.
        /// </summary>
        public IReadOnlyObservable<int> Track(StatId id)
        {
            if (!_tracked.TryGetValue(id, out var observable))
            {
                observable = new Observable<int>(Snapshot(id));
                _tracked[id] = observable;
            }

            return observable;
        }

        private void OnStatChanged(StatId id, StatValue value)
        {
            // 추적 중이 아닌 스탯은 무시한다. Observable<T>가 같은 값 대입을 걸러주므로
            // 반올림 결과가 그대로면 UI 갱신도 일어나지 않는다.
            if (_tracked.TryGetValue(id, out var observable))
                observable.Value = (int)value.Round();
        }

        private int Snapshot(StatId id) => (int)_stat.GetValue(id).Round();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stat.Changed -= OnStatChanged;
            _tracked.Clear();
        }
    }
}
