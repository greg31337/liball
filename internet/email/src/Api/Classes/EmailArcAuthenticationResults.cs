using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Contains the outcomes produced by ARC authentication.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcAuthenticationResults : IEmailArcAuthenticationResultsContract
{
    int instance;
    EmailSignatureTag instanceTag;
    EmailAuthenticationResults authenticationResults;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new ARC authentication results.
    /// </summary>
    /// <param name = "instance">The instance used by the operation.</param>
    /// <param name = "instanceTag">The instance tag used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArcAuthenticationResults(int instance, EmailSignatureTag instanceTag, EmailAuthenticationResults? authenticationResults, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (instanceTag == null || authenticationResults == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("instanceTag");
        }

        this.instance = instance;
        this.instanceTag = instanceTag;
        this.authenticationResults = authenticationResults;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return instance;
    }

    /// <summary>
    /// Gets the instance tag.
    /// </summary>
    /// <returns>The current instance tag.</returns>
    public EmailSignatureTag GetInstanceTag()
    {
        return instanceTag;
    }

    /// <summary>
    /// Gets the authentication results.
    /// </summary>
    /// <returns>The current authentication results.</returns>
    public EmailAuthenticationResults GetAuthenticationResults()
    {
        return authenticationResults;
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
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult Format()
    {
        return new EmailSignatureFormatter().FormatWithValueUsingEmailArcAuthenticationResults(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithOptions(EmailSignatureFormatOptions options)
    {
        return new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailArcAuthenticationResultsAndEmailSignatureFormatOptions(this, options);
    }

    static byte[] Copy(byte[] input)
    {
        byte[] copy = new byte[input.Length];
        input.CopyTo(copy, 0);
        return copy;
    }
}
