using System;

namespace http;

/// <summary>
/// Represents one HPACK static- or dynamic-table entry.
/// </summary>
public class HpackTableEntry
{
    /// <summary>
    /// The byte-exact lowercase field name.
    /// </summary>
    public byte[] Name = Array.Empty<byte>();

    /// <summary>
    /// The byte-exact field value.
    /// </summary>
    public byte[] Value = Array.Empty<byte>();

    /// <summary>
    /// The RFC 7541 entry size: name length plus value length plus 32 octets.
    /// </summary>
    public int Size;

    /// <summary>
    /// Indicates that an HPACK index is available.
    /// </summary>
    public bool HasIndex;

    /// <summary>
    /// The one-based HPACK index at the time of the snapshot.
    /// </summary>
    public int Index;

    /// <summary>
    /// Indicates that the entry belongs to the RFC 7541 static table.
    /// </summary>
    public bool IsStatic;

    /// <summary>
    /// Indicates that the represented field contains sensitive data.
    /// </summary>
    public bool IsSensitive;

    /// <summary>
    /// Indicates that the represented field used a never-indexed literal.
    /// </summary>
    public bool NeverIndex;
}
