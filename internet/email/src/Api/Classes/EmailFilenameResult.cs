using System;

namespace email;
/// <summary>
/// Describes the outcome of a filename operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailFilenameResult : IEmailFilenameResultContract
{
    string? originalFilename;
    string filename;
    bool usedFallback;
    bool pathStripped;
    bool charactersRemoved;
    bool charactersReplaced;
    bool unicodeNormalized;
    bool reservedNameAdjusted;
    bool truncated;
    int collisionIndex;
    /// <summary>
    /// Creates a new filename result.
    /// </summary>
    /// <param name = "originalFilename">The original filename used by the operation.</param>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <param name = "usedFallback">Whether to enable the used fallback behavior.</param>
    /// <param name = "pathStripped">Whether to enable the path stripped behavior.</param>
    /// <param name = "charactersRemoved">Whether to enable the characters removed behavior.</param>
    /// <param name = "charactersReplaced">Whether to enable the characters replaced behavior.</param>
    /// <param name = "unicodeNormalized">Whether to enable the unicode normalized behavior.</param>
    /// <param name = "reservedNameAdjusted">Whether to enable the reserved name adjusted behavior.</param>
    /// <param name = "truncated">Whether to enable the truncated behavior.</param>
    /// <param name = "collisionIndex">The zero-based collision index.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailFilenameResult(string? originalFilename, string filename, bool usedFallback, bool pathStripped, bool charactersRemoved, bool charactersReplaced, bool unicodeNormalized, bool reservedNameAdjusted, bool truncated, int collisionIndex)
    {
        if (filename == null)
        {
            throw new ArgumentNullException("filename");
        }

        if (filename.Length == 0)
        {
            throw new ArgumentException("A processed filename cannot be empty.", "filename");
        }

        if (collisionIndex == 1 || collisionIndex < 0)
        {
            throw new ArgumentOutOfRangeException("collisionIndex");
        }

        this.originalFilename = originalFilename;
        this.filename = filename;
        this.usedFallback = usedFallback;
        this.pathStripped = pathStripped;
        this.charactersRemoved = charactersRemoved;
        this.charactersReplaced = charactersReplaced;
        this.unicodeNormalized = unicodeNormalized;
        this.reservedNameAdjusted = reservedNameAdjusted;
        this.truncated = truncated;
        this.collisionIndex = collisionIndex;
    }

    /// <summary>
    /// Determines whether the original filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original filename is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalFilename()
    {
        return originalFilename != null;
    }

    /// <summary>
    /// Gets the original filename.
    /// </summary>
    /// <returns>The current original filename.</returns>
    public string? GetOriginalFilename()
    {
        return originalFilename;
    }

    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    public string GetFilename()
    {
        return filename;
    }

    /// <summary>
    /// Determines whether the operation used fallback.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool UsedFallback()
    {
        return usedFallback;
    }

    /// <summary>
    /// Determines whether the operation or source value was path stripped.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasPathStripped()
    {
        return pathStripped;
    }

    /// <summary>
    /// Determines whether the source values were characters removed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WereCharactersRemoved()
    {
        return charactersRemoved;
    }

    /// <summary>
    /// Determines whether the source values were characters replaced.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WereCharactersReplaced()
    {
        return charactersReplaced;
    }

    /// <summary>
    /// Determines whether the operation or source value was unicode normalized.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasUnicodeNormalized()
    {
        return unicodeNormalized;
    }

    /// <summary>
    /// Determines whether the operation or source value was reserved name adjusted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasReservedNameAdjusted()
    {
        return reservedNameAdjusted;
    }

    /// <summary>
    /// Determines whether the operation or source value was truncated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return truncated;
    }

    /// <summary>
    /// Determines whether the operation encountered collision.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool HadCollision()
    {
        return collisionIndex >= 2;
    }

    /// <summary>
    /// Gets the collision index.
    /// </summary>
    /// <returns>The current collision index.</returns>
    public int GetCollisionIndex()
    {
        return collisionIndex;
    }

    /// <summary>
    /// Determines whether the operation or source value was changed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasChanged()
    {
        return usedFallback || pathStripped || charactersRemoved || charactersReplaced || unicodeNormalized || reservedNameAdjusted || truncated || collisionIndex >= 2;
    }
}
