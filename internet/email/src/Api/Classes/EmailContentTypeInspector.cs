using System;

namespace email;
/// <summary>
/// Provides operations for working with content type inspector.
/// </summary>
/// <remarks>
/// Callers should observe the ownership and lifetime methods when the instance retains streams, temporary storage, or source buffers.
/// </remarks>
public class EmailContentTypeInspector : IEmailContentTypeInspectorContract
{
    /// <summary>
    /// The maximum magic prefix bytes value exposed by this API.
    /// </summary>
    internal const int MaximumMagicPrefixBytes = 64;
    internal static EmailContentTypeAnalysis InspectImplementationWithEntity(MimeEntity entity)
    {
        return InspectImplementationWithEntityAndFilenameAndPath(entity, new EmailFilename().Decode(entity), null);
    }

    /// <summary>
    /// Inspects the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The content type analysis produced by the operation.</returns>
    public EmailContentTypeAnalysis InspectWithEntity(MimeEntity entity)
    {
        return EmailContentTypeInspector.InspectImplementationWithEntity(entity);
    }

    internal static EmailContentTypeAnalysis InspectImplementationWithEntityAndFilename(MimeEntity entity, string filename)
    {
        return InspectImplementationWithEntityAndFilenameAndPath(entity, filename, null);
    }

    /// <summary>
    /// Inspects the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The content type analysis produced by the operation.</returns>
    public EmailContentTypeAnalysis InspectWithEntityAndFilename(MimeEntity entity, string filename)
    {
        return EmailContentTypeInspector.InspectImplementationWithEntityAndFilename(entity, filename);
    }

    internal static EmailContentTypeAnalysis InspectImplementationWithEntityAndFilenameAndPath(MimeEntity entity, string? filename, EmailPath? path)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (filename != null && filename.Length == 0)
            throw new ArgumentException("A filename cannot be empty when supplied.", "filename");
        string declared = (entity.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedTopLevelType() + "/" + (entity.GetMediaType() ?? throw new InvalidOperationException()).GetNormalizedSubtype();
        string? extensionType = filename == null ? null : new EmailMimeType().GetMediaTypeForFilename(filename);
        string? detected = CanInspectContent(entity) ? new EmailMimeType().DetectFromMagic(new EmailDecodedContent().ReadPrefixWithEntityAndMaximumPrefixBytes(entity, MaximumMagicPrefixBytes)) : null;
        EmailIssueList issues = new EmailIssueList();
        if (ShouldReportMismatch(declared, extensionType))
        {
            issues.Add(CreateIssue(EmailIssueCode.MediaTypeExtensionMismatch, "The filename suggests media type " + extensionType + " but Content-Type declares " + declared + ".", entity, path, "Treat the filename extension and declared media type as untrusted metadata."));
        }

        if (ShouldReportMismatch(declared, detected))
        {
            issues.Add(CreateIssue(EmailIssueCode.MediaTypeMagicMismatch, "A strong content signature suggests media type " + detected + " but Content-Type declares " + declared + ".", entity, path, "No content was changed; use the mismatch when applying display or download policy."));
        }

        return new EmailContentTypeAnalysis(declared, filename, extensionType, detected, issues);
    }

    /// <summary>
    /// Inspects the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The content type analysis produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailContentTypeAnalysis InspectWithEntityAndFilenameAndPath(MimeEntity entity, string? filename, EmailPath? path)
    {
        return EmailContentTypeInspector.InspectImplementationWithEntityAndFilenameAndPath(entity, filename, path);
    }

    static bool CanInspectContent(MimeEntity entity)
    {
        if (!entity.HasContent())
            return false;
        if (entity.HasDecodedContent())
            return true;
        return !entity.HasTransferEncoding() || entity.HasKnownTransferEncoding();
    }

    static bool ShouldReportMismatch(string declared, string? observed)
    {
        if (observed == null || new EmailMimeType().IsGeneric(declared))
            return false;
        if (new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingString(declared) == null)
            return false;
        return !new EmailMimeType().AreCompatible(declared, observed);
    }

    static EmailIssue CreateIssue(EmailIssueCode code, string description, MimeEntity entity, EmailPath? path, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, code, description, (entity.GetMediaType() ?? throw new InvalidOperationException()).GetSourcePosition(), path, "Content-Type", recovery);
    }
}
