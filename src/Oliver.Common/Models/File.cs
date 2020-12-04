using System;

namespace Oliver.Common.Models
{
    public class File
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public Version Version { get; set; }
        public byte[] Body { get; set; }
    }
}
