using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a DKIM signature.
/// </summary>
public class EmailDkimSignature : IEmailDkimSignatureContract
{
    EmailSignatureHeaderKind kind;
    EmailSignatureTagList tags;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    /// <summary>
    /// Creates a new DKIM signature.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "tags">The tags used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimSignature(EmailSignatureHeaderKind kind, EmailSignatureTagList tags, byte[] rawBytes, EmailSourcePosition sourcePosition)
    {
        if (kind != EmailSignatureHeaderKind.DkimSignature && kind != EmailSignatureHeaderKind.ArcMessageSignature)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (tags == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("tags");
        }

        this.kind = kind;
        this.tags = tags;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
    }

    /// <summary>
    /// Gets the header kind.
    /// </summary>
    /// <returns>The current header kind.</returns>
    public EmailSignatureHeaderKind GetHeaderKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the current value or state is ARC message signature.
    /// </summary>
    /// <returns><c>true</c> when the value or state is ARC message signature; otherwise, <c>false</c>.</returns>
    public bool IsArcMessageSignature()
    {
        return kind == EmailSignatureHeaderKind.ArcMessageSignature;
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
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public int GetVersion()
    {
        return GetInt("v", 1);
    }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>The current instance.</returns>
    public int GetInstance()
    {
        return GetInt("i", 0);
    }

    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    public string GetAlgorithm()
    {
        return GetValue("a", "");
    }

    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    public EmailDkimAlgorithmKind GetAlgorithmKind()
    {
        string algorithm = GetAlgorithm().ToLowerInvariant();
        if (algorithm == "rsa-sha1")
        {
            return EmailDkimAlgorithmKind.RsaSha1;
        }

        if (algorithm == "rsa-sha256")
        {
            return EmailDkimAlgorithmKind.RsaSha256;
        }

        if (algorithm == "ed25519-sha256")
        {
            return EmailDkimAlgorithmKind.Ed25519Sha256;
        }

        return algorithm.Length == 0 ? EmailDkimAlgorithmKind.Unknown : EmailDkimAlgorithmKind.Extension;
    }

    /// <summary>
    /// Gets the signature data.
    /// </summary>
    /// <returns>The current signature data.</returns>
    public string GetSignatureData()
    {
        return GetCompactValue("b");
    }

    /// <summary>
    /// Gets the body hash.
    /// </summary>
    /// <returns>The current body hash.</returns>
    public string GetBodyHash()
    {
        return GetCompactValue("bh");
    }

    /// <summary>
    /// Gets the signing domain.
    /// </summary>
    /// <returns>The current signing domain.</returns>
    public string GetSigningDomain()
    {
        return GetValue("d", "");
    }

    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    public string GetSelector()
    {
        return GetValue("s", "");
    }

    /// <summary>
    /// Gets the identity.
    /// </summary>
    /// <returns>The current identity.</returns>
    public string GetIdentity()
    {
        return GetValue("i", "");
    }

    /// <summary>
    /// Determines whether the body length is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyLength()
    {
        return tags.Contains("l");
    }

    /// <summary>
    /// Gets the body length.
    /// </summary>
    /// <returns>The current body length.</returns>
    public long GetBodyLength()
    {
        return GetLong("l", -1);
    }

    /// <summary>
    /// Determines whether the timestamp is present or available.
    /// </summary>
    /// <returns><c>true</c> when the timestamp is available; otherwise, <c>false</c>.</returns>
    public bool HasTimestamp()
    {
        return tags.Contains("t");
    }

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    /// <returns>The current timestamp.</returns>
    public long GetTimestamp()
    {
        return GetLong("t", -1);
    }

    /// <summary>
    /// Determines whether the expiration is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expiration is available; otherwise, <c>false</c>.</returns>
    public bool HasExpiration()
    {
        return tags.Contains("x");
    }

    /// <summary>
    /// Gets the expiration.
    /// </summary>
    /// <returns>The current expiration.</returns>
    public long GetExpiration()
    {
        return GetLong("x", -1);
    }

    /// <summary>
    /// Gets the header canonicalization kind.
    /// </summary>
    /// <returns>The current header canonicalization kind.</returns>
    public EmailDkimCanonicalizationKind GetHeaderCanonicalizationKind()
    {
        string value = GetCanonicalizationPart(0);
        return GetCanonicalizationKind(value);
    }

    /// <summary>
    /// Gets the body canonicalization kind.
    /// </summary>
    /// <returns>The current body canonicalization kind.</returns>
    public EmailDkimCanonicalizationKind GetBodyCanonicalizationKind()
    {
        string value = GetCanonicalizationPart(1);
        return GetCanonicalizationKind(value);
    }

    /// <summary>
    /// Gets the signed header count.
    /// </summary>
    /// <returns>The current signed header count.</returns>
    public int GetSignedHeaderCount()
    {
        return Split("h", ':').Count;
    }

    /// <summary>
    /// Gets the signed header.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current signed header.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetSignedHeader(int index)
    {
        List<string> values = Split("h", ':');
        if (index < 0 || index >= values.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return values[index];
    }

    /// <summary>
    /// Gets the signed headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signed headers.</returns>
    public List<string> GetSignedHeaders()
    {
        return Split("h", ':');
    }

    /// <summary>
    /// Gets the query methods.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current query methods.</returns>
    public List<string> GetQueryMethods()
    {
        if (!tags.Contains("q"))
        {
            return new List<string>
            {
                "dns/txt"
            };
        }

        return Split("q", ':');
    }

    /// <summary>
    /// Gets the copied headers.
    /// </summary>
    /// <returns>The current copied headers.</returns>
    public string GetCopiedHeaders()
    {
        return GetValue("z", "");
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
        return new EmailSignatureFormatter().FormatWithValueUsingEmailDkimSignature(this);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The signature format result produced by the operation.</returns>
    public EmailSignatureFormatResult FormatWithOptions(EmailSignatureFormatOptions options)
    {
        return new EmailSignatureFormatter().FormatWithValueAndOptionsUsingEmailDkimSignatureAndEmailSignatureFormatOptions(this, options);
    }

    string GetCanonicalizationPart(int part)
    {
        string value = GetValue("c", "simple/simple");
        int slash = value.IndexOf('/');
        if (part == 0)
        {
            return slash < 0 ? value : value.Substring(0, slash);
        }

        return slash < 0 ? "simple" : value.Substring(slash + 1);
    }

    static EmailDkimCanonicalizationKind GetCanonicalizationKind(string value)
    {
        if (string.Equals(value, "simple", StringComparison.OrdinalIgnoreCase))
        {
            return EmailDkimCanonicalizationKind.Simple;
        }

        if (string.Equals(value, "relaxed", StringComparison.OrdinalIgnoreCase))
        {
            return EmailDkimCanonicalizationKind.Relaxed;
        }

        return EmailDkimCanonicalizationKind.Extension;
    }

    List<string> Split(string name, char separator)
    {
        List<string> values = new List<string>();
        string text = GetValue(name, "");
        string[] pieces = text.Split(separator);
        int index = 0;
        while (index < pieces.Length)
        {
            string piece = pieces[index].Trim();
            if (piece.Length > 0)
            {
                values.Add(piece);
            }

            index++;
        }

        return values;
    }

    string GetValue(string name, string fallback)
    {
        EmailSignatureTag? tag = tags.GetFirst(name);
        return tag == null ? fallback : tag.GetValue();
    }

    string GetCompactValue(string name)
    {
        EmailSignatureTag? tag = tags.GetFirst(name);
        return tag == null ? "" : tag.GetCompactValue();
    }

    int GetInt(string name, int fallback)
    {
        long value = GetLong(name, fallback);
        return value < int.MinValue || value > int.MaxValue ? fallback : (int)value;
    }

    long GetLong(string name, long fallback)
    {
        EmailSignatureTag? tag = tags.GetFirst(name);
        if (tag == null)
        {
            return fallback;
        }

        string text = tag.GetCompactValue();
        if (text.Length == 0)
        {
            return fallback;
        }

        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current < '0' || current > '9' || value > (long.MaxValue - (current - '0')) / 10)
            {
                return fallback;
            }

            value = value * 10 + current - '0';
            index++;
        }

        return value;
    }

    static byte[] Copy(byte[] input)
    {
        byte[] copy = new byte[input.Length];
        input.CopyTo(copy, 0);
        return copy;
    }
}
