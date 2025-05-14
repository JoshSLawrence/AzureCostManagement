using Azure;
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
    //private Dictionary<string, ResourceGroup> _resourceGroups = [];

    enum QueryResultSchema
    {
        PreTaxCostUSD,
        CostUSD,
        BillingMonth,
        ResourceGroup,
    }

    public void Start()
    {
        _logger.LogInformation("Cost Service has started.");

        var subscriptions = _configuration.GetSection("Subscriptions").Get<List<string>>();

        if (subscriptions is null)
        {
            _logger.LogWarning("No subscriptions loaded from config, exiting");
            return;
        }

        var query = BuildQuery();

        foreach (var sub in subscriptions)
        {
            var data = FetchData(sub, query);
            //ProcessData(sub, data);
        }
    }

    private Response<QueryResult> FetchData(string subscription, QueryDefinition query)
    {
        var sub = new ResourceIdentifier($"/subscriptions/{subscription}");

        _logger.LogInformation("Executing cost management api query");

        Response<QueryResult>? queryResult = null;

        var maxRetries = 4;

        for (var i = 1; i <= maxRetries; i++)
        {
            try
            {
                queryResult = _armClient.UsageQuery(sub, query);
                _logger.LogInformation(queryResult.ToString());
                break;
            }
            catch (RequestFailedException error)
            {
                if (error.ErrorCode == "429" && i <= maxRetries)
                {
                    var sleep = Math.Pow(5000, i);
                    _logger.LogWarning("Requests are being throttled, retrying in {sleep} seconds", sleep);
                    Thread.Sleep((int)sleep);
                }
                else if (error.ErrorCode == "429")
                {

                    _logger.LogError("Retries exhausted, aborting operation");
                    throw new Exception("Retries exhausted, aborting operation", error);
                }
                else
                {
                    _logger.LogError("An error occurred while executing the query: {error.Message}", error.Message);
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

        //if (!ValidateQueryResultSchema(queryResult.Value.Columns))
        //{
        //    var message = "Returned Columns:\n";

        //    foreach (var column in queryResult.Value.Columns)
        //        message += column.Name + "\n";

        //    throw new InvalidQueryResultException(message);
        //}

        foreach (var c in queryResult.Value.Columns)
        {
            _logger.LogInformation("Column Name: {0}, Type: {1}", c.Name, c.QueryColumnType);
        }

        return queryResult;
    }

    private static QueryDefinition BuildQuery()
    {
        var queryDataset = new QueryDataset()
        {
            Granularity = "Monthly"
        };

        //if (mode == "ResourceGroup")
        //{
        //    queryDataset.Grouping.Add(new QueryGrouping("Dimension", "ResourceGroup"));
        //}

        //queryDataset.Aggregation.Add("preTax", new QueryAggregation("PreTaxCost", "Sum"));
        //queryDataset.Aggregation.Add("total", new QueryAggregation("Cost", "Sum"));

        queryDataset.Columns.Add("CostCenter");
        queryDataset.Columns.Add("SubscriptionGuid");
        queryDataset.Columns.Add("SubscriptionName");
        queryDataset.Columns.Add("ResourceGroup");
        queryDataset.Columns.Add("CostInBillingCurrency");

        var query = new QueryDefinition(
            exportType: "Usage",
            timeframe: "Custom",
            dataset: queryDataset
        );

        var now = DateTime.UtcNow;

        query.TimePeriod = new QueryTimePeriod(now.AddDays(-364), now);

        return query;
    }

    //private void ProcessData(string sub, Response<QueryResult> queryResult)
    //{
    //    foreach (var row in queryResult.Value.Rows)
    //    {
    //        var resourceGroupName = row[(int)QueryResultSchema.ResourceGroup].ToString();

    //        if (!_resourceGroups.ContainsKey(resourceGroupName))
    //        {
    //            var resourceGroup = new ResourceGroup(resourceGroupName, sub);
    //            _resourceGroups.Add(resourceGroup.Name, resourceGroup);
    //        }

    //        _resourceGroups[resourceGroupName].CostSnapshots.Add(new CostSnapshot(
    //            preTaxCostUSD: double.Parse(row[(int)QueryResultSchema.PreTaxCostUSD]),
    //            costUSD: double.Parse(row[(int)QueryResultSchema.CostUSD]),
    //            date: row[(int)QueryResultSchema.BillingMonth].ToString()
    //        ));
    //    }

    //    using var writer = new StreamWriter("./report.csv", false);
    //    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    //    csv.WriteRecords(_resourceGroups.Values);
    //}

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
}
