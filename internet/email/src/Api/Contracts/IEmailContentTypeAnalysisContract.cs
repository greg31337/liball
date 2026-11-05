using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailContentTypeAnalysis"/>.
/// </summary>
public interface IEmailContentTypeAnalysisContract
{
    /// <summary>
    /// Gets the declared media type.
    /// </summary>
    /// <returns>The current declared media type.</returns>
    string GetDeclaredMediaType();
    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    bool HasFilename();
    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    string? GetFilename();
    /// <summary>
    /// Determines whether the extension media type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the extension media type is available; otherwise, <c>false</c>.</returns>
    bool HasExtensionMediaType();
    /// <summary>
    /// Gets the extension media type.
    /// </summary>
    /// <returns>The current extension media type.</returns>
    string? GetExtensionMediaType();
    /// <summary>
    /// Determines whether the detected media type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the detected media type is available; otherwise, <c>false</c>.</returns>
    bool HasDetectedMediaType();
    /// <summary>
    /// Gets the detected media type.
    /// </summary>
    /// <returns>The current detected media type.</returns>
    string? GetDetectedMediaType();
    /// <summary>
    /// Gets the suggested media type.
    /// </summary>
    /// <returns>The current suggested media type.</returns>
    string GetSuggestedMediaType();
    /// <summary>
    /// Determines whether the extension mismatch is present or available.
    /// </summary>
    /// <returns><c>true</c> when the extension mismatch is available; otherwise, <c>false</c>.</returns>
    bool HasExtensionMismatch();
    /// <summary>
    /// Determines whether the magic mismatch is present or available.
    /// </summary>
    /// <returns><c>true</c> when the magic mismatch is available; otherwise, <c>false</c>.</returns>
    bool HasMagicMismatch();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
