using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Oliver.Common.Infrastructure;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string fileName;
    private readonly int maxLogSizeMb;
    private readonly int maxFilesCount;
    private readonly string archiveLogFileFormat;
    private static readonly ConcurrentDictionary<string, object> lockers = new ConcurrentDictionary<string, object>();
    private object currentLocker;

    public FileLoggerProvider(LogFile options)
    {
        fileName = Path.GetFullPath(options.FileName);
        maxLogSizeMb = options.MaxLogSizeMb;
        maxFilesCount = options.MaxFilesCount;
        archiveLogFileFormat = options.ArchiveLogFileFormat;
    }

    public ILogger CreateLogger(string categoryName)
    {
        lockers.TryAdd(fileName, new object());
        currentLocker = lockers[fileName];
        return new FileLogger(categoryName, fileName, RecalculateLogFile);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    private void RecalculateLogFile()
    {
        Task.Run(() =>
        {
            if (!File.Exists(fileName))
                return;

            lock (currentLocker)
            {
                var len = new FileInfo(fileName).Length;
                if (len / 1024 / 1024 >= maxLogSizeMb)
                {
                    var path = Path.GetDirectoryName(fileName);
                    var files = Directory.GetFiles(path, Path.GetFileName(archiveLogFileFormat).Replace("{0}", "*"));

                    RollArchiveFiles(files);

                    File.Copy(fileName, string.Format(archiveLogFileFormat, 1), true);
                    File.Delete(fileName);
                }
            }
        });
    }

    private void RollArchiveFiles(string[] files)
    {
        for (var i = files.Length <= maxFilesCount - 1 ? files.Length : maxFilesCount - 1; i > 0; i--)
        {
            var archiveFileName = Path.GetFullPath(string.Format(archiveLogFileFormat, i + 1));
            File.Copy(files[i - 1], archiveFileName, true);
        }
    }

    private class FileLogger : ILogger
    {
        private readonly string categoryName;
        private readonly string fileName;
        private readonly object locker;
        private readonly Action recalculateLogFile;

        public FileLogger(string categoryName, string fileName, Action recalculateLogFile)
        {
            this.categoryName = categoryName;
            this.fileName = fileName;
            locker = lockers[fileName];
            this.recalculateLogFile = recalculateLogFile;
        }

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (locker)
            {
                var directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            var msgs = new List<string>
            {
                $"{DateTime.Now:yyyy.MM.dd HH:mm:ss} | {logLevel} | {categoryName} | {formatter(state, exception)}",
            };

            if (exception != null)
            {
                msgs.Add($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} | {logLevel} | {categoryName} | {exception?.Message}");
                msgs.Add($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} | {logLevel} | {categoryName} | {exception?.StackTrace}");
            }

            lock (locker)
            {
                File.AppendAllLines(fileName, msgs);
            }
            recalculateLogFile();
        }
    }
}
