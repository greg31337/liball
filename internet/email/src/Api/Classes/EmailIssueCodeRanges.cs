namespace email;
/// <summary>
/// Represents an issue code ranges.
/// </summary>
public class EmailIssueCodeRanges : IEmailIssueCodeRangesContract
{
    /// <summary>
    /// The general start value exposed by this API.
    /// </summary>
    internal const int GeneralStart = 1000;
    /// <summary>
    /// The general end value exposed by this API.
    /// </summary>
    internal const int GeneralEnd = 1999;
    /// <summary>
    /// The codec start value exposed by this API.
    /// </summary>
    internal const int CodecStart = 2000;
    /// <summary>
    /// The codec end value exposed by this API.
    /// </summary>
    internal const int CodecEnd = 2999;
    /// <summary>
    /// The header start value exposed by this API.
    /// </summary>
    internal const int HeaderStart = 3000;
    /// <summary>
    /// The header end value exposed by this API.
    /// </summary>
    internal const int HeaderEnd = 3999;
    /// <summary>
    /// The MIME start value exposed by this API.
    /// </summary>
    internal const int MimeStart = 4000;
    /// <summary>
    /// The MIME end value exposed by this API.
    /// </summary>
    internal const int MimeEnd = 4999;
    /// <summary>
    /// The writing start value exposed by this API.
    /// </summary>
    internal const int WritingStart = 5000;
    /// <summary>
    /// The writing end value exposed by this API.
    /// </summary>
    internal const int WritingEnd = 5999;
    /// <summary>
    /// The processing start value exposed by this API.
    /// </summary>
    internal const int ProcessingStart = 6000;
    /// <summary>
    /// The processing end value exposed by this API.
    /// </summary>
    internal const int ProcessingEnd = 6999;
    /// <summary>
    /// The format start value exposed by this API.
    /// </summary>
    internal const int FormatStart = 7000;
    /// <summary>
    /// The format end value exposed by this API.
    /// </summary>
    internal const int FormatEnd = 7999;
    /// <summary>
    /// The authentication start value exposed by this API.
    /// </summary>
    internal const int AuthenticationStart = 8000;
    /// <summary>
    /// The authentication end value exposed by this API.
    /// </summary>
    internal const int AuthenticationEnd = 8999;
    /// <summary>
    /// The cryptography start value exposed by this API.
    /// </summary>
    internal const int CryptographyStart = 9000;
    /// <summary>
    /// The cryptography end value exposed by this API.
    /// </summary>
    internal const int CryptographyEnd = 9999;
    internal static bool IsLibraryCodeImplementation(int code)
    {
        return code >= GeneralStart && code <= CryptographyEnd;
    }

    internal static int GetRangeStartImplementation(EmailIssueCode code)
    {
        int value = (int)code;
        if (!IsLibraryCodeImplementation(value))
        {
            return 0;
        }

        return value / 1000 * 1000;
    }
}
