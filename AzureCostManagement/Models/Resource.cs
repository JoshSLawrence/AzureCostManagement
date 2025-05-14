//using AzureCostManagement.Interfaces;

//namespace AzureCostManagement.Models;

//public class Resource(string name, string subscriptionId) : ICostResource
//{
//    public string Name { get; } = name;
//    public string SubscriptiondId { get; } = subscriptionId;
//    public List<CostSnapshot> CostSnapshots { get; set; } = [];
//    public double AveragePreTaxCostUSD { get; private set; } = default;
//    public double AverageCostUSD { get; private set; } = default;

//    public void ProcessAverage()
//    {
//        double preTaxTotal = default;
//        double total = default;

//        for (var i = 0; i < CostSnapshots.Count; i++)
//        {

//            preTaxTotal += CostSnapshots[i].PreTaxCostUSD;
//            total += CostSnapshots[i].CostUSD;
//        }

//        AveragePreTaxCostUSD = preTaxTotal / (CostSnapshots.Count);
//        AverageCostUSD = total / (CostSnapshots.Count);

//        AveragePreTaxCostUSD = Math.Round(AveragePreTaxCostUSD, 2);
//        AverageCostUSD = Math.Round(AverageCostUSD, 2);
//    }
//}
