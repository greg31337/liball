using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an ARC seal.
/// </summary>
public class EmailArcSeal : IEmailArcSealContract
{
    EmailSignatureTagList tags;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new ARC seal.
    /// </summary>
    /// <param name = "tags">The tags used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArcSeal(EmailSignatureTagList tags, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (tags == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("tags");
        }

        this.tags = tags;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the tags.
    /// </summary>
    /// <returns>The current tags.</returns>
    public EmailSignatureTagList GetTags()
    {
        return tags;
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        EmailSignatureTag? tag = tags.GetFirst("i");
        if (tag == null)
        {
            return 0;
        }

        string text = tag.GetCompactValue();
        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current < '0' || current > '9' || value > (int.MaxValue - (current - '0')) / 10)
            {
                return 0;
            }

            value = value * 10 + current - '0';
            index++;
        }

        return text.Length == 0 ? 0 : value;
    }

    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    public string GetAlgorithm()
    {
        return GetValue("a", false);
    }

    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    public EmailDkimAlgorithmKind GetAlgorithmKind()
    {
        string value = GetAlgorithm().ToLowerInvariant();
        if (value == "rsa-sha1")
        {
            return EmailDkimAlgorithmKind.RsaSha1;
        }

        if (value == "rsa-sha256")
        {
            return EmailDkimAlgorithmKind.RsaSha256;
        }

        if (value == "ed25519-sha256")
        {
            return EmailDkimAlgorithmKind.Ed25519Sha256;
        }

        return value.Length == 0 ? EmailDkimAlgorithmKind.Unknown : EmailDkimAlgorithmKind.Extension;
    }

    /// <summary>
    /// Gets the signature data.
    /// </summary>
    /// <returns>The current signature data.</returns>
    public string GetSignatureData()
    {
        return GetValue("b", true);
    }

    /// <summary>
    /// Gets the signing domain.
    /// </summary>
    /// <returns>The current signing domain.</returns>
    public string GetSigningDomain()
    {
        return GetValue("d", false);
    }

    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    public string GetSelector()
    {
        return GetValue("s", false);
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    public long GetTimestamp()
    {
        EmailSignatureTag? tag = tags.GetFirst("t");
        if (tag == null)
        {
            return -1;
        }

        string text = tag.GetCompactValue();
        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current < '0' || current > '9' || value > (long.MaxValue - (current - '0')) / 10)
            {
                return -1;
            }

            value = value * 10 + current - '0';
            index++;
        }

        return text.Length == 0 ? -1 : value;
    }

    /// <summary>
    /// Gets the chain status.
    /// </summary>
    /// <returns>The current chain status.</returns>
    public EmailArcChainStatus GetChainStatus()
    {
        string value = GetValue("cv", false);
        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
        {
            return EmailArcChainStatus.None;
        }

        if (string.Equals(value, "pass", StringComparison.OrdinalIgnoreCase))
        {
            return EmailArcChainStatus.Pass;
        }

        if (string.Equals(value, "fail", StringComparison.OrdinalIgnoreCase))
        {
            return EmailArcChainStatus.Fail;
        }

        return EmailArcChainStatus.Unknown;
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
        return new EmailSignatureFormatter().FormatWithValueUsingEmailArcSeal(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithOptions(EmailSignatureFormatOptions options)
    {
        return new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailArcSealAndEmailSignatureFormatOptions(this, options);
    }

    string GetValue(string name, bool compact)
    {
        EmailSignatureTag? tag = tags.GetFirst(name);
        if (tag == null)
        {
            return "";
        }

        return compact ? tag.GetCompactValue() : tag.GetValue();
    }

    static byte[] Copy(byte[] input)
    {
        byte[] copy = new byte[input.Length];
        input.CopyTo(copy, 0);
        return copy;
    }
}
