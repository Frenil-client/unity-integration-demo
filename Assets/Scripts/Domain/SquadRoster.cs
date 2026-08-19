using System;
using System.Collections.Generic;
using StatSystem;

namespace SquadDemo.Domain
{
    /// <summary>
    /// 스쿼드와 훈련 진행을 관리하는 도메인 모델.
    /// Unity와 UI 프레임워크 어디에도 의존하지 않는 순수 C#이라, 이 계층만 따로 테스트할 수 있다.
    /// </summary>
    public sealed class SquadRoster
    {
        private static readonly (string Name, SquadPosition Position, int Sho, int Pas, int Pac, int Def)[] Prospects =
        {
            ("이도현", SquadPosition.Forward,    72, 61, 80, 34),
            ("박세훈", SquadPosition.Midfielder, 58, 79, 66, 62),
            ("정우진", SquadPosition.Defender,   41, 63, 59, 81),
            ("최민재", SquadPosition.Goalkeeper, 22, 48, 51, 74),
            ("한지훈", SquadPosition.Forward,    77, 55, 73, 30),
            ("오상현", SquadPosition.Midfielder, 63, 74, 70, 58),
        };

        private readonly List<SquadPlayer> _players = new List<SquadPlayer>();
        private readonly Random _random = new Random(20260819);
        private int _nextProspect;

        public IReadOnlyList<SquadPlayer> Players => _players;

        /// <summary>영입 가능한 선수가 남아 있는지.</summary>
        public bool CanSign => _nextProspect < Prospects.Length;

        public SquadRoster()
        {
            // 시작 스쿼드 두 명
            Sign();
            Sign();
        }

        /// <summary>다음 유망주를 영입한다. 더 없으면 null.</summary>
        public SquadPlayer Sign()
        {
            if (!CanSign) return null;

            var p = Prospects[_nextProspect++];
            var player = new SquadPlayer(p.Name, p.Position, p.Sho, p.Pas, p.Pac, p.Def);
            _players.Add(player);
            return player;
        }

        /// <summary>
        /// 무작위 선수의 무작위 능력치를 1~3 올린다.
        /// 상한(99)에 걸려 실제로 오르지 않을 수 있고, 그 경우 결과의 Improved가 false다.
        /// </summary>
        public TrainingResult TrainRandomPlayer()
        {
            if (_players.Count == 0) return default;

            var player = _players[_random.Next(_players.Count)];
            var ids = SquadPlayer.RatedStatIds;
            StatId id = ids[_random.Next(ids.Count)];
            int amount = _random.Next(1, 4);

            bool improved = player.Train(id, amount);
            return new TrainingResult(player, id, amount, improved);
        }
    }

    public readonly struct TrainingResult
    {
        public SquadPlayer Player { get; }
        public StatId StatId { get; }
        public int Amount { get; }
        public bool Improved { get; }

        public TrainingResult(SquadPlayer player, StatId statId, int amount, bool improved)
        {
            Player = player;
            StatId = statId;
            Amount = amount;
            Improved = improved;
        }

        public bool IsValid => Player != null;
    }
}
