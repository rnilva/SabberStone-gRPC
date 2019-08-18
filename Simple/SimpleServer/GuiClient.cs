using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace SimpleServer
{
    public class GuiClient
    {
        public static readonly ConcurrentDictionary<string, GuiClient> Clients
            = new ConcurrentDictionary<string, GuiClient>();

        public readonly string Id;

        public ConnectionState.Types.State State { get; set; }
        public IServerStreamWriter<PowerHistory> Channel { get; set; }
        public TaskCompletionSource<object> StreamingTCS { get; set; }
        public Match CurrentMatch { get; set; }

        public GuiClient(string id)
        {
            Id = id;
        }

        // TODO: BlockingCollection
        public async void WriteAsync(IEnumerable<PowerHistory> histories)
        {
            if (Channel == null) throw new Exception();

            foreach (PowerHistory history in histories) 
                await Channel.WriteAsync(history);
        }
    }
}
