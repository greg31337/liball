using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderValidationOptions"/>.
/// </summary>
public interface IEmailHeaderValidationOptionsContract
{
    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The current context.</returns>
    EmailHeaderValidationContext GetContext();
    /// <summary>
    /// Sets the context used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetContext(EmailHeaderValidationContext value);
    /// <summary>
    /// Gets the allow smtp UTF-8.
    /// </summary>
    /// <returns><c>true</c> when the allow smtp UTF-8 setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSmtpUtf8();
    /// <summary>
    /// Sets the allow smtp UTF-8 used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow smtp UTF-8 behavior.</param>
    void SetAllowSmtpUtf8(bool value);
    /// <summary>
    /// Gets the allow obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the allow obsolete syntax setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowObsoleteSyntax();
    /// <summary>
    /// Sets the allow obsolete syntax used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow obsolete syntax behavior.</param>
    void SetAllowObsoleteSyntax(bool value);
    /// <summary>
    /// Gets the require date.
    /// </summary>
    /// <returns><c>true</c> when the require date setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireDate();
    /// <summary>
    /// Sets the require date used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require date behavior.</param>
    void SetRequireDate(bool value);
    /// <summary>
    /// Gets the require from.
    /// </summary>
    /// <returns><c>true</c> when the require from setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireFrom();
    /// <summary>
    /// Sets the require from used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require from behavior.</param>
    void SetRequireFrom(bool value);
    /// <summary>
    /// Gets the require message ID.
    /// </summary>
    /// <returns><c>true</c> when the require message ID setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireMessageId();
    /// <summary>
    /// Sets the require message ID used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require message ID behavior.</param>
    void SetRequireMessageId(bool value);
    /// <summary>
    /// Gets the require bcc stripped.
    /// </summary>
    /// <returns><c>true</c> when the require bcc stripped setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireBccStripped();
    /// <summary>
    /// Sets the require bcc stripped used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require bcc stripped behavior.</param>
    void SetRequireBccStripped(bool value);
    /// <summary>
    /// Gets the validate structured values.
    /// </summary>
    /// <returns><c>true</c> when the validate structured values setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetValidateStructuredValues();
    /// <summary>
    /// Sets the validate structured values used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the validate structured values behavior.</param>
    void SetValidateStructuredValues(bool value);
    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    int GetMaximumLineLength();
    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLineLength(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHeaderValidationOptions Clone();
}
