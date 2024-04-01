using System.IO;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace ErrorCodeSourceGenerator;

internal class GeneratorContext : RuningContext
{
    public GeneratorContext(RuningContext context, XDocument xmlDocument, SourceProductionContext spcContext)
    {
        AssemblyName = context.AssemblyName;
        ProjectDir = context.ProjectDir;
        FilePath = context.FilePath;

        XmlDocument = xmlDocument;
        SpcContext = spcContext;
    }

    public XDocument XmlDocument { get; private set; }
    public SourceProductionContext SpcContext { get; private set; }
}

internal class RuningContext
{
    public string AssemblyName { get; set; }
    public string ProjectDir { get; set; }
    public string FilePath { get; set; }
    public string FileFullName { get { return Path.GetFileName(FilePath); } }
    public string FileNameWithoutExtension
    {
        get { return FileFullName?.Split('.')[0]; }
    }
}
