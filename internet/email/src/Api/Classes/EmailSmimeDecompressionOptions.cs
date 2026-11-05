using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Controls how S/MIME decompression operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSmimeDecompressionOptions : IEmailSmimeDecompressionOptionsContract
{
    EmailCryptographicLimits limits;
    int readBufferBytes;
    /// <summary>
    /// Creates a new S/MIME decompression options object.
    /// </summary>
    public EmailSmimeDecompressionOptions()
    {
        limits = new EmailCryptographicLimits();
        readBufferBytes = 8192;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailCryptographicLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailCryptographicLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the read buffer bytes.
    /// </summary>
    /// <returns>The current read buffer bytes.</returns>
    public int GetReadBufferBytes()
    {
        return readBufferBytes;
    }

    /// <summary>
    /// Sets the read buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetReadBufferBytes(int value)
    {
        readBufferBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSmimeDecompressionOptions Clone()
    {
        EmailSmimeDecompressionOptions copy = new EmailSmimeDecompressionOptions();
        copy.SetLimits(limits);
        copy.SetReadBufferBytes(readBufferBytes);
        return copy;
    }
}
