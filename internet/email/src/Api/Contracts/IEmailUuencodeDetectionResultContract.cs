using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailUuencodeDetectionResult"/>.
/// </summary>
public interface IEmailUuencodeDetectionResultContract
{
    /// <summary>
    /// Determines whether the current value or state is detected.
    /// </summary>
    /// <returns><c>true</c> when the value or state is detected; otherwise, <c>false</c>.</returns>
    bool IsDetected();
    /// <summary>
    /// Gets the file mode.
    /// </summary>
    /// <returns>The current file mode.</returns>
    int GetFileMode();
    /// <summary>
    /// Determines whether the file name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the file name is available; otherwise, <c>false</c>.</returns>
    bool HasFileName();
    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    string? GetFileName();
    /// <summary>
    /// Determines whether the header position is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header position is available; otherwise, <c>false</c>.</returns>
    bool HasHeaderPosition();
    /// <summary>
    /// Gets the header position.
    /// </summary>
    /// <returns>The current header position.</returns>
    EmailSourcePosition? GetHeaderPosition();
}
