using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailPartialMessageAssemblyOptionsConstants
{
    /// <summary>
    /// The default maximum fragment bytes used when the caller does not provide an override.
    /// </summary>
    public const long DefaultMaximumFragmentBytes = EmailArchiveLimitsConstants.DefaultMaxEntryBytes;
}
