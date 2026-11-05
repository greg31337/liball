using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFilename"/>.
/// </summary>
public interface IEmailFilenameContract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string? Decode(MimeEntity? entity);
    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    EmailFilenameResult SanitizeWithFilename(string? filename);
    /// <summary>
    /// Sanitizes the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailFilenameResult SanitizeWithFilenameAndOptions(string? filename, EmailFilenameOptions options);
    /// <summary>
    /// Suggests the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    EmailFilenameResult SuggestWithFilenameAndExistingFilenames(string? filename, List<string> existingFilenames);
    /// <summary>
    /// Suggests the supplied value.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "existingFilenames">The existing filenames used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The filename result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailFilenameResult SuggestWithFilenameAndExistingFilenamesAndOptions(string? filename, List<string> existingFilenames, EmailFilenameOptions options);
    /// <summary>
    /// Determines whether the current value or state is forbidden filename character.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is forbidden filename character; otherwise, <c>false</c>.</returns>
    bool IsForbiddenFilenameCharacter(char value);
}
