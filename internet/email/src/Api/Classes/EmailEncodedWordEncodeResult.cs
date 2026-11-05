using System;

namespace email;
/// <summary>
/// Describes the outcome of a encoded word encode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailEncodedWordEncodeResult : IEmailEncodedWordEncodeResultContract
{
    byte[] bytes;
    EmailEncodedWordEncoding encoding;
    int encodedWordCount;
    int foldCount;
    int finalLineColumn;
    /// <summary>
    /// Creates a new encoded word encode result.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "encoding">The encoding used by the operation.</param>
    /// <param name = "encodedWordCount">The number of items or bytes to process.</param>
    /// <param name = "foldCount">The number of items or bytes to process.</param>
    /// <param name = "finalLineColumn">The final line column used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailEncodedWordEncodeResult(byte[] bytes, EmailEncodedWordEncoding encoding, int encodedWordCount, int foldCount, int finalLineColumn)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        if (encoding < EmailEncodedWordEncoding.Base64 || encoding > EmailEncodedWordEncoding.QuotedPrintable)
        {
            throw new ArgumentOutOfRangeException("encoding");
        }

        if (encodedWordCount < 0)
        {
            throw new ArgumentOutOfRangeException("encodedWordCount");
        }

        if (foldCount < 0)
        {
            throw new ArgumentOutOfRangeException("foldCount");
        }

        if (finalLineColumn < 0)
        {
            throw new ArgumentOutOfRangeException("finalLineColumn");
        }

        this.bytes = new byte[bytes.Length];
        bytes.CopyTo(this.bytes, 0);
        this.encoding = encoding;
        this.encodedWordCount = encodedWordCount;
        this.foldCount = foldCount;
        this.finalLineColumn = finalLineColumn;
    }

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    public byte[] GetBytes()
    {
        byte[] copy = new byte[bytes.Length];
        bytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return bytes.Length;
    }

    /// <summary>
    /// Gets the encoding.
    /// </summary>
    /// <returns>The current encoding.</returns>
    public EmailEncodedWordEncoding GetEncoding()
    {
        return encoding;
    }

    /// <summary>
    /// Gets the encoded word count.
    /// </summary>
    /// <returns>The current encoded word count.</returns>
    public int GetEncodedWordCount()
    {
        return encodedWordCount;
    }

    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    public int GetFoldCount()
    {
        return foldCount;
    }

    /// <summary>
    /// Gets the final line column.
    /// </summary>
    /// <returns>The current final line column.</returns>
    public int GetFinalLineColumn()
    {
        return finalLineColumn;
    }
}
