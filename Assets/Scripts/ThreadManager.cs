using System.Threading;
using UnityEngine;

public class ThreadManager
{
    public static Thread Worker(FluidSystem instance)
    {
        Thread caller = new Thread(new ThreadStart(instance.Simulate));
        
        caller.Start();

        return caller;
    }
}
