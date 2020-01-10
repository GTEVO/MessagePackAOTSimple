using System;
using MsgDefine;
using MessagePack;
using System.Linq;
using Server;
using System.Threading.Tasks;

namespace Console1
{
    class Program
    {
        static void Main(string[] args)
        {
            LoginReqMsg msg = new LoginReqMsg {
                Account = "account",
                Password = "pwd",
                Extra = "去去去",
                Id = 0xff,
            };

            //  var bytes = Serialize(msg);

            var bytes = MessagePackSerializer.Serialize(msg);

            var obj = MessagePackSerializer.Deserialize<LoginReqMsg>(bytes);

            Console.WriteLine(MessagePackSerializer.ConvertToJson(bytes));

            new UdpServer().Start();

            Console.ReadLine();
        }


        static byte[] Serialize<T>(T msg) where T : INetworkMsg
        {
            var t = typeof(T);
            return MessagePackSerializer.Serialize(msg);
        }
    }
}
