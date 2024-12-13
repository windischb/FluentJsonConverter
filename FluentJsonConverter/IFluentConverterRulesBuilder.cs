using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentJsonConverter;

public interface IFluentJsonConverter<T>
{
    void CreateFluentRules(IFluentConverterRulesBuilder<T> rules);
}
public interface IFluentConverterRulesBuilder<T>
{
    IFluentConverterRulesBuilder<T> ForProperty<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        Action<IPropertyConfigurator<T, TProperty>> configure);

    IFluentConverterRulesBuilder<T> Ignore<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression);
    
}

public delegate TProperty ReadDelegate<out TProperty>(ref Utf8JsonReader reader);

public interface IPropertyConfigurator<T, TProperty>
{
    IPropertyConfigurator<T, TProperty> UseConverter<TConverter>() where TConverter : JsonConverter<TProperty>;
    IPropertyConfigurator<T, TProperty> UseReadConverter<TConverter>() where TConverter : JsonConverter<TProperty>;
    IPropertyConfigurator<T, TProperty> UseWriteConverter<TConverter>() where TConverter : JsonConverter<TProperty>;
    IPropertyConfigurator<T, TProperty> Ignore();
    IPropertyConfigurator<T, TProperty> Rename(string newName);

    IPropertyConfigurator<T, TProperty> Read(ReadDelegate<TProperty> inlineRead);
    IPropertyConfigurator<T, TProperty> Write(Action<Utf8JsonWriter, TProperty> inlineWrite);
}

