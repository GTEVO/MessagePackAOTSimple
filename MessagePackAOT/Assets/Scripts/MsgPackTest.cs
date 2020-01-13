using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using CommonLib;
using Debug = CommonLib.Debug;
using MsgDefine.TestMsg;
using MsgDefine.Resolvers;
using Models;
using ClientLib;

public class MsgPackTest : MonoBehaviour
{

    [SerializeField] Text text;
    [SerializeField] Text text2;
    [SerializeField] Font m_font;
    [SerializeField] Transform _msgsRoot;

    private CancellationTokenSource _cancellationTokenSource;


    private void Awake()
    {
        App.Instacne.Init();
        _cancellationTokenSource = new CancellationTokenSource();
        StaticCompositeResolver.Instance.Register(new IFormatterResolver[]
        {
            GeneratedResolver.Instance,
            MessagePack.Unity.UnityResolver.Instance,
            StandardResolver.Instance,
        });
        // Store it for reuse.
        var options = MessagePackSerializerOptions.Standard
                        .WithResolver(StaticCompositeResolver.Instance)
                        .WithCompression(MessagePackCompression.Lz4Block);
        MessagePackSerializer.DefaultOptions = options;
    }

    private void ShowAsText(string txt)
    {
        var text = new GameObject();
        text.AddComponent<RectTransform>();
        var label = text.AddComponent<Text>();
        label.fontSize = 32;
        label.text = txt;
        label.font = m_font;
        label.color = Color.red;
        text.transform.SetParent(_msgsRoot);
        text.transform.localScale = Vector3.one;
    }

    private void HandleMsg1(TestMsg1 msg)
    {
        Debug.LogFormat("M1 - {0}", msg.Name);
        ShowAsText(string.Format("handle msg => {0} - {1}", typeof(TestMsg1).FullName, msg.Name));
    }

    private void HandleMsg2(TestMsg2 msg)
    {
        Debug.LogFormat("M2 - {0}", msg.Age);
        ShowAsText(string.Format("handle msg => {0} - {1}", typeof(TestMsg2).FullName, msg.Age));
    }

    public void OnClickCancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public void OnClickDeserialize()
    {
        var loginMsg = new LoginReqMsg {
            Account = "account",
            Password = "pwd",
            Extra = "哈哈哈",
            Player = new Player {
                Name = "二逼青年",
                Mails = new Dictionary<int, Mail> { { 1, new Mail { Id = 2, Title = "abc" } } },
                Status = Status.Logining,
            },
        };
        var reqMsgBytes = MessagePackSerializer.Serialize(loginMsg);
        var reqMsgreqMsgObj = MessagePackSerializer.Deserialize<LoginReqMsg>(reqMsgBytes);

        var registerMsg = new RegisterReqMsg {
            Phone = "1350000",
            Authcode = "验证码",
        };

        var fs = new FileStream(Path.Combine(Application.dataPath, "Resources", "Test2.bytes"), FileMode.Open);
        var _as = new byte[fs.Length];
        fs.Read(_as, 0, _as.Length);
        fs.Close();
        fs.Dispose();

        var a = new CancellationTokenSource();
        Task.Factory.StartNew(() => {
            try {
                var registerMsgBytes = MessagePackSerializer.Serialize(registerMsg);
                var deserializeMsg = MessagePackSerializer.Deserialize<RegisterReqMsg>(registerMsgBytes);
                text.text = string.Format("Account : {0}-{1}-{2}", reqMsgreqMsgObj.Account
                    , reqMsgreqMsgObj.Password, reqMsgreqMsgObj.Extra);

                text2.text = string.Format("Account : {0}-{1}", deserializeMsg.Phone, deserializeMsg.Authcode);
            }
            catch {
            }
        }, _cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext()).Wait();
    }

    public void OnClickOnHandleMsg()
    {
        // msg1 先注册处理器
        MessageProcessor.RegisterHandler<TestMsg1>(HandleMsg1);
        var msg = MessageProcessor.PackageMessage(new TestMsg1 {
            Name = "GT"
        });
        MessageProcessor.ProcessMsgPack(msg);
        MessageProcessor.UnRegisterHandler<TestMsg1>(HandleMsg1);

        // msg2 先打包
        msg = MessageProcessor.PackageMessage(new TestMsg2 {
            Age = 26
        });
        MessageProcessor.RegisterHandler<TestMsg2>(HandleMsg2);
        MessageProcessor.ProcessMsgPack(msg);
        MessageProcessor.UnRegisterHandler<TestMsg2>(HandleMsg2);


        var bytes = MessagePackSerializer.Serialize(msg);
        var ip = new IPEndPoint(IPAddress.Loopback, 8000);
        var socket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(ip);
        socket.SendTo(bytes, ip);
        socket.Close();
        socket.Dispose();
    }
}
