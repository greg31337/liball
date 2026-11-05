using System;
using System.IO;

namespace email;
/// <summary>
/// Controls how storage operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailStorageOptions : IEmailStorageOptionsContract
{
    /// <summary>
    /// The default copy buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultCopyBufferBytes = 81920;
    EmailTemporaryStorageLimits limits;
    string temporaryDirectory;
    bool clearSensitiveBytes;
    int copyBufferBytes;
    /// <summary>
    /// Creates a new storage options object.
    /// </summary>
    public EmailStorageOptions()
    {
        limits = new EmailTemporaryStorageLimits();
        temporaryDirectory = Path.GetFullPath(Path.GetTempPath());
        clearSensitiveBytes = false;
        copyBufferBytes = DefaultCopyBufferBytes;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailTemporaryStorageLimits GetLimits()
    {
        return limits;
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailTemporaryStorageLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        limits = value;
    }

    /// <summary>
    /// Gets the temporary directory.
    /// </summary>
    /// <returns>The current temporary directory.</returns>
    public string GetTemporaryDirectory()
    {
        return temporaryDirectory;
    }

    /// <summary>
    /// Sets the temporary directory used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "DirectoryNotFoundException">Thrown when the operation encounters a directory not found.</exception>
    public void SetTemporaryDirectory(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Temporary directories cannot be empty.", "value");
        }

        string fullPath = Path.GetFullPath(value);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException("The temporary storage directory does not exist: " + fullPath);
        }

        temporaryDirectory = fullPath;
    }

    /// <summary>
    /// Gets the clears sensitive bytes.
    /// </summary>
    /// <returns><c>true</c> when the clears sensitive bytes setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetClearsSensitiveBytes()
    {
        return clearSensitiveBytes;
    }

    /// <summary>
    /// Sets the clears sensitive bytes used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the clears sensitive bytes behavior.</param>
    public void SetClearsSensitiveBytes(bool value)
    {
        clearSensitiveBytes = value;
    }

    /// <summary>
    /// Gets the copy buffer bytes.
    /// </summary>
    /// <returns>The current copy buffer bytes.</returns>
    public int GetCopyBufferBytes()
    {
        return copyBufferBytes;
    }

    /// <summary>
    /// Sets the copy buffer bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetCopyBufferBytes(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        copyBufferBytes = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailStorageOptions Clone()
    {
        EmailStorageOptions copy = new EmailStorageOptions();
        copy.SetLimits(limits.Clone());
        copy.SetTemporaryDirectory(temporaryDirectory);
        copy.SetClearsSensitiveBytes(clearSensitiveBytes);
        copy.SetCopyBufferBytes(copyBufferBytes);
        return copy;
    }
}
