using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Client;
using Common;
using System.Threading;
using System.Threading.Tasks;
using System;
using UnityEngine.Jobs;
using System.IO;
using MessageDefine;
using Msgs;

public class NewBehaviourScript : MonoBehaviour
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
        MessageHandlerManager.RegisterHandler<M1>(HandleMsg1);
        MessageHandlerManager.RegisterHandler<M2>(HandleMsg2);
        MessageHandlerManager.RegisterHandler<M3>(HandleMsg3);
    }

    private void HandleMsg1(M1 msg)
    {
        Debug.LogFormat("M1 - {0}", msg.Name);
    }
    private void HandleMsg2(M2 msg)
    {
        Debug.LogFormat("M2 - {0}", msg.Age);
    }
    private void HandleMsg3(M3 msg)
    {
        Debug.LogFormat("M3 - {0}", msg.Sex ? "Man" : "Woman");
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
        MessageHandlerManager.DoHandleMsg(new MessagePackage {
            Id = 1,
            Data = MessagePackSerializer.Serialize(
                new M1 {
                    Name = "GT"
                }),
        });
        MessageHandlerManager.DoHandleMsg(new MessagePackage {
            Id = 2,
            Data = MessagePackSerializer.Serialize(
                new M2 {
                    Age = 26
                }),
        });
        MessageHandlerManager.DoHandleMsg(new MessagePackage {
            Id = 3,
            Data = MessagePackSerializer.Serialize(
             new M3 {
                 Sex = true
             }),
        });

        MessageHandlerManager.UnRegisterHandler<M1>(HandleMsg1);
        // MessageHandlerManager.UnRegisterHandler<M2>(HandleMsg2);
        MessageHandlerManager.UnRegisterHandler<M3>(HandleMsg3);
    }
}
