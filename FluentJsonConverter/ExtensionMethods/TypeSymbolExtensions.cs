using Microsoft.CodeAnalysis;

namespace FluentJsonConverter.ExtensionMethods;

public static class TypeSymbolExtensions
{
    public static bool InheritsFrom(this ITypeSymbol typeSymbol, Type baseType)
    {
        var current = typeSymbol;
        while (current != null)
        {
            if (current.OriginalDefinition.ToDisplayString() == baseType.FullName)
                return true;
            current = current.BaseType;
        }
        return false;
    }
}