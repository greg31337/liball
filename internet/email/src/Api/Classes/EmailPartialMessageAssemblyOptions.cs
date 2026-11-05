using System;

namespace email;
/// <summary>
/// Controls how partial message assembly operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailPartialMessageAssemblyOptions : IEmailPartialMessageAssemblyOptionsContract
{
    /// <summary>
    /// The default maximum fragment bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumFragmentBytes = EmailArchiveLimitsConstants.DefaultMaxEntryBytes;
    int maximumFragmentCount;
    long maximumFragmentBytes;
    long maximumReassembledBytes;
    bool reparse;
    EmailParseOptions parseOptions;
    /// <summary>
    /// Creates a new partial message assembly options object.
    /// </summary>
    public EmailPartialMessageAssemblyOptions()
    {
        maximumFragmentCount = EmailArchiveLimitsConstants.DefaultMaxPartialFragmentCount;
        maximumFragmentBytes = DefaultMaximumFragmentBytes;
        maximumReassembledBytes = EmailArchiveLimitsConstants.DefaultMaxReassembledBytes;
        reparse = true;
        parseOptions = new EmailParseOptionsOperations().CreatePreserving();
    }

    /// <summary>
    /// Gets the maximum fragment count.
    /// </summary>
    /// <returns>The current maximum fragment count.</returns>
    public int GetMaximumFragmentCount()
    {
        return maximumFragmentCount;
    }

    /// <summary>
    /// Sets the maximum fragment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFragmentCount(int value)
    {
        maximumFragmentCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum fragment bytes.
    /// </summary>
    /// <returns>The current maximum fragment bytes.</returns>
    public long GetMaximumFragmentBytes()
    {
        return maximumFragmentBytes;
    }

    /// <summary>
    /// Sets the maximum fragment bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumFragmentBytes(long value)
    {
        maximumFragmentBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum reassembled bytes.
    /// </summary>
    /// <returns>The current maximum reassembled bytes.</returns>
    public long GetMaximumReassembledBytes()
    {
        return maximumReassembledBytes;
    }

    /// <summary>
    /// Sets the maximum reassembled bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumReassembledBytes(long value)
    {
        maximumReassembledBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the reparse.
    /// </summary>
    /// <returns><c>true</c> when the reparse setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetReparse()
    {
        return reparse;
    }

    /// <summary>
    /// Sets the reparse used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the reparse behavior.</param>
    public void SetReparse(bool value)
    {
        reparse = value;
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
    public EmailPartialMessageAssemblyOptions Clone()
    {
        EmailPartialMessageAssemblyOptions copy = new EmailPartialMessageAssemblyOptions();
        copy.SetMaximumFragmentCount(maximumFragmentCount);
        copy.SetMaximumFragmentBytes(maximumFragmentBytes);
        copy.SetMaximumReassembledBytes(maximumReassembledBytes);
        copy.SetReparse(reparse);
        copy.SetParseOptions(parseOptions);
        return copy;
    }
}
