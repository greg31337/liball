using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailContentTypeInspector"/>.
/// </summary>
public interface IEmailContentTypeInspectorContract
{
    /// <summary>
    /// Inspects the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The content type analysis produced by the operation.</returns>
    EmailContentTypeAnalysis InspectWithEntity(MimeEntity entity);
    /// <summary>
    /// Inspects the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The content type analysis produced by the operation.</returns>
    EmailContentTypeAnalysis InspectWithEntityAndFilename(MimeEntity entity, string filename);
    /// <summary>
    /// Inspects the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The content type analysis produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailContentTypeAnalysis InspectWithEntityAndFilenameAndPath(MimeEntity entity, string? filename, EmailPath? path);
}
