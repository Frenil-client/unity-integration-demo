using System;
using System.Collections.Generic;
using Frenil.MVVM;
using LobbyDemo.UI;
using RedDotSystem;

namespace LobbyDemo.Glue
{
    /// <summary>
    /// ViewModel의 "처리할 것이 남았다"는 수치를 레드닷 트리의 카운트로 옮기는 어댑터.
    ///
    /// LobbyViewModel은 레드닷의 존재를 모르고, RedDotSystem은 MVVM의 존재를 모른다.
    /// 둘을 아는 코드는 이 클래스 하나뿐이다.
    ///
    /// 노드는 RedDotType이 정의한 로비 구조를 그대로 쓴다. 상점·캐릭터·퀘스트 세 가지가
    /// 각각 자기 리프에 값을 넣고, 그 합계는 부모인 MainMenu에서 자동으로 만들어진다.
    /// 합산 코드는 어디에도 없다 - RedDotNode가 델타로 굴려 올릴 뿐이다.
    ///
    ///   MainMenu          헤더 배지 (합계)
    ///   ├─ Shop           └ ShopPackage      남은 소환권
    ///   ├─ Character      └ CharacterLevelUp 미확인 보상 기록
    ///   └─ Quest          └ QuestDaily       완료된 일일 임무
    /// </summary>
    public sealed class LobbyRedDotBridge : IDisposable
    {
        /// <summary>남은 소환권이 걸리는 노드.</summary>
        public const RedDotType SummonNode = RedDotType.ShopPackage;

        /// <summary>미확인 보상 기록이 걸리는 노드.</summary>
        public const RedDotType RewardRecordNode = RedDotType.CharacterLevelUp;

        /// <summary>완료된 일일 임무가 걸리는 노드.</summary>
        public const RedDotType DailyQuestNode = RedDotType.QuestDaily;

        /// <summary>세 리프의 최상위 부모. 헤더 배지가 여기에 붙어 전체 합계를 표시한다.</summary>
        public const RedDotType SummaryNode = RedDotType.MainMenu;

        private readonly List<Action> _unsubscribes = new List<Action>();
        private bool _disposed;

        public LobbyRedDotBridge(LobbyViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            Relay(viewModel.RemainingSummons, SummonNode);
            Relay(viewModel.UnreadRewards, RewardRecordNode);
            Relay(viewModel.CompletedDailyQuests, DailyQuestNode);
        }

        private void Relay(IReadOnlyObservable<int> source, RedDotType node)
        {
            Action<int> handler = count => SetCount(node, count);

            source.OnChanged += handler;
            _unsubscribes.Add(() => source.OnChanged -= handler);

            // 구독 시점의 현재 값으로 한 번 맞춘다.
            handler(source.Value);
        }

        private static void SetCount(RedDotType type, int count)
        {
            // 리프에만 값을 넣는다. 부모의 합계는 트리가 만든다.
            // RedDotTree는 첫 접근에 스스로 구성되므로 씬에 매니저가 없어도 동작한다.
            RedDotTree.SetCount(type, count);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var unsubscribe in _unsubscribes)
                unsubscribe?.Invoke();

            _unsubscribes.Clear();
        }
    }
}
