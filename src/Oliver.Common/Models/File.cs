using System.Collections.Generic;

namespace Oliver.Common.Models
{
    public class File
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string Version { get; set; } // ToDo: use type Version for sort and compare
        public List<byte> Body { get; set; } // RestSharp cannot deserialize byte[]... WTF?!
    }
}
