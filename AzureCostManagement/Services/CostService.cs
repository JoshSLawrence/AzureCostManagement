using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CostManagement;
using Azure.ResourceManager.CostManagement.Models;
using AzureCostManagement.Exceptions;
using AzureCostManagement.Interfaces;
using AzureCostManagement.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureCostManagement.Services;

public class CostService(IConfiguration configuration, ILogger<CostService> logger, ArmClient armClient) : IService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger _logger = logger;
    private readonly ArmClient _armClient = armClient;
    private Dictionary<string, ResourceGroup> _resourceGroups = new();

    public void Start()
    {
        _logger.LogInformation("Cost Service has started.");

        var subscriptions = _configuration.GetSection("Subscriptions").Get<List<string>>();

        if (subscriptions is null)
        {
            _logger.LogWarning("No subscriptions loaded from config, exiting");
            return;
        }

        var query = QueryBuilder();

        foreach (var sub in subscriptions)
        {
            var data = FetchData(sub, query);
            ProcessData(sub, data);
        }
    }

    private Response<QueryResult> FetchData(string subscription, QueryDefinition query)
    {
        var sub = new ResourceIdentifier($"/subscriptions/{subscription}");

        _logger.LogInformation("Executing cost management api query");

        Response<QueryResult>? queryResult = null;

        for (var i = 0; i < 5; i++)
        {
            try
            {
                queryResult = _armClient.UsageQuery(sub, query);
                break;
            }
            catch (RequestFailedException error)
            {
                if (error.ErrorCode == "429" && i < 4)
                {
                    var sleep = Math.Pow(5000, (i + 1));
                    _logger.LogWarning($"Requests are being throttled, retrying in {sleep} seconds");
                    Thread.Sleep((int)sleep);
                }
                else if (error.ErrorCode == "429")
                {

                    _logger.LogError("Retries exhausted, aborting operation");
                    throw new Exception("Retries exhausted, aborting operation", error);
                }
                else
                {
                    _logger.LogError("An error occurred while executing the query: {0}", error.Message);
                    throw new Exception("An error occurred while executing the query", error);
                }
            }

        }

        if (queryResult == null)
        {
            _logger.LogError("Query result is null");
            throw new Exception("Query result is null");
        }

        if (queryResult.Value.NextLink != null)
        {
            _logger.LogWarning("An additional page was found, but was not included in the dataset");
        }

        if (!ValidateQueryResultSchema(queryResult.Value.Columns))
        {
            var message = "Returned Columns:\n";

            foreach (var column in queryResult.Value.Columns)
                message += column.Name + "\n";

            throw new InvalidQueryResultException(message);
        }

        _logger.LogInformation(queryResult.ToString());

        return queryResult;
    }

    private QueryDefinition QueryBuilder()
    {
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

        return query;
    }

    private void ProcessData(string sub, Response<QueryResult> queryResult)
    {
        foreach (var row in queryResult.Value.Rows)
        {
            var resourceGroupName = row[(int)QueryResultSchema.ResourceGroup].ToString();

            if (!_resourceGroups.ContainsKey(resourceGroupName))
            {
                var resourceGroup = new ResourceGroup(resourceGroupName, sub);
                _resourceGroups.Add(resourceGroup.Name, resourceGroup);
            }

            _resourceGroups[resourceGroupName].costSnapshots.Add(new CostSnapshot(
                preTaxCost: double.Parse(row[(int)QueryResultSchema.PreTaxCost]),
                totalCost: double.Parse(row[(int)QueryResultSchema.CostUSD]),
                date: row[(int)QueryResultSchema.BillingMonth].ToString()
            ));
        }

        var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            NewLine = Environment.NewLine,
        };


        foreach (var group in _resourceGroups.Values)
        {
            group.ProcessAverage();
            var message = string.Empty;
            message += $"Resource Group: {group.Name}\n";
            message += $"Average Cost PreTax: {group.AveragePreTaxCost}\n";
            message += $"Average Cost Total: {group.AverageTotalCost}\n";
            _logger.LogInformation(message);
        }

        using (var writer = new StreamWriter("./report.csv", false))
        using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(_resourceGroups.Values);
        }
    }

    public void Stop()
    {
        _logger.LogInformation("Cost Service has stopped.");
    }

    private bool ValidateQueryResultSchema(IReadOnlyList<QueryColumn> columns)
    {
        foreach (var schema in Enum.GetValues<QueryResultSchema>())
        {
            if (!(columns[(int)schema].Name == schema.ToString()))
            {
                _logger.LogError("Schema validation failed, " +
                    "Column Name {0} at index {1} did not match {2}",
                    columns[(int)schema].Name,
                    (int)schema,
                    schema.ToString());

                return false;
            }
        }

        return true;
    }

    enum QueryResultSchema
    {
        PreTaxCost,
        CostUSD,
        BillingMonth,
        ResourceGroup,
    }
}
