using System;

namespace email;
/// <summary>
/// Represents a header token.
/// </summary>
public class EmailHeaderToken : IEmailHeaderTokenContract
{
    EmailHeaderTokenKind kind;
    byte[] rawBytes;
    byte[] valueBytes;
    EmailSourcePosition sourcePosition;
    bool obsolete;
    bool malformed;
    int quotedPairCount;
    int maximumCommentDepth;
    /// <summary>
    /// Creates a new header token.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "valueBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "obsolete">Whether to enable the obsolete behavior.</param>
    /// <param name = "malformed">Whether to enable the malformed behavior.</param>
    /// <param name = "quotedPairCount">The number of items or bytes to process.</param>
    /// <param name = "maximumCommentDepth">The maximum comment depth used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderToken(EmailHeaderTokenKind kind, byte[] rawBytes, byte[] valueBytes, EmailSourcePosition sourcePosition, bool obsolete, bool malformed, int quotedPairCount, int maximumCommentDepth)
    {
        if (kind < EmailHeaderTokenKind.Cfws || kind > EmailHeaderTokenKind.Invalid)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (rawBytes.Length == 0)
        {
            throw new ArgumentException("Header tokens cannot be empty.", "rawBytes");
        }

        if (valueBytes == null)
        {
            throw new ArgumentNullException("valueBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        if (quotedPairCount < 0)
        {
            throw new ArgumentOutOfRangeException("quotedPairCount");
        }

        if (maximumCommentDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumCommentDepth");
        }

        this.kind = kind;
        this.rawBytes = new byte[rawBytes.Length];
        rawBytes.CopyTo(this.rawBytes, 0);
        this.valueBytes = new byte[valueBytes.Length];
        valueBytes.CopyTo(this.valueBytes, 0);
        this.sourcePosition = sourcePosition;
        this.obsolete = obsolete;
        this.malformed = malformed;
        this.quotedPairCount = quotedPairCount;
        this.maximumCommentDepth = maximumCommentDepth;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailHeaderTokenKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        byte[] copy = new byte[rawBytes.Length];
        rawBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current value bytes.</returns>
    public byte[] GetValueBytes()
    {
        byte[] copy = new byte[valueBytes.Length];
        valueBytes.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public int GetByteCount()
    {
        return rawBytes.Length;
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Determines whether the current value or state is obsolete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is obsolete; otherwise, <c>false</c>.</returns>
    public bool IsObsolete()
    {
        return obsolete;
    }

    /// <summary>
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    public bool IsMalformed()
    {
        return malformed;
    }

    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    public bool IsInternationalized()
    {
        int index = 0;
        while (index < rawBytes.Length)
        {
            if (rawBytes[index] >= 128)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>
    /// Gets the quoted pair count.
    /// </summary>
    /// <returns>The current quoted pair count.</returns>
    public int GetQuotedPairCount()
    {
        return quotedPairCount;
    }

    /// <summary>
    /// Gets the maximum comment depth.
    /// </summary>
    /// <returns>The current maximum comment depth.</returns>
    public int GetMaximumCommentDepth()
    {
        return maximumCommentDepth;
    }
}
