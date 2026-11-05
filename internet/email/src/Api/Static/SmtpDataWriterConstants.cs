using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class SmtpDataWriterConstants
{
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    public const int DefaultBufferBytes = 81920;
}
