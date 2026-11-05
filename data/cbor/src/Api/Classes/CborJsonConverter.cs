namespace cbor;

/// <summary>
/// Converts between generic CBOR values and JSON text.
/// </summary>
public class CborJsonConverter : ICborJsonConverter
{
    /// <summary>Creates a CBOR JSON converter.</summary>
    public CborJsonConverter()
    {
    }

    /// <summary>Converts a CBOR value to JSON text.</summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The JSON conversion policies.</param>
    /// <returns>The converted JSON text.</returns>
    public string ConvertToJson(CborValue value, CborJsonOptions options)
    {
        CborJsonWriterCore writer = new CborJsonWriterCore(options);
        return writer.Convert(value);
    }

    /// <summary>Converts JSON text to a generic CBOR value.</summary>
    /// <param name="json">The JSON text to convert.</param>
    /// <param name="options">The JSON conversion policies.</param>
    /// <returns>The read outcome and converted value.</returns>
    public CborReadResult ConvertFromJson(string json, CborJsonOptions options)
    {
        CborJsonReaderCore reader = new CborJsonReaderCore(json, options);
        return reader.Parse();
    }
}
