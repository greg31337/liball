using System;

namespace email;
/// <summary>
/// Controls how maildir writer operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMaildirWriterOptions : IEmailMaildirWriterOptionsContract
{
    /// <summary>
    /// The default maximum collision attempts used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumCollisionAttempts = 32;
    EmailArchiveLimits limits;
    EmailWriteOptions writeOptions;
    EmailMaildirCollisionPolicy collisionPolicy;
    int maximumCollisionAttempts;
    string hostName;
    bool flushToDisk;
    /// <summary>
    /// Creates a new maildir writer options object.
    /// </summary>
    public EmailMaildirWriterOptions()
    {
        limits = new EmailArchiveLimits();
        writeOptions = new EmailWriteOptionsOperations().CreateMinimalRegeneration();
        collisionPolicy = EmailMaildirCollisionPolicy.GenerateUnique;
        maximumCollisionAttempts = DefaultMaximumCollisionAttempts;
        hostName = Environment.MachineName;
        flushToDisk = true;
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
    /// Gets the collision policy.
    /// </summary>
    /// <returns>The current collision policy.</returns>
    public EmailMaildirCollisionPolicy GetCollisionPolicy()
    {
        return collisionPolicy;
    }

    /// <summary>
    /// Sets the collision policy used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetCollisionPolicy(EmailMaildirCollisionPolicy value)
    {
        if (value < EmailMaildirCollisionPolicy.Fail || value > EmailMaildirCollisionPolicy.Overwrite)
            throw new ArgumentOutOfRangeException("value");
        collisionPolicy = value;
    }

    /// <summary>
    /// Gets the maximum collision attempts.
    /// </summary>
    /// <returns>The current maximum collision attempts.</returns>
    public int GetMaximumCollisionAttempts()
    {
        return maximumCollisionAttempts;
    }

    /// <summary>
    /// Sets the maximum collision attempts used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCollisionAttempts(int value)
    {
        maximumCollisionAttempts = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the host name.
    /// </summary>
    /// <returns>The current host name.</returns>
    public string GetHostName()
    {
        return hostName;
    }

    /// <summary>
    /// Sets the host name used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetHostName(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (value.Length == 0)
            throw new ArgumentException("A host name cannot be empty.", "value");
        hostName = value;
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
    public EmailMaildirWriterOptions Clone()
    {
        EmailMaildirWriterOptions copy = new EmailMaildirWriterOptions();
        copy.SetLimits(limits);
        copy.SetWriteOptions(writeOptions);
        copy.SetCollisionPolicy(collisionPolicy);
        copy.SetMaximumCollisionAttempts(maximumCollisionAttempts);
        copy.SetHostName(hostName);
        copy.SetFlushToDisk(flushToDisk);
        return copy;
    }
}
