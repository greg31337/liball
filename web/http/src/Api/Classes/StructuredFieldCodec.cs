namespace http;

/// <summary>
/// Provides RFC 9651 Structured Field parsing and canonical serialization.
/// </summary>
public class StructuredFieldCodec : IStructuredFieldCodec
{
    /// <summary>Creates a Structured Field codec.</summary>
    public StructuredFieldCodec()
    {
    }

    /// <summary>Parses one complete Structured Field item.</summary>
    /// <param name="input">The encoded field value bytes.</param>
    /// <returns>The parsed item and its parameters.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A required parsing limit is exceeded.</exception>
    public StructuredFieldMember ParseStructuredItem(byte[] input)
    {
        StructuredFieldParser parser = new StructuredFieldParser(input);
        return parser.ParseTopItem();
    }

    /// <summary>Parses one complete Structured Field list.</summary>
    /// <param name="input">The encoded field value bytes.</param>
    /// <returns>The ordered parsed list.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A required parsing limit is exceeded.</exception>
    public StructuredFieldValue ParseStructuredList(byte[] input)
    {
        StructuredFieldParser parser = new StructuredFieldParser(input);
        return parser.ParseTopList();
    }

    /// <summary>Parses one complete Structured Field dictionary.</summary>
    /// <param name="input">The encoded field value bytes.</param>
    /// <returns>The ordered parsed dictionary.</returns>
    /// <exception cref="HttpParseException">The field value is malformed.</exception>
    /// <exception cref="HttpLimitExceededException">A required parsing limit is exceeded.</exception>
    public StructuredFieldValue ParseStructuredDictionary(byte[] input)
    {
        StructuredFieldParser parser = new StructuredFieldParser(input);
        return parser.ParseTopDictionary();
    }

    /// <summary>Serializes an item, list, or dictionary in canonical form.</summary>
    /// <param name="value">The Structured Field value.</param>
    /// <returns>The canonical encoded field value bytes.</returns>
    /// <exception cref="HttpFieldException">The value does not represent a valid Structured Field.</exception>
    public byte[] SerializeStructuredValue(StructuredFieldValue value)
    {
        return StructuredFieldWriter.SerializeValue(value);
    }
}
