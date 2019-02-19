using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TinyWorkers;

namespace TestConsole
{
    public class CustomState
    {
        public string state1;
        public int state2;
    }

    class Program
    {
        private static void onStarted (object o, WorkerEventArgs<CustomState> e) 
        {
            Console.WriteLine($"worker {((Worker<CustomState>)o).ID} started");
        }

        private static void onStopped (object o, WorkerEventArgs<CustomState> e) 
        {
            Console.WriteLine($"worker {((Worker<CustomState>)o).ID} stopped");
        }

        static void Main(string[] args)
        {
            var workers = Worker<CustomState>.CreateWorkers(10, (worker, state) =>
            {
                Console.WriteLine(string.Format("[{0}] do job -- {1}|{2}", worker.ID, state.state1, state.state2));
                Thread.Sleep(500);
            });

            workers.ForEach((worker)=> {
                worker.Started += onStarted;
                worker.Stopped += onStopped;
                worker.State.state1 = Guid.NewGuid().ToString();
                worker.Start();
            });


            Console.ReadKey();
        }
    }
}
