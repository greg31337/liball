using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a priority.
/// </summary>
public class EmailPriority : IEmailPriorityContract
{
    EmailPriorityKind kind;
    string originalValue;
    bool xPriority;
    int numericValue;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new priority.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "originalValue">The original value used by the operation.</param>
    /// <param name = "xPriority">Whether to enable the x priority behavior.</param>
    /// <param name = "numericValue">The numeric value used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailPriority(EmailPriorityKind kind, string originalValue, bool xPriority, int numericValue, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (kind < EmailPriorityKind.NonUrgent || kind > EmailPriorityKind.Unknown)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (originalValue == null)
        {
            throw new ArgumentNullException("originalValue");
        }

        if (xPriority && (numericValue < 0 || numericValue > 5))
        {
            throw new ArgumentOutOfRangeException("numericValue");
        }

        if (!xPriority && numericValue != 0)
        {
            throw new ArgumentException("A numeric value is only valid for X-Priority.", "numericValue");
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
        this.xPriority = xPriority;
        this.numericValue = numericValue;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailPriorityKind GetKind()
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
    /// Determines whether the current value or state is x priority.
    /// </summary>
    /// <returns><c>true</c> when the value or state is x priority; otherwise, <c>false</c>.</returns>
    public bool IsXPriority()
    {
        return xPriority;
    }

    /// <summary>
    /// Gets the numeric value.
    /// </summary>
    /// <returns>The current numeric value.</returns>
    public int GetNumericValue()
    {
        return numericValue;
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
