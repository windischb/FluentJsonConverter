using System.Text.Json;
using PortTunneler;

namespace Tests;

public class FluentJsonConverterConfigTests
{
    [Fact]
    public void Use_Should_Add_Property_Converter()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new NeededConfigConverter());
        
        var json = "{\r\n  \"ServiceName\": \"sql2\",\r\n  \"LocalPort\": 1434,\r\n  \"Destination\": 1433,\r\n  \"Direct\": \"true\"\r\n}";
        var deserializedData = JsonSerializer.Deserialize<NeededServiceConfig>(json, options);

        Assert.Equal("localhost:1433", deserializedData.Destination);
        Assert.Equal(true, deserializedData.Direct);
    }
    //
    // [Fact]
    // public void Ignore_Should_Add_Property_To_Ignored_List()
    // {
    //     // Arrange
    //     var config = new FluentJsonConverterConfig<TestData>();
    //
    //     // Act
    //     var updatedConfig = config.Ignore(c => c.IsActive);
    //
    //     // Assert
    //     updatedConfig.IgnoredProperties.Should().Contain(nameof(TestData.IsActive));
    // }
    //
    // [Fact]
    // public void Configuration_Should_Be_Immutable()
    // {
    //     // Arrange
    //     var baseConfig = new FluentJsonConverterConfig<TestData>()
    //         .Use<TestConverter>(c => c.Name);
    //
    //     // Act
    //     var updatedConfig = baseConfig.Ignore(c => c.Name);
    //
    //     // Assert
    //     baseConfig.PropertyConverters.Should().HaveCount(1); // Original remains unchanged
    //     updatedConfig.IgnoredProperties.Should().Contain(nameof(TestData.Name));
    // }


}