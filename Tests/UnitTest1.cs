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
        Assert.Equal(null, deserializedData.ServiceName);
    }
  
}