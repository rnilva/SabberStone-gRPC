﻿using Grpc.Core;
using log4net;
using Newtonsoft.Json;
using SabberStoneClient.AI;
using SabberStoneContract.Model;
using SabberStoneCore.Kettle;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using static GameServerService;

namespace SabberStoneClient.Core
{
    public class GameClient
    {
        private static readonly ILog Log = Logger.Instance.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly int _port;

        private readonly string _target;

        private Channel _channel;

        private GameServerServiceClient _client;

        private GameClientState _gameClientState;

        public GameClientState GameClientState
        {
            get => _gameClientState;
            private set
            {
                var oldValue = _gameClientState;

                _gameClientState = value;

                ActionGameClientStateChange(oldValue, value);

                if (oldValue == GameClientState.InGame && value != GameClientState.InGame)
                {
                    // game left clean up
                    AfterInGame();
                }

            }
        }

        private int _sessionId;

        private string _sessionToken;

        private int _gameId;

        private int _playerId;

        private bool _logGames;

        private List<UserInfo> _userInfos;

        public string AccountName { get; private set; }

        public UserInfo MyUserInfo => _userInfos.FirstOrDefault(p => p.PlayerId == _playerId);

        public UserInfo OpUserInfo => _userInfos.FirstOrDefault(p => p.PlayerId != _playerId);

        private List<string> _fullGameHistory { get; }



        private ISabberStoneAI _sabberStoneAI;

        private ConcurrentQueue<GameServerStream> _gameServerStream;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public GameController GameController { get; }

        public GameClient(int port, ISabberStoneAI sabberStoneAI, string accountName = "", bool logGames = false)
        {
            _port = port;
            _sabberStoneAI = sabberStoneAI ?? new RandomAI();

            _target = $"127.0.0.1:{_port}";
            GameClientState = GameClientState.None;

            _gameId = -1;
            _playerId = -1;
            _logGames = logGames;

            _userInfos = new List<UserInfo>();

            AccountName = accountName;
            _fullGameHistory = new List<string>();


            _gameServerStream = new ConcurrentQueue<GameServerStream>();
            _cancellationTokenSource = new CancellationTokenSource();

            GameController = new GameController();
        }

        public void Connect()
        {
            _channel = new Channel(_target, ChannelCredentials.Insecure);
            _client = new GameServerServiceClient(_channel);
            GameClientState = GameClientState.Connected;
        }

        public void Register(string accountName, string accountPsw)
        {
            if (GameClientState != GameClientState.Connected)
            {
                Log.Warn("Client isn't connected.");
                return;
            }

            var authReply = _client.Authentication(new AuthRequest { AccountName = accountName, AccountPsw = accountPsw });

            if (!authReply.RequestState)
            {
                Log.Warn("Bad RegisterRequest.");
                return;
            }

            _sessionId = authReply.SessionId;
            _sessionToken = authReply.SessionToken;

            GameClientState = GameClientState.Registred;

            Log.Info($"Register done.");

            GameServerChannel();

            Log.Info($"Game Server Channel created.");
        }

        public void MatchGame()
        {
            if (GameClientState != GameClientState.InGame)
            {
                Log.Warn("Client isn't in a game.");
                return;
            }

            var matchGameReply = _client.MatchGame(new MatchGameRequest { GameId = _gameId }, new Metadata { new Metadata.Entry("token", _sessionToken) });

            if (!matchGameReply.RequestState)
            {
                Log.Warn("Bad MatchGameRequest.");
                return;
            }

            // TODO do something with the game object ...
            Log.Info($"Got match game successfully.");
        }

        public async void GameServerChannel()
        {
            using (var call = _client.GameServerChannel(headers: new Metadata { new Metadata.Entry("token", _sessionToken) }, cancellationToken: _cancellationTokenSource.Token))
            {
                var requestStreamWriterTask = new Task(async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (_gameServerStream.TryDequeue(out GameServerStream gameServerStream))
                        {
                            await call.RequestStream.WriteAsync(gameServerStream);
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }
                });
                requestStreamWriterTask.Start();


                await Task.Run(async () =>
                {
                    try
                    {
                        while (await call.ResponseStream.MoveNext(_cancellationTokenSource.Token))
                        { 
                            ProcessChannelMessage(call.ResponseStream.Current);
                            Thread.Sleep(5);
                        }
                    }
                    catch (RpcException exception)
                    {
                        if (exception.StatusCode != StatusCode.Cancelled)
                        {
                            Log.Error(exception.ToString());
                        }
                    }
                });
            }
        }

        public void WriteGameServerStream(MsgType messageType, bool messageState, GameData gameData)
        {
            if (_gameServerStream == null)
            {
                Log.Warn($"There is no write stream currently.");
                return;
            }

            _gameServerStream.Enqueue(new GameServerStream
            {
                MessageType = messageType,
                MessageState = messageState,
                Message = JsonConvert.SerializeObject(gameData)
            });
        }

        public void SendInvitationReply(bool accept)
        {
            WriteGameServerStream(MsgType.Invitation, accept, new GameData { GameId = _gameId, PlayerId = _playerId, GameDataType = GameDataType.None });
        }

        public void SendPowerChoicesChoice(PowerChoices powerChoices)
        {
            // clear before sent ...
            GameController.PowerChoices = null;
            WriteGameServerStream(MsgType.InGame, true, new GameData() { GameId = _gameId, PlayerId = _playerId, GameDataType = GameDataType.PowerChoices, GameDataObject = JsonConvert.SerializeObject(powerChoices) });
        }

        public void SendPowerOptionChoice(PowerOptionChoice powerOptionChoice)
        {
            // clear before sent ...
            GameController.PowerOptionList.Clear();
            WriteGameServerStream(MsgType.InGame, true, new GameData() { GameId = _gameId, PlayerId = _playerId, GameDataType = GameDataType.PowerOptions, GameDataObject = JsonConvert.SerializeObject(powerOptionChoice) });
        }

        public void Disconnect()
        {
            _client.Disconnect(new ServerRequest(), new Metadata { new Metadata.Entry("token", _sessionToken) });

            _cancellationTokenSource.Cancel();

            GameClientState = GameClientState.None;

            _channel.ShutdownAsync();
        }

        private void ProcessChannelMessage(GameServerStream current)
        {
            if (!current.MessageState)
            {
                Log.Warn($"Failed messageType {current.MessageType}, '{current.Message}'!");
                return;
            }

            GameData gameData = null;
            if (current.Message != string.Empty)
            {
                gameData = JsonConvert.DeserializeObject<GameData>(current.Message);
                //Log.Info($"GameData[Id:{gameData.GameId},Player:{gameData.PlayerId}]: {gameData.GameDataType} received");
            }
            else
            {
                //Log.Info($"Message[{current.MessageState},{current.MessageType}]: received.");
            }

            switch (current.MessageType)
            {
                case MsgType.Initialisation:
                    //GameClientState = GameClientState.Registred;
                    //registerWaiter.SetResult(new object());
                    break;

                case MsgType.Invitation:
                    _gameId = gameData.GameId;
                    _playerId = gameData.PlayerId;
                    GameClientState = GameClientState.Invited;

                    // action call here
                    ActionCallInvitation();
                    break;

                case MsgType.InGame:
                    switch (gameData.GameDataType)
                    {
                        case GameDataType.Initialisation:
                            _userInfos = JsonConvert.DeserializeObject<List<UserInfo>>(gameData.GameDataObject);
                            GameClientState = GameClientState.InGame;
                            Log.Info($"Initialized game against account {OpUserInfo.AccountName}!");

                            // action call here
                            ActionCallInitialisation();
                            break;

                        case GameDataType.PowerHistory:
                            List<IPowerHistoryEntry> powerHistoryEntries = JsonConvert.DeserializeObject<List<IPowerHistoryEntry>>(gameData.GameDataObject, new PowerHistoryConverter());
                            _fullGameHistory.Add(gameData.GameDataObject);
                            powerHistoryEntries.ForEach(p => GameController.HistoryEntries.Enqueue(p));
                            break;

                        case GameDataType.PowerChoices:
                            GameController.PowerChoices = JsonConvert.DeserializeObject<PowerChoices>(gameData.GameDataObject);

                            // action call here
                            ActionCallPowerChoices();
                            break;

                        case GameDataType.PowerOptions:
                            var powerOptions = JsonConvert.DeserializeObject<PowerOptions>(gameData.GameDataObject);
                            GameController.PowerOptionList = powerOptions.PowerOptionList;
                            if (GameController.PowerOptionList != null &&
                               GameController.PowerOptionList.Count > 0)
                            {

                                // action call here
                                ActionCallPowerOptions();
                                break;
                            }
                            break;

                        case GameDataType.Result:

                            //Log.Info($" ... ");
                            GameClientState = GameClientState.Registred;
                            break;
                    }
                    break;
            }
        }

        public void Queue(GameType gameType = GameType.Normal, DeckType deckType = DeckType.Random, string deckData = null)
        {
            if (GameClientState != GameClientState.Registred)
            {
                Log.Warn("Client isn't registred.");
                return;
            }

            var queueReply = _client.GameQueue(
                new QueueRequest
                {
                    GameType = gameType,
                    DeckType = deckType,
                    DeckData = deckData ?? string.Empty
                },
                new Metadata {
                    new Metadata.Entry("token", _sessionToken)
            });

            if (!queueReply.RequestState)
            {
                Log.Warn("Bad QueueRequest.");
                return;
            }

            GameClientState = GameClientState.Queued;
        }

        private void AfterInGame()
        {
            if (_logGames && _fullGameHistory.Any())
            {
                var gameLogFilename = $"GAME-{MyUserInfo.GameId}-{MyUserInfo.PlayerId}-{DateTime.Now.Ticks}.log";
                var gameLogFilePath = Path.Combine(Directory.GetCurrentDirectory(), gameLogFilename);
                Log.Info($"Writing game log to {gameLogFilePath}.");
                File.WriteAllText(gameLogFilePath, JsonConvert.SerializeObject(_fullGameHistory));
            }

            // clean up
            _userInfos.Clear();
            _fullGameHistory.Clear();
            while (!GameController.HistoryEntries.IsEmpty)
            {
                GameController.HistoryEntries.TryDequeue(out _);
            }
            GameController.PowerChoices = null;
            GameController.PowerOptionList.Clear();
        }

        public async virtual void ActionGameClientStateChange(GameClientState oldState, GameClientState newState)
        {
            await Task.Run(() =>
            {
                Log.Info($"GameClientStateChange: {oldState} -> {newState}");
            });
        }

        public async virtual void ActionCallInvitation()
        {
            await Task.Run(() =>
            {
                SendInvitationReply(true);
            });
        }

        public async virtual void ActionCallInitialisation()
        {
            await Task.Run(() =>
            {
                //MatchGame();
            });
        }

        public async virtual void ActionCallPowerChoices()
        {
            await Task.Run(() =>
            {
                SendPowerChoicesChoice(_sabberStoneAI.PowerChoices(GameController.PowerChoices));
            });
        }

        public async virtual void ActionCallPowerOptions()
        {
            await Task.Run(() =>
            {
                SendPowerOptionChoice(_sabberStoneAI.PowerOptions(GameController.PowerOptionList));
            });
        }
    }
}
