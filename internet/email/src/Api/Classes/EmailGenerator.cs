using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Generates email data using explicit policy settings.
/// </summary>
public class EmailGenerator : IEmailGeneratorContract
{
    internal static EmailGenerationResult GenerateImplementationWithMessage(EmailMessage message)
    {
        return GenerateImplementationWithMessageAndOptions(message, new EmailGenerationOptions());
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The generation result produced by the operation.</returns>
    public EmailGenerationResult GenerateWithMessage(EmailMessage message)
    {
        return EmailGenerator.GenerateImplementationWithMessage(message);
    }

    internal static EmailGenerationResult GenerateImplementationWithMessageAndOptions(EmailMessage message, EmailGenerationOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailGeneratorWorker worker = new EmailGeneratorWorker(options.Clone());
        EmailMessage generated = worker.GenerateMessage(message);
        return new EmailGenerationResult(generated, null, worker.GetGeneratedValues());
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The generation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailGenerationResult GenerateWithMessageAndOptions(EmailMessage message, EmailGenerationOptions options)
    {
        return EmailGenerator.GenerateImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailGenerationResult GenerateImplementationWithEntity(MimeEntity entity)
    {
        return GenerateImplementationWithEntityAndOptions(entity, new EmailGenerationOptions());
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The generation result produced by the operation.</returns>
    public EmailGenerationResult GenerateWithEntity(MimeEntity entity)
    {
        return EmailGenerator.GenerateImplementationWithEntity(entity);
    }

    internal static EmailGenerationResult GenerateImplementationWithEntityAndOptions(MimeEntity entity, EmailGenerationOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailGeneratorWorker worker = new EmailGeneratorWorker(options.Clone());
        MimeEntity generated = worker.GenerateEntity(entity);
        return new EmailGenerationResult(null, generated, worker.GetGeneratedValues());
    }

    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The generation result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailGenerationResult GenerateWithEntityAndOptions(MimeEntity entity, EmailGenerationOptions options)
    {
        return EmailGenerator.GenerateImplementationWithEntityAndOptions(entity, options);
    }
}
