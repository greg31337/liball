using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailBoundaryGeneratorConstants
{
    /// <summary>
    /// The default random byte count used when the caller does not provide an override.
    /// </summary>
    public const int DefaultRandomByteCount = 24;
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultBufferBytes = 81920;
    /// <summary>
    /// The maximum attempts value exposed by this API.
    /// </summary>
    public const int MaximumAttempts = 64;
    /// <summary>
    /// The boundary prefix value exposed by this API.
    /// </summary>
    public const string BoundaryPrefix = "=_email_";
}
