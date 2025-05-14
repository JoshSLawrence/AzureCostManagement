using AzureCostManagement.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureCostManagement.Interfaces;

internal interface ICostResource
{
    IConfiguration _configuration { get; set; }
    ILogger _logger { get; set; }
    string Name { get; set; }
    string ResourceGroupName { get; set; }
    string SubscriptionName { get; set; }
    string SubscriptionId { get; set; }
    double AveragePreTaxCost { get; set; }
    double AverageCost { get; set; }
    List<CostSnapshot> CostSnapshots { get; set; }
    void CalculateAverageCost();
}
