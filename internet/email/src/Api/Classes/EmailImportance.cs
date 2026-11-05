using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an importance.
/// </summary>
public class EmailImportance : IEmailImportanceContract
{
    EmailImportanceKind kind;
    string originalValue;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new importance.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "originalValue">The original value used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailImportance(EmailImportanceKind kind, string originalValue, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (kind < EmailImportanceKind.Low || kind > EmailImportanceKind.Unknown)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (originalValue == null)
        {
            throw new ArgumentNullException("originalValue");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.kind = kind;
        this.originalValue = originalValue;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailImportanceKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    public string GetOriginalValue()
    {
        return originalValue;
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
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
