using System;

namespace Contract
{
    public interface ILogging
    {
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(Exception exception, string message);
    }
}
