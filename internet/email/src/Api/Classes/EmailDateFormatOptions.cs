namespace email;
/// <summary>
/// Controls how date format operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailDateFormatOptions : IEmailDateFormatOptionsContract
{
    /// <summary>
    /// The default maximum output bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumOutputBytes = 128;
    int maximumOutputBytes;
    /// <summary>
    /// Creates a new date format options object.
    /// </summary>
    public EmailDateFormatOptions()
    {
        maximumOutputBytes = DefaultMaximumOutputBytes;
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailDateFormatOptions Clone()
    {
        EmailDateFormatOptions copy = new EmailDateFormatOptions();
        copy.SetMaximumOutputBytes(maximumOutputBytes);
        return copy;
    }
}
