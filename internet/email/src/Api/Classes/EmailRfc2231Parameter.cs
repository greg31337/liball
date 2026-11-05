using System;

namespace email;
/// <summary>
/// Represents a rfc 2231 parameter.
/// </summary>
public class EmailRfc2231Parameter : IEmailRfc2231ParameterContract
{
    string? name;
    byte[]? valueBytes;
    bool quoted;
    EmailSourcePosition? sourcePosition;
    /// <summary>
    /// Creates an RFC 2231 parameter with an explicit source position.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "valueBytes">The source bytes or data to process.</param>
    /// <param name = "quoted">Whether to enable the quoted behavior.</param>
    public EmailRfc2231Parameter(string name, byte[] valueBytes, bool quoted)
    {
        Initialize(name, valueBytes, quoted, new EmailSourcePosition(0, 1, 1));
    }

    internal static EmailRfc2231Parameter CreateImplementationWithNameAndValueBytesAndQuoted(string name, byte[] valueBytes, bool quoted)
    {
        return new EmailRfc2231Parameter(name, valueBytes, quoted);
    }

    internal static EmailRfc2231Parameter CreateImplementationWithNameAndValueBytesAndQuotedAndSourcePosition(string name, byte[] valueBytes, bool quoted, EmailSourcePosition sourcePosition)
    {
        EmailRfc2231Parameter value = new EmailRfc2231Parameter(name, valueBytes, quoted);
        value.Initialize(name, valueBytes, quoted, sourcePosition);
        return value;
    }

    void Initialize(string name, byte[] valueBytes, bool quoted, EmailSourcePosition sourcePosition)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Parameter names cannot be empty.", "name");
        }

        if (valueBytes == null)
        {
            throw new ArgumentNullException("valueBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.name = name;
        this.valueBytes = new byte[valueBytes.Length];
        valueBytes.CopyTo(this.valueBytes, 0);
        this.quoted = quoted;
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string? GetName()
    {
        return name;
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
        byte[] copy = new byte[(valueBytes ?? throw new InvalidOperationException()).Length];
        valueBytes.CopyTo(copy, 0);
        return copy;
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
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition? GetSourcePosition()
    {
        return sourcePosition;
    }
}
