using System;

namespace http;

/// <summary>
/// Represents one ordered HTTP field line with byte-exact value data.
/// </summary>
public class HttpHeaderField
{
    /// <summary>
    /// The field name with its original spelling.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// The field value octets.
    /// </summary>
    public byte[] Value = Array.Empty<byte>();

    /// <summary>
    /// Indicates that the field contains sensitive data.
    /// </summary>
    public bool IsSensitive;

    /// <summary>
    /// Indicates that an HPACK encoder must use a never-indexed representation.
    /// </summary>
    public bool NeverIndex;
}
