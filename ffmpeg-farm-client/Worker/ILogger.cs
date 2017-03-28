using System;

namespace FFmpegFarm.Worker
{
    public interface ILogger
    {
        void Debug(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");
        void Information(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");
        void Warn(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");
        void Exception(Exception exception, int? threadId = null, string memberName = "");
    }
}
