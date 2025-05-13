namespace AzureCostManagement.Models;

internal class CostSnapshot(double preTaxCost, double totalCost, string date)
{
    public readonly double PreTaxCost = preTaxCost;
    public readonly double TotalCost = totalCost;
    public readonly string Date = date;
}
