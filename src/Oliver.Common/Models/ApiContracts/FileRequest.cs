using Microsoft.AspNetCore.Http;

namespace Oliver.Common.Models.ApiContracts;

public record FileRequest(string Version, IFormFile Body);
