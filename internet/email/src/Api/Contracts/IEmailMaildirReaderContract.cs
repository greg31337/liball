using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMaildirReader"/>.
/// </summary>
public interface IEmailMaildirReaderContract
{
    /// <summary>
    /// Gets the root path.
    /// </summary>
    /// <returns>The current root path.</returns>
    string? GetRootPath();
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailMaildirReaderOptions GetOptions();
    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailMaildirEntry> Enumerate();
    /// <summary>
    /// Gets the entries.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current entries.</returns>
    List<EmailMaildirEntry> GetEntries();
    /// <summary>
    /// Reads the next from the input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ReadNext();
    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <returns>The maildir entry produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    /// <exception cref = "FileNotFoundException">Thrown when the operation encounters a file not found.</exception>
    EmailMaildirEntry Read(EmailMaildirFolder folder, string fileName);
    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <param name = "relativePath">The file-system or message path to use.</param>
    /// <returns>The maildir entry produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailMaildirEntry ReadRelativePath(string relativePath);
    /// <summary>
    /// Determines whether the current is present or available.
    /// </summary>
    /// <returns><c>true</c> when the current is available; otherwise, <c>false</c>.</returns>
    bool HasCurrent();
    /// <summary>
    /// Gets the current.
    /// </summary>
    /// <returns>The current current.</returns>
    EmailMaildirEntry? GetCurrent();
    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    int GetEntryCount();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
