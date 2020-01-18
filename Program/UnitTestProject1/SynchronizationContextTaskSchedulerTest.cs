using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace UnitTestProject1
{
    public class SynchronizationContextTaskSchedulerTest
    {
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
            long count = 0;
            long t = 1L * 10000 * 10000 * 1;

            TaskCompletionSource<bool> taskCompletionSource1 = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> taskCompletionSource2 = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> taskCompletionSource3 = new TaskCompletionSource<bool>();

            var t1 = new Task(() => {

                for (long i = 0; i < t; i++) {
                    count += 2;
                }
                taskCompletionSource1.SetResult(true);
            }, CancellationToken.None, TaskCreationOptions.None);
            var t2 = new Task(() => {

                for (long i = 0; i < t; i++) {
                    count += 3;
                }
                taskCompletionSource2.SetResult(true);
            }, CancellationToken.None, TaskCreationOptions.None);
            var t3 = new Task(() => {

                for (long i = 0; i < t; i++) {
                    count += 5;
                }
                taskCompletionSource3.SetResult(true);
            }, CancellationToken.None, TaskCreationOptions.None);

            if (SynchronizationContext.Current == null) {
                var synchronizationContext = new SynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }

            var ts = TaskScheduler.FromCurrentSynchronizationContext();

            t1.Start(ts);
            t2.Start(ts);
            t3.Start(ts);

            //  假设 task1、2、3的调度是在本线程的同步上下文中，下面这行应该会把本线程挂起，而task又在本线程
            //  运行，因此死锁【但是，它能正常运行，这说明上面的假设不成立】
            Task.WaitAll(taskCompletionSource1.Task, taskCompletionSource2.Task, taskCompletionSource3.Task);

            Assert.True(count == t * (2 + 3 + 5), count.ToString());
        }

    }
}
