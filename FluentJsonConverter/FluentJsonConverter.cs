
namespace FluentJsonConverter;

public interface IFluentJsonConverter<T>
{
    void CreateFluentRules(IFluentConverterRulesBuilder<T> rules);
}