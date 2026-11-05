using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMimeParser"/>.
/// </summary>
public interface IEmailMimeParserContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseWithInputUsingValueArray(byte[] input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEntityParseResult ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailEntityParseResult ParseWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEntityParseResult ParseWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied body data.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "source">The source content to read or copy from.</param>
    /// <param name = "entityRange">The entity range used by the operation.</param>
    /// <param name = "contentRange">The content range used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    MimeEntity? ParseBody(EmailHeaders? headers, EmailContent? source, EmailRange? entityRange, EmailRange? contentRange, EmailParseOptions options);
    /// <summary>
    /// Parses the supplied body result data.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "source">The source content to read or copy from.</param>
    /// <param name = "entityRange">The entity range used by the operation.</param>
    /// <param name = "contentRange">The content range used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEntityParseResult ParseBodyResult(EmailHeaders? headers, EmailContent? source, EmailRange? entityRange, EmailRange? contentRange, EmailParseOptions options);
}
