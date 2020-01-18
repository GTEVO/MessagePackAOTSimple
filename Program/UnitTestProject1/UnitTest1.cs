using ClientLib;
using System.Threading.Tasks;
using MsgDefine.TestMsg;
using CommonLib;
using Xunit;
using System.Threading;
using CommonLib.Network;
using System.Net;
using System.Buffers;

namespace UnitTestProject1
{

    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            for (int i = 0; i < 1000; i++) {
                ClientAppTest();
            }
            await Task.Delay(1000 * 60 * 5);
        }

        private void ClientAppTest()
        {
            Debug.DefaultDebugger = new Server.ServerConsoleDebugger();
            MessageProcessor.DefaultSerializer = new Server.Serializer.MsgPackBitSerializer();

            var messageProcessor = new MessageProcessor();
            if (SynchronizationContext.Current == null) {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext()); 
            }
            messageProcessor.Run(TaskScheduler.FromCurrentSynchronizationContext());

            UdpClient udpClient = new UdpClient();
            udpClient.Run(IPAddress.Parse("192.168.31.10"), 8063);
            udpClient.OnRecvKcpPackage += messageProcessor.ProcessBytePackage;

            Task.Run(async () => {
                int count = 0;
                await Task.Delay(1000);
                Assert.False(udpClient.ConectionId == 0);
                var conv = udpClient.ConectionId.ToString();
                while (count++ < 10000) {
                    await Task.Delay(33);
                    var msg = new LoginReqMsg {
                        Account = conv,
                        Password = "B",
                        Extra = count.ToString(),
                        SeqNumber = count,
                    };
                    udpClient.SendMessage(msg);
                    udpClient.SendMessage(msg);
                    udpClient.SendMessage(msg);
                }
                await Task.Delay(600 * 1000);
                udpClient.Stop();
                messageProcessor.Stop();
            });
        }
    }
}
