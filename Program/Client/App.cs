using CommonLib;
using ClientLib.Serializer;
using MsgDefine.TestMsg;
using System;
using CommonLib.Network;
using System.Threading;
using System.Threading.Tasks;
using MsgDefine;
using MessagePack;
using System.Net;

namespace ClientLib
{
    public class App : Singleton<App>
    {
        public event Action<LoginRspMsg> TestHandler;
        public UdpClient UDPClient { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

        private void OnLoginRspMsg(LoginRspMsg msg, EndPoint remote)
        {
            TestHandler?.Invoke(msg);
        }

        public void Init()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Debug.DefaultDebugger = new UnityConsoleDebugger();
            MessageProcessor.DefaultSerializer = new MsgPackBitSerializer();
            UDPClient = new UdpClient();
            UDPClient.Start();
            Task.Factory.StartNew(async () => {
                do {
                    var pack = await UDPClient.RecvAsync();
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
            }, _cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            //  
            MessageProcessor.RegisterHandler<LoginRspMsg>(OnLoginRspMsg);
        }

        public void UnInit()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
