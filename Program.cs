using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherImageApp.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Register services
        services.AddHttpClient();
        services.AddSingleton<IJobStatusService, JobStatusService>();
        services.AddSingleton<IWeatherService, WeatherService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<ITableStorageService, TableStorageService>();
    })
    .Build();

host.Run();
