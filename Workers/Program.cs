using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Services.Interfaces;
using Server.Services;
using Model;
using Microsoft.EntityFrameworkCore;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddDbContext<WootComputersSourceContext>(options =>
            options.UseSqlServer(context.Configuration["AZURE_SQL_CONNECTIONSTRING"]));

        services.AddScoped<WootService>();
        services.AddScoped<IWootClient, WootClient>();
        services.AddHttpClient<WootClient>();

        services.AddLogging();
    })
    .Build();

host.Run();
