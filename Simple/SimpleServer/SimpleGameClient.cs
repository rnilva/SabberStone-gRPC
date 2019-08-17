using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServer
{
    public class SimpleGameClient
    {
        public static readonly ConcurrentDictionary<string, SimpleGameClient> Clients =
            new ConcurrentDictionary<string, SimpleGameClient>();

        public event Action<SimpleGameClient> MatchStartedEvent;
        public TaskCompletionSource<object> MatchTCS = new TaskCompletionSource<object>();

        public readonly string Id;
        public ConnectionState.Types.State State { get; set; }
        public Match CurrentMatch { get; set; }

        public SimpleGameClient(string id)
        {
            Id = id;
            State = ConnectionState.Types.State.Connected;

            Console.WriteLine("New Game Client " + id + " is Created.");
        }
    }
}
