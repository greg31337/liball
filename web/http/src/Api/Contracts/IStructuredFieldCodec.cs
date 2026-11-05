namespace http;

/// <summary>
/// Parses and serializes HTTP Structured Fields.
/// </summary>
public interface IStructuredFieldCodec
{
    /// <summary>
    /// Parses one Structured Field item.
    /// </summary>
    /// <param name="input">The encoded field value bytes.</param>
    /// <returns>The parsed Structured Field member.</returns>
    StructuredFieldMember ParseStructuredItem(byte[] input);

    /// <summary>Parses one Structured Field list.</summary>
    /// <param name="input">The encoded field value bytes.</param>
    /// <returns>The parsed list.</returns>
    StructuredFieldValue ParseStructuredList(byte[] input);

    /// <summary>Parses one Structured Field dictionary.</summary>
    /// <param name="input">The encoded field value bytes.</param>
    /// <returns>The parsed dictionary.</returns>
    StructuredFieldValue ParseStructuredDictionary(byte[] input);

    /// <summary>Serializes a Structured Field value canonically.</summary>
    /// <param name="value">The item, list, or dictionary.</param>
    /// <returns>The canonical field value bytes.</returns>
    byte[] SerializeStructuredValue(StructuredFieldValue value);
}
