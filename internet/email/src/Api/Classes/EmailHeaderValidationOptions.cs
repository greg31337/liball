using System;

namespace email;
/// <summary>
/// Controls how header validation operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHeaderValidationOptions : IEmailHeaderValidationOptionsContract
{
    EmailHeaderValidationContext context;
    bool allowSmtpUtf8;
    bool allowObsoleteSyntax;
    bool requireDate;
    bool requireFrom;
    bool requireMessageId;
    bool requireBccStripped;
    bool validateStructuredValues;
    int maximumLineLength;
    /// <summary>
    /// Creates a new header validation options object.
    /// </summary>
    public EmailHeaderValidationOptions()
    {
        context = EmailHeaderValidationContext.Message;
        allowSmtpUtf8 = false;
        allowObsoleteSyntax = false;
        requireDate = true;
        requireFrom = true;
        requireMessageId = false;
        requireBccStripped = false;
        validateStructuredValues = true;
        maximumLineLength = 998;
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The current context.</returns>
    public EmailHeaderValidationContext GetContext()
    {
        return context;
    }

    /// <summary>
    /// Sets the context used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetContext(EmailHeaderValidationContext value)
    {
        if (value < EmailHeaderValidationContext.Message || value > EmailHeaderValidationContext.MimeEntity)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        context = value;
    }

    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSmtpUtf8()
    {
        return allowSmtpUtf8;
    }

    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    public void SetAllowSmtpUtf8(bool value)
    {
        allowSmtpUtf8 = value;
    }

    /// <summary>
    /// Gets the allow obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the allow obsolete syntax setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowObsoleteSyntax()
    {
        return allowObsoleteSyntax;
    }

    /// <summary>
    /// Sets the allow obsolete syntax used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow obsolete syntax behavior.</param>
    public void SetAllowObsoleteSyntax(bool value)
    {
        allowObsoleteSyntax = value;
    }

    /// <summary>
    /// Gets the require date.
    /// </summary>
    /// <returns><c>true</c> when the require date setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireDate()
    {
        return requireDate;
    }

    /// <summary>
    /// Sets the require date used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require date behavior.</param>
    public void SetRequireDate(bool value)
    {
        requireDate = value;
    }

    /// <summary>
    /// Gets the require from.
    /// </summary>
    /// <returns><c>true</c> when the require from setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireFrom()
    {
        return requireFrom;
    }

    /// <summary>
    /// Sets the require from used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require from behavior.</param>
    public void SetRequireFrom(bool value)
    {
        requireFrom = value;
    }

    /// <summary>
    /// Gets the require message ID.
    /// </summary>
    /// <returns><c>true</c> when the require message ID setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireMessageId()
    {
        return requireMessageId;
    }

    /// <summary>
    /// Sets the require message ID used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require message ID behavior.</param>
    public void SetRequireMessageId(bool value)
    {
        requireMessageId = value;
    }

    /// <summary>
    /// Gets the require bcc stripped.
    /// </summary>
    /// <returns><c>true</c> when the require bcc stripped setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRequireBccStripped()
    {
        return requireBccStripped;
    }

    /// <summary>
    /// Sets the require bcc stripped used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require bcc stripped behavior.</param>
    public void SetRequireBccStripped(bool value)
    {
        requireBccStripped = value;
    }

    /// <summary>
    /// Gets the validate structured values.
    /// </summary>
    /// <returns><c>true</c> when the validate structured values setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetValidateStructuredValues()
    {
        return validateStructuredValues;
    }

    /// <summary>
    /// Sets the validate structured values used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the validate structured values behavior.</param>
    public void SetValidateStructuredValues(bool value)
    {
        validateStructuredValues = value;
    }

    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    public int GetMaximumLineLength()
    {
        return maximumLineLength;
    }

    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLineLength(int value)
    {
        if (value < 1 || value > 998)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineLength = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHeaderValidationOptions Clone()
    {
        EmailHeaderValidationOptions copy = new EmailHeaderValidationOptions();
        copy.SetContext(context);
        copy.SetAllowSmtpUtf8(allowSmtpUtf8);
        copy.SetAllowObsoleteSyntax(allowObsoleteSyntax);
        copy.SetRequireDate(requireDate);
        copy.SetRequireFrom(requireFrom);
        copy.SetRequireMessageId(requireMessageId);
        copy.SetRequireBccStripped(requireBccStripped);
        copy.SetValidateStructuredValues(validateStructuredValues);
        copy.SetMaximumLineLength(maximumLineLength);
        return copy;
    }
}
