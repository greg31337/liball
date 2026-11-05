namespace cbor;

/// <summary>
/// Validates and converts values associated with registered CBOR tags.
/// </summary>
public interface ICborTagHandler
{
    /// <summary>Validates a registered tag and its generic content.</summary>
    /// <param name="tagNumber">The unsigned tag number.</param>
    /// <param name="content">The tagged content.</param>
    /// <returns>The tag handling outcome.</returns>
    CborTagResult ValidateTag(ulong tagNumber, CborValue content);

    /// <summary>Converts registered tag content to its native representation.</summary>
    /// <param name="tagNumber">The unsigned tag number.</param>
    /// <param name="content">The tagged content.</param>
    /// <returns>The tag handling outcome and native value.</returns>
    CborTagResult DecodeTag(ulong tagNumber, CborValue content);

    /// <summary>Converts a native value to registered tag content.</summary>
    /// <param name="tagNumber">The unsigned tag number.</param>
    /// <param name="nativeValue">The native value to convert.</param>
    /// <returns>The tag handling outcome and generic value.</returns>
    CborTagResult EncodeTag(ulong tagNumber, object nativeValue);
}
