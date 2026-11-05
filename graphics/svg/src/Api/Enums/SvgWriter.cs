namespace svg;

/// <summary>Defines SVG data URL payload encoding.</summary>
public enum SvgDataUrlWriteEncoding
{
    /// <summary>Append the serialized Unicode XML without payload escaping.</summary>
    Unencoded = 0,
    /// <summary>Percent-encode the serialized UTF-8 bytes.</summary>
    PercentEncoded = 1,
    /// <summary>Base64-encode the serialized UTF-8 bytes.</summary>
    Base64 = 2
}

/// <summary>Identifies the unit used by a caller-provided write buffer.</summary>
public enum SvgWriteBufferKind
{
    /// <summary>The buffer contains UTF-16 characters.</summary>
    Utf16Characters = 0,
    /// <summary>The buffer contains UTF-8 bytes.</summary>
    Utf8Bytes = 1
}
