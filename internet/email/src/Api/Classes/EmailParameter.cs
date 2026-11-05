using System;

namespace email;
/// <summary>
/// Represents a parameter.
/// </summary>
public class EmailParameter : IEmailParameterContract
{
    string name;
    byte[] rawValueBytes;
    byte[] valueBytes;
    bool quoted;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "rawValueBytes">The source bytes or data to process.</param>
    /// <param name = "valueBytes">The source bytes or data to process.</param>
    /// <param name = "quoted">Whether to enable the quoted behavior.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailParameter(string name, byte[] rawValueBytes, byte[] valueBytes, bool quoted, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (name == null || name.Length == 0)
        {
            throw new ArgumentException("A parameter name is required.", "name");
        }

        if (rawValueBytes == null)
        {
            throw new ArgumentNullException("rawValueBytes");
        }

        if (valueBytes == null)
        {
            throw new ArgumentNullException("valueBytes");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.name = name;
        this.rawValueBytes = Copy(rawValueBytes);
        this.valueBytes = Copy(valueBytes);
        this.quoted = quoted;
        this.rawBytes = Copy(rawBytes);
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
    /// Gets the value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current value bytes.</returns>
    public byte[] GetValueBytes()
    {
        return Copy(valueBytes);
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
