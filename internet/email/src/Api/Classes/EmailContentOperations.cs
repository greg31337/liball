using System;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailContent"/>.
/// </summary>
public class EmailContentOperations : IEmailContentOperationsContract
{
    /// <summary>
    /// Creates a memory configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <param name = "clearSensitiveBytes">Whether to enable the clear sensitive bytes behavior.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailContent CreateMemory(byte[]? value, EmailContentOwnership ownership, bool clearSensitiveBytes)
    {
        return EmailContent.CreateMemoryImplementation(value, ownership, clearSensitiveBytes);
    }

    /// <summary>
    /// Creates a memory copy configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "clearSensitiveBytes">Whether to enable the clear sensitive bytes behavior.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailContent CreateMemoryCopy(byte[] value, bool clearSensitiveBytes)
    {
        return EmailContent.CreateMemoryCopyImplementation(value, clearSensitiveBytes);
    }

    /// <summary>
    /// Creates a file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <returns>The content produced by the operation.</returns>
    public EmailContent CreateFile(string path, EmailContentOwnership ownership)
    {
        return EmailContent.CreateFileImplementation(path, ownership);
    }

    /// <summary>
    /// Creates a temporary file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The content produced by the operation.</returns>
    public EmailContent CreateTemporaryFile(string? path)
    {
        return EmailContent.CreateTemporaryFileImplementation(path);
    }

    /// <summary>
    /// Creates a stream configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailContent CreateStream(Stream value, EmailContentOwnership ownership)
    {
        return EmailContent.CreateStreamImplementation(value, ownership);
    }

    /// <summary>
    /// Creates a slice configured for the requested behavior.
    /// </summary>
    /// <param name = "source">The source content to read or copy from.</param>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "length">The length used by the operation.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "ObjectDisposedException">Thrown when the operation is attempted after the retained resource has been closed or disposed.</exception>
    public EmailContent CreateSlice(EmailContent? source, long offset, long length)
    {
        return EmailContent.CreateSliceImplementation(source, offset, length);
    }
}
