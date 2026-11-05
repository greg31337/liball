using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailHtmlSanitizeOptionsConstants
{
    /// <summary>
    /// The default maximum mutation count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumMutationCount = 100000;
}
