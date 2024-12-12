using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

[Generator]
public class FluentJsonConverterSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Identify candidate classes that implement IFluentJsonConverter<T>
        var candidateClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateClass(node),
                transform: static (context, _) => GetClassSymbolIfImplementsInterface(context))
            .Where(static symbol => symbol is not null)!;

        // Step 2: Combine candidate classes with the Compilation
        var combined = candidateClasses.Combine(context.CompilationProvider);

        // Step 3: Generate code
        context.RegisterSourceOutput(combined, (context, source) =>
        {
            var (classSymbol, compilation) = source;

            if (classSymbol is not INamedTypeSymbol targetClassSymbol)
                return;

            // Step 4: Parse rules and generate code
            GenerateConverterCode(context, targetClassSymbol, compilation);
        });
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: not null };
    }

    private static INamedTypeSymbol? GetClassSymbolIfImplementsInterface(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

        return classSymbol?.AllInterfaces.Any(i =>
            i.Name == "IFluentJsonConverter" && i.TypeArguments.Length == 1) == true
            ? classSymbol
            : null;
    }

    private static void GenerateConverterCode(SourceProductionContext context, INamedTypeSymbol classSymbol, Compilation compilation)
    {
        // Step 1: Get the target type (T) from IFluentJsonConverter<T>
        var fluentInterface = classSymbol.AllInterfaces.First(i =>
            i.Name == "IFluentJsonConverter" && i.TypeArguments.Length == 1);

        var targetType = fluentInterface.TypeArguments[0];

        // Step 2: Identify the "CreateFluentRules" method
        var createFluentRulesMethod = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == "CreateFluentRules");

        if (createFluentRulesMethod == null)
            return;

        // Step 3: Get all properties of the target type
        var allProperties = targetType.GetMembers().OfType<IPropertySymbol>();

        // Step 4: Use RulesParser to populate RulesContainer
        var semanticModel = compilation.GetSemanticModel(createFluentRulesMethod.DeclaringSyntaxReferences.First().SyntaxTree);
        var rulesContainer = RulesParser.ParseRules(createFluentRulesMethod, semanticModel, allProperties);

        // Step 5: Use CodeGenerator to create the Read method
        var readMethod = CodeGenerator.GenerateReadMethod(targetType.ToDisplayString(), rulesContainer);

        // Step 6: Generate the complete converter code
        var generatedCode = GenerateConverterClass(classSymbol, targetType, readMethod);
        //var formattedCode = FormatCode(generatedCode);
        // Step 7: Add the generated source to the context
        context.AddSource($"{classSymbol.Name}_Generated.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
    }

    //private static string FormatCode(string code)
    //{
    //    // Parse the code into a SyntaxTree
    //    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    //    var root = syntaxTree.GetRoot();

    //    // Create a Workspace to enable formatting
    //    var workspace = new AdhocWorkspace();

    //    // Use Roslyn's Formatter to format the syntax tree
    //    var formattedRoot = Formatter.Format(root, workspace);

    //    // Convert the formatted syntax back to a string
    //    return formattedRoot.ToFullString();
    //}

    private static string GenerateConverterClass(INamedTypeSymbol classSymbol, ITypeSymbol targetType, string readMethod)
    {
        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        var className = classSymbol.Name;
        var targetTypeName = targetType.ToDisplayString();

        return $@"
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace {namespaceName}
{{
    public partial class {className} : JsonConverter<{targetTypeName}>
    {{
        {readMethod}

        public override void Write(Utf8JsonWriter writer, {targetTypeName} value, JsonSerializerOptions options)
        {{
            // TODO: Implement Write method
            throw new NotImplementedException();
        }}
    }}
}}
";
    }
}
