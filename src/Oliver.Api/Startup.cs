using DiskQueue;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Oliver.Api.Configurations;
using Oliver.Api.Extensions;
using Oliver.Api.Middleware.Swashbuckle;
using Oliver.Api.Services;
using Oliver.Common.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Oliver.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var storageOptions = Configuration.GetOptions<Storage>();
            var dbOptions = Configuration.GetOptions<Database>();

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
                    c.OperationFilter<RemoveVersionFromParameter>();
                    c.DocumentFilter<ReplaceVersionWithExactValueInPath>();
                    c.DocInclusionPredicate((version, desc) =>
                    {
                        if (!desc.TryGetMethodInfo(out var methodInfo))
                            return false;
                        var versions = methodInfo.DeclaringType
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
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                })
                .Services
                .AddApiVersioning(c =>
                {
                    c.ReportApiVersions = true;
                    c.DefaultApiVersion = new ApiVersion(1, 0);
                })
                ;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseSwagger(c => c.RouteTemplate = "api/metadata/{documentName}/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "api/metadata";
                c.SwaggerEndpoint("v1/swagger.json", "API");
            });

            app.UseHttpsRedirection()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints => endpoints.MapControllers())
            ;
        }
        private Func<Instance, IPersistentQueue> QueueFactory(string dataStorageFolder) =>
            instance => new PersistentQueue(Path.Combine(dataStorageFolder, instance.Tenant, instance.Environment));

        private Func<ILiteDatabase> DbFactory(Database options) => () => new LiteDatabase(options.Path);
    }
}
