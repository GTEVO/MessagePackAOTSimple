using MessageDefine;
using MessagePack;
using UnityEngine;

namespace Msgs
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
    public class Position
    {
        [Key(0)]
        public Vector3 Last { get; set; }

        [Key(1)]
        public Vector3 Current { get; set; }
    }
}
