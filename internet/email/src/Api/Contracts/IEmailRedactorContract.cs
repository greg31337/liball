using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRedactor"/>.
/// </summary>
public interface IEmailRedactorContract
{
    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    EmailRedactionResult RedactWithMessage(EmailMessage message);
    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailRedactionResult RedactWithMessageAndOptions(EmailMessage message, EmailRedactionOptions options);
    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    EmailRedactionResult RedactWithEntity(MimeEntity entity);
    /// <summary>
    /// Redacts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The redaction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailRedactionResult RedactWithEntityAndOptions(MimeEntity entity, EmailRedactionOptions options);
}
