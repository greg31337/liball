using System;
using System.Net;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlEntityDecoder"/>.
/// </summary>
public interface IEmailHtmlEntityDecoderContract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string DecodeWithValue(string value);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "maximumOutputCharacters">The maximum output characters used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string DecodeWithValueAndMaximumOutputCharacters(string value, int maximumOutputCharacters);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "limits">The limits used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string DecodeWithValueAndLimits(string value, EmailHtmlLimits limits);
    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailHtmlEntityDecodeResult DecodeResultWithValue(string value);
    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "maximumOutputCharacters">The maximum output characters used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailHtmlEntityDecodeResult DecodeResultWithValueAndMaximumOutputCharacters(string value, int maximumOutputCharacters);
    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "limits">The limits used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailHtmlEntityDecodeResult DecodeResultWithValueAndLimits(string value, EmailHtmlLimits limits);
}
