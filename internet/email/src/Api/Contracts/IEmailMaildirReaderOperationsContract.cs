using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirReader"/>.
/// </summary>
public interface IEmailMaildirReaderOperationsContract
{
    /// <summary>Creates a Maildir reader using default policy.</summary>
    /// <param name = "rootPath">The Maildir root path.</param>
    /// <returns>The configured Maildir reader.</returns>
    EmailMaildirReader CreateWithRootPath(string rootPath);
    /// <summary>
    /// Creates a new maildir reader.
    /// </summary>
    /// <param name = "rootPath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured Maildir reader.</returns>
    EmailMaildirReader CreateWithRootPathAndOptions(string rootPath, EmailMaildirReaderOptions options);
}
