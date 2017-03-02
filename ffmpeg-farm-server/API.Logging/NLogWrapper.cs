using System;
using Contract;
using NLog;

namespace API.Logging
{
    public class NLogWrapper : ILogging
    {
        
        static NLogWrapper()
        {
            //Ref fix
            var appSettings = typeof(NLog.LayoutRenderers.AppSettingLayoutRenderer);
            var aspNet = typeof(NLog.Web.LayoutRenderers.AspNetApplicationValueLayoutRenderer);
        }
        
        private readonly Logger _logger;
        public NLogWrapper(string name)
        {
            try
            {
                _logger = LogManager.GetLogger(name);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Error(Exception exception, string message)
        {
            _logger.Error(exception, message);
        }
    }
}
