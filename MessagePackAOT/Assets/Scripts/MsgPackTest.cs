using MessagePack;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using CommonLib;
using Debug = CommonLib.Debug;
using MsgDefine.TestMsg;
using ClientLib;
using System.Net.Http;
using System.Collections.Generic;
using CommonLib.Network;

public class MsgPackTest : MonoBehaviour
{
    [SerializeField] Text rtt;
    [SerializeField] Text text;
    [SerializeField] Text text2;
    [SerializeField] Font m_font;
    [SerializeField] Transform _msgsRoot;

    private CancellationTokenSource _cancellationTokenSource;

    private List<App> apps = new List<App>();


    public async Task FromCurrentSynchronizationContext_Test()
    {
        long count = 0;
        long t = 1L * 1000;

        TaskCompletionSource<bool> taskCompletionSource1 = new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> taskCompletionSource2 = new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> taskCompletionSource3 = new TaskCompletionSource<bool>();

        var sig = 3;

        var t1 = new Task(async () => {

            for (long i = 0; i < t; i++) {
                count += 2;
                await Task.Delay(1);
            }
            taskCompletionSource1.SetResult(true);
            --sig;
        }, CancellationToken.None, TaskCreationOptions.None);
        var t2 = new Task(async () => {

            for (long i = 0; i < t; i++) {
                count += 3;
                await Task.Delay(1);
            }
            taskCompletionSource2.SetResult(true);
            --sig;
        }, CancellationToken.None, TaskCreationOptions.None);
        var t3 = new Task(async () => {

            for (long i = 0; i < t; i++) {
                count += 5;
                await Task.Delay(1);
            }
            taskCompletionSource3.SetResult(true);
            --sig;
        }, CancellationToken.None, TaskCreationOptions.None);

        if (SynchronizationContext.Current == null) {
            var synchronizationContext = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }

        var ts = TaskScheduler.FromCurrentSynchronizationContext();

        t1.Start(ts);
        t2.Start(ts);
        t3.Start(ts);

        await Task.Run(async () => {
            do {
                if (sig == 0)
                    break;
                await Task.Delay(10);
            } while (true);
        });


        //  Task.WaitAll(taskCompletionSource1.Task, taskCompletionSource2.Task, taskCompletionSource3.Task);

        UnityEngine.Debug.Log("result =" + (count == t * (2 + 3 + 5)));
    }

    private async Task Awake()
    {
        await FromCurrentSynchronizationContext_Test();
        App.Instacne.Init();
        StartCoroutine(Ping());
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void OnClickCreateApps()
    {
        for (int i = 0; i < 100; i++) {
            var app = new App();
            apps.Add(app);
            app.Init();
            SendMsg(app);
        }
    }

    public void OnClickCleanApps()
    {
        foreach (var item in apps) {
            item.UnInit();
        }
        apps.Clear();
    }

    private void OnDestroy()
    {
        ClickCancelSend();
        OnClickCleanApps();
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

    private void HandleMsg1(TestMsg1 msg, IReliableDataLink fromLink)
    {
        var type = msg.GetType();
        Debug.LogFormat("M1 - {0}", msg.Name);
        ShowAsText(string.Format("handle msg => {0} - {1}", type.FullName, msg.Name));
        MessageProcessor.UnRegisterHandler<TestMsg1>(HandleMsg1);
    }

    private void HandleMsg2(TestMsg2 msg, IReliableDataLink fromLink)
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
        var loginMsg = new LoginReqMsg {
            Account = "account",
            Password = "pwd",
            Extra = "哈哈哈",
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
        Debug.LogFormat("unity main {0}", Thread.CurrentThread.ManagedThreadId);


        var mainThreadTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        Task.Run(() => {
            Debug.LogFormat("Main Task Thread [{0}]", Thread.CurrentThread.ManagedThreadId);
            var msgpackTask = new Task(() => {
                Debug.LogFormat("MsgpackTask Task Thread [{0}]", Thread.CurrentThread.ManagedThreadId);
                var registerMsgBytes = MessagePackSerializer.Serialize(registerMsg);
                var deserializeMsg = MessagePackSerializer.Deserialize<RegisterReqMsg>(registerMsgBytes);
                text.text = string.Format("Account : {0}-{1}-{2}", reqMsgreqMsgObj.Account
                    , reqMsgreqMsgObj.Password, reqMsgreqMsgObj.Extra);
                text2.text = string.Format("Account : {0}-{1}", deserializeMsg.Phone, deserializeMsg.Authcode);
            });
            msgpackTask.Start(mainThreadTaskScheduler);

            var webTask = new Task(async () => {
                Debug.LogFormat("before await Thread [{0}]", Thread.CurrentThread.ManagedThreadId);
                await Wait();
                Debug.LogFormat("after await Thread [{0}]", Thread.CurrentThread.ManagedThreadId);
            });
            webTask.Start(mainThreadTaskScheduler);

        }, _cancellationTokenSource.Token);
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

    public void OnClickSendLogin()
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
        SendMsg(App.Instacne);
    }

    public void ClickCancelSend()
    {
        App.Instacne.UnInit();
    }

    private IEnumerator Ping()
    {
        var wait = new WaitForSecondsRealtime(1.2f);
        do {
            rtt.text = App.Instacne.UdpClient.Rtt.ToString();
            yield return wait;
        } while (App.Instacne.Status == App.AppStatus.Running);
    }

    private void SendMsg(App app)
    {
        Task.Run(async () => {
            await Task.Delay(1000);
            do {
                var loginReq = new LoginReqMsg {
                    Account = app.UdpClient.ConectionId.ToString(),
                    Password = "PWD",
                    Extra = System.DateTime.Now.ToFileTimeUtc().ToString()
                };
                app.UdpClient.SendMessage(loginReq);
                await Task.Delay(16);
            } while (app.Status == App.AppStatus.Running);
        });
    }
}
