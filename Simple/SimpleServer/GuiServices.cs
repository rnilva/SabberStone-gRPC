using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace SimpleServer
{
    public class GuiServices : GUIService.GUIServiceBase
    {
        public override Task<ConnectionState> ConnectGui(Id request, ServerCallContext context)
        {
            if (!GuiClient.Clients.ContainsKey(request.Value))
                GuiClient.Clients.TryAdd(request.Value, new GuiClient(request.Value));
            else
                GuiClient.Clients[request.Value].State = ConnectionState.Types.State.Connected;

            return Task.FromResult(new ConnectionState {State = ConnectionState.Types.State.Connected});
        }

        public override Task GetHistories(Id request, IServerStreamWriter<PowerHistory> responseStream, ServerCallContext context)
        {
            if (!GuiClient.Clients.TryGetValue(request.Value, out GuiClient client))
                return null;

            client.Channel = responseStream;



            return base.GetHistories(request, responseStream, context);
        }
    }
}
