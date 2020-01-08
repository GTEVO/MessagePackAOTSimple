using System;
using MessagePack;
using System.Collections.Generic;


namespace MsgDefine
{

    public enum Status
    {
        Online,
        Offline,
        Logining,
        Logout
    }

    [MessagePackObject]
    public class Mail
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Title { get; set; }
    }


    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public Dictionary<int, Mail> Hello { get; set; }

        [Key(2)]
        public List<Mail> Items { get; set; }
    }


    [MessagePackObject]
    public class LoginReqMsg : BaseNetworkMsg
    {
        [Key(1)]
        public string Account { get; set; }

        [Key(2)]
        public string Password { get; set; }

        [Key(3)]
        public string Extra { get; set; }


        [Key(4)]
        public Player Player { get; set; }


        [Key(5)]
        public Status Status { get; set; }

        private string Pl { get; set; }
    }

}
