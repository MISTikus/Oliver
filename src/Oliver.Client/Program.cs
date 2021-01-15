using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oliver.Client.Configurations;
using Oliver.Client.Executing;
using Oliver.Client.Infrastructure;
using Oliver.Client.Listening;
using RestSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Oliver.Client
{
    internal static class Program
    {
        private static async Task Main(string[] args) => await CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configHost => configHost
                .SetBasePath(Debugger.IsAttached ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
                .AddJsonFile("appsettings.json", true, true)
            //.AddEnvironmentVariables(prefix: "PREFIX_")
            )
            .ConfigureServices((hostContext, services) =>
            {
                var logOptions = hostContext.Configuration.GetOptions<LogFile>();
                var serverOptions = hostContext.Configuration.GetOptions<Server>();

                services
                    .ConfigureOptions<Configurations.Client>(hostContext.Configuration)
                    .AddTransient<IRunner, Runner>()
                    .AddTransient<IFileManager, FileManager>()
                    .AddTransient<IRestClient>(s => new RestClient(serverOptions.BaseUrl))
                    .AddSingleton<Executor>() // ToDo: check scope
                    .AddSingleton<ILogSender, LogSender>()
                    .AddSingleton<Func<IExecutor>>(s => () => s.GetRequiredService<Executor>())
                    .AddLogging(c =>
                    {
                        c.AddConsole();
                        if (!args.Contains("--nologs"))
                            c.AddProvider(new FileLoggerProvider(logOptions));
                    });

                services.AddHostedService<Listener>();
            });
        public static T GetOptions<T>(this IConfiguration configuration) where T : new()
        {
            var options = new T();
            configuration.Bind(typeof(T).Name, options);
            return options;
        }

        private static IServiceCollection ConfigureOptions<T>(this IServiceCollection services, IConfiguration configuration)
            where T : class => services
            .Configure<T>(configuration.GetSection(typeof(T).Name));
    }
}
