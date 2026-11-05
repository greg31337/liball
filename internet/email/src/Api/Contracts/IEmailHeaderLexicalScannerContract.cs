using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderLexicalScanner"/>.
/// </summary>
public interface IEmailHeaderLexicalScannerContract
{
    /// <summary>
    /// Scans the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>The header lexical result produced by the operation.</returns>
    EmailHeaderLexicalResult ScanWithInputAndMode(ReadOnlySpan<byte> input, EmailHeaderLexicalMode mode);
    /// <summary>
    /// Scans the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header lexical result produced by the operation.</returns>
    EmailHeaderLexicalResult ScanWithInputAndOptions(ReadOnlySpan<byte> input, EmailHeaderLexicalOptions options);
    /// <summary>
    /// Scans the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>The header lexical result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHeaderLexicalResult ScanWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailHeaderLexicalOptions options, EmailSourcePosition? initialPosition);
}
