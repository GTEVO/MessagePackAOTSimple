using System;
using MsgDefine;
using System.Threading.Tasks;
using CommonLib;
using CommonLib.Network;
using Server.Serializer;
using MsgDefine.TestMsg;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            LoginReqMsg msg = new LoginReqMsg {
                Account = "account",
                Password = "pwd",
                Extra = "去去去",
            };

            //  var bytes = Serialize(msg);
            var bytes = MessagePackSerializer.Serialize(msg);
            var obj = MessagePackSerializer.Deserialize<LoginReqMsg>(bytes);
            Console.WriteLine(MessagePackSerializer.ConvertToJson(bytes));
            */

            Debug.DefaultDebugger = new ServerConsoleDebugger();
            MessageProcessor.DefaultSerializer = new MsgPackBitSerializer();

            var udpServer = new UdpServer();
            udpServer.Start();

            MessageProcessor.RegisterHandler<LoginReqMsg>((msg, remote) => {
                Debug.LogFormat("LoginReqMsg - {0}|{1}|{2}", msg.Account, msg.Password, msg.Extra);
                var loginReq = new LoginRspMsg {
                    Player = new Model.Player {
                        Name = "二逼青年",
                        Status = Model.Status.Online,
                        Mails = new System.Collections.Generic.Dictionary<int, Model.Mail> {
                            {
                                234,
                                new Model.Mail {
                                    Id = 234,
                                    Title = "测试",
                                    Content ="test"
                                }
                            }
                        },
                    }
                };
                var loginReqBytes = MessageProcessor.DefaultSerializer.Serialize(
                     MessageProcessor.PackageMessage(loginReq));
                udpServer.Send(loginReqBytes, remote);
            });

            Task.Run(async () => {
                do {
                    var pack = await udpServer.RecvAsync();
                    MessagePackage messagePackage = null;
                    try {
                        var bytes = pack.bytes.Memory.Span.Slice(0, pack.size).ToArray();
                        ReadOnlyMemory<byte> readOnlyMemory = new ReadOnlyMemory<byte>(bytes);
                        messagePackage = MessageProcessor.DefaultSerializer.Deserialize<MessagePackage>(readOnlyMemory);
                        MessageProcessor.ProcessMsgPack(messagePackage, pack.remote);
                    }
                    catch (Exception e) {
                        Debug.LogErrorFormat("{0}", e);
                    }
                    finally {
                        NetworkPack.NetworkPackPool.Return(pack);
                    }
                } while (true);
            });

            Console.ReadLine();
        }

    }
}
