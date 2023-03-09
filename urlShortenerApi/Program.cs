using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((b) =>
    {
        b.AddScoped<DatabaseSessionFactory>();
        b.AddScoped<ShortUrlGenerator>();
        b.AddScoped<RandomGenerator>();

        var config = new ConfigurationBuilder()
                    .AddJsonFile("host.json")
                    .AddEnvironmentVariables()
                    .Build();

        b.Configure<DatabaseConnectionConfigurations>(config.GetSection("database"));
    })
    .Build();

host.Run();

