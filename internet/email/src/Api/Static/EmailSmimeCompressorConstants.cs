using System;
using System.Formats.Asn1;
using System.IO;
using System.IO.Compression;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailSmimeCompressorConstants
{
    /// <summary>
    /// The zlib compression object identifier value exposed by this API.
    /// </summary>
    public const string ZlibCompressionObjectIdentifier = "1.2.840.113549.1.9.16.3.8";
}
