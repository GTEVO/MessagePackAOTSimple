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
        public enum AppStatus
        {
            WaitForInit,
            Running,
            Stoped,
        }

        public UdpClient UdpClient { get; private set; }
        private MessageProcessor _messageProcessor;

        public AppStatus Status { get; private set; } = AppStatus.WaitForInit;

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
            UdpClient.Run(IPAddress.Loopback, 8063);
            UdpClient.OnRecvKcpPackage += NetworkLink_OnRecvKcpPackage;

            Status = AppStatus.Running;
        }

        private void NetworkLink_OnRecvKcpPackage(IMemoryOwner<byte> memoryOwner, int len, uint conv)
        {
            _messageProcessor.ProcessBytePackageAsync(memoryOwner, len);
        }

        public void UnInit()
        {
            UdpClient.Stop();
            _messageProcessor.Stop();
            Status = AppStatus.Stoped;
        }
    }
}
