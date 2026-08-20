using System;
using System.Collections.Generic;
using StatSystem;

namespace LobbyDemo.Domain
{
    /// <summary>
    /// 보유 캐릭터와 강화 진행을 관리하는 도메인 모델.
    /// Unity와 UI 프레임워크 어디에도 의존하지 않는 순수 C#이라, 이 계층만 따로 테스트할 수 있다.
    /// </summary>
    public sealed class HeroRoster
    {
        /// <summary>일일 임무 하나가 완료되는 강화 횟수.</summary>
        public const int EnhancementsPerQuest = 3;

        /// <summary>일일 임무 보상 1개당 붙는 축복 배율 (공격력 +10%).</summary>
        public const double BlessingPerReward = 0.10;

        private static readonly (string Name, HeroClass Class, int Atk, int Mag, int Def, int Res)[] Summonable =
        {
            ("카일런", HeroClass.Warrior, 620, 180, 540, 210),
            ("세리아", HeroClass.Mage,    190, 700, 320, 260),
            ("이든",   HeroClass.Archer,  580, 240, 360, 190),
            ("루미나", HeroClass.Priest,  210, 610, 400, 480),
            ("가레스", HeroClass.Warrior, 700, 150, 610, 230),
            ("하렌",   HeroClass.Archer,  640, 300, 340, 200),
        };

        private readonly List<Hero> _heroes = new List<Hero>();
        private readonly Random _random = new Random(20260819);

        // 축복은 이 객체 하나를 소스로 삼는다. 덕분에 몇 중첩이든
        // RemoveModifiersFrom 한 번으로 정확히 걷힌다.
        private readonly object _blessingSource = new object();

        private int _nextSummon;
        private int _enhanceProgress;
        private int _blessingStacks;

        public IReadOnlyList<Hero> Heroes => _heroes;

        /// <summary>소환 가능한 캐릭터가 남아 있는지.</summary>
        public bool CanSummon => _nextSummon < Summonable.Length;

        /// <summary>남은 소환권 수. 상점 레드닷 카운트의 소스다.</summary>
        public int RemainingSummons => Summonable.Length - _nextSummon;

        /// <summary>다음 일일 임무 완료까지 남은 강화 횟수.</summary>
        public int EnhancementsUntilQuest => EnhancementsPerQuest - _enhanceProgress;

        /// <summary>현재 축복 중첩 수.</summary>
        public int BlessingStacks => _blessingStacks;

        /// <summary>현재 축복 배율. 0.2면 공격력 +20%.</summary>
        public double BlessingPercent => _blessingStacks * BlessingPerReward;

        public HeroRoster()
        {
            // 시작 캐릭터 두 명
            Summon();
            Summon();
        }

        /// <summary>다음 캐릭터를 소환한다. 더 없으면 null.</summary>
        public Hero Summon()
        {
            if (!CanSummon) return null;

            var s = Summonable[_nextSummon++];
            var hero = new Hero(s.Name, s.Class, s.Atk, s.Mag, s.Def, s.Res);
            _heroes.Add(hero);

            // 새로 온 캐릭터에게도 지금까지 쌓인 축복을 적용한다.
            hero.SetBlessing(_blessingSource, BlessingPercent);
            return hero;
        }

        /// <summary>
        /// 무작위 캐릭터의 무작위 능력치를 10~40 올린다.
        /// 상한(999)에 걸려 실제로 오르지 않을 수 있고, 그 경우 결과의 Improved가 false다.
        /// 일정 횟수마다 일일 임무가 하나 완료된다.
        /// </summary>
        public EnhanceResult EnhanceRandomHero()
        {
            if (_heroes.Count == 0) return default;

            var hero = _heroes[_random.Next(_heroes.Count)];
            var rated = Hero.RatedStats;
            StatId id = rated[_random.Next(rated.Count)];
            int amount = _random.Next(10, 41);

            bool improved = hero.Enhance(id, amount);

            bool questCompleted = false;
            if (improved && ++_enhanceProgress >= EnhancementsPerQuest)
            {
                _enhanceProgress = 0;
                questCompleted = true;
            }

            return new EnhanceResult(hero, id, amount, improved, questCompleted);
        }

        /// <summary>
        /// 축복을 중첩시키고 보유 캐릭터 전체에 다시 적용한다.
        /// 각 캐릭터에서 기존 축복을 걷어내고 새 배율로 다시 거는 방식이라,
        /// 중첩이 늘어도 이전 보정이 남아 겹치지 않는다.
        /// </summary>
        public void AddBlessing(int stacks)
        {
            if (stacks <= 0) return;

            _blessingStacks += stacks;

            foreach (var hero in _heroes)
                hero.SetBlessing(_blessingSource, BlessingPercent);
        }
    }

    public readonly struct EnhanceResult
    {
        public Hero Hero { get; }
        public StatId StatId { get; }
        public int Amount { get; }
        public bool Improved { get; }

        /// <summary>이번 강화로 일일 임무가 하나 완료되었는지.</summary>
        public bool QuestCompleted { get; }

        public EnhanceResult(Hero hero, StatId statId, int amount, bool improved, bool questCompleted)
        {
            Hero = hero;
            StatId = statId;
            Amount = amount;
            Improved = improved;
            QuestCompleted = questCompleted;
        }

        public bool IsValid => Hero != null;
    }
}
