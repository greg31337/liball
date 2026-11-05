using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Writes header in its wire or storage format.
/// </summary>
public class EmailHeaderWriter : IEmailHeaderWriterContract
{
    internal static EmailHeaderWriteResult WriteRawImplementationWithHeaders(EmailHeaders headers)
    {
        return WriteImplementation(headers, EmailHeaderWriteMode.Raw, new EmailHeaderWriteOptions());
    }

    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WriteRawWithHeaders(EmailHeaders headers)
    {
        return EmailHeaderWriter.WriteRawImplementationWithHeaders(headers);
    }

    internal static EmailHeaderWriteResult WriteRawImplementationWithHeadersAndOptions(EmailHeaders headers, EmailHeaderWriteOptions options)
    {
        return WriteImplementation(headers, EmailHeaderWriteMode.Raw, options);
    }

    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WriteRawWithHeadersAndOptions(EmailHeaders headers, EmailHeaderWriteOptions options)
    {
        return EmailHeaderWriter.WriteRawImplementationWithHeadersAndOptions(headers, options);
    }

    internal static EmailHeaderWriteResult WritePreservingImplementationWithHeaders(EmailHeaders headers)
    {
        return WriteImplementation(headers, EmailHeaderWriteMode.Preserving, new EmailHeaderWriteOptions());
    }

    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WritePreservingWithHeaders(EmailHeaders headers)
    {
        return EmailHeaderWriter.WritePreservingImplementationWithHeaders(headers);
    }

    internal static EmailHeaderWriteResult WritePreservingImplementationWithHeadersAndOptions(EmailHeaders? headers, EmailHeaderWriteOptions options)
    {
        return WriteImplementation(headers, EmailHeaderWriteMode.Preserving, options);
    }

    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WritePreservingWithHeadersAndOptions(EmailHeaders? headers, EmailHeaderWriteOptions options)
    {
        return EmailHeaderWriter.WritePreservingImplementationWithHeadersAndOptions(headers, options);
    }

    internal static EmailHeaderWriteResult WriteCanonicalImplementationWithHeaders(EmailHeaders headers)
    {
        return WriteImplementation(headers, EmailHeaderWriteMode.Canonical, new EmailHeaderWriteOptions());
    }

    /// <summary>
    /// Writes the supplied canonical to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WriteCanonicalWithHeaders(EmailHeaders headers)
    {
        return EmailHeaderWriter.WriteCanonicalImplementationWithHeaders(headers);
    }

    internal static EmailHeaderWriteResult WriteCanonicalImplementationWithHeadersAndOptions(EmailHeaders headers, EmailHeaderWriteOptions options)
    {
        return WriteImplementation(headers, EmailHeaderWriteMode.Canonical, options);
    }

    /// <summary>
    /// Writes the supplied canonical to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WriteCanonicalWithHeadersAndOptions(EmailHeaders headers, EmailHeaderWriteOptions options)
    {
        return EmailHeaderWriter.WriteCanonicalImplementationWithHeadersAndOptions(headers, options);
    }

    internal static EmailHeaderWriteResult WriteImplementation(EmailHeaders? headers, EmailHeaderWriteMode mode, EmailHeaderWriteOptions options)
    {
        Validate(headers, mode, options);
        EmailHeaderWriterWorker worker = new EmailHeaderWriterWorker(headers, mode, options.Clone(), EmailLineEnding.CrLf, true);
        return worker.Write();
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult Write(EmailHeaders? headers, EmailHeaderWriteMode mode, EmailHeaderWriteOptions options)
    {
        return EmailHeaderWriter.WriteImplementation(headers, mode, options);
    }

    internal static EmailHeaderWriteResult WriteRawImplementationWithBlock(EmailHeaderBlockParseResult block)
    {
        return WriteBlock(block, EmailHeaderWriteMode.Raw, new EmailHeaderWriteOptions());
    }

    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WriteRawWithBlock(EmailHeaderBlockParseResult block)
    {
        return EmailHeaderWriter.WriteRawImplementationWithBlock(block);
    }

    internal static EmailHeaderWriteResult WriteRawImplementationWithBlockAndOptions(EmailHeaderBlockParseResult block, EmailHeaderWriteOptions options)
    {
        return WriteBlock(block, EmailHeaderWriteMode.Raw, options);
    }

    /// <summary>
    /// Writes the supplied raw to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WriteRawWithBlockAndOptions(EmailHeaderBlockParseResult block, EmailHeaderWriteOptions options)
    {
        return EmailHeaderWriter.WriteRawImplementationWithBlockAndOptions(block, options);
    }

    internal static EmailHeaderWriteResult WritePreservingImplementationWithBlock(EmailHeaderBlockParseResult block)
    {
        return WriteBlock(block, EmailHeaderWriteMode.Preserving, new EmailHeaderWriteOptions());
    }

    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WritePreservingWithBlock(EmailHeaderBlockParseResult block)
    {
        return EmailHeaderWriter.WritePreservingImplementationWithBlock(block);
    }

    internal static EmailHeaderWriteResult WritePreservingImplementationWithBlockAndOptions(EmailHeaderBlockParseResult block, EmailHeaderWriteOptions options)
    {
        return WriteBlock(block, EmailHeaderWriteMode.Preserving, options);
    }

    /// <summary>
    /// Writes the supplied preserving to the requested destination.
    /// </summary>
    /// <param name = "block">The block used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The header write result produced by the operation.</returns>
    public EmailHeaderWriteResult WritePreservingWithBlockAndOptions(EmailHeaderBlockParseResult block, EmailHeaderWriteOptions options)
    {
        return EmailHeaderWriter.WritePreservingImplementationWithBlockAndOptions(block, options);
    }

    static EmailHeaderWriteResult WriteBlock(EmailHeaderBlockParseResult block, EmailHeaderWriteMode mode, EmailHeaderWriteOptions options)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        Validate(block.GetHeaders(), mode, options);
        EmailLineEnding separator = block.HasSeparator() ? block.GetSeparatorLineEnding() : EmailLineEnding.None;
        EmailHeaderWriterWorker worker = new EmailHeaderWriterWorker(block.GetHeaders(), mode, options.Clone(), separator, block.HasSeparator());
        return worker.Write();
    }

    static void Validate(EmailHeaders? headers, EmailHeaderWriteMode mode, EmailHeaderWriteOptions options)
    {
        if (headers == null)
        {
            throw new ArgumentNullException("headers");
        }

        if (mode < EmailHeaderWriteMode.Raw || mode > EmailHeaderWriteMode.Canonical)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }
}
