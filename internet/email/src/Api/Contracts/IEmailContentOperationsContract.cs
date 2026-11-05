using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailContent"/>.
/// </summary>
public interface IEmailContentOperationsContract
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
    EmailContent CreateMemory(byte[]? value, EmailContentOwnership ownership, bool clearSensitiveBytes);
    /// <summary>
    /// Creates a memory copy configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "clearSensitiveBytes">Whether to enable the clear sensitive bytes behavior.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailContent CreateMemoryCopy(byte[] value, bool clearSensitiveBytes);
    /// <summary>
    /// Creates a file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <returns>The content produced by the operation.</returns>
    EmailContent CreateFile(string path, EmailContentOwnership ownership);
    /// <summary>
    /// Creates a temporary file configured for the requested behavior.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The content produced by the operation.</returns>
    EmailContent CreateTemporaryFile(string? path);
    /// <summary>
    /// Creates a stream configured for the requested behavior.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "ownership">The ownership used by the operation.</param>
    /// <returns>The content produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailContent CreateStream(Stream value, EmailContentOwnership ownership);
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
    EmailContent CreateSlice(EmailContent? source, long offset, long length);
}
