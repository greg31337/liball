using System;
using System.IO;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailStorageOptionsConstants
{
    /// <summary>
    /// The default copy buffer bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultCopyBufferBytes = 81920;
}
