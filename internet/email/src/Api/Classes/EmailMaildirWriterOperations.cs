using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMaildirWriter"/>.
/// </summary>
public class EmailMaildirWriterOperations : IEmailMaildirWriterOperationsContract
{
    /// <summary>Creates a Maildir writer using default policy.</summary>
    /// <param name = "rootPath">The Maildir root path.</param>
    /// <returns>The configured Maildir writer.</returns>
    public EmailMaildirWriter CreateWithRootPath(string rootPath)
    {
        return EmailMaildirWriter.CreateImplementationWithRootPath(rootPath);
    }

    /// <summary>
    /// Creates a new maildir writer.
    /// </summary>
    /// <param name = "rootPath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured Maildir writer.</returns>
    public EmailMaildirWriter CreateWithRootPathAndOptions(string rootPath, EmailMaildirWriterOptions options)
    {
        return EmailMaildirWriter.CreateImplementationWithRootPathAndOptions(rootPath, options);
    }
}
