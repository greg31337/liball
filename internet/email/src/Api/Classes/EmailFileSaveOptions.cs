using System;

namespace email;
/// <summary>
/// Controls how file save operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailFileSaveOptions : IEmailFileSaveOptionsContract
{
    EmailWriteOptions writeOptions;
    EmailFileOverwritePolicy overwritePolicy;
    int maximumPathCharacters;
    bool requireEmlExtension;
    bool flushToDisk;
    /// <summary>
    /// Creates a new file save options object.
    /// </summary>
    public EmailFileSaveOptions()
    {
        writeOptions = new EmailWriteOptionsOperations().CreateMinimalRegeneration();
        overwritePolicy = EmailFileOverwritePolicy.Fail;
        maximumPathCharacters = EmailArchiveLimitsConstants.DefaultMaxPathCharacters;
        requireEmlExtension = true;
        flushToDisk = true;
    }

    /// <summary>
    /// Gets the write options.
    /// </summary>
    /// <returns>The current write options.</returns>
    public EmailWriteOptions GetWriteOptions()
    {
        return writeOptions.Clone();
    }

    /// <summary>
    /// Sets the write options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetWriteOptions(EmailWriteOptions value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        writeOptions = value.Clone();
    }

    /// <summary>
    /// Gets the overwrite policy.
    /// </summary>
    /// <returns>The current overwrite policy.</returns>
    public EmailFileOverwritePolicy GetOverwritePolicy()
    {
        return overwritePolicy;
    }

    /// <summary>
    /// Sets the overwrite policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetOverwritePolicy(EmailFileOverwritePolicy value)
    {
        if (value < EmailFileOverwritePolicy.Fail || value > EmailFileOverwritePolicy.Overwrite)
            throw new ArgumentOutOfRangeException("value");
        overwritePolicy = value;
    }

    /// <summary>
    /// Gets the maximum path characters.
    /// </summary>
    /// <returns>The current maximum path characters.</returns>
    public int GetMaximumPathCharacters()
    {
        return maximumPathCharacters;
    }

    /// <summary>
    /// Sets the maximum path characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumPathCharacters(int value)
    {
        maximumPathCharacters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the require eml extension.
    /// </summary>
    /// <returns><c>true</c> when the require eml extension setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireEmlExtension()
    {
        return requireEmlExtension;
    }

    /// <summary>
    /// Sets the require eml extension used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require eml extension behavior.</param>
    public void SetRequireEmlExtension(bool value)
    {
        requireEmlExtension = value;
    }

    /// <summary>
    /// Gets the flush to disk.
    /// </summary>
    /// <returns><c>true</c> when the flush to disk setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetFlushToDisk()
    {
        return flushToDisk;
    }

    /// <summary>
    /// Sets the flush to disk used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the flush to disk behavior.</param>
    public void SetFlushToDisk(bool value)
    {
        flushToDisk = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailFileSaveOptions Clone()
    {
        EmailFileSaveOptions copy = new EmailFileSaveOptions();
        copy.SetWriteOptions(writeOptions);
        copy.SetOverwritePolicy(overwritePolicy);
        copy.SetMaximumPathCharacters(maximumPathCharacters);
        copy.SetRequireEmlExtension(requireEmlExtension);
        copy.SetFlushToDisk(flushToDisk);
        return copy;
    }
}
