using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit.Abstractions;

namespace UnitTestProject1
{

    public class TaskAssignedId : Task
    {
        public TaskAssignedId(Action action, int workerIndex)
          : base(action)
        {
            WorkerIndex = workerIndex;
        }

        public TaskAssignedId(Action action, CancellationToken token, int workerIndex)
       : base(action, token)
        {
            WorkerIndex = workerIndex;
        }

        public TaskAssignedId(Action action, TaskCreationOptions creationOptions, int workerIndex)
       : base(action, creationOptions)
        {
            WorkerIndex = workerIndex;
        }

        public TaskAssignedId(Action action, CancellationToken token, TaskCreationOptions creationOptions, int workerIndex)
            : base(action, token, creationOptions)
        {
            WorkerIndex = workerIndex;
        }

        public int WorkerIndex { get; private set; }
    }

    public class TestTaskScheduler : TaskScheduler
    {
        [ThreadStatic]
        private static WorkerThread CurrentWorkerThread;

        private readonly static int WokerCount = Environment.ProcessorCount * 2;
        private readonly static Random random = new Random();

        private readonly WorkerThread[] wokers;
        private readonly WorkerThread switchWorker;

        internal class WorkerThread
        {
            public readonly int WorkerId;
            public readonly int ThreadId;
            private readonly (AutoResetEvent _event, ConcurrentQueue<Task> _tasks) taskQueue;
            private readonly Thread thread;

            public WorkerThread(TestTaskScheduler scheduler, int workerId, bool attatchThread)
            {
                WorkerId = workerId;

                taskQueue = (new AutoResetEvent(false), new ConcurrentQueue<Task>());
                thread = new Thread(taskContext => {
                    TestTaskScheduler.CurrentWorkerThread = attatchThread ? this : null;
                    (AutoResetEvent _event, ConcurrentQueue<Task> _tasks) = ((AutoResetEvent, ConcurrentQueue<Task>))taskContext;
                    while (true) {
                        if (_tasks.TryDequeue(out var task)) {
                            scheduler.TryExecuteTask(task);
                        }
                        else {
                            _event.WaitOne();
                        }
                    }
                }) {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal,
                };

                ThreadId = thread.ManagedThreadId;
                thread.Start(taskQueue);
            }

            public void Enqueue(Task task)
            {
                taskQueue._tasks.Enqueue(task);
                taskQueue._event.Set();
            }
        }

        private TestTaskScheduler()
        {
            wokers = new WorkerThread[WokerCount];
            for (int i = 0; i < WokerCount; i++) {
                wokers[i] = new WorkerThread(this, i, true);
            }
            switchWorker = new WorkerThread(this, -1, false);
        }

        public static new TestTaskScheduler FromCurrentSynchronizationContext()
        {
            return new TestTaskScheduler();
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        protected override void QueueTask(Task task)
        {
            /*
            if (CurrentWorkerThread != null) {
                CurrentWorkerThread.Enqueue(task);
            }
            else {*/
            WorkerThread woker;
            if (task is TaskAssignedId assignedId) {
                woker = wokers[assignedId.WorkerIndex];
            }
            else {
                woker = wokers[task.Id % WokerCount];
            }
            woker.Enqueue(task);
            //}
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            //             if (!taskWasPreviouslyQueued)
            //                 return TryExecuteTask(task);
            return false;
        }

        public static int GetWorkerIndex()
        {
            return random.Next(0, WokerCount);
        }
    }

    public class SynchronizationContextTaskSchedulerTest
    {

        private readonly ITestOutputHelper _testOutputHelper;

        public SynchronizationContextTaskSchedulerTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }


        [Fact(DisplayName = "同步上下文 Send")]
        public void TestAsync()
        {
            long count = 0;
            long t = 1L * 1000;

            var tsk = new Task(() => {

                int threadId = Thread.CurrentThread.ManagedThreadId;

                if (SynchronizationContext.Current == null) {
                    var synchronizationContext = new SynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                }

                ////////////////////////////////////////////////////////////////////////////////
                TaskCompletionSource<bool> taskCompletionSource1 = new TaskCompletionSource<bool>();
                SynchronizationContext.Current.Send(async (state) => {

                    for (long i = 0; i < t; i++) {
                        int threadId1 = Thread.CurrentThread.ManagedThreadId;
                        count += 2;
                        await Task.Delay(0);
                    }

                    ((TaskCompletionSource<bool>)state).SetResult(true);

                    Assert.True(count == t * (2), count.ToString());


                }, taskCompletionSource1);

                ////////////////////////////////////////////////////////////////////////////////
                TaskCompletionSource<bool> taskCompletionSource2 = new TaskCompletionSource<bool>();
                SynchronizationContext.Current.Send(async (state) => {

                    for (long i = 0; i < t; i++) {
                        int threadId2 = Thread.CurrentThread.ManagedThreadId;
                        count += 3;
                        await Task.Delay(1);
                    }

                    ((TaskCompletionSource<bool>)state).SetResult(true);

                    Assert.False(count == t * (2 + 3), count.ToString());

                }, taskCompletionSource2);

                ////////////////////////////////////////////////////////////////////////////////

                TaskCompletionSource<bool> taskCompletionSource3 = new TaskCompletionSource<bool>();
                SynchronizationContext.Current.Send(async (state) => {

                    for (long i = 0; i < t; i++) {
                        int threadId3 = Thread.CurrentThread.ManagedThreadId;
                        count += 5;
                        await Task.Delay(1);
                    }

                     ((TaskCompletionSource<bool>)state).SetResult(true);

                }, taskCompletionSource3);

                ////////////////////////////////////////////////////////////////////////////////

                Assert.False(count == t * (2 + 3 + 5), count.ToString());

                Task.WaitAll(taskCompletionSource1.Task, taskCompletionSource2.Task, taskCompletionSource3.Task);

                Assert.True(count == t * (2 + 3 + 5), count.ToString());

            }, CancellationToken.None, TaskCreationOptions.None);

            tsk.Start();
            tsk.Wait();
        }



        [Fact(DisplayName = "同步上下文 任务调度器")]
        public void FromCurrentSynchronizationContext_Test()
        {
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;

            var sctx = SynchronizationContext.Current;

            long count = 0;
            long t = 1L * 1000 * 2;

            TaskCompletionSource<bool> taskCompletionSource1 = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> taskCompletionSource2 = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> taskCompletionSource3 = new TaskCompletionSource<bool>();

            //Task.Run(() => {


            var ts = TestTaskScheduler.FromCurrentSynchronizationContext();

            for (int i = 0; i < 1; i++) {

                var id = TestTaskScheduler.GetWorkerIndex();

                var task = new TaskAssignedId(() => {

                    var workerId = Thread.CurrentThread.ManagedThreadId;

                    var tokenSrc = new CancellationTokenSource();

                    var t1 = new TaskAssignedId(async () => {

                        Assert.True(workerId == Thread.CurrentThread.ManagedThreadId);

                        //                        var innerTask = new TaskAssignedId(async () => {
                        // 
                        //                             Assert.True(workerId == Thread.CurrentThread.ManagedThreadId);
                        // 
                        //                             for (long i = 0; i < t; i++) {
                        //                                 await Task.Delay(1);
                        // 
                        //                                 Assert.True(workerId == Thread.CurrentThread.ManagedThreadId);
                        // 
                        //                                 count += 5;
                        //                             }
                        //                             taskCompletionSource3.SetResult(true);
                        //                         }, CancellationToken.None, TaskCreationOptions.None, id);
                        //                         innerTask.Start();


                        for (long i = 0; i < t; i++) {
                            //                           await innerTask;
                            await Task.Delay(100);

                            await Task.Delay(100);

                            await Task.Delay(6000);

                            await Task.Run(() => {
                                var task = new TaskAssignedId(() => {

                                    Assert.True(workerId == Thread.CurrentThread.ManagedThreadId);

                                    Task.Delay(5000).Wait();

                                    count += 2;

                                }, id);
                                task.Start(ts);
                            });
                        }
                        taskCompletionSource1.SetResult(true);
                    }, tokenSrc.Token, TaskCreationOptions.DenyChildAttach, id);

                    t1.Start(ts);
                    //                     var t2 = new TaskAssignedId(async () => {
                    // 
                    //                         Assert.True(workerId == Thread.CurrentThread.ManagedThreadId);
                    // 
                    //                         for (long i = 0; i < t; i++) {
                    //                             await Task.Delay(1);
                    // 
                    //                             Assert.True(workerId == Thread.CurrentThread.ManagedThreadId);
                    // 
                    //                             count += 3;
                    //                         }
                    //                         taskCompletionSource2.SetResult(true);
                    //                     }, CancellationToken.None, TaskCreationOptions.None, id);
                    //                      t2.Start(ts);

                }, id);

                task.Start(ts);
            }

            //  假设 task1、2、3的调度是在本线程的同步上下文中，下面这行应该会把本线程挂起，而task又在本线程
            //  运行，因此死锁【但是，它能正常运行，这说明上面的假设不成立】

            //});

            Task.WaitAll(taskCompletionSource1.Task, taskCompletionSource2.Task, taskCompletionSource3.Task);

            Assert.True(count == t * (2 + 3 + 5), count.ToString());
        }


        [Fact]
        public async Task TaskDelayTest()
        {
            var tId = Thread.CurrentThread.ManagedThreadId;
            await Task.Factory.StartNew(() => {
                var tId2 = Thread.CurrentThread.ManagedThreadId;
                var tasks = new List<Task>();
                for (int i = 0; i < 1; i++) {
                    var index = i;
                    var task = new Task(async () => {
                        var tId3 = Thread.CurrentThread.ManagedThreadId;

                        await Task.Delay(1000);

                        await Task.Run(async () => {
                            var tId4 = Thread.CurrentThread.ManagedThreadId;
                            await Task.Delay(2000);
                            var tId5 = Thread.CurrentThread.ManagedThreadId;
                            _testOutputHelper.WriteLine(string.Format("index {0} : thread id {1}/{2}/{3}", index, tId3, tId4, tId5));
                        });

                        await Task.Delay(100);

                        _testOutputHelper.WriteLine(string.Format("awaiter index {0} : thread id {1}", index, tId3));

                    });
                    task.Start();
                    tasks.Add(task);
                }
                Thread.Sleep(1000 * 10);
            });
        }

    }
}
