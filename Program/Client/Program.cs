using MessagePack;
using MessagePack.Resolvers;

namespace Client
{
    public class Program
    {
        public static void InitMessagePack()
        {
            StaticCompositeResolver.Instance.Register(new IFormatterResolver[]
            {
                MsgDefine.Resolvers.GeneratedResolver.Instance,
                StandardResolver.Instance,
            });
            // Store it for reuse.
            var options = MessagePackSerializerOptions.Standard
                            .WithResolver(StaticCompositeResolver.Instance)
                            .WithCompression(MessagePackCompression.Lz4BlockArray);
            MessagePackSerializer.DefaultOptions = options;
        }

        /*
        static void Main(string[] args)
        {
            InitMessagePack();
            var msg = new LoginReqMsg
            {
                Account = "account",
                Password = "pwd",
                Extra = "去去去",
                Id = 0xff,
            };
            var bytes = MessagePackSerializer.Serialize(msg);

            var obj = MessagePackSerializer.Deserialize<LoginReqMsg>(bytes);

            Console.WriteLine(MessagePackSerializer.SerializeToJson(obj));

        }
        */
    }
}
