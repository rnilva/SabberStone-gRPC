using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServer
{
    public class Match
    {
        private readonly object _locker = new object();
        private TaskCompletionSource<object> _p1TCS;
        private TaskCompletionSource<object> _p2TCS;

        public readonly SimpleGameClient Player1;
        public readonly SimpleGameClient Player2;

        public bool IsCurrentPlayerP1 { get; set; }
        public GameState CurrentGameState { get; set; }

        public Match(SimpleGameClient a, SimpleGameClient b)
        {
            Player1 = a;
            Player2 = b;

            a.CurrentMatch = this;
            b.CurrentMatch = this;

            // game.CurrentPlayer
            IsCurrentPlayerP1 = true;
            _p2TCS = new TaskCompletionSource<object>();
        }

        public GameState SendOption(Option option)
        {
            try
            {
                bool isP1 = option.Id == Player1.Id;

                if (IsCurrentPlayerP1 != isP1)
                {
                    TaskCompletionSource<object> tcs = isP1 ? _p1TCS : _p2TCS;
                    Console.WriteLine($"{(isP1 ? Player1.Id : Player2.Id)} waits... it's not the current player.");
                    tcs.Task.Wait();
                }

                // Start waiting on EndTurn
                if (option.Type == Option.Types.PlayerTaskType.EndTurn)
                {
                    // game.Process()
                    Console.WriteLine(option.Id + " sends " + option.Type);
                    IsCurrentPlayerP1 = !isP1;

                    TaskCompletionSource<object> tcs; 
                    TaskCompletionSource<object> opTcs;
                    lock(_locker)
                    {
                        if (isP1)
                        {
                            tcs = _p1TCS = new TaskCompletionSource<object>();
                            _p2TCS.SetResult(new object());
                        }
                        else
                        {
                            tcs = _p2TCS = new TaskCompletionSource<object>();
                            _p1TCS.SetResult(new object());
                        }
                    }
                    tcs.Task.Wait();
                }
                else
                {
                    // game.Process();
                    Console.WriteLine(option.Id + " sends " + option.Type);
                }

                var result = new GameState
                {
                    State = GameState.Types.State.Running
                }; // result = new GameState(game);

                return result;
            }
            catch
            {
                ;
                return null;
            }
        }
    }
}
