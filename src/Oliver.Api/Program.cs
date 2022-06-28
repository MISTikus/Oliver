using Oliver.Api;

await Host
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

        string workingFolder = Environment.GetEnvironmentVariable("OLI_STORAGE__WORKINGFOLDER");
        string settingsFile = $"{workingFolder}/appsettings.json";
        if (File.Exists(settingsFile))
            c.AddJsonFile(settingsFile);
    })
    .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>())
    .Build()
    .RunAsync();