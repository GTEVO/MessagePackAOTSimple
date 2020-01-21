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
            TaskScheduler taskScheduler;
            if (SynchronizationContext.Current == null) {
                taskScheduler = TaskScheduler.Current;
            }
            else {
                taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            _messageProcessor.Run(taskScheduler);

            UdpClient = new UdpClient();
            UdpClient.Run(IPAddress.Parse("192.168.0.116"), 8063);
            UdpClient.OnRecvKcpPackage += _messageProcessor.ProcessBytePackage;

            Status = AppStatus.Running;
        }

        public void UnInit()
        {
            UdpClient.Stop();
            _messageProcessor.Stop();
            Status = AppStatus.Stoped;
        }
    }
}
