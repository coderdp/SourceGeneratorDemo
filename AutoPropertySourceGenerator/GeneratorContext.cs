using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace AutoPropertySourceGenerator;

public class GeneratorContext
{
    public SourceProductionContext SpcContext { get; private set; }
    public List<GeneratorProperty> Properties { get; set; }
    public string NameSpace { get; set; }
    public string ClassName { get; set; }
    public string FullName => $"{NameSpace}-{ClassName}";
}

public class GeneratorProperty
{
    public string FieldName { get; set; }
    public string FieldType { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
}
