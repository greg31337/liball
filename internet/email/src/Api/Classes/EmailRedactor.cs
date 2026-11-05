using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace email;
/// <summary>
/// Provides operations for working with redactor.
/// </summary>
public class EmailRedactor : IEmailRedactorContract
{
    internal static EmailRedactionResult RedactImplementationWithMessage(EmailMessage message)
    {
        return RedactImplementationWithMessageAndOptions(message, new EmailRedactionOptions());
    }

    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    public EmailRedactionResult RedactWithMessage(EmailMessage message)
    {
        return EmailRedactor.RedactImplementationWithMessage(message);
    }

    internal static EmailRedactionResult RedactImplementationWithMessageAndOptions(EmailMessage message, EmailRedactionOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailRedactorWorker worker = new EmailRedactorWorker(options.Clone());
        EmailMessage redacted = worker.RedactMessage(message);
        return new EmailRedactionResult(redacted, null, worker.GetChanges());
    }

    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailRedactionResult RedactWithMessageAndOptions(EmailMessage message, EmailRedactionOptions options)
    {
        return EmailRedactor.RedactImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailRedactionResult RedactImplementationWithEntity(MimeEntity entity)
    {
        return RedactImplementationWithEntityAndOptions(entity, new EmailRedactionOptions());
    }

    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    public EmailRedactionResult RedactWithEntity(MimeEntity entity)
    {
        return EmailRedactor.RedactImplementationWithEntity(entity);
    }

    internal static EmailRedactionResult RedactImplementationWithEntityAndOptions(MimeEntity entity, EmailRedactionOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailRedactorWorker worker = new EmailRedactorWorker(options.Clone());
        MimeEntity redacted = worker.RedactEntity(entity);
        return new EmailRedactionResult(null, redacted, worker.GetChanges());
    }

    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailRedactionResult RedactWithEntityAndOptions(MimeEntity entity, EmailRedactionOptions options)
    {
        return EmailRedactor.RedactImplementationWithEntityAndOptions(entity, options);
    }
}
