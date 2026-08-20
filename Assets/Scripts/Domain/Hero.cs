using System.Collections.Generic;
using StatSystem;

namespace LobbyDemo.Domain
{
    public enum HeroClass
    {
        Warrior,
        Mage,
        Archer,
        Priest,
    }

    /// <summary>
    /// 보유 캐릭터 한 명. 능력치 보관은 StatSystem의 <see cref="Stat"/>에 위임한다.
    ///
    /// StatId를 별칭 없이 그대로 쓴다. 이 데모가 모바일 RPG 로비인 이유가 그것으로,
    /// 패키지가 정의한 용어(공격력·마력·방어력)를 다른 도메인으로 번역하지 않아도 된다.
    /// </summary>
    public sealed class Hero
    {
        /// <summary>카드에 표시하고 강화 대상이 되는 능력치.</summary>
        private static readonly StatId[] Rated =
        {
            StatId.AttackPower,
            StatId.MagicAttack,
            StatId.Defense,
            StatId.StatusResistance,
        };

        /// <summary>능력치 상한. 넘어서는 강화는 Stat이 알아서 잘라낸다.</summary>
        public const int MaxStat = 999;

        public string Name { get; }
        public HeroClass Class { get; }
        public Stat Stats { get; }

        public Hero(string name, HeroClass heroClass, int attack, int magic, int defense, int resistance)
        {
            Name = name;
            Class = heroClass;
            Stats = new Stat();

            // 상한을 먼저 잡아야 이후 강화가 999에서 멈춘다.
            foreach (var id in Rated)
                Stats.SetMaxValue(id, (long)MaxStat);

            Stats.SetBaseValue(StatId.AttackPower,       (long)attack);
            Stats.SetBaseValue(StatId.MagicAttack,       (long)magic);
            Stats.SetBaseValue(StatId.Defense,           (long)defense);
            Stats.SetBaseValue(StatId.StatusResistance,  (long)resistance);
        }

        /// <summary>전투력. 네 능력치의 합이며 카드에 표시된다.</summary>
        public int CombatPower
        {
            get
            {
                long sum = 0;
                foreach (var id in Rated)
                    sum += Stats.GetValue(id).Round();

                return (int)sum;
            }
        }

        public int ValueOf(StatId id) => (int)Stats.GetValue(id).Round();

        /// <summary>
        /// 강화로 능력치를 올린다. 상한에 걸리면 Stat이 클램프하며,
        /// 값이 실제로 바뀌지 않으면 Stat.Changed도 발행되지 않는다.
        /// </summary>
        /// <returns>능력치가 실제로 올랐으면 true.</returns>
        public bool Enhance(StatId id, int amount)
        {
            int before = ValueOf(id);
            Stats.AddBaseValue(id, (long)amount);
            return ValueOf(id) != before;
        }

        /// <summary>
        /// 축복처럼 바깥에서 거는 보정을 이 소스 기준으로 다시 건다.
        /// 먼저 같은 소스의 기존 보정을 걷어내고 새로 붙이므로, 중첩이 늘어도
        /// 값이 겹쳐 쌓이지 않는다. percent가 0이면 걷어내기만 한다.
        ///
        /// 강화(AddBaseValue)와 달리 이건 모디파이어라 언제든 정확히 원복된다.
        /// </summary>
        public void SetBlessing(object source, double percent)
        {
            Stats.RemoveModifiersFrom(source);

            if (percent > 0)
                Stats.AddModifier(StatId.AttackPower, StatModifierType.PercentAdd, percent, source);
        }

        public static IReadOnlyList<StatId> RatedStats => Rated;

        public static string ClassLabel(HeroClass heroClass) => heroClass switch
        {
            HeroClass.Warrior => "전사",
            HeroClass.Mage    => "마법사",
            HeroClass.Archer  => "궁수",
            _                 => "사제",
        };

        public static string StatLabel(StatId id) => id switch
        {
            StatId.AttackPower      => "공격력",
            StatId.MagicAttack      => "마력",
            StatId.Defense          => "방어력",
            StatId.StatusResistance => "내성",
            _                       => StatRegistry.GetName(id),
        };
    }
}
