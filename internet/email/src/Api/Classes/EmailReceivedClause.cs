using System;
using System.Text;

namespace email;
/// <summary>
/// Represents a received clause.
/// </summary>
public class EmailReceivedClause : IEmailReceivedClauseContract
{
    EmailReceivedClauseKind kind;
    string originalName;
    string normalizedName;
    byte[] rawBytes;
    byte[] rawValueBytes;
    EmailSourcePosition sourcePosition;
    bool malformed;
    /// <summary>
    /// Creates a new received clause.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "originalName">The original name used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "rawValueBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "malformed">Whether to enable the malformed behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailReceivedClause(EmailReceivedClauseKind kind, string originalName, byte[] rawBytes, byte[] rawValueBytes, EmailSourcePosition sourcePosition, bool malformed)
    {
        if (kind < EmailReceivedClauseKind.From || kind > EmailReceivedClauseKind.Extension)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (originalName == null)
        {
            throw new ArgumentNullException("originalName");
        }

        if (kind != EmailReceivedClauseKind.Extension && originalName.Length == 0)
        {
            throw new ArgumentException("A standard Received clause must have a name.", "originalName");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (rawValueBytes == null)
        {
            throw new ArgumentNullException("rawValueBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.kind = kind;
        this.originalName = originalName;
        normalizedName = originalName.ToLowerInvariant();
        this.rawBytes = Copy(rawBytes);
        this.rawValueBytes = Copy(rawValueBytes);
        this.sourcePosition = sourcePosition;
        this.malformed = malformed;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailReceivedClauseKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the current value or state is extension.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extension; otherwise, <c>false</c>.</returns>
    public bool IsExtension()
    {
        return kind == EmailReceivedClauseKind.Extension;
    }

    /// <summary>
    /// Gets the original name.
    /// </summary>
    /// <returns>The current original name.</returns>
    public string GetOriginalName()
    {
        return originalName;
    }

    /// <summary>
    /// Gets the normalized name.
    /// </summary>
    /// <returns>The current normalized name.</returns>
    public string GetNormalizedName()
    {
        return normalizedName;
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        string text = Encoding.UTF8.GetString(rawValueBytes);
        StringBuilder value = new StringBuilder();
        int index = 0;
        bool whitespace = false;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
            {
                if (value.Length > 0)
                {
                    whitespace = true;
                }
            }
            else
            {
                if (whitespace)
                {
                    value.Append(' ');
                    whitespace = false;
                }

                value.Append(current);
            }

            index++;
        }

        return value.ToString();
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
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the raw value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw value bytes.</returns>
    public byte[] GetRawValueBytes()
    {
        return Copy(rawValueBytes);
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
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    public bool IsMalformed()
    {
        return malformed;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
