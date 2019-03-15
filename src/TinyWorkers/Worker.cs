using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TinyWorkers
{
    public class Worker<TState> where TState: class, new()
    {
        public static List<Worker<TState>> CreateWorkers(int workerCount,
                                                    Action<Worker<TState>, TState> action,
                                                    Action<Worker<TState>, TState> waitting = null,
                                                    ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            return CreateWorkers(workerCount, GenerateWorkerId, action, waitting, workerPriority);
        }

        public static List<Worker<TState>> CreateWorkers(int workerCount, 
                                                    Func<int, string> workerIdGenerator,
                                                    Action<Worker<TState>, TState> action, 
                                                    Action<Worker<TState>, TState> waitting = null, 
                                                    ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            var result = new List<Worker<TState>>();

            for (var i = 0; i < workerCount; i++)
            {
                var workerID = workerIdGenerator(i);
                var workerState = Activator.CreateInstance<TState>();
                var worker = new Worker<TState>(workerID, action, workerState, 
                                                waitting, workerPriority); 
                result.Add(worker);
            }

            return result;
        }

        //Default waitting action.
        public static Action Sleep100 = () =>
        {
            Thread.Sleep(100);
        };

        //Default worker id generation fun.
        public static Func<int, string> GenerateWorkerId = (int number) =>
        {
            return number.ToString();
        };

        public string ID;
        public Action<Worker<TState>, TState> Action;
        public Action<Worker<TState>, TState> Waitting;
        public ThreadPriority WorkerPriority;
        public bool IsRunning = false;
        public TState State;
        private Task task;
        public CancellationTokenSource CancellationTokenSource = null;


        public delegate void StartedEventHandler(object sender, WorkerEventArgs<TState> args);
        public event StartedEventHandler Started;

        public delegate void StoppedEventHandler(object sender, WorkerEventArgs<TState> args);
        public event StoppedEventHandler Stopped;

        public Worker(string workerID, Action<Worker<TState>, TState> action, TState state, 
                        Action<Worker<TState>, TState> waitting = null, 
                        ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            this.ID = workerID;
            this.Action = action;
            this.State = state;
            this.Waitting = waitting;
            this.WorkerPriority = workerPriority;
            this.CancellationTokenSource = new CancellationTokenSource();
        }

        protected virtual void OnStarted()
        {
            if(this.Started != null)
            {
                this.Started(this, new WorkerEventArgs<TState>(this.State));
            }
        } 

        protected virtual void OnStopped()
        {
            if(this.Stopped != null)
            {
                this.Stopped(this, new WorkerEventArgs<TState>(this.State));
            }
        } 

        public void Start()
        {
            var ct = this.CancellationTokenSource.Token;

            if (this.IsRunning || ct.IsCancellationRequested)
            {
                return;
            }
            this.IsRunning = true;

            if(this.task != null)
            {
                this.task.Start();
            }
            else
            {
                this.task = Task.Run(() =>
                {
                    Thread.CurrentThread.Priority = this.WorkerPriority;
                    
                    OnStarted();

                    while (!ct.IsCancellationRequested)
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

                    this.IsRunning = false;

                    OnStopped();

                }, ct);

            }

        }

        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }
    }
}
