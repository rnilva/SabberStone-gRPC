using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SimpleServer
{
    public static class MatchMaker
    {
        private static int _idGen = 0;
        private static readonly object _locker = new object();
        private static int GetNextId()
        {
            lock(_locker)
                return _idGen++;
        }

        public static ConcurrentDictionary<int, Match> Matches
            = new ConcurrentDictionary<int, Match>();
        public static GameState StartMatch(SimpleGameClient a, SimpleGameClient b)
        {
            Console.WriteLine("Match started! " + a.Id + " vs. " + b.Id);

            var newMatch = new Match(a, b);

            Matches.TryAdd(GetNextId(), newMatch);

            // Can be Match's instance method
            var newGameState = new GameState
            {
                State = GameState.Types.State.Running
            };
            newMatch.CurrentGameState = newGameState;

            a.MatchTCS.SetResult(new object());
            b.MatchTCS.SetResult(new object());

            return newGameState;
        }
    }
}
