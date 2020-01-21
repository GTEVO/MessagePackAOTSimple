using System.Threading.Tasks;
using MsgDefine.TestMsg;
using CommonLib;
using Xunit;
using System.Threading;
using CommonLib.Network;
using System.Net;
using System.Buffers;
using System.IO;
using System;
using System.Buffers.Binary;


namespace UnitTestProject1
{

    public class UnitTest1
    {
        [Fact]
        public async Task MultiClients()
        {
            for (int i = 0; i < 200; i++) {
                ClientAppTest();
            }
            await Task.Delay(1000 * 60 * 5);
        }

        [Fact]
        public void MemorySteamWrite()
        {
            var bytes = new byte[8];
            Span<byte> span = new Span<byte>(bytes);
            BinaryPrimitives.WriteInt32LittleEndian(span, 1);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), 255);

            MemoryStream stearm = new MemoryStream(ushort.MaxValue);
            stearm.Write(span);

            var memory = MemoryPool<byte>.Shared.Rent((int)stearm.Length);

            stearm.Seek(0, SeekOrigin.Begin);
            if (stearm.TryGetBuffer(out var writer)) {
                writer.AsMemory().CopyTo(memory.Memory);
            }

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
            udpClient.Run(IPAddress.Parse("192.168.0.128"), 8063);
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
