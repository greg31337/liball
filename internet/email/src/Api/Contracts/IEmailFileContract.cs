using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFile"/>.
/// </summary>
public interface IEmailFileContract
{
    /// <summary>
    /// Loads the supplied value.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The file load result produced by the operation.</returns>
    EmailFileLoadResult LoadWithPath(string path);
    /// <summary>
    /// Loads the supplied value.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The file load result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "UnauthorizedAccessException">Thrown when the operation encounters an unauthorized access.</exception>
    EmailFileLoadResult LoadWithPathAndOptions(string path, EmailFileLoadOptions options);
    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The file save result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "DirectoryNotFoundException">Thrown when the operation encounters a directory not found.</exception>
    /// <exception cref = "IOException">Thrown when the underlying storage or stream operation fails.</exception>
    /// <exception cref = "UnauthorizedAccessException">Thrown when the operation encounters an unauthorized access.</exception>
    EmailFileSaveResult Save(EmailMessage message, string path, EmailFileSaveOptions options);
}
