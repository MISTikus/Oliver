using Oliver.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using static Oliver.Common.Models.Execution.ExecutionState;

namespace Oliver.Client.Services
{
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
            this.clientFactory = () => new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        #region executions
        public Task<Execution> GetExecutionAsync(long id, CancellationToken cancellation = default)
            => GetAsync<Execution>(this.api.Route(x => x.Executions, id), cancellation: cancellation);
        public Task<long?> CheckExecutions(string tenant, string environment, CancellationToken cancellation = default)
            => GetAsync<long?>(this.api.Route(x => x.Executions, tenant, environment, "check"), timeout: 10 * 60, cancellation: cancellation);
        public Task SendExecutionLog(long executionId, bool isLastStep, Execution.StepState stepState, CancellationToken cancellation = default)
            => PutAsync(
                this.api
                    .Route(x => x.Executions, executionId)
                    .AddQuery(("result", isLastStep
                        ? (stepState.IsSuccess ? Successed : Failed)
                        : null)),
                stepState,
                cancellation);
        #endregion execution

        #region templates
        public Task<Template> GetTemplateAsync(long id, CancellationToken cancellation = default)
            => GetAsync<Template>(this.api.Route(x => x.Templates, id), cancellation: cancellation);
        public Task<long> CreateTemplateAsync(Template template, CancellationToken cancellation = default)
            => PostAsync<Template, long>(this.api.Route(x => x.Templates), template, cancellation);
        #endregion templates

        #region variables
        public Task<VariableSet> GetVariableSetAsync(long setId, CancellationToken cancellation = default)
            => GetAsync<VariableSet>(this.api.Route(x => x.Variables, setId), cancellation: cancellation);
        #endregion variables

        #region packages
        public Task<File> GetPackageAsync(string fileName, string version = null, CancellationToken cancellation = default)
            => GetAsync<File>(this.api.Route(x => x.Packages, fileName).AddQuery((nameof(version), version)), cancellation: cancellation);
        public async Task<long> CreatePackageAsync(string filePath, string version, CancellationToken cancellation = default)
            => await PostFormAsync<long>(this.api.Route(x => x.Packages),
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
            using var client = this.clientFactory();
            if (timeout.HasValue)
                client.Timeout = TimeSpan.FromSeconds(timeout.Value);
            var json = await client.GetStringAsync(url, cancellation);
            return json is null
                    ? default
                    : JsonSerializer.Deserialize<T>(json, this.jsonOptions);
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            var response = await client.PostAsJsonAsync(url, request, this.jsonOptions, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                this.errorLogger($"Failed to post. Status code is: '{response.StatusCode}'.");
                string content;
                if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                    this.errorLogger($"Message: '{content}'");
                return default;
            }
            return await response.Content.ReadFromJsonAsync<TResponse>(this.jsonOptions, cancellationToken: cancellation);
        }
        private async Task PostAsync<TRequest>(string url, TRequest request, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            var response = await client.PostAsJsonAsync(url, request, this.jsonOptions, cancellation);
            if (response.IsSuccessStatusCode)
                return;

            this.errorLogger($"Failed to post. Status code is: '{response.StatusCode}'.");
            string content;
            if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                this.errorLogger($"Message: '{content}'");
        }

        private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            var response = await client.PutAsJsonAsync(url, request, this.jsonOptions, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                this.errorLogger($"Failed to put. Status code is: '{response.StatusCode}'.");
                string content;
                if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                    this.errorLogger($"Message: '{content}'");
                return default;
            }
            return await response.Content.ReadFromJsonAsync<TResponse>(this.jsonOptions, cancellationToken: cancellation);
        }

        private async Task PutAsync<TRequest>(string url, TRequest request, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            var response = await client.PutAsJsonAsync(url, request, this.jsonOptions, cancellation);
            if (response.IsSuccessStatusCode)
                return;

            this.errorLogger($"Failed to put. Status code is: '{response.StatusCode}'.");
            string content;
            if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                this.errorLogger($"Message: '{content}'");
        }
        private async Task<TResponse> PostFormAsync<TResponse>(string url, Dictionary<string, HttpContent> body, CancellationToken cancellation = default)
        {
            var content = new MultipartFormDataContent();
            foreach (var kv in body)
            {
                content.Add(kv.Value, kv.Key);
            }

            using var client = this.clientFactory();
            var response = await client.PostAsync(url, content, cancellation);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content?.ReadAsStringAsync(cancellation);
                return json is null
                    ? default
                    : JsonSerializer.Deserialize<TResponse>(json, this.jsonOptions);
            }

            this.errorLogger($"Failed to post. Status code is: '{response.StatusCode}'.");
            string responseContent;
            if ((responseContent = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                this.errorLogger($"Message: '{responseContent}'");
            return default;
        }

        private async Task<HttpContent> CreateFileContentAsync(string filePath, string mediaType)
        {
            var fileContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = System.IO.Path.GetFileName(filePath)
            };
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return fileContent;
        }
        private HttpContent CreateStringContent(string value) => new StringContent(value);
        #endregion HTTP
    }

    public interface IApiClient
    {
        string BaseUrl { get; }

        Task<Execution> GetExecutionAsync(long id, CancellationToken cancellation = default);
        Task<long?> CheckExecutions(string tenant, string environment, CancellationToken cancellation = default);
        Task SendExecutionLog(long executionId, bool isLastStep, Execution.StepState stepState, CancellationToken cancellation = default);

        Task<Template> GetTemplateAsync(long id, CancellationToken cancellation = default);
        Task<long> CreateTemplateAsync(Template template, CancellationToken cancellation = default);

        Task<VariableSet> GetVariableSetAsync(long setId, CancellationToken cancellation = default);

        Task<File> GetPackageAsync(string fileName, string version = null, CancellationToken cancellation = default);
        Task<long> CreatePackageAsync(string filePath, string version, CancellationToken cancellation = default);
    }
}
