# FluentJsonConverter

FluentJsonConverter is a **source generator** designed to simplify and enhance the customization of JSON serialization and deserialization using a fluent API. It integrates seamlessly with `System.Text.Json` to provide a clear and maintainable way to define rules for JSON properties.

---

## **Features**

- Configure custom JSON serialization and deserialization rules for individual properties.
- Define default behavior, custom converters, and inline read/write logic.
- Skip or rename properties during serialization.
- Supports both compile-time rule validation and runtime performance optimization.
- Helps reduce boilerplate code for `JsonConverter`.

---

## **Installation**

Install the NuGet package:

```bash
dotnet add package FluentJsonConverter
```

---

## **Why Use FluentJsonConverter?**

- **Maintainability**: Define JSON rules in a centralized, fluent manner.
- **Flexibility**: Easily configure advanced scenarios like:
    - Custom converters.
    - Inline read/write logic.
    - Skipping or renaming properties.
- **Performance**: Leverages `System.Text.Json` for efficient serialization.
- **Type Safety**: Compile-time validation ensures correct usage.

---

## **Getting Started**

### 1. Define Your Model

Create your data model that needs serialization:

```csharp
public class ExampleModel {
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public string? CustomProperty { get; set; }
}
```

### 2. Implement `IFluentJsonConverter`

Create a partial class and implement the `IFluentJsonConverter<T>` interface for your model:

```csharp
public partial class ExampleModelConverter : IFluentJsonConverter<ExampleModel>
{
    public void CreateFluentRules(IFluentConverterRulesBuilder<ExampleModel> rules)
    {
        rules
            .ForProperty(x => x.Id, x => x.Rename("identifier"))
            .ForProperty(x => x.Name, x => x.UseConverter<MyCustomStringConverter>())
            .Ignore(x => x.CustomProperty);
    }
}
```


### 3. Register the Converter

Register the generated converter in your `JsonSerializerOptions`:

```csharp
var options = new JsonSerializerOptions
{
    Converters =
    {
        new ExampleModelConverter()
    }
};

var json = JsonSerializer.Serialize(new ExampleModel { Id = 1, Name = "Test", IsActive = true }, options);
```

---

## **Key API**

### **`IFluentConverterRulesBuilder<T>`**

This builder provides methods to define rules for each property:

- **`ForProperty`**: Specify custom rules for a property.
- **`Ignore`**: Skip the property during serialization.
- **`Rename`**: Rename the property in JSON.
- **`UseConverter`**: Use a custom converter for the property.
- **`Read`/`Write`**: Inline custom logic for reading or writing.

### Example Rules:

```csharp
rules
    .ForProperty(x => x.Id, x => x.Rename("identifier")) // Rename property
    .ForProperty(x => x.IsActive, x => x.UseConverter<CustomBoolConverter>()) // Use a custom converter
    .ForProperty(x => x.Name, x => x.Read((ref Utf8JsonReader reader) => reader.GetString().ToUpper())) // Inline read logic
    .Ignore(x => x.CustomProperty); // Ignore property
```

---

## **Advanced Scenarios**

### Custom Converters

Use custom converters to handle special cases:

```csharp
public class MyCustomStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()?.ToUpperInvariant() ?? string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToLowerInvariant());
    }
}
```

### Inline Logic

Define property-specific logic directly in your fluent rules:

```csharp
rules.ForProperty(x => x.CustomProperty, x => x.Read((ref Utf8JsonReader reader) => 
{
    if (reader.TokenType == JsonTokenType.String)
    {
        return $"Customized: {reader.GetString()}";
    }
    return null;
}));
```

---

## **Generated Code**

FluentJsonConverter generates a strongly-typed `JsonConverter` class based on your rules. For example, the above rules produce:

```csharp
public partial class ExampleModelConverter : JsonConverter<ExampleModel>
{
    public override ExampleModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Generated read logic
    }

    public override void Write(Utf8JsonWriter writer, ExampleModel value, JsonSerializerOptions options)
    {
        // Generated write logic
    }
}
```

---

## **Why FluentJsonConverter?**

FluentJsonConverter saves time and effort by:

- Centralizing JSON rules.
- Reducing boilerplate code.
- Providing type-safe compile-time validation.

It’s especially useful in projects with complex models and custom serialization requirements.

---

## **Contributing**

Contributions are welcome! Feel free to open issues or pull requests for improvements or new features.

---

## **License**

This project is licensed under the MIT License.

---

## **Links**

- **NuGet**: [FluentJsonConverter](https://www.nuget.org/FluentJsonConverter)
- **GitHub**: [FluentJsonConverter](https://github.com/windischb/FluentJsonConverter)
- **Documentation**: [Getting Started](https://github.com/windischb/FluentJsonConverter)