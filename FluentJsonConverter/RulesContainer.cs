using Microsoft.CodeAnalysis;

namespace FluentJsonConverter;

internal class RulesContainer
{
    private readonly Dictionary<string, PropertyRule> _rules;

    public RulesContainer()
    {
        _rules = new Dictionary<string, PropertyRule>();
    }

    public void AddOrUpdateRule(PropertyRule rule)
    {
        if (!_rules.ContainsKey(rule.TargetPropertyName))
        {
            _rules[rule.TargetPropertyName] = rule;
        }
        else
        {
            var existingRule = _rules[rule.TargetPropertyName];

            // Resolve conflicts here (later rules override earlier ones)
            existingRule.JsonPropertyName = rule.JsonPropertyName ?? existingRule.JsonPropertyName;
            existingRule.Ignore = rule.Ignore;
            existingRule.ReadConverter = rule.ReadConverter ?? existingRule.ReadConverter;
            existingRule.WriteConverter = rule.WriteConverter ?? existingRule.WriteConverter;
            existingRule.InlineReadLogic = rule.InlineReadLogic ?? existingRule.InlineReadLogic;
            existingRule.InlineWriteLogic = rule.InlineWriteLogic ?? existingRule.InlineWriteLogic;

            _rules[rule.TargetPropertyName] = existingRule;
        }
    }

    public bool ContainsRule(string propertyName) => _rules.ContainsKey(propertyName);

    public IEnumerable<PropertyRule> GetRules() => _rules.Values;

    public void AddDefaultRules(IEnumerable<IPropertySymbol> allProperties)
    {
        foreach (var property in allProperties)
        {
            if (!_rules.ContainsKey(property.Name))
            {
                _rules[property.Name] = new PropertyRule
                {
                    TargetPropertyName = property.Name,
                    TargetPropertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    JsonPropertyName = property.Name,
                    Ignore = false, // Default behavior
                    
                };
            }
        }
    }
}