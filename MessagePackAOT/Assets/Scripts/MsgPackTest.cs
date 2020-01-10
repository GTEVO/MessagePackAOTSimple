using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Client;
using Common;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Msgs;

public class MsgPackTest : MonoBehaviour
{

    [SerializeField] Text text;
    [SerializeField] Text text2;

    private void Awake()
    {
        Program.InitMessagePack();
        StaticCompositeResolver.Instance.Register(new IFormatterResolver[] {
           MsgDefine.Resolvers.GeneratedResolver.Instance,
           GeneratedResolver.Instance,
           StandardResolver.Instance,
        });
    }

    private void HandleMsg1(TestMsg1 msg)
    {
        Debug.LogFormat("M1 - {0}", msg.Name);
    }

    private void HandleMsg2(TestMsg2 msg)
    {
        Debug.LogFormat("M2 - {0}", msg.Age);
    }

    public void OnClick()
    {
        var loginMsg = new LoginReqMsg {
            Account = "account",
            Password = "pwd",
            Extra = "哈哈哈",
            Id = 0xf0,
            Player = new Player {
                Name = "二逼青年",
                Hello = new Dictionary<int, Mail> { { 1, new Mail { Id = 2, Title = "abc" } } },
                Items = new List<Mail> { new Mail { Id = 66, Title = "123" } }
            },
            Status = Status.Logining
        };
        var reqMsgBytes = MessagePackSerializer.Serialize(loginMsg);
        var reqMsgreqMsgObj = MessagePackSerializer.Deserialize<LoginReqMsg>(reqMsgBytes);

        var registerMsg = new RegisterReqMsg {
            Id = 0x10,
            Phone = 1350000,
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
                text.text = string.Format("{0}:{1}-{2}-{3}", reqMsgreqMsgObj.Id, reqMsgreqMsgObj.Account
                    , reqMsgreqMsgObj.Password, reqMsgreqMsgObj.Extra);

                text2.text = string.Format("{0}:{1}-{2}", deserializeMsg.Id, deserializeMsg.Phone, deserializeMsg.Authcode);
            }
            catch {
            }
        }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext()).Wait();
    }

    public void OnClickOnMsg()
    {
        //
        MessageProcessor.RegisterHandler<TestMsg1>(HandleMsg1);
        var msg = MessageProcessor.PackageMessage(new TestMsg1 {
            Name = "GT"
        });
        MessageProcessor.ProcessMsgPack(msg);
        MessageProcessor.UnRegisterHandler<TestMsg1>(HandleMsg1);
        //
        msg = MessageProcessor.PackageMessage(new TestMsg2 {
            Age = 26
        });
        MessageProcessor.RegisterHandler<TestMsg2>(HandleMsg2);
        MessageProcessor.ProcessMsgPack(msg);
        MessageProcessor.UnRegisterHandler<TestMsg2>(HandleMsg2);
    }
}
