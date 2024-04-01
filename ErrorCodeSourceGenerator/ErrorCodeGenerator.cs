﻿using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace ErrorCodeSourceGenerator;

[Generator(LanguageNames.CSharp)]
public class ErrorCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //是否启用调试功能
        //#if DEBUG
        //        Debugger.Launch();
        //#endif

        var compilation = context.CompilationProvider.Select(static (c, _) => c);
        var analyzerConfigOptionsProvider = context.AnalyzerConfigOptionsProvider.Select(static (c, _) => c);
        var xmlsProvider = context.AdditionalTextsProvider.Where(o => o.Path.EndsWith(".xml"));

        var combined = xmlsProvider.Combine(compilation).Select(static (item, cancelToken) =>
        {
            var path = item.Left.Path;
            var assemblyName = item.Right.AssemblyName;
            return new RuningContext
            {
                AssemblyName = assemblyName,
                FilePath = path,
            };
        }).Combine(analyzerConfigOptionsProvider).Select(static (item, cancelToken) =>
        {
            item.Right.GlobalOptions.TryGetValue("build_property.projectdir", out string projectDir);
            return new RuningContext
            {
                AssemblyName = item.Left.AssemblyName,
                FilePath = item.Left.FilePath,
                ProjectDir = projectDir,
            };
        });

        context.RegisterSourceOutput(combined, (spa, pair) =>
        {
            var context = LoadGeneratorContext(spa, pair);
            if (context == null)
            {
                return;
            }

            GeneratorCore(context);
        });
    }

    private static GeneratorContext LoadGeneratorContext(SourceProductionContext spcContext, RuningContext runingContext)
    {
        try
        {
            var xmlDocument = XDocument.Load(runingContext.FilePath);
            var generatorContext = new GeneratorContext(runingContext, xmlDocument, spcContext);
            return generatorContext;
        }
        catch
        {
            var invalidXml = new DiagnosticDescriptor("error", "invalid xml found", "file '{0}' is not a valid xml, failed to generate error", "", DiagnosticSeverity.Error, true, null, null, null);
            spcContext.ReportDiagnostic(Diagnostic.Create(invalidXml, Location.None, runingContext.FilePath));
        }

        return null;
    }

    private static void GeneratorCore(GeneratorContext context)
    {
        GeneratorErrorClass(context);
        GeneratorResources(context);
        GeneratorDesignerResources(context);
    }

    private static void GeneratorErrorClass(GeneratorContext context)
    {
        string lang = context.XmlDocument.Root.Element("lang").Value;
        if (!string.IsNullOrWhiteSpace(lang))
        {
            return;
        }

        var errorClassContent = new StringBuilder("// <auto-generated/>\r\n", 512);
        string className = Path.GetFileName(context.FilePath).Split('.')[0];

        errorClassContent.AppendLine($@"namespace {context.AssemblyName}.Errors;
using {context.AssemblyName}.Resources;
public static partial class {className} {{
");
        string lineFormat = @"public static Error {0} {{ get {{ return new Error(""{1}"", {2}); }} }}";

        string codeBase = context.XmlDocument.Root.Element("codeBase").Value;

        foreach (XElement node in context.XmlDocument.Root.Elements("error"))
        {
            var code = node.Attribute("code").Value;
            var name = node.Attribute("name").Value;
            var lineValue = string.Format(lineFormat, name, $"{codeBase}{code}", $"{className}Resources.{name}");
            errorClassContent.AppendLine(lineValue);
        }

        errorClassContent.AppendLine("}");
        var classContent = errorClassContent.ToString();
        var hintName = context.AssemblyName.Replace('.', '_') + "_" + className + "_" + ".g.cs";
        context.SpcContext.AddSource(hintName, classContent);
    }

    private static void GeneratorResources(GeneratorContext context)
    {
        string lang = context.XmlDocument.Root.Element("lang").Value;
        var resourceText = new StringBuilder(512);
        foreach (XElement node in context.XmlDocument.Root.Elements("error"))
        {
            var code = node.Attribute("code").Value;
            var name = node.Attribute("name").Value;
            var desc = node.Value;

            resourceText.AppendLine($@"<data name=""{name}"" xml:space=""preserve""><value><![CDATA[{desc}]]></value></data>");
        }

        var resources = new StringBuilder(@$"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <!--
    Microsoft ResX Schema

    Version 2.0

    The primary goals of this format is to allow a simple XML format
    that is mostly human readable. The generation and parsing of the
    various data types are done through the TypeConverter classes
    associated with the data types.

    Example:

    ... ado.net/XML headers & schema ...
    <resheader name=""resmimetype"">text/microsoft-resx</resheader>
    <resheader name=""version"">2.0</resheader>
    <resheader name=""reader"">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name=""writer"">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name=""Name1""><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name=""Color1"" type=""System.Drawing.Color, System.Drawing"">Blue</data>
    <data name=""Bitmap1"" mimetype=""application/x-microsoft.net.object.binary.base64"">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name=""Icon1"" type=""System.Drawing.Icon, System.Drawing"" mimetype=""application/x-microsoft.net.object.bytearray.base64"">
        <value>[base64 mime encoded string representing a byte array form of the .NET Framework object]</value>
        <comment>This is a comment</comment>
    </data>

    There are any number of ""resheader"" rows that contain simple
    name/value pairs.

    Each data row contains a name, and value. The row also contains a
    type or mimetype. Type corresponds to a .NET class that support
    text/value conversion through the TypeConverter architecture.
    Classes that don't support this are serialized and stored with the
    mimetype set.

    The mimetype is used for serialized objects, and tells the
    ResXResourceReader how to depersist the object. This is currently not
    extensible. For a given mimetype the value must be set accordingly:

    Note - application/x-microsoft.net.object.binary.base64 is the format
    that the ResXResourceWriter will generate, however the reader can
    read any of the formats listed below.

    mimetype: application/x-microsoft.net.object.binary.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.soap.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Soap.SoapFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.bytearray.base64
    value   : The object must be serialized into a byte array
            : using a System.ComponentModel.TypeConverter
            : and then encoded with base64 encoding.
    -->
  <xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
    <xsd:import namespace=""http://www.w3.org/XML/1998/namespace"" />
    <xsd:element name=""root"" msdata:IsDataSet=""true"">
      <xsd:complexType>
        <xsd:choice maxOccurs=""unbounded"">
          <xsd:element name=""metadata"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" use=""required"" type=""xsd:string"" />
              <xsd:attribute name=""type"" type=""xsd:string"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""assembly"">
            <xsd:complexType>
              <xsd:attribute name=""alias"" type=""xsd:string"" />
              <xsd:attribute name=""name"" type=""xsd:string"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""data"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
                <xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" msdata:Ordinal=""1"" />
              <xsd:attribute name=""type"" type=""xsd:string"" msdata:Ordinal=""3"" />
              <xsd:attribute name=""mimetype"" type=""xsd:string"" msdata:Ordinal=""4"" />
              <xsd:attribute ref=""xml:space"" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name=""resheader"">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
              </xsd:sequence>
              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name=""resmimetype"">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name=""version"">
    <value>2.0</value>
  </resheader>
  <resheader name=""reader"">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name=""writer"">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
{resourceText}
</root>
");

        var resourcesDirectory = Path.Combine(Path.GetDirectoryName(context.ProjectDir), "Resources");
        if (!Directory.Exists(resourcesDirectory))
        {
            Directory.CreateDirectory(resourcesDirectory);
        }

        var filePath = Path.Combine(resourcesDirectory, string.IsNullOrWhiteSpace(lang) ? $"{context.FileNameWithoutExtension}Resources.resx" : $"{context.FileNameWithoutExtension}Resources.{lang}.resx");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        File.WriteAllText(filePath, resources.ToString());
    }

    private static void GeneratorDesignerResources(GeneratorContext context)
    {
        string lang = context.XmlDocument.Root.Element("lang").Value;
        if (!string.IsNullOrWhiteSpace(lang))
        {
            return;
        }

        var resourceText = new StringBuilder(512);
        foreach (XElement node in context.XmlDocument.Root.Elements("error"))
        {
            var code = node.Attribute("code").Value;
            var name = node.Attribute("name").Value;
            var desc = node.Value;

            resourceText.AppendLine($@"public static string {name} {{
            get {{
                return ResourceManager.GetString(""{name}"", resourceCulture);
            }}
        }}");
        }

        var className = $"{context.FileNameWithoutExtension}Resources";
        StringBuilder sb = new StringBuilder(512);
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine($"namespace {context.AssemblyName}.Resources");
        sb.AppendLine("{");
        sb.AppendLine("  using System;");
        sb.AppendLine("  [global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"System.Resources.Tools.StronglyTypedResourceBuilder\", \"4.0.0.0\")]");
        sb.AppendLine("  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]");
        sb.AppendLine("  [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]");
        sb.AppendLine($"  public class {className}{{");
        sb.AppendLine("      private static global::System.Resources.ResourceManager resourceMan;");
        sb.AppendLine("      private static global::System.Globalization.CultureInfo resourceCulture;");
        sb.AppendLine("      [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(\"Microsoft.Performance\", \"CA1811:AvoidUncalledPrivateCode\")]");
        sb.AppendLine($"      internal {className}() {{");
        sb.AppendLine("      }");
        sb.AppendLine("      [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]");
        sb.AppendLine("      public static global::System.Resources.ResourceManager ResourceManager {");
        sb.AppendLine("          get {");
        sb.AppendLine("              if (object.ReferenceEquals(resourceMan, null)) {");
        sb.AppendLine("                  global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(\"" + $"{context.AssemblyName}.Resources.{className}\"" + ", typeof(" + className + ").Assembly);");
        sb.AppendLine("                  resourceMan = temp;");
        sb.AppendLine("              }");
        sb.AppendLine("              return resourceMan;");
        sb.AppendLine("          }");
        sb.AppendLine("      }");
        sb.AppendLine("      [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]");
        sb.AppendLine("      public static global::System.Globalization.CultureInfo Culture {");
        sb.AppendLine("          get {");
        sb.AppendLine("              return resourceCulture;");
        sb.AppendLine("          }");
        sb.AppendLine("          set {");
        sb.AppendLine("              resourceCulture = value;");
        sb.AppendLine("          }");
        sb.AppendLine("      }");

        sb.AppendLine(resourceText.ToString());

        sb.AppendLine("  }");
        sb.AppendLine("}");

        string designerCode = sb.ToString();
        var designerClassName = context.AssemblyName.Replace('.', '_') + "_" + className + ".Designer.cs";
        context.SpcContext.AddSource(designerClassName, designerCode);
    }
}