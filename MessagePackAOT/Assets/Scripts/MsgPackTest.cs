using MessagePack;
using MessagePack.Resolvers;
using MsgDefine;
using System.Collections;
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
using ClientLib;
using System.Net.Http;

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
    }

    private void OnDestroy()
    {
        App.Instacne.UnInit();
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
        var type = msg.GetType();
        Debug.LogFormat("M1 - {0}", msg.Name);
        ShowAsText(string.Format("handle msg => {0} - {1}", type.FullName, msg.Name));
        MessageProcessor.UnRegisterHandler<TestMsg1>(HandleMsg1);
    }

    private void HandleMsg2(TestMsg2 msg)
    {
        var type = msg.GetType();
        Debug.LogFormat("M2 - {0}", msg.Age);
        ShowAsText(string.Format("handle msg => {0} - {1}", type.FullName, msg.Age));
        MessageProcessor.UnRegisterHandler<TestMsg2>(HandleMsg2);
    }

    private void HandleMsg3(LoginRspMsg msg)
    {
        var type = msg.GetType();
        Debug.LogFormat("M3 - {0}", msg.Player);
        ShowAsText(string.Format("handle msg => {0} - {1}|{2}", type.FullName, msg.Player.Name, msg.Player.Status));
    }

    public void OnClickCancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public void OnClickDeserialize()
    {
        var loginMsg = new LoginReqMsg
        {
            Account = "account",
            Password = "pwd",
            Extra = "哈哈哈",
        };
        var reqMsgBytes = MessagePackSerializer.Serialize(loginMsg);
        var reqMsgreqMsgObj = MessagePackSerializer.Deserialize<LoginReqMsg>(reqMsgBytes);

        var registerMsg = new RegisterReqMsg
        {
            Phone = "1350000",
            Authcode = "验证码",
        };

        var fs = new FileStream(Path.Combine(Application.dataPath, "Resources", "Test2.bytes"), FileMode.Open);
        var _as = new byte[fs.Length];
        fs.Read(_as, 0, _as.Length);
        fs.Close();
        fs.Dispose();

        var a = new CancellationTokenSource();
        Debug.LogFormat("unity main {0}", Thread.CurrentThread.ManagedThreadId);


        var mainThreadTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        Task.Factory.StartNew(async () =>
         {
             try
             {
                 Debug.LogFormat("before await {0}", Thread.CurrentThread.ManagedThreadId);
                 await Wait();
                 Debug.LogFormat("after await {0}", Thread.CurrentThread.ManagedThreadId);
                 var registerMsgBytes = MessagePackSerializer.Serialize(registerMsg);
                 var deserializeMsg = MessagePackSerializer.Deserialize<RegisterReqMsg>(registerMsgBytes);
                 text.text = string.Format("Account : {0}-{1}-{2}", reqMsgreqMsgObj.Account
                     , reqMsgreqMsgObj.Password, reqMsgreqMsgObj.Extra);
                 text2.text = string.Format("Account : {0}-{1}", deserializeMsg.Phone, deserializeMsg.Authcode);
             }
             catch (System.Exception e)
             {
                 UnityEngine.Debug.LogError(e);
             }
             return true;
         }, _cancellationTokenSource.Token, TaskCreationOptions.None
           , mainThreadTaskScheduler);
    }

    private async Task RequstHttp()
    {
        Debug.LogFormat("work task start {0}", Thread.CurrentThread.ManagedThreadId);
        await Task.Delay(1000);
        HttpClient http = new HttpClient();
        var txt = await http.GetStringAsync("http://www.baidu.com");
        UnityEngine.Debug.Log(txt);
        Debug.LogFormat("work task stop {0}", Thread.CurrentThread.ManagedThreadId);
    }

    private async Task Wait()
    {
        Debug.LogFormat("async task A {0}", Thread.CurrentThread.ManagedThreadId);
        /// await RequstHttp();
        await Task.Run(RequstHttp);
        Debug.LogFormat("async task B {0}", Thread.CurrentThread.ManagedThreadId);
        await Task.Delay(1 * 1000);
        Debug.LogFormat("async task C {0}", Thread.CurrentThread.ManagedThreadId);
        await Task.Delay(1 * 1000);
        Debug.LogFormat("async task D {0}", Thread.CurrentThread.ManagedThreadId);

        //  task 调用方式
        //  测试表明：
        //  1、对于使用new创建的task，await关键字不能让task被调度，需要显式调用Start()，调度器默认使用 TaskScheduler.Current
        //  2、Task.RunTask、Task.Factory.StartNew创建的task，调度器默认使用 TaskScheduler.Default
        //  3、默认情况下，TaskScheduler.Current = TaskScheduler.Default
        //  4、异步方法的调用，同new创建的task一样，并且也遵循 2、3
        //  5、异步task中使用 await 一个 task之后，当前调度会变成这个task的调度器
    }

    public void OnClickOnHandleMsg()
    {
        /*
        // msg1 先注册处理器
        MessageProcessor.RegisterHandler<TestMsg1>(HandleMsg1);
        var msg = MessageProcessor.PackageMessage(new TestMsg1 {
            Name = "GT"
        });
        MessageProcessor.ProcessBytePackageAsync(msg);

        // msg2 先打包
        msg = MessageProcessor.PackageMessage(new TestMsg2 {
            Age = 26
        });
        MessageProcessor.RegisterHandler<TestMsg2>(HandleMsg2);
        MessageProcessor.ProcessBytePackageAsync(msg);
        */

        //
        sending = true;
        StartCoroutine(SendMsg());

    }

    public void ClickCancelSend()
    {
        sending = false;
    }

    bool sending = true;

    private IEnumerator SendMsg()
    {
        var loginReq = new LoginReqMsg
        {
            Account = "A",
            Password = "PWD",
            Extra = "额外"
        };
        do
        {
            App.Instacne.UdpClient.SendMessage(loginReq);
            yield return null;
        } while (sending);
    }
}
