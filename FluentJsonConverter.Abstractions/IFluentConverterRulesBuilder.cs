using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace FluentJsonConverter.Abstractions;

public interface IFluentConverterRulesBuilder<T>
{
    IFluentConverterRulesBuilder<T> For<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        Action<IPropertyConfigurator<T, TProperty>> configure);

    IFluentConverterRulesBuilder<T> Ignore<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression);

    IFluentConverterRulesBuilder<T> Include<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression);

    IFluentConverterRulesBuilder<T> ForAllOtherProperties(
        Action<IAllPropertiesConfigurator> configure);
}


public interface IPropertyConfigurator<T, TProperty>
{
    IPropertyConfigurator<T, TProperty> UseConverter<TConverter>() where TConverter : JsonConverter;
    IPropertyConfigurator<T, TProperty> Ignore();
    IPropertyConfigurator<T, TProperty> Rename(string newName);
}

public interface IAllPropertiesConfigurator
{
    void UseDefaultSerialization();
    void Ignore();
}
