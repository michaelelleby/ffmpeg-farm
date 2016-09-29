using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace FFmpegFarm.ConsoleHost
{
    public class Program
    {
        private const string Logo = @"
____ ____ _  _ ___  ____ ____ ____ ____ ____ _  _    _ _ _ ____ ____ _  _ ____ ____ 
|___ |___ |\/| |__] |___ | __ |___ |__| |__/ |\/|    | | | |  | |__/ |_/  |___ |__/ 
|    |    |  | |    |___ |__] |    |  | |  \ |  |    |_|_| |__| |  \ | \_ |___ |  \ 
                                                                                    ";
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var cfg = builder.Build();
            Console.WindowWidth = 100;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Logo);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Configuration : \n{cfg["FFmpegPath"]}\n{cfg["ControllerApi"]}\n{cfg["threads"]} threads.\n\n");
            Console.WriteLine("Press ctrl+x to exit...\n");
            var exitEvent = new ManualResetEvent(false);
            var cancelSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
                cancelSource.Cancel();
            };
            var logger = new ConsoleLogger();
            var tasks = new List<Task>();
            for (var x = 0; x < int.Parse(cfg["threads"]); x++)
            {
                var task =
                    Task.Factory.StartNew(
                        () => new Worker.Node(cfg["FFmpegPath"], cfg["ControllerApi"], logger).Run(cancelSource.Token));
                task.ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnCanceled);
                
                tasks.Add(task);
            }
            while (!cancelSource.IsCancellationRequested)
            {
                while (!Console.KeyAvailable && !cancelSource.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }
                var input = Console.ReadKey(true);
                
                if (input.Key == ConsoleKey.X && input.Modifiers.HasFlag(ConsoleModifiers.Control))
                    cancelSource.Cancel();
            }
            while (tasks.Any(t=>!t.IsCompleted))
            {
                Thread.Sleep(10);
                Console.Write(".");
            }
            Console.WriteLine("\nShut done completed... Press any key.");
            Console.ReadKey();
        }
    }
}
