using MessagePack;
using Models;
using System.Collections.Generic;

public enum Status
{
    Offline,
    Logining,
    Logouting,
    Outline,
}

namespace Models
{
    [MessagePackObject]
    public class Mail
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Title { get; set; }

        [Key(2)]
        public string Content { get; set; }
    }

    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public Dictionary<int, Mail> Mails { get; set; }

        [Key(2)]
        public Status Status { get; set; }
    }
}

namespace MsgDefine.TestMsg
{
    [MessagePackObject]
    [MessageId(1)]
    public class TestMsg1
    {
        [Key(0)]
        public string Name { get; set; }
    }

    [MessagePackObject]
    [MessageId(2)]
    public class TestMsg2
    {
        [Key(0)]
        public int Age { get; set; }
    }

    [MessagePackObject]
    [MessageId(3)]
    public class LoginReqMsg
    {
        [Key(0)]
        public string Account { get; set; }

        [Key(1)]
        public string Password { get; set; }

        [Key(2)]
        public string Extra { get; set; }

        [Key(3)]
        public Player Player { get; set; }

    }

    [MessagePackObject]
    [MessageId(4)]
    public class RegisterReqMsg
    {
        [Key(0)]
        public string Phone { get; set; }

        [Key(1)]
        public string Authcode { get; set; }
    }
}
