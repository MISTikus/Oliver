using Microsoft.AspNetCore.Http;

namespace Oliver.Common.Models.ApiContracts
{
    public class FileRequest
    {
        public string Version { get; set; }
        public IFormFile Body { get; set; }
    }
}
