using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirWriter"/>.
/// </summary>
public interface IEmailMaildirWriterOperationsContract
{
    /// <summary>Creates a Maildir writer using default policy.</summary>
    /// <param name = "rootPath">The Maildir root path.</param>
    /// <returns>The configured Maildir writer.</returns>
    EmailMaildirWriter CreateWithRootPath(string rootPath);
    /// <summary>
    /// Creates a new maildir writer.
    /// </summary>
    /// <param name = "rootPath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured Maildir writer.</returns>
    EmailMaildirWriter CreateWithRootPathAndOptions(string rootPath, EmailMaildirWriterOptions options);
}
