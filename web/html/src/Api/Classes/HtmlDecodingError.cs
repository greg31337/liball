using System;

namespace html;
/// <summary>Describes one recoverable byte-decoding error and its source range.</summary>
public class HtmlDecodingError : IHtmlDecodingError
{
    internal int byteOffset;
    internal int byteLength;
    internal HtmlDecodingError(int byteOffset, int byteLength)
    {
        if (byteOffset < 0)
        {
            throw new ArgumentOutOfRangeException("byteOffset", byteOffset, "A byte offset cannot be negative.");
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException("byteLength", byteLength, "A decoding error must consume at least one byte.");
        }

        this.byteOffset = byteOffset;
        this.byteLength = byteLength;
    }

    /// <summary>Gets the stable diagnostic code describing this decoding replacement.</summary>
    /// <returns>The malformed-sequence or input-limit diagnostic code.</returns>
    public HtmlDiagnosticCode GetCode()
    {
        return HtmlDiagnosticCode.EncodingInvalidSequence;
    }

    /// <summary>Gets where the malformed sequence begins in the original byte input.</summary>
    /// <returns>The zero-based byte offset of the replaced sequence.</returns>
    public int GetByteOffset()
    {
        return this.byteOffset;
    }

    /// <summary>Gets how many original input bytes belong to the malformed sequence.</summary>
    /// <returns>The positive byte length of the replaced sequence.</returns>
    public int GetByteLength()
    {
        return this.byteLength;
    }
}
