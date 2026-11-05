using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailStorageOptions"/>.
/// </summary>
public interface IEmailStorageOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailTemporaryStorageLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailTemporaryStorageLimits value);
    /// <summary>
    /// Gets the temporary directory.
    /// </summary>
    /// <returns>The current temporary directory.</returns>
    string GetTemporaryDirectory();
    /// <summary>
    /// Sets the temporary directory used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "DirectoryNotFoundException">Thrown when the operation encounters a directory not found.</exception>
    void SetTemporaryDirectory(string value);
    /// <summary>
    /// Gets the clears sensitive bytes.
    /// </summary>
    /// <returns><c>true</c> when the clears sensitive bytes setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetClearsSensitiveBytes();
    /// <summary>
    /// Sets the clears sensitive bytes used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the clears sensitive bytes behavior.</param>
    void SetClearsSensitiveBytes(bool value);
    /// <summary>
    /// Gets the copy buffer bytes.
    /// </summary>
    /// <returns>The current copy buffer bytes.</returns>
    int GetCopyBufferBytes();
    /// <summary>
    /// Sets the copy buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetCopyBufferBytes(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailStorageOptions Clone();
}
