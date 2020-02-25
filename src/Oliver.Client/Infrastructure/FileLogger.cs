using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Oliver.Client.Infrastructure
{
    internal class FileLogger : ILogger
    {
        private readonly string categoryName;
        private readonly string fileName;
        private readonly object locker;
        private readonly Action recalculateLogFile;

        public FileLogger(string categoryName, string fileName, object locker, Action recalculateLogFile)
        {
            this.categoryName = categoryName;
            this.fileName = fileName;
            this.locker = locker;
            this.recalculateLogFile = recalculateLogFile;
        }

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (this.locker)
            {
                if (!Directory.Exists(Path.GetDirectoryName(this.fileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(this.fileName));

                var msgs = new List<string>
                {
                    $"{DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss")}|{logLevel.ToString()}|{this.categoryName}| {formatter(state, exception)}",
                };

                if (exception != null)
                {
                    msgs.Add($"{DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss")}|{logLevel.ToString()}|{this.categoryName}| {exception?.Message}");
                    msgs.Add($"{DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss")}|{logLevel.ToString()}|{this.categoryName}| {exception?.StackTrace}");
                }

                File.AppendAllLines(this.fileName, msgs);
            }
            this.recalculateLogFile();
        }
    }
}
