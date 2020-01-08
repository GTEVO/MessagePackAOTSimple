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

public class NewBehaviourScript : MonoBehaviour
{

    [SerializeField] Text text;
    [SerializeField] Text text2;

    private void Awake()
    {
        Program.InitMessagePack();
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
            catch (Exception e) {
            }
        }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext()).Wait();
    }
}
