using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TinyWorkers
{
    public class Worker<TState>
    {
        public static List<Worker<TState>> CreateWorkers(int workNumber, Action<Worker<TState>, TState> action, 
                                                    Action<Worker<TState>, TState> waitting = null, 
                                                    ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            var result = new List<Worker<TState>>();

            for (var i = 0; i < workNumber; i++)
            {
                var workerID = i.ToString();
                var workerState = Activator.CreateInstance<TState>();
                var worker = new Worker<TState>(workerID, action, workerState, waitting, workerPriority); 
                result.Add(worker);
            }

            return result;
        }

        //Default waitting action.
        public static Action Sleep100 = () =>
        {
            Thread.Sleep(100);
        };

        public string ID;
        public Action<Worker<TState>, TState> Action;
        public Action<Worker<TState>, TState> Waitting;
        public ThreadPriority WorkerPriority;
        public bool IsRunning = false;
        public TState State;
        private Task task;

        public Worker(string workerID, Action<Worker<TState>, TState> action, TState state, Action<Worker<TState>, TState> 
                        waitting = null, ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            this.ID = workerID;
            this.Action = action;
            this.State = state;
            this.Waitting = waitting;
        }

        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }
            this.IsRunning = true;

            if (this.task != null)
            {
                this.task.Dispose();
                this.task = null;
            }

            this.task = Task.Run(() =>
            {
                Thread.CurrentThread.Priority = this.WorkerPriority;
                Console.WriteLine(string.Format("{0}:{1} started", ID, Thread.CurrentThread.ManagedThreadId));
                while (this.IsRunning)
                {
                    Action.Invoke(this, State);
                    if (Waitting != null)
                    {
                        Waitting.Invoke(this, State);
                    }
                    else
                    {
                        Sleep100.Invoke();
                    }
                }
            });

        }

        public void Stop(int millisecondsTimeout)
        {
            this.task.Wait(millisecondsTimeout);
            this.task.Dispose();
            this.task = null;
            this.IsRunning = false;
        }
    }
}
