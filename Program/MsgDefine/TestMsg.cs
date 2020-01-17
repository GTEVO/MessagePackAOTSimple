using MessagePack;
using Model;


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
    public class TestMsg3
    {
        [Key(0)]
        public string Address { get; set; }
    }

    [MessagePackObject]
    [MessageId(4)]
    public class LoginReqMsg
    {
        [Key(0)]
        public string Account { get; set; }

        [Key(1)]
        public string Password { get; set; }

        [Key(2)]
        public string Extra { get; set; }

        [Key(3)]
        public int SeqNumber { get; set; }

    }

    [MessagePackObject]
    [MessageId(5)]
    public class LoginRspMsg
    {
        [Key(0)]
        public Player Player { get; set; }

        [Key(1)]
        public int SeqNumber { get; set; }
    }

    [MessagePackObject]
    [MessageId(6)]
    public class RegisterReqMsg
    {
        [Key(0)]
        public string Phone { get; set; }

        [Key(1)]
        public string Authcode { get; set; }
    }
}
