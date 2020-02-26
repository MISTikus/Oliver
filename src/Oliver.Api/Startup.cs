using DiskQueue;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Oliver.Api.Configurations;
using Oliver.Api.Extensions;
using Oliver.Common.Models;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Oliver.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var storageOptions = Configuration.GetOptions<QueueStorage>();
            var dbOptions = Configuration.GetOptions<Database>();

            services
                .AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" }))
                .AddSingleton(s => QueueFactory(storageOptions))
                .AddSingleton(s => DbFactory(dbOptions))
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                });
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
        private Func<Instance, IPersistentQueue> QueueFactory(QueueStorage options) =>
            instance => new PersistentQueue(Path.Combine(options.Folder, instance.Tenant, instance.Environment));

        private Func<ILiteDatabase> DbFactory(Database options) => () => new LiteDatabase(options.Path);
    }
}
