using System;
using System.Threading;

namespace DX12Windows
{
    static class Utils
    {
        public static void Run(params Thread[] tasks)
        {
            Array.ForEach(tasks, (t) => t.Start());
            Array.ForEach(tasks, (t) => t.Join());
        }
    }
}
