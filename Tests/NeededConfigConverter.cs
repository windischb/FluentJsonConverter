using System.Text.Json;
using System.Text.Json.Serialization;
using FluentJsonConverter;
using PortTunneler;

namespace Tests;

public partial class NeededConfigConverter : IFluentJsonConverter<NeededServiceConfig>
{
    public void CreateFluentRules(IFluentConverterRulesBuilder<NeededServiceConfig> rules) =>
        rules
            .ForProperty(x => x.Destination,
                x => x.Read((ref Utf8JsonReader r) =>
                    r.TokenType == JsonTokenType.Number ? $"localhost:{r.GetInt32()}" : r.GetString()!))
            .ForProperty(x => x.Direct,
                x => x.Rename("dir").Read((ref Utf8JsonReader reader) => reader.TokenType == JsonTokenType.String
                    ? bool.Parse(reader.GetString()!)
                    : reader.GetBoolean()))
            .Ignore(x => x.ServiceName);

}