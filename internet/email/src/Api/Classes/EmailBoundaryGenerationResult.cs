using System;

namespace email;
/// <summary>
/// Describes the outcome of a boundary generation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailBoundaryGenerationResult : IEmailBoundaryGenerationResultContract
{
    string boundary;
    int attemptCount;
    int collisionCount;
    long scannedByteCount;
    bool deterministic;
    /// <summary>
    /// Creates a new boundary generation result.
    /// </summary>
    /// <param name = "boundary">The boundary used by the operation.</param>
    /// <param name = "attemptCount">The number of items or bytes to process.</param>
    /// <param name = "collisionCount">The number of items or bytes to process.</param>
    /// <param name = "scannedByteCount">The number of items or bytes to process.</param>
    /// <param name = "deterministic">Whether to enable the deterministic behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBoundaryGenerationResult(string boundary, int attemptCount, int collisionCount, long scannedByteCount, bool deterministic)
    {
        if (boundary == null)
        {
            throw new ArgumentNullException("boundary");
        }

        if (boundary.Length == 0)
        {
            throw new ArgumentException("A generated boundary cannot be empty.", "boundary");
        }

        if (attemptCount < 1 || collisionCount < 0 || collisionCount >= attemptCount || scannedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("attemptCount");
        }

        this.boundary = boundary;
        this.attemptCount = attemptCount;
        this.collisionCount = collisionCount;
        this.scannedByteCount = scannedByteCount;
        this.deterministic = deterministic;
    }

    /// <summary>
    /// Gets the boundary.
    /// </summary>
    /// <returns>The current boundary.</returns>
    public string GetBoundary()
    {
        return boundary;
    }

    /// <summary>
    /// Gets the attempt count.
    /// </summary>
    /// <returns>The current attempt count.</returns>
    public int GetAttemptCount()
    {
        return attemptCount;
    }

    /// <summary>
    /// Gets the collision count.
    /// </summary>
    /// <returns>The current collision count.</returns>
    public int GetCollisionCount()
    {
        return collisionCount;
    }

    /// <summary>
    /// Gets the scanned byte count.
    /// </summary>
    /// <returns>The current scanned byte count.</returns>
    public long GetScannedByteCount()
    {
        return scannedByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is deterministic.
    /// </summary>
    /// <returns><c>true</c> when the value or state is deterministic; otherwise, <c>false</c>.</returns>
    public bool IsDeterministic()
    {
        return deterministic;
    }
}
