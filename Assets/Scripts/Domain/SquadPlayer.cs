using System.Collections.Generic;
using StatSystem;

namespace SquadDemo.Domain
{
    public enum SquadPosition
    {
        Forward,
        Midfielder,
        Defender,
        Goalkeeper,
    }

    /// <summary>
    /// 선수 한 명. 능력치 보관은 StatSystem의 <see cref="Stat"/>에 위임한다.
    ///
    /// StatSystem은 RPG 용어로 스탯을 정의하지만 값 저장·클램프·변경 통지라는 역할 자체는
    /// 장르와 무관하다. 그래서 패키지를 고치지 않고, 이 데모의 도메인 용어로 읽는 별칭만 둔다.
    /// 실제 프로젝트라면 StatId enum을 그 프로젝트 용어로 정의했을 것이다.
    /// </summary>
    public sealed class SquadPlayer
    {
        public const StatId Shooting  = StatId.AttackPower;
        public const StatId Passing   = StatId.AttackSpeed;
        public const StatId Pace      = StatId.MoveSpeed;
        public const StatId Defending = StatId.Defense;

        /// <summary>능력치 상한. 넘어서는 훈련은 Stat이 알아서 잘라낸다.</summary>
        public const int MaxRating = 99;

        private static readonly StatId[] RatedStats = { Shooting, Passing, Pace, Defending };

        public string Name { get; }
        public SquadPosition Position { get; }
        public Stat Stats { get; }

        public SquadPlayer(string name, SquadPosition position, int shooting, int passing, int pace, int defending)
        {
            Name = name;
            Position = position;
            Stats = new Stat();

            // 상한을 먼저 잡아야 이후 훈련이 99에서 멈춘다.
            foreach (var id in RatedStats)
                Stats.SetMaxValue(id, (long)MaxRating);

            Stats.SetValue(Shooting,  (long)shooting);
            Stats.SetValue(Passing,   (long)passing);
            Stats.SetValue(Pace,      (long)pace);
            Stats.SetValue(Defending, (long)defending);
        }

        /// <summary>네 능력치의 평균. 카드에 표시되는 종합 등급이다.</summary>
        public int Overall
        {
            get
            {
                long sum = 0;
                foreach (var id in RatedStats)
                    sum += Stats.GetValue(id).Round();

                return (int)(sum / RatedStats.Length);
            }
        }

        public int RatingOf(StatId id) => (int)Stats.GetValue(id).Round();

        /// <summary>
        /// 훈련으로 능력치를 올린다. 상한에 걸리면 Stat이 클램프하며,
        /// 값이 실제로 바뀌지 않으면 Stat.Changed도 발행되지 않는다.
        /// </summary>
        /// <returns>능력치가 실제로 올랐으면 true.</returns>
        public bool Train(StatId id, int amount)
        {
            int before = RatingOf(id);
            Stats.AddValue(id, (long)amount);
            return RatingOf(id) != before;
        }

        public static IReadOnlyList<StatId> RatedStatIds => RatedStats;

        public static string PositionLabel(SquadPosition position) => position switch
        {
            SquadPosition.Forward    => "FW",
            SquadPosition.Midfielder => "MF",
            SquadPosition.Defender   => "DF",
            _                        => "GK",
        };

        public static string DisplayName(StatId id) => id switch
        {
            Shooting  => "슛",
            Passing   => "패스",
            Pace      => "스피드",
            Defending => "수비",
            _         => StatRegistry.GetName(id),
        };
    }
}
