using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReplyBuilder"/>.
/// </summary>
public interface IEmailReplyBuilderContract
{
    /// <summary>
    /// Creates a reply configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReplyWithOriginalAndAuthor(EmailMessage original, EmailAddress author);
    /// <summary>
    /// Creates a reply configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "replyText">The reply text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReplyWithOriginalAndAuthorAndReplyText(EmailMessage original, EmailAddress author, string replyText);
    /// <summary>
    /// Creates a reply configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReplyWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailReplyOptions options);
    /// <summary>
    /// Creates a reply all configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReplyAllWithOriginalAndAuthor(EmailMessage original, EmailAddress author);
    /// <summary>
    /// Creates a reply all configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "replyText">The reply text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReplyAllWithOriginalAndAuthorAndReplyText(EmailMessage original, EmailAddress author, string replyText);
    /// <summary>
    /// Creates a reply all configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateReplyAllWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailReplyOptions options);
    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string NormalizeSubject(string? value);
}
