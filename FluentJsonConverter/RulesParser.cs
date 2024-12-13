using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FluentJsonConverter;

internal static class RulesParser
{
    public static RulesContainer ParseRules(IMethodSymbol methodSymbol, SemanticModel semanticModel, IEnumerable<IPropertySymbol> allProperties, SourceProductionContext context)
    {
        var rulesContainer = new RulesContainer();

        // Add default rules first
        rulesContainer.AddDefaultRules(allProperties);

        var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
            return rulesContainer;

        // Handle both block and expression-bodied methods
        SyntaxNode? methodBody = methodSyntax.Body ?? (SyntaxNode?)methodSyntax.ExpressionBody;

        if (methodBody == null)
            return rulesContainer;

        foreach (var invocation in methodBody.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;

                if (methodName == "ForProperty" && invocation.ArgumentList.Arguments.Count >= 2)
                {
                    var propertyExpression = invocation.ArgumentList.Arguments[0].Expression;
                    var configureAction = invocation.ArgumentList.Arguments[1].Expression;

                    if (propertyExpression is LambdaExpressionSyntax lambda &&
                        lambda.Body is MemberAccessExpressionSyntax propertyAccess)
                    {
                        var propertyName = propertyAccess.Name.Identifier.Text;

                        var propertySymbol = semanticModel.GetSymbolInfo(propertyAccess).Symbol as IPropertySymbol;
                        var propertyType = propertySymbol?.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "unknown";

                        var configuration = ParseConfiguration(configureAction, propertyName, propertyType, semanticModel, context);

                        rulesContainer.AddOrUpdateRule(new PropertyRule
                        {
                            TargetPropertyName = propertyName,
                            TargetPropertyType = propertyType,
                            JsonPropertyName = configuration.Rename ?? propertyName,
                            Ignore = configuration.Ignore,
                            ReadConverter = configuration.ReadConverter,
                            WriteConverter = configuration.WriteConverter,
                            InlineReadLogic = configuration.InlineReadLogic,
                            InlineWriteLogic = configuration.InlineWriteLogic,
                        });
                    }
                }
                else if (methodName == "Ignore" && invocation.ArgumentList.Arguments.Count == 1)
                {
                    if (invocation.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax lambda &&
                        lambda.Body is MemberAccessExpressionSyntax propertyAccess)
                    {
                        var propertyName = propertyAccess.Name.Identifier.Text;

                        rulesContainer.AddOrUpdateRule(new PropertyRule
                        {
                            TargetPropertyName = propertyName,
                            JsonPropertyName = propertyName,
                            Ignore = true
                        });
                    }
                }
            }
        }

        return rulesContainer;
    }

    private static PropertyConfiguration ParseConfiguration(ExpressionSyntax configureAction, string propertyName, string propertyType, SemanticModel semanticModel,
        SourceProductionContext context)
    {
        var configuration = new PropertyConfiguration();

        if (configureAction is LambdaExpressionSyntax lambda &&
            lambda.Body is InvocationExpressionSyntax invocation)
        {
            var currentInvocation = invocation;

            while (currentInvocation != null)
            {
                if (currentInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = memberAccess.Name.Identifier.Text;

                    switch (methodName)
                    {
                        case "Rename":
                            HandleRename(currentInvocation, configuration);
                            break;

                        case "Ignore":
                            configuration.Ignore = true;
                            break;

                        case "UseConverter":
                            AnalyzeAndValidateConverter(memberAccess, semanticModel, context);
                            HandleUseConverter(memberAccess, configuration);
                            break;

                        case "UseReadConverter":
                            HandleUseReadConverter(memberAccess, configuration);
                            break;

                        case "UseWriteConverter":
                            HandleUseWriteConverter(memberAccess, configuration);
                            break;

                        case "Read":
                            HandleReadLambda(currentInvocation, propertyName, propertyType, configuration);
                            break;

                        case "Write":
                            HandleWriteLambda(currentInvocation, propertyName, propertyType, configuration);
                            break;

                        default:
                            throw new NotSupportedException($"Unsupported method: {methodName}");
                    }

                    currentInvocation = memberAccess.Expression as InvocationExpressionSyntax;
                }
                else
                {
                    break;
                }
            }
        }

        return configuration;
    }

    private static void HandleRename(InvocationExpressionSyntax invocation, PropertyConfiguration configuration)
    {
        var renameArg = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (renameArg?.Expression is LiteralExpressionSyntax literal)
        {
            configuration.Rename = literal.Token.ValueText;
        }
    }

    private static void HandleUseConverter(MemberAccessExpressionSyntax memberAccess, PropertyConfiguration configuration)
    {
        if (memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.FirstOrDefault() is TypeSyntax typeArgument)
        {
            configuration.ReadConverter = typeArgument.ToString();
            configuration.WriteConverter = typeArgument.ToString();
        }
    }

    private static void HandleUseReadConverter(MemberAccessExpressionSyntax memberAccess, PropertyConfiguration configuration)
    {
        if (memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.FirstOrDefault() is TypeSyntax typeArgument)
        {
            configuration.ReadConverter = typeArgument.ToString();
        }
    }

    private static void HandleUseWriteConverter(MemberAccessExpressionSyntax memberAccess, PropertyConfiguration configuration)
    {
        if (memberAccess.Name is GenericNameSyntax genericName &&
            genericName.TypeArgumentList.Arguments.FirstOrDefault() is TypeSyntax typeArgument)
        {
            configuration.WriteConverter = typeArgument.ToString();
        }
    }

    private static void HandleReadLambda(InvocationExpressionSyntax invocation, string propertyName, string propertyType, PropertyConfiguration configuration)
    {
        var readArg = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (readArg?.Expression is LambdaExpressionSyntax readLambda)
        {
            var parameterName = GetLambdaParameterAndBody(readLambda, "reader").ParameterName;

            var logic = GenerateInlineReadHelperMethod(propertyType, propertyName, parameterName, readLambda.Body);
            configuration.InlineReadLogic = logic;
        }
    }

    private static void HandleWriteLambda(InvocationExpressionSyntax invocation, string propertyName, string propertyType, PropertyConfiguration configuration)
    {
        var writeArg = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (writeArg?.Expression is LambdaExpressionSyntax writeLambda)
        {
            var parameterNames = GetWriteLambdaParameterAndBody(writeLambda, "writer").ParameterNames;
            var logic = GenerateInlineWriteHelperMethod(propertyType, propertyName, parameterNames, writeLambda.Body);
            configuration.InlineWriteLogic = logic;
        }
    }

    private static string GenerateInlineReadHelperMethod(string targetPropertyType, string propertyName, string parameterName, SyntaxNode lambdaBody)
    {
        string bodyLogic;

        if (lambdaBody is BlockSyntax block)
        {
            // Extract the logic inside the block directly (removes outer braces)
            bodyLogic = block.Statements.ToFullString();
        }
        else if (lambdaBody is ExpressionSyntax expression)
        {
            // Directly use the expression with a return statement
            bodyLogic = $"return {expression.ToFullString()};";
        }
        else
        {
            throw new NotSupportedException($"Unsupported lambda body type: {lambdaBody.GetType().Name}");
        }

        return $@"
        private {targetPropertyType} Read_{propertyName}(ref Utf8JsonReader {parameterName})
        {{
            {bodyLogic}
        }}
    ".Trim();
    }

    private static string GenerateInlineWriteHelperMethod(string targetPropertyType, string propertyName, List<string> parameterName, SyntaxNode lambdaBody)
    {
        string bodyLogic;

        if (lambdaBody is BlockSyntax block)
        {
            // Extract the logic inside the block directly (removes outer braces)
            bodyLogic = block.Statements.ToFullString();
        }
        else if (lambdaBody is ExpressionSyntax expression)
        {
            // Directly use the expression with a return statement
            bodyLogic = $"return {expression.ToFullString()};";
        }
        else
        {
            throw new NotSupportedException($"Unsupported lambda body type: {lambdaBody.GetType().Name}");
        }

        return $@"
        private void Write_{propertyName}(Utf8JsonWriter {parameterName[0]}, {targetPropertyType} {parameterName[1]})
        {{
            {bodyLogic}
        }}
    ".Trim();
    }

    private static (string ParameterName, string Body) GetLambdaParameterAndBody(LambdaExpressionSyntax lambda, string defaultParameterName)
    {
        string parameterName = lambda switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.Text,
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.FirstOrDefault()?.Identifier.Text ?? defaultParameterName,
            _ => defaultParameterName
        };

        string body = lambda.Body.ToFullString();
        return (parameterName, body);
    }

    private static (List<string> ParameterNames, string Body) GetWriteLambdaParameterAndBody(LambdaExpressionSyntax lambda, string defaultParameterName)
    {
        var parameterNames = new List<string>();

        switch (lambda)
        {
            case SimpleLambdaExpressionSyntax simpleLambda:
            {
                parameterNames.Add(simpleLambda.Parameter.Identifier.Text);
                break;
            }
            case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
            {
                if (parenthesizedLambda.ParameterList.Parameters.Count > 0)
                {
                    foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
                    {
                        parameterNames.Add(parameter.Identifier.Text);
                    }
                }
                else
                {
                    parameterNames.Add(defaultParameterName);
                }


                break;
            }
            default:
            {
                parameterNames.Add(defaultParameterName);
                break;
            }

        }


        string body = lambda.Body.ToFullString();
        return (parameterNames, body);
    }

    private static void AnalyzeAndValidateConverter(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, SourceProductionContext context)
    {
        // Validate the converter and report diagnostics if needed
        var diagnostics = AnalyzeUseConverter(memberAccess.Parent as InvocationExpressionSyntax, semanticModel);
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static IEnumerable<Diagnostic> AnalyzeUseConverter(InvocationExpressionSyntax? invocation, SemanticModel semanticModel)
    {
        var diagnostics = new List<Diagnostic>();

        if (invocation == null) return diagnostics;

        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null)
            return diagnostics;

        var typeArgument = methodSymbol.TypeArguments.FirstOrDefault();
        if (typeArgument == null)
        {
            diagnostics.Add(CreateDiagnostic(invocation, "Invalid UseConverter: Missing type argument."));
            return diagnostics;
        }

        if (typeArgument.IsAbstract)
        {
            diagnostics.Add(CreateDiagnostic(invocation, $"The specified converter '{typeArgument.Name}' is abstract and cannot be used."));
        }

        return diagnostics;
    }

    private static Diagnostic CreateDiagnostic(SyntaxNode node, string message)
    {
        return Diagnostic.Create(
            new DiagnosticDescriptor(
                id: "FLUENT001",
                title: "Invalid Converter",
                messageFormat: message,
                category: "SourceGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            node.GetLocation());
    }
}