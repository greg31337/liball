using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpReader"/>.
/// </summary>
public interface IEmailOpenPgpReaderContract
{
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailOpenPgpReadResult ParseWithInput(ReadOnlySpan<byte> input);
    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailOpenPgpReadResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailOpenPgpReaderOptions options);
}
