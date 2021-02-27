﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Oliver.Common.Infrastructure
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string fileName;
        private readonly int maxLogSizeMb;
        private readonly int maxFilesCount;
        private readonly string archiveLogFileFormat;
        private readonly object locker = new object();

        public FileLoggerProvider(LogFile options)
        {
            this.fileName = Path.GetFullPath(options.FileName);
            this.maxLogSizeMb = options.MaxLogSizeMb;
            this.maxFilesCount = options.MaxFilesCount;
            this.archiveLogFileFormat = options.ArchiveLogFileFormat;
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this.fileName, this.locker, RecalculateLogFile);
        public void Dispose() => GC.SuppressFinalize(this);

        private void RecalculateLogFile()
        {
            Task.Run(() =>
            {
                if (!File.Exists(this.fileName))
                    return;

                lock (this.locker)
                {
                    var len = new FileInfo(this.fileName).Length;
                    if (len / 1024 / 1024 >= this.maxLogSizeMb)
                    {
                        var path = Path.GetDirectoryName(this.fileName);
                        var files = Directory.GetFiles(path, Path.GetFileName(this.archiveLogFileFormat).Replace("{0}", "*"));

                        RollArchiveFiles(files);

                        File.Copy(this.fileName, string.Format(this.archiveLogFileFormat, 1), true);
                        File.Delete(this.fileName);
                    }
                }
            });
        }

        private void RollArchiveFiles(string[] files)
        {
            for (var i = files.Length <= this.maxFilesCount - 1 ? files.Length : this.maxFilesCount - 1; i > 0; i--)
            {
                var archiveFileName = Path.GetFullPath(string.Format(this.archiveLogFileFormat, i + 1));
                File.Copy(files[i - 1], archiveFileName, true);
            }
        }

        private class FileLogger : ILogger
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
                }

                var msgs = new List<string>
                {
                    $"{DateTime.Now:yyyy.MM.dd HH:mm:ss} | {logLevel} | {this.categoryName} | {formatter(state, exception)}",
                };

                if (exception != null)
                {
                    msgs.Add($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} | {logLevel} | {this.categoryName} | {exception?.Message}");
                    msgs.Add($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} | {logLevel} | {this.categoryName} | {exception?.StackTrace}");
                }

                lock (this.locker)
                {
                    File.AppendAllLines(this.fileName, msgs);
                }
                this.recalculateLogFile();
            }
        }
    }
}
