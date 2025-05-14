//using Azure;
//using Azure.ResourceManager.CostManagement.Models;
//using AzureCostManagement.Interfaces;
//using CsvHelper.Configuration.Attributes;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//namespace AzureCostManagement.Models;

//internal class ResourceGroup(IConfiguration configuration, ILogger<ResourceGroup> logger, string name, string subscriptionId) : ICostResource
//{
//    IConfiguration _configuration = configuration;
//    ILogger<ResourceGroup> _logger = logger;
//    public string Name { get; } = name;
//    public string SubscriptionName { get; } = subscriptionId;
//    public string SubscriptionId { get; } = subscriptionId;
//    public double AveragePreTaxCost { get; private set; } = default;
//    public double AverageCost { get; private set; } = default;
//    public QueryDefinition Query { get; set; }
//    [Ignore]
//    public List<CostSnapshot> CostSnapshots { get; set; } = [];

//    enum QueryResultSchema
//    {
//        PreTaxCost,
//        Cost,
//        BillingMonth,
//        ResourceGroup,
//    }

//    public bool ValidateQueryResponseSchema(Response<QueryResult> queryResult)
//    {
//        var columns = queryResult.Value.Columns;

//        foreach (var schema in Enum.GetValues<QueryResultSchema>())
//        {
//            if (!(columns[(int)schema].Name == schema.ToString()))
//            {
//                _logger.LogError("Schema validation failed, " +
//                    "Column Name {0} at index {1} did not match {2}",
//                    columns[(int)schema].Name,
//                    (int)schema,
//                    schema.ToString());

//                return false;
//            }
//        }

//        return true;
//    }

//    public void CalculateAverageCost()
//    {
//        double preTaxTotal = default;
//        double total = default;

//        for (var i = 0; i < CostSnapshots.Count; i++)
//        {

//            preTaxTotal += CostSnapshots[i].PreTaxCostUSD;
//            total += CostSnapshots[i].CostUSD;
//        }

//        AveragePreTaxCost = preTaxTotal / (CostSnapshots.Count);
//        AverageCost = total / (CostSnapshots.Count);

//        AveragePreTaxCost = Math.Round(AveragePreTaxCost, 4);
//        AverageCost = Math.Round(AverageCost, 4);
//    }
//}
