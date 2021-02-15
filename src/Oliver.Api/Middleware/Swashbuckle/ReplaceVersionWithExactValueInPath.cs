using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Oliver.Api.Middleware.Swashbuckle
{
    public class ReplaceVersionWithExactValueInPath : IDocumentFilter
    {
        public void Apply(OpenApiDocument doc, DocumentFilterContext context)
        {
            var dict = doc.Paths.ToDictionary(
                    path => path.Key.Replace("v{version}", doc.Info.Version),
                    path => path.Value);
            doc.Paths.Clear();
            foreach (var key in dict.Keys)
                doc.Paths.Add(key, dict[key]);
        }
    }

}
