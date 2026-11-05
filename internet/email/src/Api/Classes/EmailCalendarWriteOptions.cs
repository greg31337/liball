using System;

namespace email;
/// <summary>
/// Controls how calendar write operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailCalendarWriteOptions : IEmailCalendarWriteOptionsContract
{
    /// <summary>
    /// The default maximum output bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumOutputBytes = 10485760;
    int maximumOutputBytes;
    int foldOctets;
    string charset;
    /// <summary>
    /// Creates a new calendar write options object.
    /// </summary>
    public EmailCalendarWriteOptions()
    {
        maximumOutputBytes = DefaultMaximumOutputBytes;
        foldOctets = 75;
        charset = "utf-8";
    }

    /// <summary>
    /// Gets the maximum output bytes.
    /// </summary>
    /// <returns>The current maximum output bytes.</returns>
    public int GetMaximumOutputBytes()
    {
        return maximumOutputBytes;
    }

    /// <summary>
    /// Sets the maximum output bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumOutputBytes(int value)
    {
        maximumOutputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the fold octets.
    /// </summary>
    /// <returns>The current fold octets.</returns>
    public int GetFoldOctets()
    {
        return foldOctets;
    }

    /// <summary>
    /// Sets the fold octets used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFoldOctets(int value)
    {
        if (value < 4 || value > 998)
            throw new ArgumentOutOfRangeException("value");
        foldOctets = value;
    }

    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    public string GetCharset()
    {
        return charset;
    }

    /// <summary>
    /// Sets the charset used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCharset(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetLookupResult lookup = registry.Lookup(value);
        if (!lookup.IsFound())
            throw new ArgumentException("The calendar output charset is unsupported.", "value");
        if (!string.Equals(lookup.GetCanonicalName(), "utf-8", StringComparison.OrdinalIgnoreCase) && !string.Equals(lookup.GetCanonicalName(), "us-ascii", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Calendar output supports UTF-8 or US-ASCII so content-line octets remain interoperable.", "value");
        charset = lookup.GetCanonicalName() ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailCalendarWriteOptions Clone()
    {
        EmailCalendarWriteOptions copy = new EmailCalendarWriteOptions();
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        copy.SetFoldOctets(foldOctets);
        copy.SetCharset(charset);
        return copy;
    }
}
