using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAuthenticationMethodResult"/>.
/// </summary>
public interface IEmailAuthenticationMethodResultContract
{
    /// <summary>
    /// Gets the method.
    /// </summary>
    /// <returns>The current method.</returns>
    string GetMethod();
    /// <summary>
    /// Gets the normalized method.
    /// </summary>
    /// <returns>The current normalized method.</returns>
    string GetNormalizedMethod();
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
    /// Determines whether the reason is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reason is available; otherwise, <c>false</c>.</returns>
    bool HasReason();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string? GetReason();
    /// <summary>
    /// Determines whether the operation or source value was reason quoted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasReasonQuoted();
    /// <summary>
    /// Gets the property count.
    /// </summary>
    /// <returns>The current property count.</returns>
    int GetPropertyCount();
    /// <summary>
    /// Gets the property.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current property.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAuthenticationProperty GetProperty(int index);
    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    List<EmailAuthenticationProperty> GetProperties();
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
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    bool IsMalformed();
}
