using System;

namespace email;
/// <summary>
/// Represents a disposition.
/// </summary>
public class EmailDisposition : IEmailDispositionContract
{
    string dispositionType;
    EmailParameterList parameters;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new disposition.
    /// </summary>
    /// <param name = "dispositionType">The disposition type used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDisposition(string? dispositionType, EmailParameterList parameters, byte[] rawBytes, EmailSourcePosition? sourcePosition)
    {
        if (dispositionType == null || dispositionType.Length == 0)
        {
            throw new ArgumentException("A disposition type is required.", "dispositionType");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.dispositionType = dispositionType;
        this.parameters = parameters;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the disposition type.
    /// </summary>
    /// <returns>The current disposition type.</returns>
    public string GetDispositionType()
    {
        return dispositionType;
    }

    /// <summary>
    /// Gets the normalized disposition type.
    /// </summary>
    /// <returns>The current normalized disposition type.</returns>
    public string GetNormalizedDispositionType()
    {
        return dispositionType.ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether the current value or state is inline.
    /// </summary>
    /// <returns><c>true</c> when the value or state is inline; otherwise, <c>false</c>.</returns>
    public bool IsInline()
    {
        return string.Equals(dispositionType, "inline", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is attachment.
    /// </summary>
    /// <returns><c>true</c> when the value or state is attachment; otherwise, <c>false</c>.</returns>
    public bool IsAttachment()
    {
        return string.Equals(dispositionType, "attachment", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the current value or state is extension.
    /// </summary>
    /// <returns><c>true</c> when the value or state is extension; otherwise, <c>false</c>.</returns>
    public bool IsExtension()
    {
        return !IsInline() && !IsAttachment();
    }

    /// <summary>
    /// Gets the effective disposition type.
    /// </summary>
    /// <returns>The current effective disposition type.</returns>
    public string GetEffectiveDispositionType()
    {
        return IsExtension() ? "attachment" : GetNormalizedDispositionType();
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    public EmailParameterList GetParameters()
    {
        return parameters;
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
