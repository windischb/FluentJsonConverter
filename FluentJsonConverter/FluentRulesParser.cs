using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class FluentRulesParser
{
    public static List<FluentRule> ParseRules(MethodDeclarationSyntax method)
    {
        var rules = new List<FluentRule>();

        if (method.Body != null)
        {
            foreach (var statement in method.Body.Statements)
            {
                if (statement is ExpressionStatementSyntax exprStatement)
                {
                    var rule = ParseFluentRule(exprStatement.Expression);
                    if (rule != null)
                        rules.Add(rule);
                }
            }
        }
        else if (method.ExpressionBody?.Expression is InvocationExpressionSyntax expr)
        {
            var rule = ParseFluentRule(expr);
            if (rule != null)
                rules.Add(rule);
        }

        return rules;
    }

    private static FluentRule? ParseFluentRule(ExpressionSyntax expression)
    {
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var rule = new FluentRule
            {
                MethodName = memberAccess.Name.Identifier.Text,
                Arguments = invocation.ArgumentList.Arguments.Select(arg => arg.ToString()).ToList()
            };

            if (memberAccess.Expression is InvocationExpressionSyntax innerInvocation)
            {
                rule.NestedRules = ParseFluentRule(innerInvocation)?.NestedRules;
            }

            return rule;
        }

        return null;
    }
}

public class FluentRule
{
    public string MethodName { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public List<FluentRule>? NestedRules { get; set; }
}