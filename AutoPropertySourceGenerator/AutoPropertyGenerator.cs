using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoPropertySourceGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //是否启用调试功能
        //#if DEBUG
        //        Debugger.Launch();
        //#endif

        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource(
           "AutoPropertyAttribute.g.cs", SourceText.From(AutoPropertySourceGenerationHelper.AutoPropertyAttribute, Encoding.UTF8)));

        var provider = context.SyntaxProvider.CreateSyntaxProvider(
        static (node, _) => IsSyntaxTargetForGeneration(node),
        static (context, _) => GetSemanticTargetForGeneration(context)
        )
        .Where(x => x is not null)
        .Collect();

        context.RegisterImplementationSourceOutput(provider, Emit);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax classSyntax && classSyntax.AttributeLists.Count > 0;

    private static GeneratorContext GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        bool withAutoPropertyAttribute = false;
        // loop through all the attributes on the method
        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            if (withAutoPropertyAttribute)
            {
                break;
            }

            foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
            {
                if (withAutoPropertyAttribute)
                {
                    break;
                }

                IMethodSymbol attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                if (attributeSymbol == null)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                var attributeName = attributeSyntax.Name.ToString();
                if (attributeName == "AutoProperty")
                {
                    withAutoPropertyAttribute = true;
                    break;
                }
            }
        }

        if (!withAutoPropertyAttribute)
        {
            return default;
        }

        var namespaceDeclaration = AutoPropertySourceGenerationHelper.GetNamespace(classDeclarationSyntax);
        var className = classDeclarationSyntax.Identifier.Text;
        var generatorContext = new GeneratorContext()
        {
            NameSpace = namespaceDeclaration,
            ClassName = className,
            Properties = new List<GeneratorProperty>(),
        };

        var fields = classDeclarationSyntax.Members.OfType<FieldDeclarationSyntax>()
            .Where(o => o.AttributeLists.Count > 0);
        foreach (var fieldSyntax in fields)
        {
            bool fieldWithAutoPropertyAttribute = false;
            // loop through all the attributes on the method
            foreach (AttributeListSyntax attributeListSyntax in fieldSyntax.AttributeLists)
            {
                if (fieldWithAutoPropertyAttribute)
                {
                    break;
                }

                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (fieldWithAutoPropertyAttribute)
                    {
                        break;
                    }

                    var constructorArgumentsAndValues = attributeSyntax.ArgumentList?.Arguments
                               .Select(arg => arg.ToString())
                               .ToList() ?? new List<string>();

                    var attributeName = attributeSyntax.Name.ToString();
                    if (attributeName == "AutoProperty")
                    {
                        generatorContext.Properties.Add(new GeneratorProperty
                        {
                            FieldName = fieldSyntax.Declaration.Variables.First().Identifier.Text,
                            FieldType = fieldSyntax.Declaration.Type?.ToString(),
                            Name = constructorArgumentsAndValues.FirstOrDefault()?.Trim('"'),
                            Summary = constructorArgumentsAndValues.LastOrDefault()?.Trim('"'),
                        });
                        fieldWithAutoPropertyAttribute = true;
                        break;
                    }
                }
            }
        }

        return generatorContext;
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<GeneratorContext> generatorContext)
    {
        var group = generatorContext.GroupBy(x => x.FullName);

        foreach (var g in group)
        {
            var fullName = g.Key;
            var fullArray = fullName.Split('-');
            var nameSpace = fullArray[0];
            var className = fullArray[1];

            var properties = g.SelectMany(o => o.Properties).ToList();
            if (!properties.Any())
            {
                continue;
            }

            var generatedSource = Generate(nameSpace, className, g.SelectMany(o => o.Properties).ToList());
            var filename = GetFilename(nameSpace, className);
            context.AddSource(filename, SourceText.From(generatedSource, Encoding.UTF8));
        }
    }

    private static string Generate(string nameSpace, string className, List<GeneratorProperty> properties)
    {
        var buffer = new StringBuilder(512);
        buffer.AppendLine($"namespace {nameSpace};");
        buffer.AppendLine($"public partial class {className}{{");
        foreach (var property in properties)
        {
            var propertyName = property.Name;
            if (string.IsNullOrWhiteSpace(property.Name))
            {
                propertyName = AutoPropertySourceGenerationHelper.ConvertToPropertyName(property.FieldName);
            }
            if (!string.IsNullOrWhiteSpace(property.Summary))
            {
                buffer.AppendLine("/// <summary>");
                buffer.AppendLine($"/// {property.Summary}");
                buffer.AppendLine("/// </summary>");
            }
            buffer.AppendLine(@$"public {property.FieldType} {propertyName}
    {{
        get => {property.FieldName};
        protected set
        {{
            if ({property.FieldName} == value)
            {{
                return;
            }}

            {property.FieldName} = value;
            PropertyChanged();
        }}
    }}");
        }
        buffer.AppendLine($"}}");
        return buffer.ToString();
    }

    private static string GetFilename(string nameSpace, string className)
    {
        var fileName = nameSpace.Replace('.', '_') + className.Replace('.', '_') + ".g.cs";
        return fileName;
    }
}
