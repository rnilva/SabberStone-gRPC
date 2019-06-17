﻿using Grpc.Core;
using SabberStoneClient;
using SabberStoneServer.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SabberStoneXConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");

            //var port = 50051;
            //var target = $"127.0.0.1:{port}";

            //var server = new GameServer();
            //server.Start();

            //Task clientA = CreateClientTask(target, new GameServerStream()
            //{
            //    Message = "testing"
            //}, 250);

            //Task clientB = CreateClientTask(target, new GameServerStream()
            //{
            //    Message = "lying"
            //}, 500);

            //Task clientC = CreateClientTask(target, new GameServerStream()
            //{
            //    Message = "sabber"
            //}, 750);

            //while(!clientA.IsCompleted || !clientB.IsCompleted || !clientC.IsCompleted)
            //{
            //    Thread.Sleep(1000);
            //}

            //server.Stop();
            //Console.ReadKey();

            var port = 50051;

            GameServer server = new GameServer(port);
            server.Start();

            var matchMaker = server.GetMatchMakerService();
            matchMaker.Start(7);

            Task clientA = CreateGameClientTask(port, "Test1", "");
            Thread.Sleep(1000);

            Task clientB = CreateGameClientTask(port, "Test2", "");
            Thread.Sleep(1000);

            while (!clientA.IsCompleted || !clientB.IsCompleted)
            {
                //Console.WriteLine("waiting...");
                Thread.Sleep(5000);
            }

            server.Stop();
            Console.ReadKey();
        }

        private static async Task CreateGameClientTask(int port, string accountName, string accountpsw)
        {
            GameClient client = new GameClient(port, true);

            client.Connect();

            client.Register(accountName, accountpsw);

            Thread.Sleep(2000);

            if (client.GameClientState == GameClientState.Registred)
            {
                client.Queue();
            }

            var waiter = Task.Run(async () =>
            {
                while (client.GameClientState != GameClientState.None)
                {
                    Thread.Sleep(2000);
                };
                await client.Disconnect();
            });

            await waiter;
        }


        //private static int index = 1;

        //private static async Task CreateClientTask(string target, GameServerStream gameServerStream, int sleepMs)
        //{
        //    var channel = new Channel(target, ChannelCredentials.Insecure);
        //    var client = new GameServerService.GameServerServiceClient(channel);

        //    // authentificate
        //    var reply1 = client.Authentication(new AuthRequest { AccountName = $"Test::{index++}", AccountPsw = string.Empty });

        //    using (var call = client.GameServerChannel(headers: new Metadata { new Metadata.Entry("token", reply1.SessionToken) }))
        //    {
        //        var request = Task.Run(async () =>
        //        {
        //            for (int i = 0; i < 10; i++)
        //            {
        //                Thread.Sleep(sleepMs);
        //                await call.RequestStream.WriteAsync(gameServerStream);
        //            }
        //        });
        //        var response = Task.Run(async () =>
        //        {
        //            while (await call.ResponseStream.MoveNext(CancellationToken.None))
        //            {
        //                Console.WriteLine($"{call.ResponseStream.Current.Message}");
        //            };
        //        });

        //        await request;
        //        //await response;
        //    }

        //}
    }
}
