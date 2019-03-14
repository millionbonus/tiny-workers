# TinyWorkers #

TinyWorkers is a library which help you create mulitthreaded program.

## License ##
Licensed under the MIT license.

## Installation ##

Manual download
``` bash
https://github.com/millionbonus/ncurses/releases
``` 

Use package manager
``` bash
$ Install-Package TinyWorkers
```

Use .net cli
``` bash
$ dotnet add package TinyWorkers
```

## Usage ##
``` csharp
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
```
