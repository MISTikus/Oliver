using DiskQueue;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Oliver.Api.Configurations;
using Oliver.Api.Middleware.Swashbuckle;
using Oliver.Api.Services;
using Oliver.Common.Extensions;
using Oliver.Common.Infrastructure;
using Oliver.Common.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

namespace Oliver.Api;

public class Startup
{
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        LogFile logOptions = Configuration.GetOptions<LogFile>();
        Storage storageOptions = Configuration.GetOptions<Storage>();
        Database dbOptions = Configuration.GetOptions<Database>();

        services
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
                c.OperationFilter<RemoveVersionFromParameter>();
                c.DocumentFilter<ReplaceVersionWithExactValueInPath>();
                c.DocInclusionPredicate((version, desc) =>
                {
                    if (!desc.TryGetMethodInfo(out System.Reflection.MethodInfo methodInfo))
                        return false;
                    IEnumerable<ApiVersion> versions = methodInfo.DeclaringType
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(a => a.Versions);
                    return versions.Any(v => $"v{v}" == version);
                });
            })
            .AddSingleton(s => QueueFactory(storageOptions.QueuesFolder))
            .AddSingleton(s => DbFactory(dbOptions))
            .AddSingleton<IBlobStorage>(c => new FileSystemStorage(storageOptions.BlobFolder))
            .AddSingleton<Func<IBlobStorage>>(c => () => c.GetRequiredService<IBlobStorage>())
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            })
            .Services
            .AddApiVersioning(c =>
            {
                c.ReportApiVersions = true;
                c.DefaultApiVersion = new ApiVersion(1, 0);
            })
            .AddLogging(c =>
            {
                c.AddConsole();
                if (!Configuration.GetValue<bool>("nologs"))
                    c.AddProvider(new FileLoggerProvider(logOptions));
            })
            ;
    }

    public void Configure(IApplicationBuilder app,
        IWebHostEnvironment env,
        ILoggerFactory logFactory)
    {
        ILogger<Startup> logger = logFactory.CreateLogger<Startup>();
        logger.LogInformation("Starting application...");
        logger.LogInformation("Initialize...");

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseSwagger(c => c.RouteTemplate = "api/metadata/{documentName}/swagger.json");
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "api/metadata";
            c.SwaggerEndpoint("v1/swagger.json", "API");
        });


        app.UseHttpsRedirection()
            .UseStaticFiles()
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            })
        ;
        logger.LogInformation("Application initialized...");
    }
    private static Func<Instance, IPersistentQueue> QueueFactory(string dataStorageFolder) =>
        instance => new PersistentQueue(Path.Combine(dataStorageFolder, instance.Tenant, instance.Environment));

    private static Func<ILiteDatabase> DbFactory(Database options) => () => new LiteDatabase(options.Path);
}
