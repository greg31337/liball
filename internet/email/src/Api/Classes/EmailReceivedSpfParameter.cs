using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a received spf parameter.
/// </summary>
public class EmailReceivedSpfParameter : IEmailReceivedSpfParameterContract
{
    string name;
    string value;
    bool quoted;
    byte[] rawBytes;
    byte[] rawValueBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new received spf parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "quoted">Whether to enable the quoted behavior.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "rawValueBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReceivedSpfParameter(string name, string? value, bool quoted, byte[] rawBytes, byte[] rawValueBytes, EmailSourcePosition sourcePosition)
    {
        if (name == null || name.Length == 0)
        {
            throw new ArgumentException("A Received-SPF parameter name is required.", "name");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (rawBytes == null || rawValueBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        this.name = name;
        this.value = value;
        this.quoted = quoted;
        this.rawBytes = Copy(rawBytes);
        this.rawValueBytes = Copy(rawValueBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the normalized name.
    /// </summary>
    /// <returns>The current normalized name.</returns>
    public string GetNormalizedName()
    {
        return name.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Determines whether the operation or source value was quoted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasQuoted()
    {
        return quoted;
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

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
