using Azure.Identity;
using Azure.ResourceManager;
using AzureCostManagement.Interfaces;
using AzureCostManagement.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;

namespace AzureCostManagement;

public class Program
{
    public static void Main(string[] args)
    {
        var options = new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
        };
        var credential = new DefaultAzureCredential(options);
        var host = HostBuilder(args, credential).Build();
        var worker = ActivatorUtilities.CreateInstance<Worker>(host.Services);
        worker.Run();
    }

    public static IHostBuilder HostBuilder(string[] args, DefaultAzureCredential credential)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                        optional: true, reloadOnChange: true);

            })
            .ConfigureServices((context, services) =>
        {
            services.AddScoped<IService, CostService>();
            services.AddSingleton(new GraphServiceClient(credential));
            services.AddSingleton(new ArmClient(credential));
        });
    }
}
