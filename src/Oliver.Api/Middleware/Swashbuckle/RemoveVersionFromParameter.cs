using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Oliver.Api.Middleware.Swashbuckle
{
    public class RemoveVersionFromParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters.First(p => p.Name == "version");
            operation.Parameters.Remove(versionParameter);
        }
    }

}
