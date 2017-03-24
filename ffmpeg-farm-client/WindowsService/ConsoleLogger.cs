using System;
using System.Threading;
using FFmpegFarm.Worker;

namespace FFmpegFarm.WindowsService
{
    internal class ConsoleLogger : ILogger
    {
        private static readonly int[] Palette = { 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15 };
        private void WriteThreadId(int? threadId, string memberName)
        {
            var id = threadId ?? Thread.CurrentThread.ManagedThreadId;
            Console.ForegroundColor = (ConsoleColor) Palette[(id%Palette.Length)];
            Console.Write($"{id} ({memberName})");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" : ");
        }
        private static readonly object Lock = new object();
        public void Debug(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            lock (Lock)
            {
                WriteThreadId(threadId, memberName);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(text);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public void Information(string text, int? threadId = null, string memberName = "")
        {
            lock (Lock)
            {
                WriteThreadId(threadId, memberName);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(text);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public void Warn(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            lock (Lock)
            {
                WriteThreadId(threadId, memberName);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(text);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public void Exception(Exception exception, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            lock (Lock)
            {
                WriteThreadId(threadId, memberName);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception.Message);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(exception.StackTrace);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
