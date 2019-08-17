using System;
using Grpc.Core;

namespace SimpleServer
{
    class Program
    {
        private const int DEFAULT_PORT = 50051;

        static void Main(string[] args)
        {
            int port = DEFAULT_PORT;
            if (args.Length > 1)
                port = Int32.Parse(args[0]);

            var server = new Server
            {
                Services = {Simple.BindService(new Services())},
                Ports = {new ServerPort("localhost", port, ServerCredentials.Insecure)}
            };

            server.Start();

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }   
    }
}
