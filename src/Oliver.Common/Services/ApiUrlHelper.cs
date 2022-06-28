namespace Oliver.Client.Services;

public class ApiUrlHelper
{
    private readonly string apiUrl;

    public ApiUrlHelper(string version) => apiUrl = $"api/v{version}";

    public string Executions => $"{apiUrl}/executions";
    public string Packages => $"{apiUrl}/packages";
    public string Templates => $"{apiUrl}/templates";
    public string Variables => $"{apiUrl}/variables";

    public string Route(Func<ApiUrlHelper, string> picker, params object[] routes)
        => $"{picker(this)}/{string.Join('/', routes)}";
}

public static class ApiUrlHelperExtensions
{
    /// <summary>
    /// Add query parameters to url
    /// </summary>
    /// <param name="url">source url</param>
    /// <param name="parameters">array of parameters (has to be primitive objects)</param>
    /// <returns>url with query parameters</returns>
    public static string AddQuery(this string url, params (string key, object value)[] parameters)
        => url +
        (parameters.Any(x => !string.IsNullOrWhiteSpace(x.key) && !string.IsNullOrWhiteSpace(x.value?.ToString()))
            ? "?" + string.Join('&', Encode(parameters))
            : "");

    private static string[] Encode((string key, object value)[] parameters) => parameters
        .Where(x => !string.IsNullOrWhiteSpace(x.key) && !string.IsNullOrWhiteSpace(x.value?.ToString()))
        .Select(x => $"{x.key}={x.value}")
        .ToArray();
}
