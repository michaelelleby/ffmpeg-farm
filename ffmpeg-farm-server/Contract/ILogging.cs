using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
