using System;

namespace email;
/// <summary>
/// Describes the outcome of a uuencode detection operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailUuencodeDetectionResult : IEmailUuencodeDetectionResultContract
{
    bool detected;
    int fileMode;
    string? fileName;
    EmailSourcePosition? headerPosition;
    EmailUuencodeDetectionResult(bool detected, int fileMode, string? fileName, EmailSourcePosition? headerPosition)
    {
        this.detected = detected;
        this.fileMode = fileMode;
        this.fileName = fileName;
        this.headerPosition = headerPosition;
    }

    internal static EmailUuencodeDetectionResult CreateNotDetectedImplementation()
    {
        return new EmailUuencodeDetectionResult(false, 0, null, null);
    }

    internal static EmailUuencodeDetectionResult CreateDetectedImplementation(int fileMode, string fileName, EmailSourcePosition headerPosition)
    {
        new EmailUuencode().ValidateMetadata(fileMode, fileName);
        if (headerPosition == null)
        {
            throw new ArgumentNullException("headerPosition");
        }

        return new EmailUuencodeDetectionResult(true, fileMode, fileName, headerPosition);
    }

    /// <summary>
    /// Determines whether the current value or state is detected.
    /// </summary>
    /// <returns><c>true</c> when the value or state is detected; otherwise, <c>false</c>.</returns>
    public bool IsDetected()
    {
        return detected;
    }

    /// <summary>
    /// Gets the file mode.
    /// </summary>
    /// <returns>The current file mode.</returns>
    public int GetFileMode()
    {
        return fileMode;
    }

    /// <summary>
    /// Determines whether the file name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the file name is available; otherwise, <c>false</c>.</returns>
    public bool HasFileName()
    {
        return fileName != null;
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    /// <returns>The current file name.</returns>
    public string? GetFileName()
    {
        return fileName;
    }

    /// <summary>
    /// Determines whether the header position is present or available.
    /// </summary>
    /// <returns><c>true</c> when the header position is available; otherwise, <c>false</c>.</returns>
    public bool HasHeaderPosition()
    {
        return headerPosition != null;
    }

    /// <summary>
    /// Gets the header position.
    /// </summary>
    /// <returns>The current header position.</returns>
    public EmailSourcePosition? GetHeaderPosition()
    {
        return headerPosition;
    }
}
