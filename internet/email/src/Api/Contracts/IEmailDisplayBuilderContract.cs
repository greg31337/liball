using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDisplayBuilder"/>.
/// </summary>
public interface IEmailDisplayBuilderContract
{
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The display model produced by the operation.</returns>
    EmailDisplayModel BuildWithRoot(MimeEntity root);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The display model produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDisplayModel BuildWithRootAndOptions(MimeEntity root, EmailDisplayOptions options);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The display model produced by the operation.</returns>
    EmailDisplayModel BuildWithMessage(EmailMessage message);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The display model produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailDisplayModel BuildWithMessageAndOptions(EmailMessage message, EmailDisplayOptions options);
}
