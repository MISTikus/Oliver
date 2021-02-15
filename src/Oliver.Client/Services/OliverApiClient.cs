using Microsoft.Extensions.Logging;
using Oliver.Common.Models;
using System;
using System.Net.Http;
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
        private readonly ILogger<OliverApiClient> logger;


        public OliverApiClient(string baseUrl, ApiUrlHelper api, JsonSerializerOptions jsonOptions, ILogger<OliverApiClient> logger)
        {
            BaseUrl = baseUrl;
            this.api = api;
            this.jsonOptions = jsonOptions;
            this.logger = logger;
            this.clientFactory = () => new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

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
        public Task<Template> GetTemplateAsync(long id, CancellationToken cancellation = default)
            => GetAsync<Template>(this.api.Route(x => x.Templates, id), cancellation: cancellation);
        public Task<VariableSet> GetVariableSetAsync(long setId, CancellationToken cancellation = default)
            => GetAsync<VariableSet>(this.api.Route(x => x.Variables, setId), cancellation: cancellation);
        public Task<File> GetArchiveAsync(string fileName, string version = null, CancellationToken cancellation = default)
            => GetAsync<File>(this.api.Route(x => x.Packages, fileName).AddQuery((nameof(version), version)), cancellation: cancellation);

        private async Task<T> GetAsync<T>(string url, int? timeout = default, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            if (timeout.HasValue)
                client.Timeout = TimeSpan.FromSeconds(timeout.Value);
            var json = await client.GetStringAsync(url, cancellation);
            var response = JsonSerializer.Deserialize<T>(json, this.jsonOptions); //await client.GetFromJsonAsync<T>(url, this.jsonOptions, cancellation);
            return response;
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            var response = await client.PostAsJsonAsync(url, request, this.jsonOptions, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError($"Failed to post. Status code is: '{response.StatusCode}'.");
                string content;
                if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                    this.logger.LogError($"Message: '{content}'");
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

            this.logger.LogError($"Failed to post. Status code is: '{response.StatusCode}'.");
            string content;
            if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                this.logger.LogError($"Message: '{content}'");
        }

        private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellation = default)
        {
            using var client = this.clientFactory();
            var response = await client.PutAsJsonAsync(url, request, this.jsonOptions, cancellation);
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError($"Failed to put. Status code is: '{response.StatusCode}'.");
                string content;
                if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                    this.logger.LogError($"Message: '{content}'");
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

            this.logger.LogError($"Failed to put. Status code is: '{response.StatusCode}'.");
            string content;
            if ((content = await response.Content.ReadAsStringAsync(cancellation)) is not null)
                this.logger.LogError($"Message: '{content}'");
        }
    }

    public interface IApiClient
    {
        string BaseUrl { get; }
        Task<Execution> GetExecutionAsync(long id, CancellationToken cancellation = default);
        Task<long?> CheckExecutions(string tenant, string environment, CancellationToken cancellation = default);
        Task SendExecutionLog(long executionId, bool isLastStep, Execution.StepState stepState, CancellationToken cancellation = default);
        Task<Template> GetTemplateAsync(long id, CancellationToken cancellation = default);
        Task<VariableSet> GetVariableSetAsync(long setId, CancellationToken cancellation = default);
        Task<File> GetArchiveAsync(string fileName, string version = null, CancellationToken cancellation = default);
    }
}
