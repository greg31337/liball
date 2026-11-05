using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRelatedIndex"/>.
/// </summary>
public interface IEmailRelatedIndexOperationsContract
{
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The related index produced by the operation.</returns>
    EmailRelatedIndex BuildWithRoot(MimeEntity? root);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The related index produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailRelatedIndex BuildWithRootAndOptions(MimeEntity? root, EmailRelatedIndexOptions options);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The related index produced by the operation.</returns>
    EmailRelatedIndex BuildWithMessage(EmailMessage message);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The related index produced by the operation.</returns>
    EmailRelatedIndex BuildWithMessageAndOptions(EmailMessage message, EmailRelatedIndexOptions options);
    /// <summary>
    /// Finds the matching HTML references.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<string> FindHtmlReferences(string html);
}
