using AzureCostManagement.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureCostManagement.Models;

internal class CostResource(IConfiguration configuration, ILogger<CostResource> logger) : ICostResource
{
    public IConfiguration _configuration { get; set; } = configuration;
    public ILogger _logger { get; set; } = logger;
    public required string Name { get; set; }
    public required string ResourceGroupName { get; set; }
    public required string SubscriptionName { get; set; }
    public required string SubscriptionId { get; set; }
    public double AveragePreTaxCost { get; set; } = default;
    public double AverageCost { get; set; } = default;
    public List<CostSnapshot> CostSnapshots { get; set; } = [];

    public virtual void CalculateAverageCost()
    {
        _logger.LogDebug("Calculating average cost for resource: {Name} in subscription: {SubscriptionId}", Name, SubscriptionId);

        double preTaxTotal = default;
        double total = default;

        for (var i = 0; i < CostSnapshots.Count; i++)
        {
            preTaxTotal += CostSnapshots[i].PreTaxCost;
            total += CostSnapshots[i].Cost;
        }

        AveragePreTaxCost = preTaxTotal / (CostSnapshots.Count);
        AverageCost = total / (CostSnapshots.Count);

        AveragePreTaxCost = Math.Round(AveragePreTaxCost, 4);
        AverageCost = Math.Round(AverageCost, 4);
    }
}
