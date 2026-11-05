using System;

namespace email;
/// <summary>
/// Controls how maildir reader operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMaildirReaderOptions : IEmailMaildirReaderOptionsContract
{
    EmailArchiveLimits limits;
    EmailParseOptions parseOptions;
    /// <summary>
    /// Creates a new maildir reader options object.
    /// </summary>
    public EmailMaildirReaderOptions()
    {
        limits = new EmailArchiveLimits();
        parseOptions = new EmailParseOptionsOperations().CreatePreserving();
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailArchiveLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailArchiveLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the parse options.
    /// </summary>
    /// <returns>The current parse options.</returns>
    public EmailParseOptions GetParseOptions()
    {
        return parseOptions.Clone();
    }

    /// <summary>
    /// Sets the parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetParseOptions(EmailParseOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        parseOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailMaildirReaderOptions Clone()
    {
        EmailMaildirReaderOptions copy = new EmailMaildirReaderOptions();
        copy.SetLimits(limits);
        copy.SetParseOptions(parseOptions);
        return copy;
    }
}
