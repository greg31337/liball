using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an auto submitted.
/// </summary>
public class EmailAutoSubmitted : IEmailAutoSubmittedContract
{
    EmailAutoSubmittedKind kind;
    string originalToken;
    string normalizedToken;
    EmailParameterList parameters;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new auto submitted.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "originalToken">The original token used by the operation.</param>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAutoSubmitted(EmailAutoSubmittedKind kind, string originalToken, EmailParameterList parameters, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (kind < EmailAutoSubmittedKind.No || kind > EmailAutoSubmittedKind.Extension)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (originalToken == null)
        {
            throw new ArgumentNullException("originalToken");
        }

        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.kind = kind;
        this.originalToken = originalToken;
        normalizedToken = originalToken.ToLowerInvariant();
        this.parameters = parameters;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailAutoSubmittedKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the original token.
    /// </summary>
    /// <returns>The current original token.</returns>
    public string GetOriginalToken()
    {
        return originalToken;
    }

    /// <summary>
    /// Gets the normalized token.
    /// </summary>
    /// <returns>The current normalized token.</returns>
    public string GetNormalizedToken()
    {
        return normalizedToken;
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    public EmailParameterList GetParameters()
    {
        return parameters;
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

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
