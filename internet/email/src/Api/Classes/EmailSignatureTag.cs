using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a signature tag.
/// </summary>
public class EmailSignatureTag : IEmailSignatureTagContract
{
    string name;
    string value;
    string compactValue;
    byte[] rawBytes;
    byte[] rawValueBytes;
    EmailSourcePosition sourcePosition;
    bool malformed;
    /// <summary>
    /// Creates a new signature tag.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "compactValue">The compact value used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "rawValueBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "malformed">Whether to enable the malformed behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSignatureTag(string name, string value, string compactValue, byte[] rawBytes, byte[] rawValueBytes, EmailSourcePosition sourcePosition, bool malformed)
    {
        if (name == null || value == null || compactValue == null || rawBytes == null || rawValueBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("name");
        }

        this.name = name;
        this.value = value;
        this.compactValue = compactValue;
        this.rawBytes = Copy(rawBytes);
        this.rawValueBytes = Copy(rawValueBytes);
        this.sourcePosition = sourcePosition;
        this.malformed = malformed;
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
    /// Gets the compact value.
    /// </summary>
    /// <returns>The current compact value.</returns>
    public string GetCompactValue()
    {
        return compactValue;
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

    static byte[] Copy(byte[] input)
    {
        byte[] copy = new byte[input.Length];
        input.CopyTo(copy, 0);
        return copy;
    }
}
