namespace json;

/// <summary>Validates publicly mutable JSON model graphs.</summary>
public interface IJsonValueValidator
{
    /// <summary>Validates one JSON value graph and its resource limits.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="limits">The resource limits to apply.</param>
    /// <returns>The validation outcome.</returns>
    JsonOperationResult ValidateValue(JsonValue value, JsonLimits limits);

    /// <summary>Validates one JSON document and its resource limits.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="limits">The resource limits to apply.</param>
    /// <returns>The validation outcome.</returns>
    JsonOperationResult ValidateDocument(JsonDocument document, JsonLimits limits);
}
