using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailRelatedIndex"/>.
/// </summary>
public class EmailRelatedIndexOperations : IEmailRelatedIndexOperationsContract
{
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The related index produced by the operation.</returns>
    public EmailRelatedIndex BuildWithRoot(MimeEntity? root)
    {
        return EmailRelatedIndex.BuildImplementationWithRoot(root);
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The related index produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailRelatedIndex BuildWithRootAndOptions(MimeEntity? root, EmailRelatedIndexOptions options)
    {
        return EmailRelatedIndex.BuildImplementationWithRootAndOptions(root, options);
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The related index produced by the operation.</returns>
    public EmailRelatedIndex BuildWithMessage(EmailMessage message)
    {
        return EmailRelatedIndex.BuildImplementationWithMessage(message);
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The related index produced by the operation.</returns>
    public EmailRelatedIndex BuildWithMessageAndOptions(EmailMessage message, EmailRelatedIndexOptions options)
    {
        return EmailRelatedIndex.BuildImplementationWithMessageAndOptions(message, options);
    }

    /// <summary>
    /// Finds the matching HTML references.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<string> FindHtmlReferences(string html)
    {
        return EmailRelatedIndex.FindHtmlReferencesImplementation(html);
    }
}
