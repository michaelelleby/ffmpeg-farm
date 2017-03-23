using System;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using FFmpegFarm.Worker;

namespace FFmpegFarm.WindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                RunAsConsole();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] {new Service()});
            }
        }

        private const string Logo = @"
____ ____ _  _ ___  ____ ____ ____ ____ ____ _  _    _ _ _ ____ ____ _  _ ____ ____ 
|___ |___ |\/| |__] |___ | __ |___ |__| |__/ |\/|    | | | |  | |__/ |_/  |___ |__/ 
|    |    |  | |    |___ |__] |    |  | |  \ |  |    |_|_| |__| |  \ | \_ |___ |  \ 
                                                                                    ";

        private static void RunAsConsole()
        {
            ILogger logger = new ConsoleLogger();
            var token = new CancellationTokenSource();
            var client = new FFmpegClient(logger, token);

            var cfg = ConfigurationManager.AppSettings;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Logo);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Configuration : \n{cfg["FFmpegPath"]}\n{cfg["ControllerApi"]}\n{cfg["threads"]} threads.\n\n");
            Console.WriteLine("Press ctrl+x to exit...\n");
            var exitEvent = new ManualResetEvent(false);
            var cancelSource = new CancellationTokenSource();

            client.Start();

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
                cancelSource.Cancel();
            };

            ConsoleKeyInfo keyInfo;
            while (!cancelSource.IsCancellationRequested &&
                   !(Console.KeyAvailable && (keyInfo = Console.ReadKey(false)).Key == ConsoleKey.X
                     && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)))
            {
                Thread.Sleep(100);
            }

            client.Stop();

            Console.WriteLine("\nShut done completed... Press any key.");
            Console.ReadKey();
        }
    }
}
