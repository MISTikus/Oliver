namespace Oliver.Client.Configurations
{
    internal class LogFile
    {
        public string FileName { get; set; }
        public string ArchiveLogFileFormat { get; set; }
        public int MaxLogSizeMb { get; set; }
        public int MaxFilesCount { get; set; }
    }
}
