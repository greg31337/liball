using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSignatureFormatter"/>.
/// </summary>
public interface IEmailSignatureFormatterContract
{
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithValueUsingEmailDkimSignature(EmailDkimSignature value);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(EmailDkimSignature value, EmailSignatureFormatOptions options);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithValueUsingEmailArcSeal(EmailArcSeal value);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(EmailArcSeal value, EmailSignatureFormatOptions options);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithValueUsingEmailArcAuthenticationResults(EmailArcAuthenticationResults value);
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    EmailSignatureFormatResult FormatWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(EmailArcAuthenticationResults value, EmailSignatureFormatOptions options);
}
