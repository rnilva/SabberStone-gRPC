﻿using Grpc.Core;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabberStoneContract.Model
{
    public enum UserState
    {
        None,
        Queued,
        Invited,
        Prepared,
        InGame
    }

    public enum PlayerState
    {
        None,
        Invitation,
        Game,
        Quit
    }

    public struct MessageLog
    {
        public MsgType Type;
        public bool State;
        public GameDataType GameDataType;
        public string GameDataObject;

        public override string ToString()
        {
            return $"[{Type}][{GameDataType}]";
        }
    }

    public class UserDataInfo : UserInfo
    {
        public virtual string Token { get; set; }
        public virtual string Peer { get; set; }
        public virtual IServerStreamWriter<GameServerStream> ResponseStream { get; set; }

        public Stack<MessageLog> Logs = new Stack<MessageLog>();
    }

    public class UserInfo
    {
        public virtual int SessionId { get; set; }
        public virtual string AccountName { get; set; }
        public virtual UserState UserState { get; set; }
        public virtual int GameId { get; set; }
        public virtual DeckType DeckType { get; set; }
        public virtual string DeckData { get; set; }
        public virtual PlayerState PlayerState { get; set; }
        public virtual int PlayerId { get; set; }
        public UserInfo OpenUserInfo()
        {
            return new UserInfo()
            {
                SessionId = 0,
                AccountName = AccountName,
                UserState = UserState.None,
                GameId = GameId,
                DeckType = DeckType.None,
                DeckData = null,
                PlayerState = PlayerState.None,
                PlayerId = PlayerId
            };
        }
    }

    public enum GameDataType
    {
        None,
        PowerHistory,
        PowerOptions,
        PowerChoices,
        Concede,
        Result,
        Initialisation
    }

    public class GameData
    {
        public virtual int GameId { get; set; }
        public virtual int PlayerId { get; set; }
        public virtual GameDataType GameDataType { get; set; }
        public virtual string GameDataObject { get; set; }
    }

    public class PowerChoices
    {
        public virtual ChoiceType ChoiceType { get; set; }
        public virtual List<int> Entities { get; set; }
    }

    public class PowerOptions
    {
        public virtual int Index { get; set; }
        public virtual List<PowerOption> PowerOptionList { get; set; }
    }

    public class PowerOptionChoice
    {
        public virtual PowerOption PowerOption { get; set; }
        public virtual int Target { get; set; }
        public virtual int Position { get; set; }
        public virtual int SubOption { get; set; }
    }
}
