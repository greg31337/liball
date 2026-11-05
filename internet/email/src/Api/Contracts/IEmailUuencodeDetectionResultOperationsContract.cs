using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailUuencodeDetectionResult"/>.
/// </summary>
public interface IEmailUuencodeDetectionResultOperationsContract
{
    /// <summary>
    /// Creates a not detected configured for the requested behavior.
    /// </summary>
    /// <returns>The uuencode detection result produced by the operation.</returns>
    EmailUuencodeDetectionResult CreateNotDetected();
    /// <summary>
    /// Creates a detected configured for the requested behavior.
    /// </summary>
    /// <param name = "fileMode">The file mode used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <param name = "headerPosition">The header position used by the operation.</param>
    /// <returns>The uuencode detection result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailUuencodeDetectionResult CreateDetected(int fileMode, string fileName, EmailSourcePosition headerPosition);
}
