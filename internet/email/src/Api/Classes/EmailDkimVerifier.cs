using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides operations for working with DKIM verifier.
/// </summary>
public class EmailDkimVerifier : IEmailDkimVerifierContract
{
    internal static EmailDkimVerificationResult VerifyImplementationWithHeadersAndBodyAndKeys(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys)
    {
        return VerifyImplementationWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, new EmailDkimVerificationOptions());
    }

    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    public EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeys(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys)
    {
        return EmailDkimVerifier.VerifyImplementationWithHeadersAndBodyAndKeys(headers, body, keys);
    }

    internal static EmailDkimVerificationResult VerifyImplementationWithHeadersAndBodyAndKeysAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys, EmailDkimVerificationOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (keys == null)
            throw new ArgumentNullException("keys");
        if (options == null)
            throw new ArgumentNullException("options");
        return VerifyCore(headers, body, new EmailDkimVerificationKeyListResolver(keys), options);
    }

    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeysAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys, EmailDkimVerificationOptions options)
    {
        return EmailDkimVerifier.VerifyImplementationWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, options);
    }

    internal static EmailDkimVerificationResult VerifyImplementationWithHeadersAndBodyAndKeyResolver(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver)
    {
        return VerifyImplementationWithHeadersAndBodyAndKeyResolverAndOptions(headers, body, keyResolver, new EmailDkimVerificationOptions());
    }

    /// <summary>
    /// Verifies the supplied value using keys resolved on demand.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keyResolver">The caller-supplied key resolver used by the operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    public EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeyResolver(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver)
    {
        return EmailDkimVerifier.VerifyImplementationWithHeadersAndBodyAndKeyResolver(headers, body, keyResolver);
    }

    internal static EmailDkimVerificationResult VerifyImplementationWithHeadersAndBodyAndKeyResolverAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailDkimVerificationOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (keyResolver == null)
            throw new ArgumentNullException("keyResolver");
        if (options == null)
            throw new ArgumentNullException("options");
        return VerifyCore(headers, body, keyResolver, options);
    }

    /// <summary>
    /// Verifies the supplied value using keys resolved on demand.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keyResolver">The caller-supplied key resolver used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailDkimVerificationResult VerifyWithHeadersAndBodyAndKeyResolverAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailDkimVerificationOptions options)
    {
        return EmailDkimVerifier.VerifyImplementationWithHeadersAndBodyAndKeyResolverAndOptions(headers, body, keyResolver, options);
    }

    static EmailDkimVerificationResult VerifyCore(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailDkimVerificationOptions options)
    {
        EmailDkimVerificationOptions snapshot = options.Clone();
        long verificationTime = snapshot.HasVerificationTime() ? snapshot.GetVerificationTime() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        List<EmailDkimSignatureVerificationResult> results = new List<EmailDkimSignatureVerificationResult>();
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            if (header.GetNormalizedName() == "dkim-signature")
            {
                if (results.Count >= snapshot.GetMaximumSignatureCount())
                    throw new EmailLimitException("MaxDkimSignatureCount", snapshot.GetMaximumSignatureCount(), results.Count + 1);
                results.Add(VerifySignature(headers, body, header, keyResolver, snapshot, verificationTime));
            }

            index++;
        }

        return new EmailDkimVerificationResult(results);
    }

    static EmailDkimSignatureVerificationResult VerifySignature(EmailHeaders headers, ReadOnlySpan<byte> body, EmailHeader header, IEmailDkimKeyResolver keyResolver, EmailDkimVerificationOptions options, long verificationTime)
    {
        byte[] raw = header.GetRawValue();
        if (raw.Length > options.GetMaximumSignatureHeaderBytes())
            throw new EmailLimitException("MaxDkimSignatureHeaderBytes", options.GetMaximumSignatureHeaderBytes(), raw.Length);
        EmailSignatureParserOptions parserOptions = new EmailSignatureParserOptions();
        parserOptions.SetMode(EmailStructuredValueParseMode.Strict);
        parserOptions.SetMaximumInputBytes(options.GetMaximumSignatureHeaderBytes());
        parserOptions.SetMaximumTags(options.GetMaximumSignatureTags());
        EmailDkimSignatureParseResult parsed = new EmailDkimSignatureParser().ParseWithInputAndOptions(raw, parserOptions);
        if (!parsed.IsSuccess())
            return Result(header, EmailDkimVerificationStatus.SignatureSyntaxError, "The DKIM-Signature field is malformed.", parsed, null, null, null, false, false);
        EmailDkimSignature? signature = parsed.GetValue();
        if ((signature ?? throw new InvalidOperationException()).GetSignedHeaderCount() > options.GetMaximumSignedHeaderCount())
            throw new EmailLimitException("MaxDkimSelectedHeaderCount", options.GetMaximumSignedHeaderCount(), signature.GetSignedHeaderCount());
        string syntaxReason = ValidateSignatureValues(signature);
        if (syntaxReason.Length > 0)
            return Result(header, EmailDkimVerificationStatus.SignatureSyntaxError, syntaxReason, parsed, null, null, null, false, false);
        bool legacySha1 = signature.GetAlgorithmKind() == EmailDkimAlgorithmKind.RsaSha1;
        if (legacySha1 && !options.GetAllowLegacyRsaSha1())
            return Result(header, EmailDkimVerificationStatus.UnsupportedAlgorithm, "Legacy rsa-sha1 verification is disabled unless explicitly enabled.", parsed, null, null, null, false, false);
        if (!legacySha1 && signature.GetAlgorithmKind() != EmailDkimAlgorithmKind.RsaSha256)
            return Result(header, EmailDkimVerificationStatus.UnsupportedAlgorithm, "The DKIM signing algorithm is not enabled for verification.", parsed, null, null, null, false, false);
        EmailHashAlgorithm hashAlgorithm = legacySha1 ? EmailHashAlgorithm.Sha1 : EmailHashAlgorithm.Sha256;
        int expectedBodyHashBytes = legacySha1 ? 20 : 32;
        if (Convert.FromBase64String(signature.GetBodyHash()).Length != expectedBodyHashBytes)
            return Result(header, EmailDkimVerificationStatus.SignatureSyntaxError, "The body hash length does not match the signature hash algorithm.", parsed, null, null, null, false, false);
        if (signature.GetHeaderCanonicalizationKind() == EmailDkimCanonicalizationKind.Extension || signature.GetBodyCanonicalizationKind() == EmailDkimCanonicalizationKind.Extension)
            return Result(header, EmailDkimVerificationStatus.UnsupportedCanonicalization, "The DKIM canonicalization algorithm is unsupported.", parsed, null, null, null, false, false);
        if (!HasDnsTextQuery(signature))
            return Result(header, EmailDkimVerificationStatus.UnsupportedQueryMethod, "The signature does not specify a supported key query method.", parsed, null, null, null, false, false);
        if (!IdentityMatches(signature, false))
            return Result(header, EmailDkimVerificationStatus.IdentityDomainMismatch, "The AUID domain is not the signing domain or one of its subdomains.", parsed, null, null, null, false, false);
        EmailDkimVerificationKey? binding = EmailDkimKeyResolution.Resolve(keyResolver, signature.GetSigningDomain(), signature.GetSelector());
        if (binding == null)
            return Result(header, EmailDkimVerificationStatus.KeyNotFound, "No caller-supplied key matches the signing domain and selector.", parsed, null, null, null, false, false);
        EmailDkimKey key = binding.GetKey();
        if (!key.IsStructurallyValid())
            return Result(header, EmailDkimVerificationStatus.KeySyntaxError, "The supplied DKIM key is structurally invalid.", parsed, binding, null, null, false, false);
        if (key.IsRevoked() || !key.HasPublicKey())
            return Result(header, EmailDkimVerificationStatus.KeyRevoked, "The supplied DKIM key is revoked.", parsed, binding, null, null, false, false);
        if (!key.IsSizePolicyCompliant())
            return Result(header, EmailDkimVerificationStatus.KeySizePolicyViolation, "The supplied DKIM key violates its parsed key-size policy.", parsed, binding, null, null, false, false);
        if (!key.AllowsService("email"))
            return Result(header, EmailDkimVerificationStatus.KeyServiceNotAllowed, "The supplied DKIM key is not authorized for email.", parsed, binding, null, null, false, false);
        string hashName = legacySha1 ? "sha1" : "sha256";
        if (!key.AllowsHashAlgorithm(hashName))
            return Result(header, EmailDkimVerificationStatus.KeyHashNotAllowed, "The supplied DKIM key does not allow the signature hash algorithm.", parsed, binding, null, null, false, false);
        if (key.GetAlgorithmKind() != EmailDkimKeyAlgorithmKind.Rsa || !key.HasRsaPublicKey())
            return Result(header, EmailDkimVerificationStatus.KeyAlgorithmMismatch, "The supplied key is not suitable for the RSA signature algorithm.", parsed, binding, null, null, false, false);
        if (key.RequiresStrictIdentity() && !IdentityMatches(signature, true))
            return Result(header, EmailDkimVerificationStatus.IdentityDomainMismatch, "The key's strict-identity flag does not permit a subdomain AUID.", parsed, binding, null, null, false, false);
        if (signature.HasTimestamp() && IsBeyondSkew(signature.GetTimestamp(), verificationTime, options.GetAllowedClockSkewSeconds()))
            return Result(header, EmailDkimVerificationStatus.TimestampInFuture, "The DKIM signature timestamp is in the future.", parsed, binding, null, null, false, false);
        if (signature.HasExpiration() && IsBeyondSkew(verificationTime, signature.GetExpiration(), options.GetAllowedClockSkewSeconds()))
            return Result(header, EmailDkimVerificationStatus.SignatureExpired, "The DKIM signature has expired.", parsed, binding, null, null, false, false);
        EmailDkimBodyCanonicalizationOptions bodyOptions = new EmailDkimBodyCanonicalizationOptions();
        bodyOptions.SetKind(signature.GetBodyCanonicalizationKind());
        bodyOptions.SetHashAlgorithm(hashAlgorithm);
        bodyOptions.SetMaximumInputBytes(options.GetMaximumBodyInputBytes());
        bodyOptions.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedBodyBytes());
        bodyOptions.SetMaximumHashedBytes(options.GetMaximumCanonicalizedBodyBytes());
        if (signature.HasBodyLength())
            bodyOptions.SetBodyLengthLimit(signature.GetBodyLength());
        EmailDkimBodyCanonicalizationResult bodyResult = new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(body, bodyOptions).GetResult();
        if (!bodyResult.IsBodyLengthLimitSatisfied())
            return Result(header, EmailDkimVerificationStatus.BodyLengthInvalid, "The body-length tag exceeds the canonicalized body length.", parsed, binding, bodyResult, null, false, false);
        byte[] expectedBodyHash = Convert.FromBase64String(signature.GetBodyHash());
        bool bodyHashValid = bodyResult.GetHash().Matches(expectedBodyHash);
        if (!bodyHashValid)
            return Result(header, EmailDkimVerificationStatus.BodyHashMismatch, "The canonicalized body hash does not match bh=.", parsed, binding, bodyResult, null, false, false);
        EmailDkimHeaderCanonicalizationOptions headerOptions = new EmailDkimHeaderCanonicalizationOptions();
        headerOptions.SetKind(signature.GetHeaderCanonicalizationKind());
        headerOptions.SetMaximumSelectedHeaderCount(options.GetMaximumSignedHeaderCount());
        headerOptions.SetMaximumInputBytes(options.GetMaximumCanonicalizedHeaderBytes());
        headerOptions.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedHeaderBytes());
        EmailDkimHeaderCanonicalizationResult headerResult = new EmailDkimCanonicalizer().CanonicalizeHeadersWithHeadersAndSignatureAndSignatureHeaderAndOptions(headers, signature, header, headerOptions);
        byte[] headerHash = HashHeaders(headerResult.GetCanonicalizedHeaders(), hashAlgorithm);
        byte[] signatureBytes = Convert.FromBase64String(signature.GetSignatureData());
        bool signatureValid = VerifyRsa(key, headerHash, signatureBytes, hashAlgorithm);
        if (!signatureValid)
            return Result(header, EmailDkimVerificationStatus.SignatureMismatch, "The RSA signature does not match the canonicalized headers.", parsed, binding, bodyResult, headerResult, true, false);
        bool unsignedBody = bodyResult.GetUnsignedCanonicalizedByteCount() > 0;
        bool policyRejected = options.GetRequireTimestamp() && !signature.HasTimestamp() || options.GetRejectTestingKeys() && key.IsTesting() || options.GetRejectUnsignedBodyContent() && unsignedBody;
        if (policyRejected)
            return CreateResult(header, EmailDkimVerificationStatus.PolicyRejected, EmailDkimVerificationPolicyStatus.Rejected, "The signature verified cryptographically but was rejected by local verification policy.", parsed, binding, bodyResult, headerResult, true, true, unsignedBody);
        EmailDkimVerificationPolicyStatus policyStatus = key.IsTesting() || unsignedBody || legacySha1 ? EmailDkimVerificationPolicyStatus.AcceptedWithWarnings : EmailDkimVerificationPolicyStatus.Accepted;
        string successReason = legacySha1 ? "The legacy rsa-sha1 DKIM signature verified under explicit opt-in policy." : "The DKIM signature verified successfully.";
        return CreateResult(header, EmailDkimVerificationStatus.Pass, policyStatus, successReason, parsed, binding, bodyResult, headerResult, true, true, unsignedBody);
    }

    static string ValidateSignatureValues(EmailDkimSignature signature)
    {
        if (!EmailDkimVerificationValidation.IsDomain(signature.GetSigningDomain()) || !EmailDkimVerificationValidation.IsDomain(signature.GetSelector()))
            return "The signing domain or selector is invalid.";
        if (!HasSignedHeader(signature, "From"))
            return "The signed-header list does not include From.";
        if (!HasValidIdentitySyntax(signature))
            return "The DKIM AUID is malformed.";
        if (signature.HasTimestamp() && signature.HasExpiration() && signature.GetExpiration() <= signature.GetTimestamp())
            return "The signature expiration is not later than its timestamp.";
        if (signature.HasBodyLength() && signature.GetBodyLength() < 0)
            return "The body-length tag is too large to process.";
        return "";
    }

    static bool HasSignedHeader(EmailDkimSignature signature, string expected)
    {
        List<string> names = signature.GetSignedHeaders();
        int index = 0;
        while (index < names.Count)
        {
            if (string.Equals(names[index], expected, StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
    }

    static bool HasValidIdentitySyntax(EmailDkimSignature signature)
    {
        string identity = signature.GetIdentity();
        if (identity.Length == 0)
            return true;
        int at = identity.LastIndexOf('@');
        if (at < 0 || at == identity.Length - 1)
            return false;
        int index = 0;
        while (index < identity.Length)
        {
            char current = identity[index];
            if (current <= 32 || current == 127)
                return false;
            index++;
        }

        return EmailDkimVerificationValidation.IsDomain(identity.Substring(at + 1));
    }

    static bool IdentityMatches(EmailDkimSignature signature, bool strict)
    {
        string domain = signature.GetSigningDomain();
        string identity = signature.GetIdentity();
        string identityDomain = domain;
        if (identity.Length > 0)
            identityDomain = identity.Substring(identity.LastIndexOf('@') + 1);
        if (string.Equals(identityDomain, domain, StringComparison.OrdinalIgnoreCase))
            return true;
        if (strict)
            return false;
        return identityDomain.Length > domain.Length && identityDomain[identityDomain.Length - domain.Length - 1] == '.' && identityDomain.EndsWith(domain, StringComparison.OrdinalIgnoreCase);
    }

    static bool HasDnsTextQuery(EmailDkimSignature signature)
    {
        List<string> methods = signature.GetQueryMethods();
        int index = 0;
        while (index < methods.Count)
        {
            if (string.Equals(methods[index], "dns/txt", StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
    }

    static byte[] HashHeaders(byte[] value, EmailHashAlgorithm algorithm)
    {
        if (algorithm == EmailHashAlgorithm.Sha1)
            return SHA1.HashData(value);
        return SHA256.HashData(value);
    }

    static bool VerifyRsa(EmailDkimKey key, byte[] hash, byte[] signature, EmailHashAlgorithm algorithm)
    {
        string pem = "-----BEGIN RSA PUBLIC KEY-----\n" + Convert.ToBase64String((key.GetRsaPublicKey() ?? throw new InvalidOperationException())) + "\n-----END RSA PUBLIC KEY-----";
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(pem);
            HashAlgorithmName algorithmName = algorithm == EmailHashAlgorithm.Sha1 ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
            return rsa.VerifyHash(hash, signature, algorithmName, RSASignaturePadding.Pkcs1);
        }
    }

    static bool IsBeyondSkew(long later, long earlier, long skew)
    {
        if (later <= earlier)
            return false;
        return later - earlier > skew;
    }

    static EmailDkimSignatureVerificationResult Result(EmailHeader header, EmailDkimVerificationStatus status, string reason, EmailDkimSignatureParseResult parsed, EmailDkimVerificationKey? key, EmailDkimBodyCanonicalizationResult? body, EmailDkimHeaderCanonicalizationResult? headers, bool bodyHashValid, bool signatureValid)
    {
        return CreateResult(header, status, EmailDkimVerificationPolicyStatus.NotEvaluated, reason, parsed, key, body, headers, bodyHashValid, signatureValid, body != null && body.GetUnsignedCanonicalizedByteCount() > 0);
    }

    static EmailDkimSignatureVerificationResult CreateResult(EmailHeader header, EmailDkimVerificationStatus status, EmailDkimVerificationPolicyStatus policyStatus, string reason, EmailDkimSignatureParseResult parsed, EmailDkimVerificationKey? key, EmailDkimBodyCanonicalizationResult? body, EmailDkimHeaderCanonicalizationResult? headers, bool bodyHashValid, bool signatureValid, bool unsignedBody)
    {
        bool testing = key != null && key.GetKey().IsTesting();
        bool strict = key != null && key.GetKey().RequiresStrictIdentity();
        return new EmailDkimSignatureVerificationResult(header, status, policyStatus, reason, parsed, key, body, headers, bodyHashValid, signatureValid, testing, strict, unsignedBody);
    }
}
