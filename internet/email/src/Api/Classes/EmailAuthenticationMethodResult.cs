using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a authentication method operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailAuthenticationMethodResult : IEmailAuthenticationMethodResultContract
{
    string method;
    bool versionPresent;
    int version;
    string result;
    EmailAuthenticationResultKind resultKind;
    bool reasonPresent;
    string? reason;
    bool reasonQuoted;
    List<EmailAuthenticationProperty> properties;
    List<EmailAuthenticationComment> comments;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    bool malformed;
    /// <summary>
    /// Creates a new authentication method result.
    /// </summary>
    /// <param name = "method">The method used by the operation.</param>
    /// <param name = "versionPresent">Whether to enable the version present behavior.</param>
    /// <param name = "version">The version used by the operation.</param>
    /// <param name = "result">The result used by the operation.</param>
    /// <param name = "resultKind">The result kind used by the operation.</param>
    /// <param name = "reasonPresent">Whether to enable the reason present behavior.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "reasonQuoted">Whether to enable the reason quoted behavior.</param>
    /// <param name = "properties">The properties used by the operation.</param>
    /// <param name = "comments">The comments used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "malformed">Whether to enable the malformed behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAuthenticationMethodResult(string method, bool versionPresent, int version, string result, EmailAuthenticationResultKind resultKind, bool reasonPresent, string? reason, bool reasonQuoted, List<EmailAuthenticationProperty> properties, List<EmailAuthenticationComment> comments, byte[] rawBytes, EmailSourcePosition sourcePosition, bool malformed)
    {
        if (method == null || method.Length == 0)
        {
            throw new ArgumentException("An authentication method is required.", "method");
        }

        if (versionPresent && version < 0)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        if (result == null || result.Length == 0)
        {
            throw new ArgumentException("An authentication result is required.", "result");
        }

        if (resultKind < EmailAuthenticationResultKind.None || resultKind > EmailAuthenticationResultKind.Unknown)
        {
            throw new ArgumentOutOfRangeException("resultKind");
        }

        if (reasonPresent && reason == null || !reasonPresent && reason != null)
        {
            throw new ArgumentException("Reason presence and value are inconsistent.", "reason");
        }

        if (properties == null || comments == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("properties");
        }

        this.method = method;
        this.versionPresent = versionPresent;
        this.version = version;
        this.result = result;
        this.resultKind = resultKind;
        this.reasonPresent = reasonPresent;
        this.reason = reason;
        this.reasonQuoted = reasonQuoted;
        this.properties = new List<EmailAuthenticationProperty>(properties);
        this.comments = new List<EmailAuthenticationComment>(comments);
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
        this.malformed = malformed;
    }

    /// <summary>
    /// Gets the method.
    /// </summary>
    /// <returns>The current method.</returns>
    public string GetMethod()
    {
        return method;
    }

    /// <summary>
    /// Gets the normalized method.
    /// </summary>
    /// <returns>The current normalized method.</returns>
    public string GetNormalizedMethod()
    {
        return method.ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    public bool HasVersion()
    {
        return versionPresent;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public int GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    public string GetResult()
    {
        return result;
    }

    /// <summary>
    /// Gets the normalized result.
    /// </summary>
    /// <returns>The current normalized result.</returns>
    public string GetNormalizedResult()
    {
        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the result kind.
    /// </summary>
    /// <returns>The current result kind.</returns>
    public EmailAuthenticationResultKind GetResultKind()
    {
        return resultKind;
    }

    /// <summary>
    /// Determines whether the reason is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reason is available; otherwise, <c>false</c>.</returns>
    public bool HasReason()
    {
        return reasonPresent;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public string? GetReason()
    {
        return reason;
    }

    /// <summary>
    /// Determines whether the operation or source value was reason quoted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasReasonQuoted()
    {
        return reasonQuoted;
    }

    /// <summary>
    /// Gets the property count.
    /// </summary>
    /// <returns>The current property count.</returns>
    public int GetPropertyCount()
    {
        return properties.Count;
    }

    /// <summary>
    /// Gets the property.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current property.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAuthenticationProperty GetProperty(int index)
    {
        if (index < 0 || index >= properties.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return properties[index];
    }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    public List<EmailAuthenticationProperty> GetProperties()
    {
        return new List<EmailAuthenticationProperty>(properties);
    }

    /// <summary>
    /// Gets the comment count.
    /// </summary>
    /// <returns>The current comment count.</returns>
    public int GetCommentCount()
    {
        return comments.Count;
    }

    /// <summary>
    /// Gets the comment.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current comment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAuthenticationComment GetComment(int index)
    {
        if (index < 0 || index >= comments.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return comments[index];
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    public bool IsMalformed()
    {
        return malformed;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
