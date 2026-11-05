using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageId"/>.
/// </summary>
public interface IEmailMessageIdContract
{
    /// <summary>
    /// Gets the ID left.
    /// </summary>
    /// <returns>The current ID left.</returns>
    string GetIdLeft();
    /// <summary>
    /// Gets the ID right.
    /// </summary>
    /// <returns>The current ID right.</returns>
    string GetIdRight();
    /// <summary>
    /// Determines whether the current value or state is right domain literal.
    /// </summary>
    /// <returns><c>true</c> when the value or state is right domain literal; otherwise, <c>false</c>.</returns>
    bool IsRightDomainLiteral();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    bool UsesObsoleteSyntax();
    /// <summary>
    /// Determines whether the operation or source value was recovered.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasRecovered();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message ID format result produced by the operation.</returns>
    EmailMessageIdFormatResult FormatWithOptions(EmailMessageIdFormatOptions options);
}
