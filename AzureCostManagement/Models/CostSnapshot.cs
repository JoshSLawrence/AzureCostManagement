namespace AzureCostManagement.Models;

public class CostSnapshot(double preTaxCostUSD, double costUSD, string date)
{
    public readonly double PreTaxCost = preTaxCostUSD;
    public readonly double Cost = costUSD;
    public readonly string Date = date;
}
