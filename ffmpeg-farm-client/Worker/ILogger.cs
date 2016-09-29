using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FFmpegFarm.Worker
{
    public interface ILogger
    {
        void Debug(string text);
        void Warn(string text);
        void Exception(Exception exception);
    }
}
