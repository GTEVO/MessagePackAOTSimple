using MessageDefine;
using MessagePack;


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
}
