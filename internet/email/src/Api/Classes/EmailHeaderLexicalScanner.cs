using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with header lexical scanner.
/// </summary>
public class EmailHeaderLexicalScanner : IEmailHeaderLexicalScannerContract
{
    internal static EmailHeaderLexicalResult ScanImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailHeaderLexicalMode mode)
    {
        EmailHeaderLexicalOptions options = new EmailHeaderLexicalOptions();
        options.SetMode(mode);
        return ScanImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Scans the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>The header lexical result produced by the operation.</returns>
    public EmailHeaderLexicalResult ScanWithInputAndMode(ReadOnlySpan<byte> input, EmailHeaderLexicalMode mode)
    {
        return EmailHeaderLexicalScanner.ScanImplementationWithInputAndMode(input, mode);
    }

    internal static EmailHeaderLexicalResult ScanImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailHeaderLexicalOptions options)
    {
        return ScanImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Scans the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header lexical result produced by the operation.</returns>
    public EmailHeaderLexicalResult ScanWithInputAndOptions(ReadOnlySpan<byte> input, EmailHeaderLexicalOptions options)
    {
        return EmailHeaderLexicalScanner.ScanImplementationWithInputAndOptions(input, options);
    }

    internal static EmailHeaderLexicalResult ScanImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailHeaderLexicalOptions options, EmailSourcePosition? initialPosition)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }

        byte[] source = input.ToArray();
        EmailHeaderLexicalWorker worker = new EmailHeaderLexicalWorker(source, options.Clone(), initialPosition);
        return worker.Scan();
    }

    /// <summary>
    /// Scans the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>The header lexical result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHeaderLexicalResult ScanWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailHeaderLexicalOptions options, EmailSourcePosition? initialPosition)
    {
        return EmailHeaderLexicalScanner.ScanImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }
}
