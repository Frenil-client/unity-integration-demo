using System.Collections.Generic;
using Frenil.MVVM;
using LobbyDemo.Domain;
using LobbyDemo.UI;
using NUnit.Framework;
using StatSystem;

namespace LobbyDemo.Tests
{
    /// <summary>
    /// 화면 없이 도는 테스트다. 도메인과 ViewModel이 Unity에 의존하지 않기 때문에
    /// Canvas도 GameObject도 만들지 않고 흐름 전체를 검증할 수 있다 —
    /// unity-mvvm이 ViewModel을 MonoBehaviour로 만들지 않은 이유가 이것이다.
    /// </summary>
    public class LobbyViewModelTests
    {
        private static Hero NewHero() => new Hero("테스트", HeroClass.Warrior, 500, 500, 500, 500);

        [Test]
        public void StartsWithInitialHeroesAndNoNotifications()
        {
            using var vm = new LobbyViewModel();

            Assert.AreEqual(2, vm.Cards.Count);
            Assert.AreEqual(0, vm.UnreadRewards.Value);
            Assert.AreEqual(0, vm.CompletedDailyQuests.Value);
            Assert.IsTrue(vm.CanSummon.Value);
            Assert.Greater(vm.RemainingSummons.Value, 0);
        }

        // 캐릭터를 소환해도 기존 카드는 다시 만들어지지 않아야 한다.
        // Added 델타 한 건만 나가는지가 그 보증이다.
        [Test]
        public void SummonNextHero_EmitsSingleAddedAtTail()
        {
            using var vm = new LobbyViewModel();
            var changes = new List<ListChange<HeroCardViewModel>>();
            vm.Cards.OnChanged += change => changes.Add(change);

            vm.SummonNextHero();

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(ListChangeType.Added, changes[0].Type);
            Assert.AreEqual(2, changes[0].Index);
            Assert.AreEqual(3, vm.Cards.Count);
        }

        [Test]
        public void SummonNextHero_ExhaustsThePool()
        {
            using var vm = new LobbyViewModel();

            while (vm.CanSummon.Value)
                vm.SummonNextHero();

            Assert.IsFalse(vm.CanSummon.Value);
            Assert.AreEqual(0, vm.RemainingSummons.Value);
        }

        [Test]
        public void Enhancing_AccumulatesRecords_AndUnreadCountTracksThem()
        {
            using var vm = new LobbyViewModel();

            for (int i = 0; i < 5; i++)
                vm.EnhanceRandomHero();

            Assert.Greater(vm.Reports.Count, 0);
            Assert.AreEqual(vm.Reports.Count, vm.UnreadRewards.Value);
        }

        // 세 레드닷 소스가 서로 다른 가지에 있으므로, 각각 독립적으로 오르내려야 한다.
        [Test]
        public void DailyQuest_CompletesEveryFixedNumberOfEnhancements()
        {
            using var vm = new LobbyViewModel();

            for (int i = 0; i < HeroRoster.EnhancementsPerQuest; i++)
                vm.EnhanceRandomHero();

            Assert.AreEqual(1, vm.CompletedDailyQuests.Value);
        }

        [Test]
        public void ClaimDailyQuests_ClearsTheCount()
        {
            using var vm = new LobbyViewModel();
            for (int i = 0; i < HeroRoster.EnhancementsPerQuest; i++)
                vm.EnhanceRandomHero();

            vm.ClaimDailyQuests();

            Assert.AreEqual(0, vm.CompletedDailyQuests.Value);
        }

        // 볼 것이 없으면 빈 팝업을 띄우지 않는다.
        [Test]
        public void OpenRewardPopup_WithNothingToRead_StaysClosed()
        {
            using var vm = new LobbyViewModel();

            vm.OpenRewardPopup();

            Assert.IsFalse(vm.IsRewardPopupOpen.Value);
        }

        // 여는 순간을 "확인"으로 본다 - 레드닷은 이때 꺼지고, 내용은 그대로 남아 있어야 한다.
        [Test]
        public void OpenRewardPopup_OpensAndClearsUnreadCount()
        {
            using var vm = new LobbyViewModel();
            for (int i = 0; i < 5; i++) vm.EnhanceRandomHero();
            int reportCount = vm.Reports.Count;

            vm.OpenRewardPopup();

            Assert.IsTrue(vm.IsRewardPopupOpen.Value);
            Assert.AreEqual(0, vm.UnreadRewards.Value);
            Assert.AreEqual(reportCount, vm.Reports.Count, "여는 것만으로 목록이 비면 안 된다");
        }

        [Test]
        public void CloseRewardPopup_ClosesAndEmptiesTheList()
        {
            using var vm = new LobbyViewModel();
            for (int i = 0; i < 5; i++) vm.EnhanceRandomHero();
            vm.OpenRewardPopup();

            vm.CloseRewardPopup();

            Assert.IsFalse(vm.IsRewardPopupOpen.Value);
            Assert.AreEqual(0, vm.Reports.Count);
            Assert.AreEqual(0, vm.UnreadRewards.Value);
        }

        // X 버튼과 바깥 클릭이 같은 경로로 들어오므로, 닫힌 상태에서 또 불려도 안전해야 한다.
        [Test]
        public void CloseRewardPopup_WhenAlreadyClosed_DoesNothing()
        {
            using var vm = new LobbyViewModel();
            for (int i = 0; i < 3; i++) vm.EnhanceRandomHero();
            int reportCount = vm.Reports.Count;

            vm.CloseRewardPopup();

            Assert.AreEqual(reportCount, vm.Reports.Count);
            Assert.AreEqual(reportCount, vm.UnreadRewards.Value);
        }

        // 임무 보상은 축복(모디파이어)이다. 기본값은 건드리지 않고 최종값만 올린다.
        [Test]
        public void AddBlessing_RaisesFinalAttack_ButLeavesBaseAlone()
        {
            var roster = new HeroRoster();
            var hero = roster.Heroes[0];          // 카일런, 공격력 620
            int baseAttack = (int)hero.Stats.GetBaseValue(StatId.AttackPower).Round();

            roster.AddBlessing(1);                // +10%

            Assert.AreEqual(baseAttack, (int)hero.Stats.GetBaseValue(StatId.AttackPower).Round(),
                "기본값은 그대로여야 한다");
            Assert.AreEqual(682, hero.ValueOf(StatId.AttackPower), "620 × 1.1");
        }

        // 중첩할 때 이전 축복을 걷어내고 새 배율로 다시 걸므로 곱해지지 않는다.
        // 걷어내지 않으면 620 × 1.1 × 1.1 = 750이 되어버린다.
        [Test]
        public void AddBlessing_Stacks_WithoutCompounding()
        {
            var roster = new HeroRoster();
            var hero = roster.Heroes[0];

            roster.AddBlessing(1);
            roster.AddBlessing(1);

            Assert.AreEqual(2, roster.BlessingStacks);
            Assert.AreEqual(744, hero.ValueOf(StatId.AttackPower), "620 × 1.2 (1.1 × 1.1 이 아니다)");
        }

        [Test]
        public void Blessing_AppliesToHeroesSummonedLater()
        {
            var roster = new HeroRoster();
            roster.AddBlessing(2);                // +20%

            var summoned = roster.Summon();       // 이든, 공격력 580

            Assert.AreEqual(696, summoned.ValueOf(StatId.AttackPower), "580 × 1.2");
        }

        // 임무를 수령하면 그 기록이 보상 목록에 남고, 읽으라는 알림이 보상 버튼에 켜진다.
        [Test]
        public void ClaimDailyQuests_AddsRecord_AndLightsTheRewardDot()
        {
            using var vm = new LobbyViewModel();
            for (int i = 0; i < HeroRoster.EnhancementsPerQuest; i++)
                vm.EnhanceRandomHero();

            vm.OpenRewardPopup();
            vm.CloseRewardPopup();                // 기존 기록을 비워 둔다
            Assert.AreEqual(0, vm.Reports.Count);
            Assert.AreEqual(0, vm.UnreadRewards.Value);

            vm.ClaimDailyQuests();

            Assert.AreEqual(1, vm.Reports.Count, "임무 보상도 기록에 남는다");
            Assert.AreEqual(1, vm.UnreadRewards.Value);
            Assert.AreEqual(0, vm.CompletedDailyQuests.Value);
        }

        // Stat 모디파이어 -> 브리지 -> Observable -> 카드까지 이어지는지.
        [Test]
        public void ClaimDailyQuests_RaisesCombatPowerOnExistingCards()
        {
            using var vm = new LobbyViewModel();
            for (int i = 0; i < HeroRoster.EnhancementsPerQuest; i++)
                vm.EnhanceRandomHero();

            int before = vm.Cards[0].CombatPower.Value;

            vm.ClaimDailyQuests();

            Assert.Greater(vm.Cards[0].CombatPower.Value, before);
        }

        // Stat.Changed -> StatObservableBridge -> Observable 로 이어지는 경로.
        // 이 경로가 데모의 핵심이라 값 하나하나를 고정한다.
        [Test]
        public void StatChange_FlowsIntoValueObservable()
        {
            var hero = NewHero();
            using var card = new HeroCardViewModel(hero);

            Assert.AreEqual(500, card.ValueOf(StatId.AttackPower).Value);
            Assert.AreEqual(2000, card.CombatPower.Value);

            hero.Enhance(StatId.AttackPower, 40);

            Assert.AreEqual(540, card.ValueOf(StatId.AttackPower).Value);
            Assert.AreEqual(2040, card.CombatPower.Value);
        }

        [Test]
        public void ZeroDeltaEnhance_DoesNotNotify()
        {
            var hero = NewHero();
            using var card = new HeroCardViewModel(hero);

            int notifications = 0;
            card.CombatPower.OnChanged += _ => notifications++;

            hero.Enhance(StatId.AttackPower, 0);

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void Enhance_ClampsAtMaxStat()
        {
            var hero = NewHero();
            using var card = new HeroCardViewModel(hero);

            hero.Enhance(StatId.AttackPower, 9999);

            Assert.AreEqual(Hero.MaxStat, card.ValueOf(StatId.AttackPower).Value);
            Assert.IsFalse(hero.Enhance(StatId.AttackPower, 10), "상한에서는 개선이 없어야 한다");
        }

        [Test]
        public void MaxedOut_TrueOnlyWhenEveryStatIsCapped()
        {
            var hero = NewHero();
            using var card = new HeroCardViewModel(hero);

            hero.Enhance(StatId.AttackPower, 9999);
            Assert.IsFalse(card.MaxedOut.Value);

            foreach (var id in Hero.RatedStats)
                hero.Enhance(id, 9999);

            Assert.IsTrue(card.MaxedOut.Value);
        }

        // Dispose가 Stat.Changed 구독을 풀지 않으면 카드가 사라진 뒤에도
        // 능력치 변화가 죽은 ViewModel을 계속 깨운다.
        [Test]
        public void Dispose_StopsObservingTheStat()
        {
            var hero = NewHero();
            var card = new HeroCardViewModel(hero);

            int notifications = 0;
            card.CombatPower.OnChanged += _ => notifications++;
            card.Dispose();

            hero.Enhance(StatId.AttackPower, 30);

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void Dispose_ClearsCards()
        {
            var vm = new LobbyViewModel();
            vm.Dispose();

            Assert.AreEqual(0, vm.Cards.Count);
        }
    }
}
