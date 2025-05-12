using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CostManagement;
using Azure.ResourceManager.CostManagement.Models;
using AzureCostManagement.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureCostManagement.Services;

public class CostService(IConfiguration configuration, ILogger<CostService> logger, ArmClient armClient) : IService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;
    private readonly ArmClient _armClient = armClient;

    public void Start()
    {
        _logger.LogInformation("Cost Service has started.");

        // TODO: Get the subscription ID from the configuration, prompt, or args 
        // NOTE: This is a placeholder. You need to replace it with the actual subscription ID until the above is implemented.
        var sub = new ResourceIdentifier("");

        var queryDataset = new QueryDataset()
        {
            Granularity = "Monthly"
        };

        queryDataset.Aggregation.Add("preTax", new QueryAggregation("PreTaxCost", "Sum"));
        queryDataset.Aggregation.Add("total", new QueryAggregation("CostUSD", "Sum"));
        queryDataset.Grouping.Add(new QueryGrouping("Dimension", "ResourceGroup"));

        var query = new QueryDefinition(
            exportType: "Usage",
            timeframe: "Custom",
            dataset: queryDataset
        );

        var now = DateTime.UtcNow;

        query.TimePeriod = new QueryTimePeriod(now.AddDays(-364), now);

        _logger.LogInformation("Executing cost management api query");

        var queryResult = _armClient.UsageQuery(sub, query);

        _logger.LogInformation(queryResult.ToString());

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var message = "Available columns: ";

        foreach (var col in queryResult.Value.Columns)
        {
                message += $"{col.Name}, ";
        }

        foreach (var row in queryResult.Value.Rows)
        {
                message = string.Empty;

            for (var i = 0; i < queryResult.Value.Columns.Count; i++)
            {
                    message += $"{queryResult.Value.Columns[i].Name}: {row[i].ToString()} ";
                }

                _logger.LogDebug(message);
            }
            }

        if (queryResult.Value.NextLink != null)
        {
            _logger.LogWarning("An additional page was found, but was not included in the dataset");
        }
    }

    public void Stop()
    {
        _logger.LogInformation("Cost Service has stopped.");
    }
}
