using CommonLib;
using ClientLib.Serializer;
using MsgDefine.TestMsg;
using System;
using CommonLib.Network;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace ClientLib
{
    public class App : Singleton<App>
    {
        public UdpClient UdpClient { get; private set; }
        MessageProcessor _messageProcessor;


        public void Init()
        {
            Debug.DefaultDebugger = new UnityConsoleDebugger();
            MessageProcessor.DefaultSerializer = new MsgPackBitSerializer();

            _messageProcessor = new MessageProcessor();
            if (SynchronizationContext.Current == null) {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            }
            _messageProcessor.Run(TaskScheduler.FromCurrentSynchronizationContext());

            UdpClient = new UdpClient();
            UdpClient.Run();
            UdpClient.OnRecvKcpPackage += NetworkLink_OnRecvKcpPackage;
        }

        private void NetworkLink_OnRecvKcpPackage(IMemoryOwner<byte> memoryOwner, int len, uint conv)
        {
            MessageProcessor.ProcessBytePackageAsync(memoryOwner, len);
        }

        public void UnInit()
        {
            UdpClient.Stop();
            _messageProcessor.Stop();
        }
    }
}
