using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimKeyParser"/>.
/// </summary>
public interface IEmailDkimKeyParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDkimKeyParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDkimKeyParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailDkimKeyParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options, EmailSourcePosition initialPosition);
    /// <summary>
    /// Imports the subject public key info.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    EmailDkimKeyParseResult ImportSubjectPublicKeyInfoWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Imports the subject public key info.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimKeyParseResult ImportSubjectPublicKeyInfoWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options);
    /// <summary>
    /// Imports the RSA public key.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    EmailDkimKeyParseResult ImportRsaPublicKeyWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Imports the RSA public key.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimKeyParseResult ImportRsaPublicKeyWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options);
    /// <summary>
    /// Imports the pem.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    EmailDkimKeyParseResult ImportPemWithInput(string input);
    /// <summary>
    /// Imports the pem.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDkimKeyParseResult ImportPemWithInputAndOptions(string input, EmailDkimKeyParserOptions options);
}
