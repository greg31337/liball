using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFilenameResult"/>.
/// </summary>
public interface IEmailFilenameResultContract
{
    /// <summary>
    /// Determines whether the original filename is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original filename is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalFilename();
    /// <summary>
    /// Gets the original filename.
    /// </summary>
    /// <returns>The current original filename.</returns>
    string? GetOriginalFilename();
    /// <summary>
    /// Gets the filename.
    /// </summary>
    /// <returns>The current filename.</returns>
    string GetFilename();
    /// <summary>
    /// Determines whether the operation used fallback.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool UsedFallback();
    /// <summary>
    /// Determines whether the operation or source value was path stripped.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasPathStripped();
    /// <summary>
    /// Determines whether the source values were characters removed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WereCharactersRemoved();
    /// <summary>
    /// Determines whether the source values were characters replaced.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WereCharactersReplaced();
    /// <summary>
    /// Determines whether the operation or source value was unicode normalized.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasUnicodeNormalized();
    /// <summary>
    /// Determines whether the operation or source value was reserved name adjusted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasReservedNameAdjusted();
    /// <summary>
    /// Determines whether the operation or source value was truncated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasTruncated();
    /// <summary>
    /// Determines whether the operation encountered collision.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool HadCollision();
    /// <summary>
    /// Gets the collision index.
    /// </summary>
    /// <returns>The current collision index.</returns>
    int GetCollisionIndex();
    /// <summary>
    /// Determines whether the operation or source value was changed.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasChanged();
}
