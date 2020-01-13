using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public enum Status
    {
        Offline,
        Logining,
        Logouting,
        Online,
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
