using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailTextAnalysisOptionsConstants
{
    /// <summary>
    /// The default maximum characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumCharacters = 10000000;
    /// <summary>
    /// The default maximum lines used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumLines = 100000;
    /// <summary>
    /// The default maximum snippet characters used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumSnippetCharacters = 200;
}
