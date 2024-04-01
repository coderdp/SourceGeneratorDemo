namespace AutoPropertySourceGeneratorTest.Models;

using AutoPropertySourceGenerator;

[AutoProperty]
public partial class Order : BaseEntity
{
    public string Id { get; protected set; }
    public string OrderNo { get; protected set; }
    public DateTime CreateAt { get; protected set; }

    [AutoProperty("TestRefId", "客户订单Id")]
    private string _refId;

    [AutoProperty]
    private OrderStatus _status;

    public Order(string id)
    {
        Id = id;
        Status = OrderStatus.Draft;
        CreateAt = DateTime.UtcNow;
    }

    public bool ChangeRefId(string refId)
    {
        if (refId == TestRefId)
        {
            return false;
        }

        TestRefId = refId;
        return true;
    }

    public bool Pay()
    {
        if (Status == OrderStatus.Paid)
        {
            return false;
        }

        Status = OrderStatus.Paid;
        return true;
    }
}
