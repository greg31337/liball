using System;

namespace email;
/// <summary>
/// Controls how validation operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailValidationOptions : IEmailValidationOptionsContract
{
    bool validateSyntax;
    bool validateSemantics;
    bool requireMimeVersion;
    int maximumDepth;
    int maximumEntityCount;
    long maximumContentBytes;
    EmailHeaderValidationOptions messageHeaderOptions;
    EmailHeaderValidationOptions entityHeaderOptions;
    /// <summary>
    /// Creates a new validation options object.
    /// </summary>
    public EmailValidationOptions()
    {
        validateSyntax = true;
        validateSemantics = true;
        requireMimeVersion = true;
        maximumDepth = EmailTraversalConstants.DefaultMaximumDepth;
        maximumEntityCount = EmailTraversalConstants.DefaultMaximumEntityCount;
        maximumContentBytes = EmailDecodingLimitsConstants.DefaultMaxDecodedBytes;
        messageHeaderOptions = new EmailHeaderValidationOptions();
        entityHeaderOptions = new EmailHeaderValidationOptions();
        entityHeaderOptions.SetContext(EmailHeaderValidationContext.MimeEntity);
        entityHeaderOptions.SetRequireDate(false);
        entityHeaderOptions.SetRequireFrom(false);
        entityHeaderOptions.SetRequireMessageId(false);
    }

    /// <summary>
    /// Gets the validate syntax.
    /// </summary>
    /// <returns><c>true</c> when the validate syntax setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetValidateSyntax()
    {
        return validateSyntax;
    }

    /// <summary>
    /// Sets the validate syntax used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the validate syntax behavior.</param>
    public void SetValidateSyntax(bool value)
    {
        validateSyntax = value;
    }

    /// <summary>
    /// Gets the validate semantics.
    /// </summary>
    /// <returns><c>true</c> when the validate semantics setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetValidateSemantics()
    {
        return validateSemantics;
    }

    /// <summary>
    /// Sets the validate semantics used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the validate semantics behavior.</param>
    public void SetValidateSemantics(bool value)
    {
        validateSemantics = value;
    }

    /// <summary>
    /// Gets the require MIME version.
    /// </summary>
    /// <returns><c>true</c> when the require MIME version setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireMimeVersion()
    {
        return requireMimeVersion;
    }

    /// <summary>
    /// Sets the require MIME version used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require MIME version behavior.</param>
    public void SetRequireMimeVersion(bool value)
    {
        requireMimeVersion = value;
    }

    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    public int GetMaximumDepth()
    {
        return maximumDepth;
    }

    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumDepth(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumDepth = value;
    }

    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    public int GetMaximumEntityCount()
    {
        return maximumEntityCount;
    }

    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEntityCount(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumEntityCount = value;
    }

    /// <summary>
    /// Gets the maximum content bytes.
    /// </summary>
    /// <returns>The current maximum content bytes.</returns>
    public long GetMaximumContentBytes()
    {
        return maximumContentBytes;
    }

    /// <summary>
    /// Sets the maximum content bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumContentBytes(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumContentBytes = value;
    }

    /// <summary>
    /// Gets the message header options.
    /// </summary>
    /// <returns>The current message header options.</returns>
    public EmailHeaderValidationOptions GetMessageHeaderOptions()
    {
        return messageHeaderOptions.Clone();
    }

    /// <summary>
    /// Sets the message header options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetMessageHeaderOptions(EmailHeaderValidationOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.GetContext() != EmailHeaderValidationContext.Message)
        {
            throw new ArgumentException("Message header options require message context.", "value");
        }

        messageHeaderOptions = value.Clone();
    }

    /// <summary>
    /// Gets the entity header options.
    /// </summary>
    /// <returns>The current entity header options.</returns>
    public EmailHeaderValidationOptions GetEntityHeaderOptions()
    {
        return entityHeaderOptions.Clone();
    }

    /// <summary>
    /// Sets the entity header options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetEntityHeaderOptions(EmailHeaderValidationOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.GetContext() != EmailHeaderValidationContext.MimeEntity)
        {
            throw new ArgumentException("Entity header options require MIME-entity context.", "value");
        }

        entityHeaderOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailValidationOptions Clone()
    {
        EmailValidationOptions copy = new EmailValidationOptions();
        copy.SetValidateSyntax(validateSyntax);
        copy.SetValidateSemantics(validateSemantics);
        copy.SetRequireMimeVersion(requireMimeVersion);
        copy.SetMaximumDepth(maximumDepth);
        copy.SetMaximumEntityCount(maximumEntityCount);
        copy.SetMaximumContentBytes(maximumContentBytes);
        copy.SetMessageHeaderOptions(messageHeaderOptions);
        copy.SetEntityHeaderOptions(entityHeaderOptions);
        return copy;
    }
}
