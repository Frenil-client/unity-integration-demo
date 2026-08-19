using System;
using Frenil.MVVM;
using RedDotSystem;
using SquadDemo.UI;

namespace SquadDemo.Glue
{
    /// <summary>
    /// ViewModel의 "미확인 개수"를 레드닷 트리의 카운트로 옮기는 어댑터.
    ///
    /// SquadViewModel은 레드닷의 존재를 모르고, RedDotSystem은 MVVM의 존재를 모른다.
    /// 둘을 아는 코드는 이 클래스 하나뿐이다.
    ///
    /// 노드 선택에 대하여: RedDotType은 패키지가 제공하는 enum이라 데모가 값을 추가할 수 없어,
    /// 기존 Character 계열 노드에 데모의 의미를 얹었다. 실제 프로젝트라면 RedDotType을
    /// 그 프로젝트의 콘텐츠로 정의한다. enum 키 방식의 트레이드오프가 그대로 드러나는 지점이다
    /// (타입 안전하고 Inspector에서 고르기 좋지만, 새 콘텐츠를 추가하려면 클라이언트를 다시 빌드해야 한다).
    /// </summary>
    public sealed class SquadRedDotBridge : IDisposable
    {
        /// <summary>미확인 훈련 리포트 수가 걸리는 노드.</summary>
        public const RedDotType TrainingReportsNode = RedDotType.CharacterLevelUp;

        /// <summary>영입 가능 인원이 걸리는 노드.</summary>
        public const RedDotType SigningsNode = RedDotType.CharacterEquipment;

        /// <summary>위 둘의 부모. 합계가 자동 집계되어 헤더 배지에 표시된다.</summary>
        public const RedDotType SummaryNode = RedDotType.Character;

        private readonly IReadOnlyObservable<int> _unreadReports;
        private readonly IReadOnlyObservable<int> _availableSignings;
        private readonly Action<int> _onReportsChanged;
        private readonly Action<int> _onSigningsChanged;

        private bool _disposed;

        public SquadRedDotBridge(SquadViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            _unreadReports = viewModel.UnreadReports;
            _availableSignings = viewModel.AvailableSignings;

            _onReportsChanged = count => SetCount(TrainingReportsNode, count);
            _onSigningsChanged = count => SetCount(SigningsNode, count);

            _unreadReports.OnChanged += _onReportsChanged;
            _availableSignings.OnChanged += _onSigningsChanged;

            // 구독 시점의 현재 값으로 한 번 맞춘다.
            _onReportsChanged(_unreadReports.Value);
            _onSigningsChanged(_availableSignings.Value);
        }

        private static void SetCount(RedDotType type, int count)
        {
            var manager = RedDotManager.Instance;
            if (manager == null) return;

            // 리프에만 값을 넣는다. 부모(Character)와 그 위(MainMenu)의 합계는
            // RedDotNode가 델타로 알아서 굴려 올린다.
            manager.SetCount(type, count);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _unreadReports.OnChanged -= _onReportsChanged;
            _availableSignings.OnChanged -= _onSigningsChanged;
        }
    }
}
