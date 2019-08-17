using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace SimpleClient
{
    class Program
    {
        private const int DEFAULT_PORT = 50051;

        static async Task Main(string[] args)
        {
            int port = DEFAULT_PORT;
            if (args.Length > 1)
                port = Int32.Parse(args[0]);

            // Estanlish a gRPC connection
            var channel = new Channel("localhost", port, ChannelCredentials.Insecure);
            var stub = new Simple.SimpleClient(channel);

            Task.Run(() => RunClient(stub, "Client1"));
            Task.Run(() => RunClient(stub, "Client2"));

            Console.ReadKey();
        }

        static async Task RunClient(Simple.SimpleClient stub, string clientId)
        {
            var id = new Id {Value = clientId};

            // Connect -> register the id of this client
            stub.Connect(id);

            // Queue -> Wait for a new matchup
            GameState game = stub.Queue(id);

            Console.WriteLine(clientId + " entered to a new match");

            for (int i = 0; i < 100; i++)
                stub.SendOption(GenerateRandomOption(clientId));
        }

        static readonly Random rnd = new Random();

        static Option GenerateRandomOption(string id)
        {
            return new Option
            {
                Id = id,
                Type = (Option.Types.PlayerTaskType) rnd.Next(7)
            };
        }
    }
}
