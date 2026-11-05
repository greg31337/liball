using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReceivedSpf"/>.
/// </summary>
public interface IEmailReceivedSpfContract
{
    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    string GetResult();
    /// <summary>
    /// Gets the normalized result.
    /// </summary>
    /// <returns>The current normalized result.</returns>
    string GetNormalizedResult();
    /// <summary>
    /// Gets the result kind.
    /// </summary>
    /// <returns>The current result kind.</returns>
    EmailAuthenticationResultKind GetResultKind();
    /// <summary>
    /// Gets the comment count.
    /// </summary>
    /// <returns>The current comment count.</returns>
    int GetCommentCount();
    /// <summary>
    /// Gets the comment.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current comment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAuthenticationComment GetComment(int index);
    /// <summary>
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    int GetParameterCount();
    /// <summary>
    /// Gets the parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailReceivedSpfParameter GetParameter(int index);
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    List<EmailReceivedSpfParameter> GetParameters();
    /// <summary>
    /// Gets the first parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first parameter.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailReceivedSpfParameter? GetFirstParameter(string name);
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The authentication format result produced by the operation.</returns>
    EmailAuthenticationFormatResult Format();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    EmailAuthenticationFormatResult FormatWithOptions(EmailAuthenticationFormatOptions options);
}
