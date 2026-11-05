namespace cbor;

/// <summary>
/// Converts between generic CBOR values and JSON text.
/// </summary>
public interface ICborJsonConverter
{
    /// <summary>Converts a CBOR value to JSON text.</summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The JSON conversion policies.</param>
    /// <returns>The converted JSON text.</returns>
    string ConvertToJson(CborValue value, CborJsonOptions options);

    /// <summary>Converts JSON text to a generic CBOR value.</summary>
    /// <param name="json">The JSON text to convert.</param>
    /// <param name="options">The JSON conversion policies.</param>
    /// <returns>The read outcome and converted value.</returns>
    CborReadResult ConvertFromJson(string json, CborJsonOptions options);
}
