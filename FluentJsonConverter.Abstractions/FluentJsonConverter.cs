
namespace FluentJsonConverter.Abstractions;

public interface IFluentJsonConverter<T>
{
    void CreateFluentRules(IFluentConverterRulesBuilder<T> rules);
}