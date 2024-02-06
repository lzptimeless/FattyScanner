using Serilog.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;

namespace FattyScanner.Logger
{
    public static class AppLogger
    {
        #region fields
        private const string LogFileName = "log.txt";
        private static ILoggerFactory? _loggerFactory;
        private static Microsoft.Extensions.Logging.ILogger? _defaultLogger;
        #endregion

        #region properties
        public static Microsoft.Extensions.Logging.ILogger Default
        {
            get
            {
                if (_defaultLogger == null)
                {
                    _defaultLogger = CreateLogger("Default");
                }

                return _defaultLogger;
            }
        }

        public static ILoggerFactory Factory
        {
            get
            {
                if (_loggerFactory == null) throw new ApplicationException("Should call Setup first.");

                return _loggerFactory;
            }
        }
        #endregion

        #region public methods
        public static void Setup()
        {
            try
            {
                string logFilePath = GetLogFilePath();
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: logFilePath,
                        fileSizeLimitBytes: 1024 * 1024 * 10,
                        rollOnFileSizeLimit: true)
                    .CreateLogger();

                var loggerFactory = new LoggerFactory();
                _loggerFactory = loggerFactory.AddSerilog(Log.Logger);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[FattyScanner] AppLogger setup failed.");
                Trace.WriteLine(ex.ToString());
            }
        }

        public static Microsoft.Extensions.Logging.ILogger CreateLogger(Type type)
        {
            if (_loggerFactory == null) throw new ApplicationException("Should call Setup first.");

            return _loggerFactory.CreateLogger(type);
        }

        public static Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            if (_loggerFactory == null) throw new ApplicationException("Should call Setup first.");

            return _loggerFactory.CreateLogger(categoryName);
        }

        public static void TryFlush()
        {
            try
            {
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[FattyScanner] AppLogger flush failed.");
                Trace.WriteLine(ex.ToString());
            }
        }
        #endregion

        #region private methods
        private static string GetLogFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, LogFileName);

            return path;
        }
        #endregion
    }
}
