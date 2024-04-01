using System.Runtime.CompilerServices;

namespace AutoPropertySourceGeneratorTest.Models;

public class BaseEntity
{
    private readonly ICollection<string> _changedProperties = new HashSet<string>();

    public IEnumerable<string> ChangedProperties => _changedProperties.ToList().AsReadOnly();

    protected void PropertyChanged([CallerMemberName] string propertyName = "")
    {
        _changedProperties.Add(propertyName);
    }
}
