using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace FFmpegFarm.ConsoleHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var cfg = builder.Build();
            Console.WriteLine("Press ctrl+c to exit...");
            var exitEvent = new ManualResetEvent(false);
            var cancelSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
                cancelSource.Cancel();
            };
            var threads = new List<Thread>();
            for (var x = 0; x < int.Parse(cfg["threads"]); x++)
            {
                var thread = new Thread(() => new Worker.Node(cfg["FFmpegPath"], cfg["ControllerApi"]).Run(cancelSource.Token));
                threads.Add(thread);
                thread.Start();
            }
            exitEvent.WaitOne();
            foreach (var thread in threads)
            {
                thread.Join();
                Console.WriteLine("Thread exited");
            }
        }
    }
}
