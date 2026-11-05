using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRelatedReference"/>.
/// </summary>
public interface IEmailRelatedReferenceContract
{
    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    string GetOriginalValue();
    /// <summary>
    /// Gets the normalized value.
    /// </summary>
    /// <returns>The current normalized value.</returns>
    string GetNormalizedValue();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailRelatedReferenceKind GetKind();
    /// <summary>
    /// Gets the source entity.
    /// </summary>
    /// <returns>The current source entity.</returns>
    MimeEntity GetSourceEntity();
    /// <summary>
    /// Gets the source path.
    /// </summary>
    /// <returns>The current source path.</returns>
    EmailPath GetSourcePath();
    /// <summary>
    /// Determines whether the current value or state is resolved.
    /// </summary>
    /// <returns><c>true</c> when the value or state is resolved; otherwise, <c>false</c>.</returns>
    bool IsResolved();
    /// <summary>
    /// Gets the resolved entity.
    /// </summary>
    /// <returns>The current resolved entity.</returns>
    MimeEntity? GetResolvedEntity();
    /// <summary>
    /// Determines whether the current value or state is missing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is missing; otherwise, <c>false</c>.</returns>
    bool IsMissing();
}
