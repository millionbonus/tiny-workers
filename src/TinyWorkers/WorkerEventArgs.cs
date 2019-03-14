using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyWorkers
{
    public class WorkerEventArgs<TState>
    {
        public TState State;
        public WorkerEventArgs(TState state)
        {
            this.State = state;
        }
    }
}
