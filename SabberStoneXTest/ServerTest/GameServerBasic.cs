using Grpc.Core;
using Xunit;
using SabberStoneServer.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SabberStoneXTest.ServerTest
{
    public class GameServerBasic
    {
        private readonly GameServer _server;

        private int _port = 50051;

        private string _target;

        public GameServerBasic()
        {
            _server = new GameServer(_port);
            _target = $"127.0.0.1:{_port}";
        }

        [Fact]
        public void PingRequest()
        {
            _server.Start();

            var channel = new Channel(_target, ChannelCredentials.Insecure);
            Assert.Equal(ChannelState.Idle, channel.State);

            var client = new GameServerService.GameServerServiceClient(channel);
            Assert.Equal(ChannelState.Idle, channel.State);

            var reply = client.Ping(new PingRequest { Message = string.Empty });

            Assert.True(reply.RequestState);
            Assert.Equal("Ping", reply.RequestMessage);

            _server.Stop();
        }

        [Fact]
        public void AuthRequest()
        {
            _server.Start();

            var channel = new Channel(_target, ChannelCredentials.Insecure);
            Assert.Equal(ChannelState.Idle, channel.State);

            var client = new GameServerService.GameServerServiceClient(channel);
            Assert.Equal(ChannelState.Idle, channel.State);

            var reply1 = client.Authentication(new AuthRequest { AccountName = "Test", AccountPsw = string.Empty });

            Assert.True(reply1.RequestState);
            Assert.Equal(10000, reply1.SessionId);

            var reply2 = client.Authentication(new AuthRequest { AccountName = "Test", AccountPsw = string.Empty });
            Assert.False(reply2.RequestState);

            _server.Stop();
        }

        [Fact]
        public async void GameQueueRequest()
        {
            _server.Start();

            var channel1 = new Channel(_target, ChannelCredentials.Insecure);
            Assert.Equal(ChannelState.Idle, channel1.State);

            var client1 = new GameServerService.GameServerServiceClient(channel1);
            Assert.Equal(ChannelState.Idle, channel1.State);

            var reply1 = client1.Authentication(new AuthRequest { AccountName = "Test", AccountPsw = string.Empty });

            Assert.True(reply1.RequestState);
            Assert.Equal(10000, reply1.SessionId);

            // Initialisation
            using (var call = client1.GameServerChannel(headers: new Metadata { new Metadata.Entry("token", reply1.SessionToken) }))
            {
                await call.RequestStream.WriteAsync(new GameServerStream
                {
                    MessageType = MsgType.Initialisation,
                    Message = string.Empty
                });

                Assert.True(await call.ResponseStream.MoveNext());
                Assert.Equal(MsgType.Initialisation, call.ResponseStream.Current.MessageType);
                Assert.True(call.ResponseStream.Current.MessageState);
            }

            // GameQueue
            var reply2 = client1.GameQueue(
                new QueueRequest
                {
                    GameType = GameType.Normal,
                    DeckType = DeckType.Random,
                    DeckData = string.Empty
                },
                new Metadata {
                    new Metadata.Entry("token", reply1.SessionToken)
                });

            Assert.True(reply2.RequestState);

            _server.Stop();
        }

        [Fact]
        public async Task GameServerChannelAsync()
        {
            _server.Start();

            var channel1 = new Channel(_target, ChannelCredentials.Insecure);
            Assert.Equal(ChannelState.Idle, channel1.State);

            var client1 = new GameServerService.GameServerServiceClient(channel1);
            Assert.Equal(ChannelState.Idle, channel1.State);

            var reply1 = client1.Authentication(new AuthRequest { AccountName = "Test", AccountPsw = string.Empty });

            Assert.True(reply1.RequestState);
            Assert.Equal(10000, reply1.SessionId);

            using (var call = client1.GameServerChannel(headers: new Metadata { new Metadata.Entry("token", reply1.SessionToken) }))
            {
                await call.RequestStream.WriteAsync(new GameServerStream
                {
                    MessageType = MsgType.Initialisation,
                    Message = string.Empty
                });

                Assert.True(await call.ResponseStream.MoveNext());
                Assert.Equal(MsgType.Initialisation, call.ResponseStream.Current.MessageType);
                Assert.True(call.ResponseStream.Current.MessageState);
            }

            _server.Stop();
        }
    }
}
