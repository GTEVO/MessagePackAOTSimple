using System;
using System.Threading.Tasks;
using CommonLib;
using CommonLib.Network;
using Server.Serializer;
using MsgDefine.TestMsg;
using System.Buffers.Binary;
using System.Threading;
using System.Net;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.DefaultDebugger = new ServerConsoleDebugger();
            MessageProcessor.DefaultSerializer = new MsgPackBitSerializer();
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

            MessageProcessor messageProcessor = new MessageProcessor();
            messageProcessor.Run(TaskScheduler.Default);
            MessageProcessor.RegisterHandler<LoginReqMsg>((msg, fromLink) => {
                var rsp = new LoginRspMsg {
                    Player = new Model.Player {
                        Status = Model.Status.Online,
                        Name = "二逼青年"
                    },
                    SeqNumber = msg.SeqNumber,
                };
                var bytes = MessageProcessor.PackageMessage(rsp);
                fromLink.SendToRemoteAsync(bytes.buffer, 0, bytes.len);
                //  Debug.LogFormat("{0}-{1}-{2}", msg.Account, msg.Password, msg.Extra);
            });

            var udpServer = new UdpServer(messageProcessor);
            udpServer.Start(IPAddress.Any, 8063);

            do {
                var input = Console.ReadLine();
            } while (true);

            /*
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
            */
        }

    }
}
