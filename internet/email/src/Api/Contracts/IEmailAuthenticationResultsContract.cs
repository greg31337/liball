using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAuthenticationResults"/>.
/// </summary>
public interface IEmailAuthenticationResultsContract
{
    /// <summary>
    /// Gets the authentication service identifier.
    /// </summary>
    /// <returns>The current authentication service identifier.</returns>
    string GetAuthenticationServiceIdentifier();
    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    bool HasVersion();
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    int GetVersion();
    /// <summary>
    /// Determines whether the no results is present or available.
    /// </summary>
    /// <returns><c>true</c> when the no results is available; otherwise, <c>false</c>.</returns>
    bool HasNoResults();
    /// <summary>
    /// Gets the result count.
    /// </summary>
    /// <returns>The current result count.</returns>
    int GetResultCount();
    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current result.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAuthenticationMethodResult GetResult(int index);
    /// <summary>
    /// Gets the results.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current results.</returns>
    List<EmailAuthenticationMethodResult> GetResults();
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
