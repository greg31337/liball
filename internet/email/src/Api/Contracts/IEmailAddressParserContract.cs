using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressParser"/>.
/// </summary>
public interface IEmailAddressParserContract
{
    /// <summary>
    /// Parses the supplied mailbox data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseMailbox(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied group data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseGroup(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseListWithInputAndMode(ReadOnlySpan<byte> input, EmailAddressParseMode mode);
    /// <summary>
    /// Parses the supplied list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseListWithInputAndOptions(ReadOnlySpan<byte> input, EmailAddressParserOptions options);
    /// <summary>
    /// Parses the supplied list data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailAddressParseResult ParseListWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailAddressParserOptions options, EmailSourcePosition? initialPosition);
}
