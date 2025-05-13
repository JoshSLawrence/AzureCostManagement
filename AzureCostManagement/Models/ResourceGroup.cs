using AzureCostManagement.Interfaces;
using CsvHelper.Configuration.Attributes;

namespace AzureCostManagement.Models;

internal class ResourceGroup(string name, string subscriptionId) : ICostResource
{
    public string SubscriptionId { get; } = subscriptionId;
    public string Name { get; } = name;
    [Ignore]
    public List<CostSnapshot> costSnapshots { get; set; } = [];
    public double AveragePreTaxCost { get; private set; } = default;
    public double AverageTotalCost { get; private set; } = default;

    public void ProcessAverage()
    {
        double preTaxTotal = default;
        double total = default;

        for (var i = 0; i < costSnapshots.Count; i++)
        {

            preTaxTotal += costSnapshots[i].PreTaxCost;
            total += costSnapshots[i].TotalCost;
        }

        AveragePreTaxCost = preTaxTotal / (costSnapshots.Count);
        AverageTotalCost = total / (costSnapshots.Count);

        AveragePreTaxCost = Math.Round(AveragePreTaxCost, 2);
        AverageTotalCost = Math.Round(AverageTotalCost, 2);
    }
}
