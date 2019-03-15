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

## Worker's flow ##

The flow of worker as below.
``` flow
              (canceled)
              ------------------------------
              |                            |
              |          (stop)            v
start --> **action** --> **waiting**  ---> end
            ^                |
            |                |
            |----------------|
              (continue)
```


## Usage ##

All you need to do is defining the action and state (if need).
Then start the worker.

For example:
``` csharp

    // 1. Define state
    var state = "state";

    // 2. Defined action
    var action = (w, s)=>{ Console.WriteLine($"worder: {w.ID}, state: {s}"); };

    // 3. Create a worker
    var worker = new Worker<string>("worker1", action, state); 

    // 4. start to run
    worker.Start();
```


There is more completed example below.
``` csharp
    // This is a custom state which can help you keep extra information
    public class CustomState
    {
        public string ComputedData;
        public int Count;
        public DateTime ActionTime;
    }

    class Program
    {
        // When a worker about to start doing its job. The OnStarted event fired.
        private static void onStarted (object o, WorkerEventArgs<CustomState> e) 
        {
            Console.WriteLine($"worker - {((Worker<CustomState>)o).ID} started");
        }

        // When a worker about to stop doing its job. The OnStopped event fired.
        private static void onStopped (object o, WorkerEventArgs<CustomState> e) 
        {
            Console.WriteLine($"worker - {((Worker<CustomState>)o).ID} stopped");
        }

        static void Main(string[] args)
        {
            // Create 10 workers and define what will those workers would do.
            var workers = Worker<CustomState>.CreateWorkers(10, (worker, state) =>
            {
                //Let's do some calculate here

                state.Count++;
                state.ActionTime = DateTime.Now;
                state.ComputedData = Guid.NewGuid().ToString();

                Console.WriteLine($"worker[{worker.ID}] does the job -- {state.ComputedData} | {state.Count} | {state.ActionTime.ToString("HH:mm:ss")}");

                //Stop after 10 times calculation.
                if (state.Count >= 10)
                {
                    worker.Stop();
                }

            });

            workers.ForEach((worker)=> {
                // Subscribe events;
                worker.Started += onStarted;
                worker.Stopped += onStopped;

                // By default, when worker complete the job. It will wait for 100ms then do the job again and again.
                // However, you can do things while waitting.
                // Just do things there...
                worker.Waitting = (w, s) =>
                {
                    Console.WriteLine($"worker[{w.ID}] does something else while waitting...");

                    Thread.Sleep(1000 * 3);
                };

                // Setup state
                worker.State.ComputedData = Guid.NewGuid().ToString();
                
                // Start to work.
                worker.Start();
            });


            Console.ReadKey();
        }
    }
```
