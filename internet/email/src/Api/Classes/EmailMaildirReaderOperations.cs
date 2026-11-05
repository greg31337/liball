using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMaildirReader"/>.
/// </summary>
public class EmailMaildirReaderOperations : IEmailMaildirReaderOperationsContract
{
    /// <summary>Creates a Maildir reader using default policy.</summary>
    /// <param name = "rootPath">The Maildir root path.</param>
    /// <returns>The configured Maildir reader.</returns>
    public EmailMaildirReader CreateWithRootPath(string rootPath)
    {
        return EmailMaildirReader.CreateImplementationWithRootPath(rootPath);
    }

    /// <summary>
    /// Creates a new maildir reader.
    /// </summary>
    /// <param name = "rootPath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured Maildir reader.</returns>
    public EmailMaildirReader CreateWithRootPathAndOptions(string rootPath, EmailMaildirReaderOptions options)
    {
        return EmailMaildirReader.CreateImplementationWithRootPathAndOptions(rootPath, options);
    }
}
