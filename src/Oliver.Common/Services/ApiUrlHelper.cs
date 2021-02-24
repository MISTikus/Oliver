using System;
using System.Linq;

namespace Oliver.Client.Services
{
    public class ApiUrlHelper
    {
        private readonly string apiUrl;

        public ApiUrlHelper(string version) => this.apiUrl = $"api/v{version}";

        public string Executions => $"{this.apiUrl}/executions";
        public string Packages => $"{this.apiUrl}/packages";
        public string Templates => $"{this.apiUrl}/templates";
        public string Variables => $"{this.apiUrl}/variables";

        public string Route(Func<ApiUrlHelper, string> picker, params object[] routes)
            => $"{picker(this)}/{string.Join('/', routes)}";
    }

    public static class ApiUrlHelperExtensions
    {
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
}
