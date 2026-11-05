using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with DKIM signer.
/// </summary>
public class EmailDkimSigner : IEmailDkimSignerContract
{
    /// <summary>
    /// The RSA sha 1 prohibited reason value exposed by this API.
    /// </summary>
    internal const string RsaSha1ProhibitedReason = "RSA-SHA1 DKIM signing is prohibited because SHA-1 is a historic DKIM algorithm and new signatures must use a modern algorithm.";
    /// <summary>
    /// The ed 25519 sha 256 unsupported reason value exposed by this API.
    /// </summary>
    internal const string Ed25519Sha256UnsupportedReason = "Ed25519-SHA256 DKIM signing is unavailable because the target .NET 10 runtime does not expose a built-in Ed25519 signing primitive, and this library does not add an external cryptography dependency.";
    internal static bool IsRsaSha1SigningSupportedImplementation()
    {
        return false;
    }

    /// <summary>
    /// Determines whether the current value or state is RSA sha 1 signing supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is RSA sha 1 signing supported; otherwise, <c>false</c>.</returns>
    public bool IsRsaSha1SigningSupported()
    {
        return EmailDkimSigner.IsRsaSha1SigningSupportedImplementation();
    }

    internal static EmailDkimSigningAttemptResult GetRsaSha1SigningSupportImplementation()
    {
        return new EmailDkimSigningAttemptResult(EmailDkimSigningAttemptStatus.ProhibitedAlgorithm, EmailDkimAlgorithmKind.RsaSha1, null, RsaSha1ProhibitedReason);
    }

    /// <summary>
    /// Gets the RSA sha 1 signing support.
    /// </summary>
    /// <returns>The current RSA sha 1 signing support.</returns>
    public EmailDkimSigningAttemptResult GetRsaSha1SigningSupport()
    {
        return EmailDkimSigner.GetRsaSha1SigningSupportImplementation();
    }

    internal static EmailDkimSigningAttemptResult SignRsaSha1Implementation(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (privateKey == null)
            throw new ArgumentNullException("privateKey");
        if (options == null)
            throw new ArgumentNullException("options");
        return GetRsaSha1SigningSupportImplementation();
    }

    /// <summary>
    /// Signs the RSA sha 1.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing attempt result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimSigningAttemptResult SignRsaSha1(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options)
    {
        return EmailDkimSigner.SignRsaSha1Implementation(headers, body, privateKey, options);
    }

    internal static bool IsEd25519Sha256SupportedImplementation()
    {
        return false;
    }

    /// <summary>
    /// Determines whether the current value or state is ed 25519 sha 256 supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is ed 25519 sha 256 supported; otherwise, <c>false</c>.</returns>
    public bool IsEd25519Sha256Supported()
    {
        return EmailDkimSigner.IsEd25519Sha256SupportedImplementation();
    }

    internal static EmailDkimSigningAttemptResult GetEd25519Sha256SupportImplementation()
    {
        return new EmailDkimSigningAttemptResult(EmailDkimSigningAttemptStatus.UnsupportedAlgorithm, EmailDkimAlgorithmKind.Ed25519Sha256, null, Ed25519Sha256UnsupportedReason);
    }

    /// <summary>
    /// Gets the ed 25519 sha 256 support.
    /// </summary>
    /// <returns>The current ed 25519 sha 256 support.</returns>
    public EmailDkimSigningAttemptResult GetEd25519Sha256Support()
    {
        return EmailDkimSigner.GetEd25519Sha256SupportImplementation();
    }

    internal static EmailDkimSigningAttemptResult SignEd25519Sha256Implementation(EmailHeaders headers, ReadOnlySpan<byte> body, byte[] privateKey, EmailDkimSigningOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (privateKey == null)
            throw new ArgumentNullException("privateKey");
        if (options == null)
            throw new ArgumentNullException("options");
        return GetEd25519Sha256SupportImplementation();
    }

    /// <summary>
    /// Signs the ed 25519 sha 256.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing attempt result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimSigningAttemptResult SignEd25519Sha256(EmailHeaders headers, ReadOnlySpan<byte> body, byte[] privateKey, EmailDkimSigningOptions options)
    {
        return EmailDkimSigner.SignEd25519Sha256Implementation(headers, body, privateKey, options);
    }

    internal static EmailDkimSigningResult SignImplementation(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (privateKey == null)
            throw new ArgumentNullException("privateKey");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDkimSigningOptions snapshot = options.Clone();
        Validate(headers, privateKey, snapshot);
        long timestamp = snapshot.HasTimestamp() ? snapshot.GetTimestamp() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        EmailDkimSigningValidation.RequireTime(timestamp, "timestamp");
        if (snapshot.HasExpiration() && snapshot.GetExpiration() <= timestamp)
            throw new ArgumentException("A DKIM expiration must be later than its signature timestamp.", "options");
        EmailDkimBodyCanonicalizationOptions bodyOptions = new EmailDkimBodyCanonicalizationOptions();
        bodyOptions.SetKind(snapshot.GetBodyCanonicalizationKind());
        bodyOptions.SetHashAlgorithm(EmailHashAlgorithm.Sha256);
        bodyOptions.SetMaximumInputBytes(snapshot.GetMaximumBodyInputBytes());
        bodyOptions.SetMaximumCanonicalizedBytes(snapshot.GetMaximumCanonicalizedBodyBytes());
        bodyOptions.SetMaximumHashedBytes(snapshot.GetMaximumCanonicalizedBodyBytes());
        if (snapshot.HasBodyLengthLimit())
            bodyOptions.SetBodyLengthLimit(snapshot.GetBodyLengthLimit());
        EmailDkimBodyCanonicalizationOutput bodyOutput = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(body, bodyOptions);
        EmailDkimBodyCanonicalizationResult bodyResult = bodyOutput.GetResult();
        string unsignedValue = BuildValue(snapshot, timestamp, bodyResult, "");
        EmailHeader unsignedHeader = CreateHeader(unsignedValue);
        EnforceSignatureHeaderLimit(unsignedHeader, snapshot);
        EmailDkimHeaderCanonicalizationOptions headerOptions = new EmailDkimHeaderCanonicalizationOptions();
        headerOptions.SetKind(snapshot.GetHeaderCanonicalizationKind());
        headerOptions.SetMaximumSelectedHeaderCount(snapshot.GetMaximumSignedHeaderCount());
        headerOptions.SetMaximumInputBytes(snapshot.GetMaximumCanonicalizedHeaderBytes());
        headerOptions.SetMaximumCanonicalizedBytes(snapshot.GetMaximumCanonicalizedHeaderBytes());
        EmailDkimHeaderCanonicalizationResult headerResult = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignedHeaderNamesAndSignatureHeaderAndOptions(headers, snapshot.GetSignedHeaders(), unsignedHeader, headerOptions);
        byte[] headerHash = new EmailHash().ComputeExactWithContentAndAlgorithm(headerResult.GetCanonicalizedHeaders(), EmailHashAlgorithm.Sha256).GetDigestBytes();
        byte[] signatureBytes = privateKey.SignHash(headerHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string finalValue = BuildValue(snapshot, timestamp, bodyResult, Convert.ToBase64String(signatureBytes));
        EmailHeader finalHeader = CreateHeader(finalValue);
        EnforceSignatureHeaderLimit(finalHeader, snapshot);
        byte[] finalRawValue = finalHeader.GetRawValue();
        EmailSignatureParserOptions parserOptions = new EmailSignatureParserOptions();
        parserOptions.SetMode(EmailStructuredValueParseMode.Strict);
        parserOptions.SetMaximumInputBytes(finalRawValue.Length);
        EmailDkimSignatureParseResult parsed = new EmailDkimSignatureParser().ParseWithInputAndOptions(finalRawValue, parserOptions);
        if (!parsed.IsSuccess() || !parsed.HasValue())
            throw new InvalidOperationException("The generated DKIM-Signature header did not pass strict parsing.");
        return new EmailDkimSigningResult(finalHeader, parsed.GetValue(), bodyResult, headerResult, headerHash, signatureBytes, timestamp);
    }

    /// <summary>
    /// Signs the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailDkimSigningResult Sign(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options)
    {
        return EmailDkimSigner.SignImplementation(headers, body, privateKey, options);
    }

    internal static EmailDkimSigningResult SignAndInsertImplementation(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options)
    {
        EmailDkimSigningResult result = SignImplementation(headers, body, privateKey, options);
        headers.Insert(0, result.GetHeader());
        return result;
    }

    /// <summary>
    /// Signs the and insert.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM signing result produced by the operation.</returns>
    public EmailDkimSigningResult SignAndInsert(EmailHeaders headers, ReadOnlySpan<byte> body, RSA privateKey, EmailDkimSigningOptions options)
    {
        return EmailDkimSigner.SignAndInsertImplementation(headers, body, privateKey, options);
    }

    static void Validate(EmailHeaders headers, RSA privateKey, EmailDkimSigningOptions options)
    {
        if (options.GetDomain().Length == 0)
            throw new ArgumentException("A DKIM signing domain is required.", "options");
        if (options.GetSelector().Length == 0)
            throw new ArgumentException("A DKIM selector is required.", "options");
        if (!headers.Contains("From"))
            throw new ArgumentException("A message must contain a From header before it can be DKIM-signed.", "headers");
        if (!ContainsHeaderName(options.GetSignedHeaders(), "From"))
            throw new ArgumentException("The DKIM signed-header list must include From.", "options");
        if (options.GetSignedHeaderCount() > options.GetMaximumSignedHeaderCount())
            throw new EmailLimitException("MaxDkimSelectedHeaderCount", options.GetMaximumSignedHeaderCount(), options.GetSignedHeaderCount());
        int keyBits = privateKey.KeySize;
        if (keyBits < options.GetMinimumRsaKeyBits() || keyBits > options.GetMaximumRsaKeyBits())
            throw new ArgumentException("The RSA key size is outside the configured DKIM signing policy.", "privateKey");
        int index = 0;
        while (index < options.GetSignedHeaderCount())
        {
            string name = options.GetSignedHeader(index);
            if (name.Length + 4 > 998)
                throw new ArgumentException("A signed header name cannot fit on an Internet header line.", "options");
            index++;
        }
    }

    static string BuildValue(EmailDkimSigningOptions options, long timestamp, EmailDkimBodyCanonicalizationResult body, string signature)
    {
        StringBuilder value = new StringBuilder();
        value.Append(" v=1; a=rsa-sha256;");
        AppendTagLine(value, "c", CanonicalizationName(options.GetHeaderCanonicalizationKind()) + "/" + CanonicalizationName(options.GetBodyCanonicalizationKind()));
        AppendTagLine(value, "d", options.GetDomain());
        AppendTagLine(value, "s", options.GetSelector());
        AppendTagLine(value, "t", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (options.HasExpiration())
            AppendTagLine(value, "x", options.GetExpiration().ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (options.HasBodyLengthLimit())
            AppendTagLine(value, "l", body.GetHashedByteCount().ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendSignedHeaders(value, options);
        AppendTagLine(value, "bh", Convert.ToBase64String(body.GetHash().GetDigestBytes()));
        AppendSignature(value, signature, options.GetPreferredLineLength());
        return value.ToString();
    }

    static void AppendTagLine(StringBuilder output, string name, string value)
    {
        if ((long)name.Length + value.Length + 3 > 998)
            throw new ArgumentException("A DKIM tag cannot fit on an Internet header line.", "value");
        output.Append("\r\n\t");
        output.Append(name);
        output.Append('=');
        output.Append(value);
        output.Append(';');
    }

    static void AppendSignedHeaders(StringBuilder output, EmailDkimSigningOptions options)
    {
        output.Append("\r\n\th=");
        int lineLength = 3;
        int index = 0;
        while (index < options.GetSignedHeaderCount())
        {
            string name = options.GetSignedHeader(index);
            if (index > 0)
            {
                if (lineLength + 1 + name.Length > options.GetPreferredLineLength())
                {
                    output.Append("\r\n\t:");
                    lineLength = 2;
                }
                else
                {
                    output.Append(':');
                    lineLength++;
                }
            }

            output.Append(name);
            lineLength += name.Length;
            index++;
        }

        output.Append(';');
    }

    static void AppendSignature(StringBuilder output, string signature, int preferredLineLength)
    {
        output.Append("\r\n\tb=");
        int offset = 0;
        int capacity = preferredLineLength - 3;
        while (offset < signature.Length)
        {
            int count = signature.Length - offset;
            if (count > capacity)
                count = capacity;
            output.Append(signature, offset, count);
            offset += count;
            if (offset < signature.Length)
            {
                output.Append("\r\n\t");
                capacity = preferredLineLength - 1;
            }
        }
    }

    static EmailHeader CreateHeader(string value)
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("DKIM-Signature", Encoding.ASCII.GetBytes(value), EmailHeaderValueKind.DkimSignature);
        header.SetLineEnding(EmailLineEnding.CrLf);
        return header;
    }

    static void EnforceSignatureHeaderLimit(EmailHeader header, EmailDkimSigningOptions options)
    {
        int length = header.GetRawFieldBytes().Length;
        if (length > options.GetMaximumSignatureHeaderBytes())
            throw new EmailLimitException("MaxDkimSignatureHeaderBytes", options.GetMaximumSignatureHeaderBytes(), length);
    }

    static bool ContainsHeaderName(List<string> names, string expected)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (string.Equals(names[index], expected, StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
    }

    static string CanonicalizationName(EmailDkimCanonicalizationKind value)
    {
        return value == EmailDkimCanonicalizationKind.Simple ? "simple" : "relaxed";
    }
}
