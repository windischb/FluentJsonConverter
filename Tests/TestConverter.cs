using System.Text.Json;
using System.Text.Json.Serialization;
using FluentJsonConverter;
using PortTunneler;

namespace Tests;

public partial class NeededConfigConverter : IFluentJsonConverter<NeededServiceConfig>
{
    public void CreateFluentRules(IFluentConverterRulesBuilder<NeededServiceConfig> rules)
    {
        rules
            .ForProperty(x => x.Destination, x => x.Read((ref Utf8JsonReader r) =>
            {
                if (r.TokenType == JsonTokenType.Number)
                {
                    return $"localhost:{r.GetInt32()}";
                }

                return r.GetString();
            }))
            .ForProperty(x => x.Direct, x => x.Read((ref Utf8JsonReader reader) =>
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return bool.Parse(reader.GetString());
                }

                return reader.GetBoolean();
            }))
            ;

    }

}