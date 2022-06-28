using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oliver.Client.Configurations;
using Oliver.Client.Executing;
using Oliver.Client.Services;
using Oliver.Common.Extensions;
using Oliver.Common.Infrastructure;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configHost => configHost
        .SetBasePath(Debugger.IsAttached
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(Environment.ProcessPath)) // Process.GetCurrentProcess().MainModule.FileName
        .AddJsonFile("appsettings.json", true, true)
    )
    .ConfigureServices((hostContext, services) =>
    {
        LogFile logOptions = hostContext.Configuration.GetOptions<LogFile>();
        Server serverOptions = hostContext.Configuration.GetOptions<Server>();

        services
            .Configure<Client>(hostContext.Configuration.GetSection(typeof(Client).Name))
            .AddTransient<IRunner, Runner>()
            .AddTransient<IFileManager, FileManager>()
            .AddSingleton(s =>
            {
                JsonSerializerOptions opts = new();
                opts.Converters.Add(new JsonStringEnumConverter());
                opts.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                opts.PropertyNameCaseInsensitive = true;
                return opts;
            })
            .AddSingleton<IApiClient>(s => new OliverApiClient(
                serverOptions.BaseUrl,
                new ApiUrlHelper(serverOptions.ApiVersion),
                s.GetService<JsonSerializerOptions>(),
                m => s.GetService<ILogger<OliverApiClient>>().LogError(m)))
            .AddSingleton<ILogSender, LogSender>()
            .AddSingleton<Executor>()
            .AddSingleton<Func<IExecutor>>(s => () => s.GetRequiredService<Executor>())
            .AddLogging(c =>
            {
                c.AddConsole();
                if (!args.Contains("?nologs"))
                    c.AddProvider(new FileLoggerProvider(logOptions));
            });

        services.AddHostedService<Listener>();
    })
    .Build()
    .RunAsync();