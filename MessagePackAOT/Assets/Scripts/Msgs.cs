using MessageDefine;
using MessagePack;


namespace Msgs
{

    [MessagePackObject]
    [MsgId(1)]
    public class M1
    {
        [Key(0)]
        public string Name { get; set; }
    }

    [MessagePackObject]
    [MsgId(2)]
    public class M2
    {
        [Key(0)]
        public int Age { get; set; }
    }

    [MessagePackObject]
    public class M3
    {
        [Key(0)]
        public bool Sex { get; set; }
    }
}
