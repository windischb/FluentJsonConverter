namespace FluentJsonConverter;

internal class PropertyRule
{
    public string TargetPropertyType { get; set; }
    public string TargetPropertyName { get; set; }
    public string? JsonPropertyName { get; set; }
    public bool Ignore { get; set; }
    public string? ReadConverter { get; set; }
    public string? WriteConverter { get; set; }
    public string? InlineReadLogic { get; set; }
    public string? InlineWriteLogic { get; set; }
}

internal class PropertyConfiguration
{
    public string? Rename { get; set; }
    public bool Ignore { get; set; }
    public string? ReadConverter { get; set; } // ForProperty UseReadConverter
    public string? WriteConverter { get; set; } // ForProperty UseWriteConverter
    public string? InlineReadLogic { get; set; } // ForProperty Read method
    public string? InlineWriteLogic { get; set; } // ForProperty Write method
}