using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMimeType"/>.
/// </summary>
public interface IEmailMimeTypeContract
{
    /// <summary>
    /// Gets the media type for extension.
    /// </summary>
    /// <param name = "extension">The extension used by the operation.</param>
    /// <returns>The current media type for extension.</returns>
    string? GetMediaTypeForExtension(string extension);
    /// <summary>
    /// Gets the media type for filename.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The current media type for filename.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string? GetMediaTypeForFilename(string filename);
    /// <summary>
    /// Gets the preferred extension.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The current preferred extension.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string? GetPreferredExtensionWithMediaTypeUsingEmailMediaType(EmailMediaType mediaType);
    /// <summary>
    /// Gets the preferred extension.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The current preferred extension.</returns>
    string? GetPreferredExtensionWithMediaTypeUsingString(string mediaType);
    /// <summary>
    /// Detects the from magic.
    /// </summary>
    /// <param name = "prefix">The prefix used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    string? DetectFromMagic(ReadOnlySpan<byte> prefix);
    /// <summary>
    /// Determines whether the compared values are compatible.
    /// </summary>
    /// <param name = "firstMediaType">The first media type used by the operation.</param>
    /// <param name = "secondMediaType">The second media type used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool AreCompatible(string firstMediaType, string secondMediaType);
    /// <summary>
    /// Determines whether the current value or state is generic.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state is generic; otherwise, <c>false</c>.</returns>
    bool IsGeneric(string mediaType);
}
