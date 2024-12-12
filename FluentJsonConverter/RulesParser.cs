using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;
using FluentJsonConverter.ExtensionMethods;

internal static class RulesParser
{
    public static RulesContainer ParseRules(IMethodSymbol methodSymbol, SemanticModel semanticModel, IEnumerable<IPropertySymbol> allProperties)
    {
        var rulesContainer = new RulesContainer();

        // Add default rules first
        rulesContainer.AddDefaultRules(allProperties);

        var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
            return rulesContainer;

        foreach (var invocation in methodSyntax.Body?.DescendantNodes().OfType<InvocationExpressionSyntax>() ?? Enumerable.Empty<InvocationExpressionSyntax>())
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

                        var configuration = ParseConfiguration(configureAction, propertyName, propertyType);

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

    private static PropertyConfiguration ParseConfiguration(ExpressionSyntax configureAction,string propertyName, string propertyType)
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

                    if (methodName == "Rename")
                    {
                        var renameArg = currentInvocation.ArgumentList.Arguments.FirstOrDefault();
                        if (renameArg?.Expression is LiteralExpressionSyntax literal)
                        {
                            configuration.Rename = literal.Token.ValueText;
                        }
                    }
                    else if (methodName == "Ignore")
                    {
                        configuration.Ignore = true;
                    }
                    else if (methodName == "UseConverter")
                    {
                       

                        if (memberAccess.Name is GenericNameSyntax genericName &&
                            genericName.TypeArgumentList.Arguments.FirstOrDefault() is TypeSyntax typeArgument)
                        {
                            configuration.ReadConverter = typeArgument.ToString();
                            configuration.WriteConverter = typeArgument.ToString();
                        }
                    }
                    else if (methodName == "UseReadConverter")
                    {
                        if (memberAccess.Name is GenericNameSyntax genericName &&
                            genericName.TypeArgumentList.Arguments.FirstOrDefault() is TypeSyntax typeArgument)
                        {
                            configuration.ReadConverter = typeArgument.ToString();
                        }
                    }
                    else if (methodName == "UseWriteConverter")
                    {
                        if (memberAccess.Name is GenericNameSyntax genericName &&
                            genericName.TypeArgumentList.Arguments.FirstOrDefault() is TypeSyntax typeArgument)
                        {
                            configuration.WriteConverter = typeArgument.ToString();
                        }
                    }
                    else if (methodName == "Read")
                    {
                       

                            var readArg = currentInvocation.ArgumentList.Arguments.FirstOrDefault();
                        if (readArg?.Expression is LambdaExpressionSyntax readLambda)
                        {
                            string parameterName = readLambda switch
                            {
                                SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.Identifier.Text,
                                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters.FirstOrDefault()?.Identifier.Text ?? "reader",
                                _ => "reader"
                            };

                                // Replace the parameter name with "reader" in the lambda body
                                var logic = GenerateInlineReadHelperMethod(propertyType, propertyName, parameterName, readLambda.Body.ToString());

                                // Wrap the logic in a complete method body
                                configuration.InlineReadLogic = logic;
                            
                        }
                    }
                    else if (methodName == "Write")
                    {
                        var readArg = currentInvocation.ArgumentList.Arguments.FirstOrDefault();
                        if (readArg?.Expression is LambdaExpressionSyntax readLambda)
                        {
                            // Extract the body of the lambda expression
                            configuration.InlineWriteLogic = readLambda.Body.ToString();
                        }
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

    private static string GenerateInlineReadHelperMethod(string targetPropertyType, string propertyName, string parameterName, string inlineLogic)
    {
        return $@"
            private {targetPropertyType} Read_{propertyName}(ref Utf8JsonReader {parameterName})
            {inlineLogic}
            ";
    }

    private static IEnumerable<Diagnostic> AnalyzeUseConverter(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var diagnostics = new List<Diagnostic>();

        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null || methodSymbol.Name != "UseConverter")
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
        
        if (!typeArgument.InheritsFrom(typeof(JsonConverter<>)))
        {
            diagnostics.Add(CreateDiagnostic(invocation, $"The specified converter '{typeArgument.Name}' does not inherit from JsonConverter<T>."));
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
