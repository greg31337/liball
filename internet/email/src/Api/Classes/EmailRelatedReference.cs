using System;

namespace email;
/// <summary>
/// Represents a related reference.
/// </summary>
public class EmailRelatedReference : IEmailRelatedReferenceContract
{
    string originalValue;
    string normalizedValue;
    EmailRelatedReferenceKind kind;
    MimeEntity sourceEntity;
    EmailPath sourcePath;
    MimeEntity? resolvedEntity;
    bool missing;
    /// <summary>
    /// Creates a new related reference.
    /// </summary>
    /// <param name = "originalValue">The original value used by the operation.</param>
    /// <param name = "normalizedValue">The normalized value used by the operation.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "sourceEntity">The source entity used by the operation.</param>
    /// <param name = "sourcePath">The file-system or message path to use.</param>
    /// <param name = "resolvedEntity">The resolved entity used by the operation.</param>
    /// <param name = "missing">Whether to enable the missing behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRelatedReference(string originalValue, string normalizedValue, EmailRelatedReferenceKind kind, MimeEntity sourceEntity, EmailPath sourcePath, MimeEntity? resolvedEntity, bool missing)
    {
        if (originalValue == null || originalValue.Length == 0)
        {
            throw new ArgumentException("A related reference value is required.", "originalValue");
        }

        if (normalizedValue == null || normalizedValue.Length == 0)
        {
            throw new ArgumentException("A normalized related reference value is required.", "normalizedValue");
        }

        if (kind < EmailRelatedReferenceKind.ContentId || kind > EmailRelatedReferenceKind.External)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (sourceEntity == null)
        {
            throw new ArgumentNullException("sourceEntity");
        }

        if (sourcePath == null)
        {
            throw new ArgumentNullException("sourcePath");
        }

        if (missing && (kind == EmailRelatedReferenceKind.External || resolvedEntity != null))
        {
            throw new ArgumentException("Only unresolved local references can be missing.", "missing");
        }

        this.originalValue = originalValue;
        this.normalizedValue = normalizedValue;
        this.kind = kind;
        this.sourceEntity = sourceEntity;
        this.sourcePath = sourcePath;
        this.resolvedEntity = resolvedEntity;
        this.missing = missing;
    }

    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    public string GetOriginalValue()
    {
        return originalValue;
    }

    /// <summary>
    /// Gets the normalized value.
    /// </summary>
    /// <returns>The current normalized value.</returns>
    public string GetNormalizedValue()
    {
        return normalizedValue;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailRelatedReferenceKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the source entity.
    /// </summary>
    /// <returns>The current source entity.</returns>
    public MimeEntity GetSourceEntity()
    {
        return sourceEntity;
    }

    /// <summary>
    /// Gets the source path.
    /// </summary>
    /// <returns>The current source path.</returns>
    public EmailPath GetSourcePath()
    {
        return sourcePath;
    }

    /// <summary>
    /// Determines whether the current value or state is resolved.
    /// </summary>
    /// <returns><c>true</c> when the value or state is resolved; otherwise, <c>false</c>.</returns>
    public bool IsResolved()
    {
        return resolvedEntity != null;
    }

    /// <summary>
    /// Gets the resolved entity.
    /// </summary>
    /// <returns>The current resolved entity.</returns>
    public MimeEntity? GetResolvedEntity()
    {
        return resolvedEntity;
    }

    /// <summary>
    /// Determines whether the current value or state is missing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is missing; otherwise, <c>false</c>.</returns>
    public bool IsMissing()
    {
        return missing;
    }
}
