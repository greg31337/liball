using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Holds optional metadata needed to reproduce or inspect an original CBOR representation.
/// </summary>
public class CborRepresentation
{
    /// <summary>The byte width of the encoded integer or length argument, or zero for an inline argument.</summary>
    public int HeadArgumentWidth;
    /// <summary>The definite or indefinite length form of a string or container.</summary>
    public CborLengthKind LengthKind;
    /// <summary>The encoded byte length of each indefinite string chunk, in source order.</summary>
    public List<int>? ChunkByteLengths;
    /// <summary>The original floating-point width.</summary>
    public CborFloatWidth FloatWidth;
    /// <summary>The original floating-point bits.</summary>
    public ulong FloatBits;
    /// <summary>Indicates whether <see cref="FloatBits"/> contains retained representation data.</summary>
    public bool HasFloatBits;
    /// <summary>Indicates whether an encoded source range was retained.</summary>
    public bool HasSourceRange;
    /// <summary>The absolute offset of the encoded item in its source.</summary>
    public long SourceOffset;
    /// <summary>The encoded length of the item in bytes.</summary>
    public long SourceLength;
}
