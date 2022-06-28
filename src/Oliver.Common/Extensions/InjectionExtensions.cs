using Microsoft.Extensions.Configuration;

namespace Oliver.Common.Extensions;

public static class InjectionExtensions
{
    public static T GetOptions<T>(this IConfiguration configuration) where T : new() =>
        configuration.GetRequiredSection(typeof(T).Name).Get<T>();
}
