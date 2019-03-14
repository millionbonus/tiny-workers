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


        public delegate void StartedEventHandler(object sender, WorkerEventArgs<TState> args);
        public event StartedEventHandler Started;

        public delegate void StoppedEventHandler(object sender, WorkerEventArgs<TState> args);
        public event StoppedEventHandler Stopped;

        public Worker(string workerID, Action<Worker<TState>, TState> action, TState state, Action<Worker<TState>, TState> 
                        waitting = null, ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            this.ID = workerID;
            this.Action = action;
            this.State = state;
            this.Waitting = waitting;
            this.WorkerPriority = workerPriority;
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
                
                OnStarted();

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

        public void Stop(int millisecondsTimeout = -1)
        {
            this.task.Wait(millisecondsTimeout);
            this.task.Dispose();
            this.task = null;
            this.IsRunning = false;

            OnStopped();
        }
    }
}
