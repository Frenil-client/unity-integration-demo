using System.Collections.Generic;
using Frenil.MVVM;
using NUnit.Framework;
using SquadDemo.Domain;
using SquadDemo.UI;

namespace SquadDemo.Tests
{
    /// <summary>
    /// 화면 없이 도는 테스트다. ViewModel과 도메인이 Unity에 의존하지 않기 때문에
    /// Canvas도 GameObject도 만들지 않고 흐름 전체를 검증할 수 있다 —
    /// unity-mvvm이 ViewModel을 MonoBehaviour로 만들지 않은 이유가 이것이다.
    /// </summary>
    public class SquadViewModelTests
    {
        [Test]
        public void StartsWithInitialSquadAndNoNotifications()
        {
            using var vm = new SquadViewModel();

            Assert.AreEqual(2, vm.Cards.Count);
            Assert.AreEqual(0, vm.UnreadReports.Value);
            Assert.IsTrue(vm.CanSign.Value);
        }

        // 선수를 영입해도 기존 카드는 다시 만들어지지 않아야 한다.
        // Added 델타 한 건만 나가는지가 그 보증이다.
        [Test]
        public void SignNextPlayer_EmitsSingleAddedAtTail()
        {
            using var vm = new SquadViewModel();
            var changes = new List<ListChange<PlayerCardViewModel>>();
            vm.Cards.OnChanged += change => changes.Add(change);

            vm.SignNextPlayer();

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(ListChangeType.Added, changes[0].Type);
            Assert.AreEqual(2, changes[0].Index);
            Assert.AreEqual(3, vm.Cards.Count);
        }

        [Test]
        public void SignNextPlayer_ExhaustsProspectPool()
        {
            using var vm = new SquadViewModel();

            while (vm.CanSign.Value)
                vm.SignNextPlayer();

            Assert.IsFalse(vm.CanSign.Value);
            Assert.AreEqual(0, vm.AvailableSignings.Value);
        }

        [Test]
        public void Training_AccumulatesUnreadReports_AndClaimingClearsThem()
        {
            using var vm = new SquadViewModel();

            for (int i = 0; i < 5; i++)
                vm.TrainRandomPlayer();

            Assert.Greater(vm.UnreadReports.Value, 0);

            vm.ClaimTrainingReports();

            Assert.AreEqual(0, vm.UnreadReports.Value);
        }

        // Stat.Changed -> StatObservableBridge -> Observable 로 이어지는 경로.
        // 이 경로가 데모의 핵심이라 값 하나하나를 고정한다.
        [Test]
        public void StatChange_FlowsIntoRatingObservable()
        {
            var player = new SquadPlayer("테스트", SquadPosition.Forward, 50, 50, 50, 50);
            using var card = new PlayerCardViewModel(player);

            Assert.AreEqual(50, card.RatingOf(SquadPlayer.Shooting).Value);
            Assert.AreEqual(50, card.Overall.Value);

            player.Train(SquadPlayer.Shooting, 4);

            Assert.AreEqual(54, card.RatingOf(SquadPlayer.Shooting).Value);
            Assert.AreEqual(51, card.Overall.Value);
        }

        [Test]
        public void ZeroDeltaTraining_DoesNotNotify()
        {
            var player = new SquadPlayer("테스트", SquadPosition.Forward, 50, 50, 50, 50);
            using var card = new PlayerCardViewModel(player);

            int notifications = 0;
            card.Overall.OnChanged += _ => notifications++;

            player.Train(SquadPlayer.Shooting, 0);

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void Training_ClampsAtMaxRating()
        {
            var player = new SquadPlayer("테스트", SquadPosition.Forward, 50, 50, 50, 50);
            using var card = new PlayerCardViewModel(player);

            player.Train(SquadPlayer.Shooting, 500);

            Assert.AreEqual(SquadPlayer.MaxRating, card.RatingOf(SquadPlayer.Shooting).Value);
            Assert.IsFalse(player.Train(SquadPlayer.Shooting, 5), "상한에서는 개선이 없어야 한다");
        }

        [Test]
        public void MaxedOut_TrueOnlyWhenEveryRatedStatIsCapped()
        {
            var player = new SquadPlayer("테스트", SquadPosition.Forward, 50, 50, 50, 50);
            using var card = new PlayerCardViewModel(player);

            player.Train(SquadPlayer.Shooting, 500);
            Assert.IsFalse(card.MaxedOut.Value);

            foreach (var id in SquadPlayer.RatedStatIds)
                player.Train(id, 500);

            Assert.IsTrue(card.MaxedOut.Value);
        }

        // Dispose가 Stat.Changed 구독을 풀지 않으면 카드가 사라진 뒤에도
        // 선수 스탯 변화가 죽은 ViewModel을 계속 깨운다.
        [Test]
        public void Dispose_StopsObservingTheStat()
        {
            var player = new SquadPlayer("테스트", SquadPosition.Forward, 50, 50, 50, 50);
            var card = new PlayerCardViewModel(player);

            int notifications = 0;
            card.Overall.OnChanged += _ => notifications++;
            card.Dispose();

            player.Train(SquadPlayer.Shooting, 10);

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void Dispose_ClearsCards()
        {
            var vm = new SquadViewModel();
            vm.Dispose();

            Assert.AreEqual(0, vm.Cards.Count);
        }
    }
}
