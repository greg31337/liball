using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailForwardBuilder"/>.
/// </summary>
public interface IEmailForwardBuilderContract
{
    /// <summary>
    /// Creates an inline configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateInlineWithOriginalAndAuthor(EmailMessage original, EmailAddress author);
    /// <summary>
    /// Creates an inline configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "forwardText">The forward text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateInlineWithOriginalAndAuthorAndForwardText(EmailMessage original, EmailAddress author, string forwardText);
    /// <summary>
    /// Creates an inline configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateInlineWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailForwardOptions options);
    /// <summary>
    /// Creates an attached message configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateAttachedMessageWithOriginalAndAuthor(EmailMessage original, EmailAddress author);
    /// <summary>
    /// Creates an attached message configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "forwardText">The forward text used by the operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateAttachedMessageWithOriginalAndAuthorAndForwardText(EmailMessage original, EmailAddress author, string forwardText);
    /// <summary>
    /// Creates an attached message configured for the requested behavior.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "author">The author used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The composer produced by the operation.</returns>
    EmailComposer CreateAttachedMessageWithOriginalAndAuthorAndOptions(EmailMessage original, EmailAddress author, EmailForwardOptions options);
    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string NormalizeSubject(string? value);
}
