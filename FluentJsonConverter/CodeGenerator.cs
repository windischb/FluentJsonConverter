using System.Text;

internal static class CodeGenerator
{
    public static string GenerateReadMethod(string targetTypeName, RulesContainer rulesContainer)
    {
        var readMethod = new StringBuilder();
        var helperMethods = new StringBuilder();

        readMethod.AppendLine($"public override {targetTypeName} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        readMethod.AppendLine("{");
        readMethod.AppendLine("    if (reader.TokenType != JsonTokenType.StartObject)");
        readMethod.AppendLine("        throw new JsonException(\"Expected StartObject token.\");");
        readMethod.AppendLine();
        readMethod.AppendLine($"    var result = new {targetTypeName}();");
        readMethod.AppendLine();
        readMethod.AppendLine("    while (reader.Read())");
        readMethod.AppendLine("    {");
        readMethod.AppendLine("        if (reader.TokenType == JsonTokenType.EndObject)");
        readMethod.AppendLine("            return result;");
        readMethod.AppendLine();
        readMethod.AppendLine("        if (reader.TokenType != JsonTokenType.PropertyName)");
        readMethod.AppendLine("            throw new JsonException(\"Expected PropertyName token.\");");
        readMethod.AppendLine();
        readMethod.AppendLine("        var propertyName = reader.GetString();");
        readMethod.AppendLine("        if (!reader.Read())");
        readMethod.AppendLine(
            "            throw new JsonException(\"Unexpected end of JSON while reading property value.\");");
        readMethod.AppendLine();
        readMethod.AppendLine("        switch (propertyName)");
        readMethod.AppendLine("        {");

        foreach (var rule in rulesContainer.GetRules())
        {
            if (rule.Ignore)
            {
                readMethod.AppendLine($"            case \"{rule.JsonPropertyName}\":");
                readMethod.AppendLine("                reader.Skip();");
                readMethod.AppendLine("                break;");
            }
            else if (!string.IsNullOrEmpty(rule.InlineReadLogic))
            {
                helperMethods.AppendLine(rule.InlineReadLogic);
                readMethod.AppendLine($"            case \"{rule.JsonPropertyName}\":");
                readMethod.AppendLine($"                result.{rule.TargetPropertyName} = Read_{rule.TargetPropertyName}(ref reader);");
                readMethod.AppendLine("                break;");
            }
            else if (!string.IsNullOrEmpty(rule.ReadConverter))
            {
                readMethod.AppendLine($"            case \"{rule.JsonPropertyName}\":");
                readMethod.AppendLine($"                var custom_{rule.TargetPropertyName}_ReadConverter = new {rule.ReadConverter}();");
                readMethod.AppendLine($"                result.{rule.TargetPropertyName} = custom_{rule.TargetPropertyName}_ReadConverter.Read(ref reader, typeof({rule.TargetPropertyType}), options);");
                readMethod.AppendLine("                break;");
            }
            else
            {
                readMethod.AppendLine($"            case \"{rule.JsonPropertyName}\":");
                readMethod.AppendLine($"                result.{rule.TargetPropertyName} = JsonSerializer.Deserialize<{rule.TargetPropertyType}>(ref reader, options);");
                readMethod.AppendLine("                break;");
            }
        }

        readMethod.AppendLine("            default:");
        readMethod.AppendLine("                reader.Skip(); // Skip unknown properties");
        readMethod.AppendLine("                break;");
        readMethod.AppendLine("        }");
        readMethod.AppendLine("    }");
        readMethod.AppendLine();
        readMethod.AppendLine("    throw new JsonException(\"Unexpected end of JSON: Missing closing object brace.\");");
        readMethod.AppendLine("}");

        return helperMethods.ToString() + readMethod.ToString();
    }
}
