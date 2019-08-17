using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace SimpleServer
{
    public class Services : Simple.SimpleBase
    {
        private MatchQueue _matchQueue = new MatchQueue();


        public override Task<ConnectionState> Connect(Id request, ServerCallContext context)
        {
            if (!SimpleGameClient.Clients.ContainsKey(request.Value))
                SimpleGameClient.Clients.TryAdd(request.Value, new SimpleGameClient(request.Value));
            else
                SimpleGameClient.Clients[request.Value].State = ConnectionState.Types.State.Connected;

            return Task.FromResult(new ConnectionState {State = ConnectionState.Types.State.Connected});
        }

        public override Task<GameState> Queue(Id request, ServerCallContext context)
        {
            if (!SimpleGameClient.Clients.TryGetValue(request.Value, out SimpleGameClient client))
                return Task.FromResult(new GameState {State = GameState.Types.State.Invalid, ErrorCode = 1});

            Console.WriteLine(request.Value + " is queueing...");

            return Task.FromResult(_matchQueue.Enqueue(client));
        }

        public override Task<ConnectionState> Disconnect(Id request, ServerCallContext context)
        {
            if (!SimpleGameClient.Clients.TryGetValue(request.Value, out SimpleGameClient client))
                return Task.FromResult(new ConnectionState {State = ConnectionState.Types.State.Invalid});

            client.State = ConnectionState.Types.State.Disconnected;
            Console.WriteLine("Game Client " + request.Value + " is disconnected.");
            return Task.FromResult(new ConnectionState {State = ConnectionState.Types.State.Disconnected});
        }

        public override Task<GameState> SendOption(Option request, ServerCallContext context)
        {
            if (!SimpleGameClient.Clients.TryGetValue(request.Id, out SimpleGameClient client))
            {
                ;
                return Task.FromResult(new GameState {State = GameState.Types.State.Invalid, ErrorCode = 1});
            }

            //return Processor.Process(client.CurrentMatch, request);

            return Task.FromResult(client.CurrentMatch.SendOption(request));
        }
    }
}
