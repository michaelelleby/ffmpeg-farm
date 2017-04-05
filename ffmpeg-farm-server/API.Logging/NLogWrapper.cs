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
            _logger = LogManager.GetLogger(name);
        }

        public void Debug(string message)
        {
            if (_logger == null)
                return;

            _logger.Debug(message);
        }

        public void Info(string message)
        {
            if (_logger == null)
                return;

            _logger.Info(message);
        }

        public void Warn(string message)
        {
            if (_logger == null)
                return;

            _logger.Warn(message);
        }

        public void Error(Exception exception, string message)
        {
            if (_logger == null)
                return;

            _logger.Error(exception, message);
        }
    }
}
