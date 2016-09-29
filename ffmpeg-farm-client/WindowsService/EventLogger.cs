using System;
using System.Diagnostics;
using System.Threading;
using FFmpegFarm.Worker;

namespace FFmpegFarm.WindowsService
{
    public class EventLogger : ILogger
    {
        private readonly EventLog _eventLog;

        private int GetId(int? threadId)
        {
            return threadId ?? Thread.CurrentThread.ManagedThreadId;
        }
        public EventLogger(EventLog eventLog)
        {
            _eventLog = eventLog;
        }

        public void Debug(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            #if DEBUG
            _eventLog.WriteEntry($"DEBUG: {memberName} : {text}", EventLogEntryType.Information, GetId(threadId));
            #endif
        }

        public void Information(string text, int? threadId = null, string memberName = "")
        {
            _eventLog.WriteEntry($"{memberName} : {text}", EventLogEntryType.Information, GetId(threadId));
        }

        public void Warn(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            _eventLog.WriteEntry($"{memberName} : {text}", EventLogEntryType.Warning, GetId(threadId));
        }

        public void Exception(Exception exception, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            _eventLog.WriteEntry($"{memberName} : {exception.Message} \n StackTrace : {exception.StackTrace}", EventLogEntryType.Error, GetId(threadId));
        }
    }
}