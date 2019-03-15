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
        /// <summary>
        /// Create multiple workers.
        /// </summary>
        /// <param name="workerCount">Specified number of worker to be created</param>
        /// <param name="action">Job action</param>
        /// <param name="waitting">Waitting action</param>
        /// <param name="workerPriority">Task priority</param>
        /// <returns>A list of workers</returns>
        public static List<Worker<TState>> CreateWorkers(int workerCount,
                                                    Action<Worker<TState>, TState> action,
                                                    Action<Worker<TState>, TState> waitting = null,
                                                    ThreadPriority workerPriority = ThreadPriority.BelowNormal)
        {
            return CreateWorkers(workerCount, GenerateWorkerId, action, waitting, workerPriority);
        }

        /// <summary>
        /// Create multiple workers.
        /// </summary>
        /// <param name="workerCount">Specified number of worker to be created</param>
        /// <param name="workerIdGenerator">A Func which used to generate worker ID</param>
        /// <param name="action">Job action</param>
        /// <param name="waitting">Waitting action</param>
        /// <param name="workerPriority">Task priority</param>
        /// <returns>A list of workers</returns>
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

        /// <summary>
        /// Default waitting action.
        /// </summary>
        public static Action Sleep100 = () =>
        {
            Thread.Sleep(100);
        };

        /// <summary>
        /// Default worker id generating func.
        /// </summary>
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

        /// <summary>
        /// Create a worker.
        /// </summary>
        /// <param name="workerID">Specified worker ID</param>
        /// <param name="action">Job action</param>
        /// <param name="waitting">Waitting action</param>
        /// <param name="workerPriority">Task priority</param>
        /// <returns>A worker</returns>
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

        /// <summary>
        /// Invoke Started event.
        /// </summary>
        protected virtual void OnStarted()
        {
            if(this.Started != null)
            {
                this.Started(this, new WorkerEventArgs<TState>(this.State));
            }
        } 

        /// <summary>
        /// Invoke Stopped event.
        /// </summary>
        protected virtual void OnStopped()
        {
            if(this.Stopped != null)
            {
                this.Stopped(this, new WorkerEventArgs<TState>(this.State));
            }
        } 

        /// <summary>
        /// Start to execute job action repeatedly.
        /// </summary>
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

                        if (!ct.IsCancellationRequested)
                        {
                            if (Waitting != null)
                            {
                                Waitting.Invoke(this, State);
                            }
                            else
                            {
                                Sleep100.Invoke();
                            }
                        }

                    }

                    ct.WaitHandle.WaitOne();

                    this.IsRunning = false;

                    OnStopped();

                }, ct);

            }

        }

        /// <summary>
        /// Stop to execute job action.
        /// </summary>
        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }

    }
}
