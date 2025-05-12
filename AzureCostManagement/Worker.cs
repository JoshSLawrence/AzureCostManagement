using AzureCostManagement.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureCostManagement;

public class Worker(IConfiguration configuration, ILogger<Worker> logger, IService service)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;
    private readonly IService _service = service;

    public void Run()
    {
        _logger.LogInformation("Worker started");
        _service.Start();
        _service.Stop();
    }
}
