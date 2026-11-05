using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a received spf.
/// </summary>
public class EmailReceivedSpf : IEmailReceivedSpfContract
{
    string result;
    EmailAuthenticationResultKind resultKind;
    List<EmailAuthenticationComment> comments;
    List<EmailReceivedSpfParameter> parameters;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new received spf.
    /// </summary>
    /// <param name = "result">The result used by the operation.</param>
    /// <param name = "resultKind">The result kind used by the operation.</param>
    /// <param name = "comments">The comments used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailReceivedSpf(string result, EmailAuthenticationResultKind resultKind, List<EmailAuthenticationComment> comments, List<EmailReceivedSpfParameter> parameters, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (result == null || result.Length == 0)
        {
            throw new ArgumentException("A Received-SPF result is required.", "result");
        }

        if (resultKind < EmailAuthenticationResultKind.None || resultKind > EmailAuthenticationResultKind.Unknown)
        {
            throw new ArgumentOutOfRangeException("resultKind");
        }

        if (comments == null || parameters == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("comments");
        }

        this.result = result;
        this.resultKind = resultKind;
        this.comments = new List<EmailAuthenticationComment>(comments);
        this.parameters = new List<EmailReceivedSpfParameter>(parameters);
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
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
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    public int GetParameterCount()
    {
        return parameters.Count;
    }

    /// <summary>
    /// Gets the parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailReceivedSpfParameter GetParameter(int index)
    {
        if (index < 0 || index >= parameters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return parameters[index];
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    public List<EmailReceivedSpfParameter> GetParameters()
    {
        return new List<EmailReceivedSpfParameter>(parameters);
    }

    /// <summary>
    /// Gets the first parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first parameter.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailReceivedSpfParameter? GetFirstParameter(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < parameters.Count)
        {
            if (string.Equals(parameters[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                return parameters[index];
            }

            index++;
        }

        return null;
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
    /// Formats the supplied value.
    /// </summary>
    /// <returns>The authentication format result produced by the operation.</returns>
    public EmailAuthenticationFormatResult Format()
    {
        return new EmailAuthenticationFormatter().FormatReceivedSpfWithValue(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The authentication format result produced by the operation.</returns>
    public EmailAuthenticationFormatResult FormatWithOptions(EmailAuthenticationFormatOptions options)
    {
        return new EmailAuthenticationFormatter().FormatReceivedSpfWithValueAndOptions(this, options);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
