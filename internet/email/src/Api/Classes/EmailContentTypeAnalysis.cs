using System;

namespace email;
/// <summary>
/// Represents a content type analysis.
/// </summary>
/// <remarks>
/// Callers should observe the ownership and lifetime methods when the instance retains streams, temporary storage, or source buffers.
/// </remarks>
public class EmailContentTypeAnalysis : IEmailContentTypeAnalysisContract
{
    string declaredMediaType;
    string? filename;
    string? extensionMediaType;
    string? detectedMediaType;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new content type analysis.
    /// </summary>
    /// <param name = "declaredMediaType">The declared media type used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "extensionMediaType">The extension media type used by the operation.</param>
    /// <param name = "detectedMediaType">The detected media type used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailContentTypeAnalysis(string declaredMediaType, string? filename, string? extensionMediaType, string? detectedMediaType, EmailIssueList issues)
    {
        if (declaredMediaType == null || declaredMediaType.Length == 0)
            throw new ArgumentException("A declared media type is required.", "declaredMediaType");
        if (filename != null && filename.Length == 0)
            throw new ArgumentException("A filename cannot be empty when supplied.", "filename");
        if (extensionMediaType != null && extensionMediaType.Length == 0)
            throw new ArgumentException("An extension media type cannot be empty when supplied.", "extensionMediaType");
        if (detectedMediaType != null && detectedMediaType.Length == 0)
            throw new ArgumentException("A detected media type cannot be empty when supplied.", "detectedMediaType");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.declaredMediaType = declaredMediaType;
        this.filename = filename;
        this.extensionMediaType = extensionMediaType;
        this.detectedMediaType = detectedMediaType;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the declared media type.
    /// </summary>
    /// <returns>The current declared media type.</returns>
    public string GetDeclaredMediaType()
    {
        return declaredMediaType;
    }

    /// <summary>
    /// Determines whether the filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the filename is available; otherwise, <c>false</c>.</returns>
    public bool HasFilename()
    {
        return filename != null;
    }

    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    public string? GetFilename()
    {
        return filename;
    }

    /// <summary>
    /// Determines whether the extension media type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the extension media type is available; otherwise, <c>false</c>.</returns>
    public bool HasExtensionMediaType()
    {
        return extensionMediaType != null;
    }

    /// <summary>
    /// Gets the extension media type.
    /// </summary>
    /// <returns>The current extension media type.</returns>
    public string? GetExtensionMediaType()
    {
        return extensionMediaType;
    }

    /// <summary>
    /// Determines whether the detected media type is present or available.
    /// </summary>
    /// <returns><c>true</c> when the detected media type is available; otherwise, <c>false</c>.</returns>
    public bool HasDetectedMediaType()
    {
        return detectedMediaType != null;
    }

    /// <summary>
    /// Gets the detected media type.
    /// </summary>
    /// <returns>The current detected media type.</returns>
    public string? GetDetectedMediaType()
    {
        return detectedMediaType;
    }

    /// <summary>
    /// Gets the suggested media type.
    /// </summary>
    /// <returns>The current suggested media type.</returns>
    public string GetSuggestedMediaType()
    {
        bool generic = new EmailMimeType().IsGeneric(declaredMediaType);
        bool known = new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingString(declaredMediaType) != null;
        if (detectedMediaType != null && (generic || known && !new EmailMimeType().AreCompatible(declaredMediaType, detectedMediaType)))
            return detectedMediaType;
        if (extensionMediaType != null && (generic || known && !new EmailMimeType().AreCompatible(declaredMediaType, extensionMediaType)))
            return extensionMediaType;
        return declaredMediaType;
    }

    /// <summary>
    /// Determines whether the extension mismatch is present or available.
    /// </summary>
    /// <returns><c>true</c> when the extension mismatch is available; otherwise, <c>false</c>.</returns>
    public bool HasExtensionMismatch()
    {
        return issues.ContainsCode(EmailIssueCode.MediaTypeExtensionMismatch);
    }

    /// <summary>
    /// Determines whether the magic mismatch is present or available.
    /// </summary>
    /// <returns><c>true</c> when the magic mismatch is available; otherwise, <c>false</c>.</returns>
    public bool HasMagicMismatch()
    {
        return issues.ContainsCode(EmailIssueCode.MediaTypeMagicMismatch);
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }
}
