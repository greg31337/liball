using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailBase64Constants
{
    /// <summary>
    /// The default line length used when the caller does not provide an override.
    /// </summary>
    public const int DefaultLineLength = 76;
    /// <summary>
    /// The minimum line length value exposed by this API.
    /// </summary>
    public const int MinimumLineLength = 4;
    /// <summary>
    /// The maximum line length value exposed by this API.
    /// </summary>
    public const int MaximumLineLength = 76;
}
