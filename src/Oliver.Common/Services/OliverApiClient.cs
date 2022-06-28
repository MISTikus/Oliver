using Oliver.Common.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using static Oliver.Common.Models.Execution.ExecutionState;

namespace Oliver.Client.Services;

public class OliverApiClient : IApiClient
{
    public string BaseUrl { get; private set; }
    private readonly Func<HttpClient> clientFactory;
    private readonly ApiUrlHelper api;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly Action<string> errorLogger;

    public OliverApiClient(string baseUrl, ApiUrlHelper api, JsonSerializerOptions jsonOptions, Action<string> errorLogger)
    {
        BaseUrl = baseUrl;
        this.api = api;
        this.jsonOptions = jsonOptions;
        this.errorLogger = errorLogger;
        clientFactory = () => new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    #region executions

    public Task<Execution> GetExecutionAsync(long id, CancellationToken cancellation = default)
        => GetAsync<Execution>(api.Route(x => x.Executions, id), cancellation: cancellation);

    public Task<long?> CheckExecutions(string tenant, string environment, CancellationToken cancellation = default)
        => GetAsync<long?>(api.Route(x => x.Executions, tenant, environment, "check"), timeout: 10 * 60, cancellation: cancellation);

    public Task SendExecutionLog(long executionId, bool isLastStep, Execution.StepState stepState, CancellationToken cancellation = default)
        => PutAsync(
            api
                .Route(x => x.Executions, executionId)
                .AddQuery(("result", isLastStep
                    ? (stepState.IsSuccess ? Successed : Failed)
                    : null)),
            stepState,
            cancellation);

    public Task<long> CreateExecutionAsync(Execution execution, CancellationToken cancellation = default)
        => PostAsync<Execution, long>(api.Route(x => x.Executions), execution, cancellation);

    #endregion execution

    #region templates

    public Task<Template> GetTemplateAsync(long id, CancellationToken cancellation = default)
        => GetAsync<Template>(api.Route(x => x.Templates, id), cancellation: cancellation);

    public Task<long> CreateTemplateAsync(Template template, CancellationToken cancellation = default)
        => PostAsync<Template, long>(api.Route(x => x.Templates), template, cancellation);

    #endregion templates

    #region variables

    public Task<VariableSet> GetVariableSetAsync(long setId, CancellationToken cancellation = default)
        => GetAsync<VariableSet>(api.Route(x => x.Variables, setId), cancellation: cancellation);

    public Task<long> CreateVariableSetAsync(VariableSet variableSet, CancellationToken cancellation = default)
        => PostAsync<VariableSet, long>(api.Route(x => x.Variables), variableSet, cancellation);

    #endregion variables

    #region packages

    public Task<Common.Models.File> GetPackageAsync(string fileName, string version = null, CancellationToken cancellation = default)
        => GetAsync<Common.Models.File>(api.Route(x => x.Packages, fileName).AddQuery((nameof(version), version)), cancellation: cancellation);

    public async Task<string> CreatePackageAsync(string filePath, string version, CancellationToken cancellation = default)
        => await PostFormAsync<string>(api.Route(x => x.Packages),
            new Dictionary<string, HttpContent>
            {
                ["Body"] = await CreateFileContentAsync(filePath, "application/zip"),
                ["Version"] = CreateStringContent(version)
            },
            cancellation);

    #endregion packages

    #region HTTP

    private async Task<T> GetAsync<T>(string url, int? timeout = default, CancellationToken cancellation = default)
    {
        using HttpClient client = clientFactory();
        if (timeout.HasValue)
            client.Timeout = TimeSpan.FromSeconds(timeout.Value);
        var json = await client.GetStringAsync(url, cancellation);
        return json is null
                ? default
                : JsonSerializer.Deserialize<T>(json, jsonOptions);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellation = default)
    {
        using HttpClient client = clientFactory();
        HttpResponseMessage response = await client.PostAsJsonAsync(url, request, jsonOptions, cancellation);
        if (!response.IsSuccessStatusCode)
        {
            errorLogger($"Failed to post. Status code is: '{response.StatusCode}'.");
            string content;
            if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                errorLogger($"Message: '{content}'");
            return default;
        }
        return await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions, cancellationToken: cancellation);
    }

    private async Task PostAsync<TRequest>(string url, TRequest request, CancellationToken cancellation = default)
    {
        using HttpClient client = clientFactory();
        HttpResponseMessage response = await client.PostAsJsonAsync(url, request, jsonOptions, cancellation);
        if (response.IsSuccessStatusCode)
            return;

        errorLogger($"Failed to post. Status code is: '{response.StatusCode}'.");
        string content;
        if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
            errorLogger($"Message: '{content}'");
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellation = default)
    {
        using HttpClient client = clientFactory();
        HttpResponseMessage response = await client.PutAsJsonAsync(url, request, jsonOptions, cancellation);
        if (!response.IsSuccessStatusCode)
        {
            errorLogger($"Failed to put. Status code is: '{response.StatusCode}'.");
            string content;
            if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                errorLogger($"Message: '{content}'");
            return default;
        }
        return await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions, cancellationToken: cancellation);
    }

    private async Task PutAsync<TRequest>(string url, TRequest request, CancellationToken cancellation = default)
    {
        using HttpClient client = clientFactory();
        HttpResponseMessage response = await client.PutAsJsonAsync(url, request, jsonOptions, cancellation);
        if (response.IsSuccessStatusCode)
            return;

        errorLogger($"Failed to put. Status code is: '{response.StatusCode}'.");
        string content;
        if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
            errorLogger($"Message: '{content}'");
    }

    private async Task<TResponse> PostFormAsync<TResponse>(string url, Dictionary<string, HttpContent> body, CancellationToken cancellation = default)
    {
        MultipartFormDataContent content = new();
        foreach (KeyValuePair<string, HttpContent> kv in body)
        {
            if (kv.Value?.Headers?.Contains("filename") ?? false)
            {
                content.Add(kv.Value, kv.Key, kv.Value.Headers.GetValues("filename").Single());
                kv.Value.Headers.Remove("filename");
            }
            else
            {
                content.Add(kv.Value, kv.Key);
            }
        }

        using HttpClient client = clientFactory();
        HttpResponseMessage response = await client.PostAsync(url, content, cancellation);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content?.ReadAsStringAsync(cancellation);
            if (typeof(TResponse) == typeof(string))
                return (TResponse)(object)json;
            return json is null
                ? default
                : JsonSerializer.Deserialize<TResponse>(json, jsonOptions);
        }

        errorLogger($"Failed to post. Status code is: '{response.StatusCode}'.");
        string responseContent;
        if ((responseContent = await response.Content.ReadAsStringAsync(cancellation)) is not null)
            errorLogger($"Message: '{responseContent}'");
        return default;
    }

    private static async Task<HttpContent> CreateFileContentAsync(string filePath, string mediaType)
    {
        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        ByteArrayContent fileContent = new(bytes, 0, bytes.Length);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        fileContent.Headers.Add("filename", System.IO.Path.GetFileName(filePath));
        return fileContent;
    }

    private static HttpContent CreateStringContent(string value) => new StringContent(value);

    #endregion HTTP
}

public interface IApiClient
{
    string BaseUrl { get; }

    Task<Execution> GetExecutionAsync(long id, CancellationToken cancellation = default);
    Task<long?> CheckExecutions(string tenant, string environment, CancellationToken cancellation = default);
    Task SendExecutionLog(long executionId, bool isLastStep, Execution.StepState stepState, CancellationToken cancellation = default);
    Task<long> CreateExecutionAsync(Execution execution, CancellationToken cancellation = default);

    Task<Template> GetTemplateAsync(long id, CancellationToken cancellation = default);
    Task<long> CreateTemplateAsync(Template template, CancellationToken cancellation = default);

    Task<VariableSet> GetVariableSetAsync(long setId, CancellationToken cancellation = default);
    Task<long> CreateVariableSetAsync(VariableSet variableSet, CancellationToken cancellation = default);

    Task<Common.Models.File> GetPackageAsync(string fileName, string version = null, CancellationToken cancellation = default);
    Task<string> CreatePackageAsync(string filePath, string version, CancellationToken cancellation = default);
}
