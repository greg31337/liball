using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailAddressFormatOptionsConstants
{
    /// <summary>
    /// The default maximum line length used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumLineLength = 76;
    /// <summary>
    /// The default maximum encoded words used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumEncodedWords = EmailEncodedWordConstants.DefaultMaximumEncodedWords;
}
