using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBoundaryGenerationResult"/>.
/// </summary>
public interface IEmailBoundaryGenerationResultContract
{
    /// <summary>
    /// Gets the boundary.
    /// </summary>
    /// <returns>The current boundary.</returns>
    string GetBoundary();
    /// <summary>
    /// Gets the attempt count.
    /// </summary>
    /// <returns>The current attempt count.</returns>
    int GetAttemptCount();
    /// <summary>
    /// Gets the collision count.
    /// </summary>
    /// <returns>The current collision count.</returns>
    int GetCollisionCount();
    /// <summary>
    /// Gets the scanned byte count.
    /// </summary>
    /// <returns>The current scanned byte count.</returns>
    long GetScannedByteCount();
    /// <summary>
    /// Determines whether the current value or state is deterministic.
    /// </summary>
    /// <returns><c>true</c> when the value or state is deterministic; otherwise, <c>false</c>.</returns>
    bool IsDeterministic();
}
