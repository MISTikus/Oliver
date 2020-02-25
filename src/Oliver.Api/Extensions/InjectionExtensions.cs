using Microsoft.Extensions.Configuration;

namespace Oliver.Api.Extensions
{
    public static class InjectionExtensions
    {
        public static T GetOptions<T>(this IConfiguration configuration) where T : new()
        {
            var options = new T();
            configuration.Bind(typeof(T).Name, options);
            return options;
        }
    }
}
