using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFlowedText"/>.
/// </summary>
public interface IEmailFlowedTextContract
{
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string DecodeWithValueAndDeleteSpace(string value, bool deleteSpace);
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string DecodeWithValueAndOptions(string value, EmailFlowedTextOptions options);
    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailFlowedTextDecodeResult DecodeResultWithValueAndDeleteSpace(string value, bool deleteSpace);
    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailFlowedTextDecodeResult DecodeResultWithValueAndOptions(string value, EmailFlowedTextOptions options);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string EncodeWithValueUsingString(string value);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    string EncodeWithValueAndDeleteSpace(string value, bool deleteSpace);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(string value, EmailFlowedTextOptions options);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string EncodeWithValueUsingEmailFlowedTextDecodeResult(EmailFlowedTextDecodeResult value);
    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string EncodeWithValueAndOptionsUsingEmailFlowedTextDecodeResultAndEmailFlowedTextOptions(EmailFlowedTextDecodeResult value, EmailFlowedTextOptions options);
}
