using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageClassifier"/>.
/// </summary>
public interface IEmailMessageClassifierContract
{
    /// <summary>
    /// Classifies the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The message classification produced by the operation.</returns>
    EmailMessageClassification ClassifyWithMessage(EmailMessage message);
    /// <summary>
    /// Classifies the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message classification produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMessageClassification ClassifyWithMessageAndOptions(EmailMessage message, EmailMessageClassificationOptions options);
}
