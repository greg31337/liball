using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailUuencodeDetectionResult"/>.
/// </summary>
public class EmailUuencodeDetectionResultOperations : IEmailUuencodeDetectionResultOperationsContract
{
    /// <summary>
    /// Creates a not detected configured for the requested behavior.
    /// </summary>
    /// <returns>The uuencode detection result produced by the operation.</returns>
    public EmailUuencodeDetectionResult CreateNotDetected()
    {
        return EmailUuencodeDetectionResult.CreateNotDetectedImplementation();
    }

    /// <summary>
    /// Creates a detected configured for the requested behavior.
    /// </summary>
    /// <param name = "fileMode">The file mode used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <param name = "headerPosition">The header position used by the operation.</param>
    /// <returns>The uuencode detection result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailUuencodeDetectionResult CreateDetected(int fileMode, string fileName, EmailSourcePosition headerPosition)
    {
        return EmailUuencodeDetectionResult.CreateDetectedImplementation(fileMode, fileName, headerPosition);
    }
}
