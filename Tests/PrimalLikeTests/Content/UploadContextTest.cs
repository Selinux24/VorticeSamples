using NUnit.Framework;
using System.Threading;

namespace PrimalLikeTests.Content
{
    public class UploadContextTest
    {
        const int numThreads = 8;
        bool shutdown = false;
        readonly Thread[] workers = new Thread[numThreads];

        // Test preparation
        [OneTimeSetUp]
        public void Setup()
        {
            //Initalize worker threads
            for (int i = 0; i < numThreads; i++)
            {
                workers[i] = new Thread(BufferWorker);
            }
        }

        private void BufferWorker()
        {
            while (!shutdown)
            {
                
            }
        }

        [Test()]
        public void BufferWorkersTest()
        {
            // Start worker threads
            for (int i = 0; i < numThreads; i++)
            {
                workers[i].Start();
            }

            // Wait for a while
            Thread.Sleep(1000);

            // Shutdown worker threads
            shutdown = true;
            for (int i = 0; i < numThreads; i++)
            {
                workers[i].Join();
            }
        }
    }
}
