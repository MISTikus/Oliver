using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Oliver.Api
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args)
            .Build()
            .Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(c =>
            {
                c.AddEnvironmentVariables();

                var settingsFolder = Environment.GetEnvironmentVariable("SETTINGS_FOLDER");
                var settingsFile = $"{settingsFolder}/appsettings.json";
                if (System.IO.File.Exists(settingsFile))
                    c.AddJsonFile(settingsFile);
            })
            .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());
    }
}
