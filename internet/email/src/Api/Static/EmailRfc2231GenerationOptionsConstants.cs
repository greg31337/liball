using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailRfc2231GenerationOptionsConstants
{
    /// <summary>
    /// The default maximum line length used when the caller does not provide an override.
    /// </summary>
    public const int DefaultMaximumLineLength = 78;
    /// <summary>
    /// The maximum header line length value exposed by this API.
    /// </summary>
    public const int MaximumHeaderLineLength = 998;
}
