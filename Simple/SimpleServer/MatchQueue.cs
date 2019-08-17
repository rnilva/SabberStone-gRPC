using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleServer
{
    public class MatchQueue
    {
        private readonly object _locker = new object();
        private readonly ConcurrentQueue<SimpleGameClient> _queue 
            = new ConcurrentQueue<SimpleGameClient>();

        //private long _period = 1000;
        private readonly Timer _timer;

        public MatchQueue()
        {
            //Task.Run(PeriodicMatch);
            _timer = new Timer((e) => { PeriodicMatch(); }, null,
                TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public GameState Enqueue(SimpleGameClient client)
        {
            _queue.Enqueue(client);

            client.MatchTCS.Task.Wait();

            return client.CurrentMatch.CurrentGameState;
        }

        public void PeriodicMatch()
        {
            while (_queue.Count > 1)
            {
                _queue.TryDequeue(out var c1);
                _queue.TryDequeue(out var c2);

                MatchMaker.StartMatch(c1, c2);
            }
        }
    }
}
