using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Serializes messages and MIME entities using preserving or canonical output policies.
/// </summary>
public class EmailWriter : IEmailWriterContract
{
    internal static EmailStreamWriteResult WriteImplementationWithMessageAndOutput(EmailMessage message, Stream output)
    {
        return WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, output, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteWithMessageAndOutput(EmailMessage message, Stream output)
    {
        return EmailWriter.WriteImplementationWithMessageAndOutput(message, output);
    }

    internal static EmailStreamWriteResult WriteImplementationWithMessageAndOutputAndOptions(EmailMessage message, Stream output, EmailWriteOptions options)
    {
        return WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, output, options);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteWithMessageAndOutputAndOptions(EmailMessage message, Stream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteImplementationWithMessageAndOutputAndOptions(message, output, options);
    }

    internal static EmailWriteResult WriteImplementationWithMessage(EmailMessage message)
    {
        return WriteMessageImplementationWithMessageAndOptions(message, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The write result produced by the operation.</returns>
    public EmailWriteResult WriteWithMessage(EmailMessage message)
    {
        return EmailWriter.WriteImplementationWithMessage(message);
    }

    internal static EmailWriteResult WriteImplementationWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        return WriteMessageImplementationWithMessageAndOptions(message, options);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The write result produced by the operation.</returns>
    public EmailWriteResult WriteWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        return EmailWriter.WriteImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailWriteResult WriteMessageImplementationWithMessage(EmailMessage message)
    {
        return WriteMessageImplementationWithMessageAndOptions(message, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The write result produced by the operation.</returns>
    public EmailWriteResult WriteMessageWithMessage(EmailMessage message)
    {
        return EmailWriter.WriteMessageImplementationWithMessage(message);
    }

    internal static EmailWriteResult WriteMessageImplementationWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailWriterWorker worker = new EmailWriterWorker(message, options.Clone());
        return worker.Write();
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailWriteResult WriteMessageWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndStream(EmailMessage message, Stream output)
    {
        return WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, output, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndStream(EmailMessage message, Stream output)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndStream(message, output);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(EmailMessage message, Stream output, EmailWriteOptions options)
    {
        ValidateMessageOutput(message, options);
        ValidateStream(output);
        return EmailStreamingWriter.WriteMessage(message, options.Clone(), EmailOutputSink.CreateWithStream(output));
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(EmailMessage message, Stream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, output, options);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(EmailMessage message, EmailCountingStream output, EmailWriteOptions options)
    {
        ValidateMessageOutput(message, options);
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        return EmailStreamingWriter.WriteMessage(message, options.Clone(), EmailOutputSink.CreateWithCounter(output));
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(EmailMessage message, EmailCountingStream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(message, output, options);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndEmailCountingStream(EmailMessage message, EmailCountingStream output)
    {
        return WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(message, output, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndEmailCountingStream(EmailMessage message, EmailCountingStream output)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndEmailCountingStream(message, output);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(EmailMessage message, EmailHashingStream output, EmailWriteOptions options)
    {
        ValidateMessageOutput(message, options);
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        return EmailStreamingWriter.WriteMessage(message, options.Clone(), EmailOutputSink.CreateWithHashing(output));
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(EmailMessage message, EmailHashingStream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(message, output, options);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndEmailHashingStream(EmailMessage message, EmailHashingStream output)
    {
        return WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(message, output, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndEmailHashingStream(EmailMessage message, EmailHashingStream output)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndEmailHashingStream(message, output);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndSmtpDataWriterAndEmailWriteOptions(EmailMessage message, SmtpDataWriter output, EmailWriteOptions options)
    {
        ValidateMessageOutput(message, options);
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        return EmailStreamingWriter.WriteMessage(message, options.Clone(), EmailOutputSink.CreateWithSmtpData(output));
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndSmtpDataWriterAndEmailWriteOptions(EmailMessage message, SmtpDataWriter output, EmailWriteOptions options)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndSmtpDataWriterAndEmailWriteOptions(message, output, options);
    }

    internal static EmailStreamWriteResult WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndSmtpDataWriter(EmailMessage message, SmtpDataWriter output)
    {
        return WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndSmtpDataWriterAndEmailWriteOptions(message, output, new EmailWriteOptions());
    }

    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndSmtpDataWriter(EmailMessage message, SmtpDataWriter output)
    {
        return EmailWriter.WriteMessageImplementationWithMessageAndOutputUsingEmailMessageAndSmtpDataWriter(message, output);
    }

    internal static EmailWriteResult WriteEntityImplementationWithEntity(MimeEntity entity)
    {
        return WriteEntityImplementationWithEntityAndOptions(entity, new EmailWriteOptionsOperations().CreateCanonicalInternet());
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The write result produced by the operation.</returns>
    public EmailWriteResult WriteEntityWithEntity(MimeEntity entity)
    {
        return EmailWriter.WriteEntityImplementationWithEntity(entity);
    }

    internal static EmailWriteResult WriteEntityImplementationWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailWriteOptions snapshot = options.Clone();
        if (!snapshot.IsCanonical())
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.WritePolicyViolation, "Standalone MIME entity writing requires canonical mode because no containing source is available.", null, null, null, "Select canonical Internet or canonical SMTPUTF8 output."));
            long sourceBytes = entity.HasSourceRange() ? (entity.GetSourceRange() ?? throw new InvalidOperationException()).GetLength() : 0;
            return new EmailWriteResult(snapshot.GetMode(), new byte[0], issues, sourceBytes, 0, 0, 0, 0);
        }

        EmailCanonicalWriterWorker worker = new EmailCanonicalWriterWorker(snapshot);
        byte[]? bytes = worker.WriteEntity(entity);
        EmailIssueList workerIssues = worker.GetIssues();
        long entitySourceBytes = entity.HasSourceRange() ? (entity.GetSourceRange() ?? throw new InvalidOperationException()).GetLength() : 0;
        if (bytes == null)
        {
            return new EmailWriteResult(snapshot.GetMode(), new byte[0], workerIssues, entitySourceBytes, 0, 0, 0, 0);
        }

        return new EmailWriteResult(snapshot.GetMode(), bytes, workerIssues, entitySourceBytes, 0, bytes.Length, 0, 1);
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailWriteResult WriteEntityWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOptions(entity, options);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndStream(MimeEntity entity, Stream output)
    {
        return WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(entity, output, new EmailWriteOptionsOperations().CreateCanonicalInternet());
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndStream(MimeEntity entity, Stream output)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndStream(entity, output);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(MimeEntity entity, Stream output, EmailWriteOptions options)
    {
        ValidateEntityOutput(entity, options);
        ValidateStream(output);
        return EmailStreamingWriter.WriteEntity(entity, options.Clone(), EmailOutputSink.CreateWithStream(output));
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(MimeEntity entity, Stream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(entity, output, options);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(MimeEntity entity, EmailCountingStream output, EmailWriteOptions options)
    {
        ValidateEntityOutput(entity, options);
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        return EmailStreamingWriter.WriteEntity(entity, options.Clone(), EmailOutputSink.CreateWithCounter(output));
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(MimeEntity entity, EmailCountingStream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(entity, output, options);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndEmailCountingStream(MimeEntity entity, EmailCountingStream output)
    {
        return WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(entity, output, new EmailWriteOptionsOperations().CreateCanonicalInternet());
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndEmailCountingStream(MimeEntity entity, EmailCountingStream output)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndEmailCountingStream(entity, output);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(MimeEntity entity, EmailHashingStream output, EmailWriteOptions options)
    {
        ValidateEntityOutput(entity, options);
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        return EmailStreamingWriter.WriteEntity(entity, options.Clone(), EmailOutputSink.CreateWithHashing(output));
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(MimeEntity entity, EmailHashingStream output, EmailWriteOptions options)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(entity, output, options);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndEmailHashingStream(MimeEntity entity, EmailHashingStream output)
    {
        return WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(entity, output, new EmailWriteOptionsOperations().CreateCanonicalInternet());
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndEmailHashingStream(MimeEntity entity, EmailHashingStream output)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndEmailHashingStream(entity, output);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndSmtpDataWriterAndEmailWriteOptions(MimeEntity entity, SmtpDataWriter output, EmailWriteOptions options)
    {
        ValidateEntityOutput(entity, options);
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        return EmailStreamingWriter.WriteEntity(entity, options.Clone(), EmailOutputSink.CreateWithSmtpData(output));
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndSmtpDataWriterAndEmailWriteOptions(MimeEntity entity, SmtpDataWriter output, EmailWriteOptions options)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndSmtpDataWriterAndEmailWriteOptions(entity, output, options);
    }

    internal static EmailStreamWriteResult WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndSmtpDataWriter(MimeEntity entity, SmtpDataWriter output)
    {
        return WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndSmtpDataWriterAndEmailWriteOptions(entity, output, new EmailWriteOptionsOperations().CreateCanonicalInternet());
    }

    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    public EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndSmtpDataWriter(MimeEntity entity, SmtpDataWriter output)
    {
        return EmailWriter.WriteEntityImplementationWithEntityAndOutputUsingMimeEntityAndSmtpDataWriter(entity, output);
    }

    internal static byte[] ToArrayImplementationWithMessage(EmailMessage message)
    {
        return WriteMessageImplementationWithMessage(message).GetBytes();
    }

    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ToArrayWithMessage(EmailMessage message)
    {
        return EmailWriter.ToArrayImplementationWithMessage(message);
    }

    internal static byte[] ToArrayImplementationWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        return WriteMessageImplementationWithMessageAndOptions(message, options).GetBytes();
    }

    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ToArrayWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        return EmailWriter.ToArrayImplementationWithMessageAndOptions(message, options);
    }

    internal static byte[] ToArrayImplementationWithEntity(MimeEntity entity)
    {
        return WriteEntityImplementationWithEntity(entity).GetBytes();
    }

    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ToArrayWithEntity(MimeEntity entity)
    {
        return EmailWriter.ToArrayImplementationWithEntity(entity);
    }

    internal static byte[] ToArrayImplementationWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options)
    {
        return WriteEntityImplementationWithEntityAndOptions(entity, options).GetBytes();
    }

    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ToArrayWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options)
    {
        return EmailWriter.ToArrayImplementationWithEntityAndOptions(entity, options);
    }

    internal static EmailTextWriteResult WriteTextImplementationWithMessage(EmailMessage message)
    {
        return WriteTextImplementationWithMessageAndOptions(message, new EmailTextWriteOptions());
    }

    /// <summary>
    /// Serializes a message known to contain only textual MIME leaves and strictly converts the canonical bytes to text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The text write result produced by the operation.</returns>
    public EmailTextWriteResult WriteTextWithMessage(EmailMessage message)
    {
        return EmailWriter.WriteTextImplementationWithMessage(message);
    }

    internal static EmailTextWriteResult WriteTextImplementationWithMessageAndOptions(EmailMessage message, EmailTextWriteOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailTextWriteOptions snapshot = options.Clone();
        EmailIssueList issues = new EmailIssueList();
        if (message.HasBody())
        {
            ValidateTextualEntity(message.GetBody() ?? throw new InvalidOperationException(), snapshot, issues);
        }

        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Error))
        {
            return ApplyTextFailureBehavior(new EmailTextWriteResult(null, snapshot.GetCharsetName(), null, false, snapshot.GetDecodeTransferEncodings(), null, issues), snapshot);
        }

        EmailWriteResult writeResult = WriteMessageImplementationWithMessageAndOptions(message, CreateBoundedWriteOptions(snapshot));
        return ConvertWriteResultToText(writeResult, snapshot, issues);
    }

    /// <summary>
    /// Serializes a message known to contain only textual MIME leaves and strictly converts the byte-writer output to text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown for an unsuccessful result when the configured failure behavior is <see cref = "EmailTextWriteFailureBehavior.Throw"/>.</exception>
    public EmailTextWriteResult WriteTextWithMessageAndOptions(EmailMessage message, EmailTextWriteOptions options)
    {
        return EmailWriter.WriteTextImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailTextWriteResult WriteTextImplementationWithEntity(MimeEntity entity)
    {
        return WriteTextImplementationWithEntityAndOptions(entity, new EmailTextWriteOptions());
    }

    /// <summary>
    /// Serializes a MIME entity known to contain only textual leaves and strictly converts the canonical bytes to text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The text write result produced by the operation.</returns>
    public EmailTextWriteResult WriteTextWithEntity(MimeEntity entity)
    {
        return EmailWriter.WriteTextImplementationWithEntity(entity);
    }

    internal static EmailTextWriteResult WriteTextImplementationWithEntityAndOptions(MimeEntity entity, EmailTextWriteOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailTextWriteOptions snapshot = options.Clone();
        EmailIssueList issues = new EmailIssueList();
        ValidateTextualEntity(entity, snapshot, issues);
        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Error))
        {
            return ApplyTextFailureBehavior(new EmailTextWriteResult(null, snapshot.GetCharsetName(), null, false, snapshot.GetDecodeTransferEncodings(), null, issues), snapshot);
        }

        EmailWriteResult writeResult = WriteEntityImplementationWithEntityAndOptions(entity, CreateBoundedWriteOptions(snapshot));
        return ConvertWriteResultToText(writeResult, snapshot, issues);
    }

    /// <summary>
    /// Serializes a MIME entity known to contain only textual leaves and strictly converts the byte-writer output to text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown for an unsuccessful result when the configured failure behavior is <see cref = "EmailTextWriteFailureBehavior.Throw"/>.</exception>
    public EmailTextWriteResult WriteTextWithEntityAndOptions(MimeEntity entity, EmailTextWriteOptions options)
    {
        return EmailWriter.WriteTextImplementationWithEntityAndOptions(entity, options);
    }

    internal static long CountBytesImplementationWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        EmailCountingStream counter = new EmailCountingStream();
        EmailStreamWriteResult result = WriteMessageImplementationWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(message, counter, options);
        EnsureSuccessfulCount(result);
        return counter.GetByteCount();
    }

    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The computed count or size.</returns>
    public long CountBytesWithMessageAndOptions(EmailMessage message, EmailWriteOptions options)
    {
        return EmailWriter.CountBytesImplementationWithMessageAndOptions(message, options);
    }

    internal static long CountBytesImplementationWithMessage(EmailMessage message)
    {
        return CountBytesImplementationWithMessageAndOptions(message, new EmailWriteOptions());
    }

    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The computed count or size.</returns>
    public long CountBytesWithMessage(EmailMessage message)
    {
        return EmailWriter.CountBytesImplementationWithMessage(message);
    }

    internal static long CountBytesImplementationWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options)
    {
        EmailCountingStream counter = new EmailCountingStream();
        EmailStreamWriteResult result = WriteEntityImplementationWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(entity, counter, options);
        EnsureSuccessfulCount(result);
        return counter.GetByteCount();
    }

    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The computed count or size.</returns>
    public long CountBytesWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options)
    {
        return EmailWriter.CountBytesImplementationWithEntityAndOptions(entity, options);
    }

    internal static long CountBytesImplementationWithEntity(MimeEntity entity)
    {
        return CountBytesImplementationWithEntityAndOptions(entity, new EmailWriteOptionsOperations().CreateCanonicalInternet());
    }

    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The computed count or size.</returns>
    public long CountBytesWithEntity(MimeEntity entity)
    {
        return EmailWriter.CountBytesImplementationWithEntity(entity);
    }

    static EmailWriteOptions CreateBoundedWriteOptions(EmailTextWriteOptions options)
    {
        EmailWriteOptions writeOptions = options.GetWriteOptions();
        if (writeOptions.GetMaximumOutputBytes() > options.GetMaximumBytes())
        {
            writeOptions.SetMaximumOutputBytes(options.GetMaximumBytes());
        }

        return writeOptions;
    }

    static EmailTextWriteResult ConvertWriteResultToText(EmailWriteResult writeResult, EmailTextWriteOptions options, EmailIssueList issues)
    {
        issues.AddRange(writeResult.GetIssues());
        if (!writeResult.IsSuccess())
        {
            return ApplyTextFailureBehavior(new EmailTextWriteResult(null, options.GetCharsetName(), null, false, options.GetDecodeTransferEncodings(), writeResult, issues), options);
        }

        byte[] bytes = writeResult.GetBytes();
        if (bytes.Length > options.GetMaximumBytes())
        {
            issues.Add(CreateTextIssue(EmailIssueSeverity.Error, EmailIssueCode.TextSerializationTransferDecodeFailed, "The serialized byte output exceeds the configured text serialization limit.", "No text was produced."));
            return ApplyTextFailureBehavior(new EmailTextWriteResult(null, options.GetCharsetName(), null, false, options.GetDecodeTransferEncodings(), writeResult, issues), options);
        }

        EmailCharsetDecodeOptions charsetOptions = new EmailCharsetDecodeOptions();
        charsetOptions.SetFallbackPolicy(EmailCharsetFallbackPolicy.Strict);
        charsetOptions.SetBomPolicy(EmailCharsetBomPolicy.Ignore);
        charsetOptions.SetMaximumCharacters(options.GetMaximumCharacters());
        EmailCharsetDecodeResult decoded = new EmailCharsetRegistry().Decode(bytes, options.GetCharsetName(), charsetOptions);
        issues.AddRange(decoded.GetIssues());
        if (!decoded.HasResolvedCharset() || issues.HasSeverityAtLeast(EmailIssueSeverity.Error))
        {
            return ApplyTextFailureBehavior(new EmailTextWriteResult(null, options.GetCharsetName(), decoded.GetResolvedCharset(), false, options.GetDecodeTransferEncodings(), writeResult, issues), options);
        }

        string decodedText = decoded.GetText();
        string normalized = NormalizeTextNewlines(decodedText, options.GetNewlinePolicy());
        bool changed = !string.Equals(decodedText, normalized, StringComparison.Ordinal);
        if (changed)
        {
            issues.Add(CreateTextIssue(EmailIssueSeverity.Information, EmailIssueCode.TextNewlineNormalized, "Serialized text line endings were normalized.", "The byte-oriented write result was retained unchanged."));
        }

        return ApplyTextFailureBehavior(new EmailTextWriteResult(normalized, options.GetCharsetName(), decoded.GetResolvedCharset(), changed, options.GetDecodeTransferEncodings(), writeResult, issues), options);
    }

    static void ValidateTextualEntity(MimeEntity entity, EmailTextWriteOptions options, EmailIssueList issues)
    {
        if (entity.IsContainer())
        {
            int childIndex = 0;
            while (childIndex < entity.GetChildCount())
            {
                ValidateTextualEntity(entity.GetChildAt(childIndex), options, issues);
                childIndex++;
            }

            return;
        }

        if (entity.HasNestedMessage())
        {
            EmailMessage nested = entity.GetNestedMessage() ?? throw new InvalidOperationException();
            if (nested.HasBody())
            {
                ValidateTextualEntity(nested.GetBody() ?? throw new InvalidOperationException(), options, issues);
            }

            return;
        }

        EmailMediaType mediaType = entity.GetMediaType() ?? throw new InvalidOperationException();
        if (!string.Equals(mediaType.GetTopLevelType(), "text", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateTextIssue(EmailIssueSeverity.Error, EmailIssueCode.TextSerializationNotTextual, "The MIME tree contains a non-textual leaf with media type " + mediaType.GetTopLevelType() + "/" + mediaType.GetSubtype() + ".", "Use the byte-oriented writer for messages or entities containing non-textual data."));
            return;
        }

        if (!options.GetDecodeTransferEncodings() || !entity.HasRawContent() && !entity.HasDecodedContent())
        {
            return;
        }

        if (entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding())
        {
            issues.Add(CreateTextIssue(EmailIssueSeverity.Error, EmailIssueCode.TextSerializationTransferDecodeFailed, "A textual MIME leaf uses an unsupported content-transfer-encoding.", "Use the byte-oriented writer or supply decoded content."));
            return;
        }

        EmailContent content = entity.HasDecodedContent() ? entity.GetDecodedContent() ?? throw new InvalidOperationException() : entity.GetRawContent() ?? throw new InvalidOperationException();
        if (content.HasKnownLength() && content.GetLength() > options.GetMaximumBytes())
        {
            issues.Add(CreateTextIssue(EmailIssueSeverity.Error, EmailIssueCode.TextSerializationTransferDecodeFailed, "A textual MIME leaf exceeds the configured transfer-decoding limit.", "Increase the text serialization byte limit or use the byte-oriented writer."));
            return;
        }

        EmailTextDecodeOptions decodeOptions = new EmailTextDecodeOptions();
        decodeOptions.SetFallbackCharset(options.GetCharsetName());
        long maximumDecodedBytes = options.GetMaximumBytes();
        if (maximumDecodedBytes > int.MaxValue)
        {
            maximumDecodedBytes = int.MaxValue;
        }

        decodeOptions.SetMaximumDecodedBytes(maximumDecodedBytes);
        EmailCharsetDecodeOptions leafCharsetOptions = decodeOptions.GetCharsetOptions();
        leafCharsetOptions.SetFallbackPolicy(EmailCharsetFallbackPolicy.Strict);
        leafCharsetOptions.SetMaximumCharacters(options.GetMaximumCharacters());
        decodeOptions.SetCharsetOptions(leafCharsetOptions);
        EmailTextDecodeResult decodeResult = new EmailTextDecoder().DecodeWithEntityAndOptions(entity, decodeOptions);
        issues.AddRange(decodeResult.GetIssues());
        if (!decodeResult.IsSuccess())
        {
            issues.Add(CreateTextIssue(EmailIssueSeverity.Error, EmailIssueCode.TextSerializationTransferDecodeFailed, "A textual MIME leaf could not be transfer-decoded and charset-decoded without errors.", "Use the byte-oriented writer or correct the MIME content metadata."));
        }
    }

    static string NormalizeTextNewlines(string value, EmailTextNewlinePolicy policy)
    {
        if (policy == EmailTextNewlinePolicy.Preserve)
        {
            return value;
        }

        StringBuilder output = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r')
            {
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index++;
                }

                output.Append(policy == EmailTextNewlinePolicy.NormalizeToLf ? "\n" : "\r\n");
            }
            else if (current == '\n')
            {
                output.Append(policy == EmailTextNewlinePolicy.NormalizeToLf ? "\n" : "\r\n");
            }
            else
            {
                output.Append(current);
            }

            index++;
        }

        return output.ToString();
    }

    static EmailIssue CreateTextIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, null, recovery);
    }

    static EmailTextWriteResult ApplyTextFailureBehavior(EmailTextWriteResult result, EmailTextWriteOptions options)
    {
        if (!result.IsSuccess() && options.GetFailureBehavior() == EmailTextWriteFailureBehavior.Throw)
        {
            throw new InvalidOperationException("Text serialization failed under the configured policy.");
        }

        return result;
    }

    static void ValidateMessageOutput(EmailMessage message, EmailWriteOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    static void ValidateEntityOutput(MimeEntity entity, EmailWriteOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    static void ValidateStream(Stream output)
    {
        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", "output");
        }
    }

    static void EnsureSuccessfulCount(EmailStreamWriteResult result)
    {
        if (!result.IsSuccess())
        {
            throw new InvalidOperationException("The email could not be serialized for byte counting.");
        }
    }
}
