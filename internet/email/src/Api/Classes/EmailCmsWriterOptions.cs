using System;

namespace email;
/// <summary>
/// Controls how CMS writer operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCmsWriterOptions : IEmailCmsWriterOptionsContract
{
    EmailCryptographicLimits limits;
    /// <summary>
    /// Creates a new CMS writer options object.
    /// </summary>
    public EmailCmsWriterOptions()
    {
        limits = new EmailCryptographicLimits();
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCmsWriterOptions Clone()
    {
        EmailCmsWriterOptions copy = new EmailCmsWriterOptions();
        copy.SetLimits(limits);
        return copy;
    }
}
