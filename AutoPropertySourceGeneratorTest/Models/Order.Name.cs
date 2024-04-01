namespace AutoPropertySourceGeneratorTest.Models;

using AutoPropertySourceGenerator;
public partial class Order
{
    [AutoProperty]
    private string _name;

    public bool ChangeName()
    {
        Name = string.Empty;
        return true;
    }
}
