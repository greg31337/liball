using System;

namespace http;

/// <summary>
/// Represents one ordered field parameter and its source representation.
/// </summary>
public class HttpParameter
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Indicates that the parameter or directive has a value.
    /// </summary>
    public bool HasValue;

    /// <summary>
    /// The parameter value octets.
    /// </summary>
    public byte[] Value = Array.Empty<byte>();

    /// <summary>
    /// Indicates that the source value used quoted-string syntax.
    /// </summary>
    public bool IsQuoted;
}
