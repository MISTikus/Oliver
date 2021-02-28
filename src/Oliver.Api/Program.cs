using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

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
                c.AddEnvironmentVariables("OLI_");

                /*
                 Default args prefixes are: '/', '--', '-'
                 But no boolean switch support
                 Now we can use '?' prefix for boolean switch, ex:
                    Oliver.Api.exe ?nologs
                    SomeWorker.exe ?shouldSendNotification
                 */
                c.AddInMemoryCollection(args.Where(x => x.StartsWith("?")).ToDictionary(x => x[1..], v => "true"));

                var workingFolder = Environment.GetEnvironmentVariable("OLI_STORAGE__WORKINGFOLDER");
                var settingsFile = $"{workingFolder}/appsettings.json";
                if (System.IO.File.Exists(settingsFile))
                    c.AddJsonFile(settingsFile);
            })
            .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());
    }
}
