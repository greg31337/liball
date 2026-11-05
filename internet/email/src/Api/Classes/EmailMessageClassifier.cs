using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with message classifier.
/// </summary>
public class EmailMessageClassifier : IEmailMessageClassifierContract
{
    internal static EmailMessageClassification ClassifyImplementationWithMessage(EmailMessage message)
    {
        return ClassifyImplementationWithMessageAndOptions(message, new EmailMessageClassificationOptions());
    }

    /// <summary>
    /// Classifies the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The message classification produced by the operation.</returns>
    public EmailMessageClassification ClassifyWithMessage(EmailMessage message)
    {
        return EmailMessageClassifier.ClassifyImplementationWithMessage(message);
    }

    internal static EmailMessageClassification ClassifyImplementationWithMessageAndOptions(EmailMessage message, EmailMessageClassificationOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailMessageClassifierWorker worker = new EmailMessageClassifierWorker(message, options.Clone());
        return worker.Classify();
    }

    /// <summary>
    /// Classifies the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message classification produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageClassification ClassifyWithMessageAndOptions(EmailMessage message, EmailMessageClassificationOptions options)
    {
        return EmailMessageClassifier.ClassifyImplementationWithMessageAndOptions(message, options);
    }
}
