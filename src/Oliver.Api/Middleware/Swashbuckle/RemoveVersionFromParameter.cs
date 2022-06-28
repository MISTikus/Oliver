using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Oliver.Api.Middleware.Swashbuckle;

public class RemoveVersionFromParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        OpenApiParameter versionParameter = operation.Parameters.First(p => p.Name == "version");
        operation.Parameters.Remove(versionParameter);
    }
}