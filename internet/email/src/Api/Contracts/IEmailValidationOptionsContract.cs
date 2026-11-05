using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailValidationOptions"/>.
/// </summary>
public interface IEmailValidationOptionsContract
{
    /// <summary>
    /// Gets the validate syntax.
    /// </summary>
    /// <returns><c>true</c> when the validate syntax setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetValidateSyntax();
    /// <summary>
    /// Sets the validate syntax used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the validate syntax behavior.</param>
    void SetValidateSyntax(bool value);
    /// <summary>
    /// Gets the validate semantics.
    /// </summary>
    /// <returns><c>true</c> when the validate semantics setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetValidateSemantics();
    /// <summary>
    /// Sets the validate semantics used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the validate semantics behavior.</param>
    void SetValidateSemantics(bool value);
    /// <summary>
    /// Gets the require MIME version.
    /// </summary>
    /// <returns><c>true</c> when the require MIME version setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRequireMimeVersion();
    /// <summary>
    /// Sets the require MIME version used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the require MIME version behavior.</param>
    void SetRequireMimeVersion(bool value);
    /// <summary>
    /// Gets the maximum depth.
    /// </summary>
    /// <returns>The current maximum depth.</returns>
    int GetMaximumDepth();
    /// <summary>
    /// Sets the maximum depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumDepth(int value);
    /// <summary>
    /// Gets the maximum entity count.
    /// </summary>
    /// <returns>The current maximum entity count.</returns>
    int GetMaximumEntityCount();
    /// <summary>
    /// Sets the maximum entity count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEntityCount(int value);
    /// <summary>
    /// Gets the maximum content bytes.
    /// </summary>
    /// <returns>The current maximum content bytes.</returns>
    long GetMaximumContentBytes();
    /// <summary>
    /// Sets the maximum content bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumContentBytes(long value);
    /// <summary>
    /// Gets the message header options.
    /// </summary>
    /// <returns>The current message header options.</returns>
    EmailHeaderValidationOptions GetMessageHeaderOptions();
    /// <summary>
    /// Sets the message header options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetMessageHeaderOptions(EmailHeaderValidationOptions value);
    /// <summary>
    /// Gets the entity header options.
    /// </summary>
    /// <returns>The current entity header options.</returns>
    EmailHeaderValidationOptions GetEntityHeaderOptions();
    /// <summary>
    /// Sets the entity header options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetEntityHeaderOptions(EmailHeaderValidationOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailValidationOptions Clone();
}
