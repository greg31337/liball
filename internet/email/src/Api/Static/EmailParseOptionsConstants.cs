using System;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailParseOptionsConstants
{
    /// <summary>
    /// The untrusted max message bytes value exposed by this API.
    /// </summary>
    public const long UntrustedMaxMessageBytes = 10485760;
    /// <summary>
    /// The untrusted max header bytes value exposed by this API.
    /// </summary>
    public const long UntrustedMaxHeaderBytes = 65536;
    /// <summary>
    /// The untrusted max header field count value exposed by this API.
    /// </summary>
    public const int UntrustedMaxHeaderFieldCount = 200;
    /// <summary>
    /// The untrusted max line bytes value exposed by this API.
    /// </summary>
    public const int UntrustedMaxLineBytes = 16384;
    /// <summary>
    /// The untrusted max nesting depth value exposed by this API.
    /// </summary>
    public const int UntrustedMaxNestingDepth = 15;
    /// <summary>
    /// The untrusted max part count value exposed by this API.
    /// </summary>
    public const int UntrustedMaxPartCount = 250;
    /// <summary>
    /// The untrusted max parameters per header value exposed by this API.
    /// </summary>
    public const int UntrustedMaxParametersPerHeader = 50;
    /// <summary>
    /// The untrusted max addresses per header value exposed by this API.
    /// </summary>
    public const int UntrustedMaxAddressesPerHeader = 200;
    /// <summary>
    /// The untrusted max encoded words per header value exposed by this API.
    /// </summary>
    public const int UntrustedMaxEncodedWordsPerHeader = 100;
    /// <summary>
    /// The untrusted max decoded bytes value exposed by this API.
    /// </summary>
    public const long UntrustedMaxDecodedBytes = 20971520;
    /// <summary>
    /// The untrusted max expansion ratio value exposed by this API.
    /// </summary>
    public const int UntrustedMaxExpansionRatio = 20;
    /// <summary>
    /// The untrusted memory threshold bytes value exposed by this API.
    /// </summary>
    public const long UntrustedMemoryThresholdBytes = 262144;
    /// <summary>
    /// The untrusted max temporary bytes value exposed by this API.
    /// </summary>
    public const long UntrustedMaxTemporaryBytes = 20971520;
    /// <summary>
    /// The untrusted max temporary file count value exposed by this API.
    /// </summary>
    public const int UntrustedMaxTemporaryFileCount = 16;
    /// <summary>
    /// The untrusted max open stream count value exposed by this API.
    /// </summary>
    public const int UntrustedMaxOpenStreamCount = 16;
    /// <summary>
    /// The untrusted max materialized bytes value exposed by this API.
    /// </summary>
    public const long UntrustedMaxMaterializedBytes = 10485760;
}
