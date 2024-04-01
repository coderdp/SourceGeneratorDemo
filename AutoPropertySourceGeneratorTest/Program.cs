using AutoPropertySourceGeneratorTest.Models;

namespace AutoPropertySourceGeneratorTest;

internal class Program
{
    static void Main(string[] args)
    {
        var order = new Order("1");
        var hasChange = order.ChangeRefId("240401001");
        if (hasChange)
        {
            PrintChangedProperties(order);
        }

        var order2 = new Order("2");
        var hasChange2 = order2.ChangeRefId("240401002");
        hasChange2 |= order2.Pay();
        if (hasChange2)
        {
            PrintChangedProperties(order2);
        }

        Console.WriteLine("end");
        Console.ReadLine();
    }

    private static void PrintChangedProperties(Order order)
    {
        Console.WriteLine($"order id :{order.Id}");
        foreach (var changedProperty in order.ChangedProperties)
        {
            Console.WriteLine($"changed: {changedProperty}");
        }
    }
}
