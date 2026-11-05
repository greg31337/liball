using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides operations for working with ARC verifier.
/// </summary>
public class EmailArcVerifier : IEmailArcVerifierContract
{
    internal static EmailArcVerificationResult VerifyImplementationWithHeadersAndBodyAndKeys(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys)
    {
        return VerifyImplementationWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, new EmailArcVerificationOptions());
    }

    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The ARC verification result produced by the operation.</returns>
    public EmailArcVerificationResult VerifyWithHeadersAndBodyAndKeys(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys)
    {
        return EmailArcVerifier.VerifyImplementationWithHeadersAndBodyAndKeys(headers, body, keys);
    }

    internal static EmailArcVerificationResult VerifyImplementationWithHeadersAndBodyAndKeysAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys, EmailArcVerificationOptions options)
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
    /// <returns>The ARC verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArcVerificationResult VerifyWithHeadersAndBodyAndKeysAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, List<EmailDkimVerificationKey> keys, EmailArcVerificationOptions options)
    {
        return EmailArcVerifier.VerifyImplementationWithHeadersAndBodyAndKeysAndOptions(headers, body, keys, options);
    }

    internal static EmailArcVerificationResult VerifyImplementationWithHeadersAndBodyAndKeyResolver(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver)
    {
        return VerifyImplementationWithHeadersAndBodyAndKeyResolverAndOptions(headers, body, keyResolver, new EmailArcVerificationOptions());
    }

    /// <summary>
    /// Verifies the supplied value using keys resolved on demand.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "keyResolver">The caller-supplied key resolver used by the operation.</param>
    /// <returns>The ARC verification result produced by the operation.</returns>
    public EmailArcVerificationResult VerifyWithHeadersAndBodyAndKeyResolver(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver)
    {
        return EmailArcVerifier.VerifyImplementationWithHeadersAndBodyAndKeyResolver(headers, body, keyResolver);
    }

    internal static EmailArcVerificationResult VerifyImplementationWithHeadersAndBodyAndKeyResolverAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailArcVerificationOptions options)
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
    /// <returns>The ARC verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArcVerificationResult VerifyWithHeadersAndBodyAndKeyResolverAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailArcVerificationOptions options)
    {
        return EmailArcVerifier.VerifyImplementationWithHeadersAndBodyAndKeyResolverAndOptions(headers, body, keyResolver, options);
    }

    static EmailArcVerificationResult VerifyCore(EmailHeaders headers, ReadOnlySpan<byte> body, IEmailDkimKeyResolver keyResolver, EmailArcVerificationOptions options)
    {
        EmailArcVerificationOptions snapshot = options.Clone();
        EmailArcSetCollectionOptions collectionOptions = snapshot.GetCollectionOptions();
        collectionOptions.SetMaximumTags(snapshot.GetMaximumSignatureTags());
        EmailArcSetCollectionResult collection = new EmailArcSetCollector().CollectWithHeadersAndOptions(headers, collectionOptions);
        EmailArcSignatureVerificationResult[] messageSignatures = new EmailArcSignatureVerificationResult[51];
        EmailArcSignatureVerificationResult[] seals = new EmailArcSignatureVerificationResult[51];
        InitializeResults(collection, messageSignatures, seals);
        if (collection.GetStatus() == EmailArcSetCollectionStatus.None)
        {
            return CreateResult(EmailArcVerificationStatus.None, EmailArcVerificationFailureKind.None, 0, "The message contains no ARC chain.", collection, messageSignatures, seals, false, 0);
        }

        if (collection.GetStatus() != EmailArcSetCollectionStatus.Valid)
        {
            return CreateResult(EmailArcVerificationStatus.Fail, EmailArcVerificationFailureKind.Structure, 0, "The ARC chain structure is invalid.", collection, messageSignatures, seals, false, 0);
        }

        long verificationTime = snapshot.HasVerificationTime() ? snapshot.GetVerificationTime() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int maximumInstance = collection.GetMaximumInstance();
        EmailArcSet? newestSet = collection.GetSetByInstance(maximumInstance);
        messageSignatures[maximumInstance] = VerifyMessageSignature(headers, body, (newestSet ?? throw new InvalidOperationException()).GetMessageSignatureField(0), keyResolver, snapshot, verificationTime);
        if (!messageSignatures[maximumInstance].IsSuccess())
        {
            return CreateResult(EmailArcVerificationStatus.Fail, EmailArcVerificationFailureKind.MessageSignature, maximumInstance, "The newest ARC-Message-Signature failed verification.", collection, messageSignatures, seals, false, 0);
        }

        bool hasOldestPass = snapshot.GetVerifyPriorMessageSignatures();
        int oldestPass = 0;
        if (snapshot.GetVerifyPriorMessageSignatures())
        {
            int instance = maximumInstance - 1;
            while (instance >= 1)
            {
                EmailArcSet? set = collection.GetSetByInstance(instance);
                messageSignatures[instance] = VerifyMessageSignature(headers, body, (set ?? throw new InvalidOperationException()).GetMessageSignatureField(0), keyResolver, snapshot, verificationTime);
                if (!messageSignatures[instance].IsSuccess())
                {
                    oldestPass = instance + 1;
                    break;
                }

                instance--;
            }
        }

        int sealInstance = maximumInstance;
        while (sealInstance >= 1)
        {
            EmailArcSet? set = collection.GetSetByInstance(sealInstance);
            seals[sealInstance] = VerifySeal(collection, (set ?? throw new InvalidOperationException()).GetSealField(0), keyResolver, snapshot, verificationTime);
            if (!seals[sealInstance].IsSuccess())
            {
                return CreateResult(EmailArcVerificationStatus.Fail, EmailArcVerificationFailureKind.Seal, sealInstance, "ARC-Seal instance " + sealInstance + " failed verification.", collection, messageSignatures, seals, hasOldestPass, oldestPass);
            }

            sealInstance--;
        }

        return CreateResult(EmailArcVerificationStatus.Pass, EmailArcVerificationFailureKind.None, 0, "The ARC chain verified successfully.", collection, messageSignatures, seals, hasOldestPass, oldestPass);
    }

    static EmailArcSignatureVerificationResult VerifyMessageSignature(EmailHeaders headers, ReadOnlySpan<byte> body, EmailArcHeaderField field, IEmailDkimKeyResolver keyResolver, EmailArcVerificationOptions options, long verificationTime)
    {
        EmailHeader header = headers.GetAt(field.GetSourceIndex());
        EnforceHeaderLimit(header, options);
        if (!field.IsParseSuccess() || !field.HasMessageSignature())
            return Result(field, EmailArcSignatureVerificationStatus.SignatureSyntaxError, "The ARC-Message-Signature field is malformed.", null, null, null, null, false, false);
        EmailDkimSignature? signature = field.GetMessageSignature();
        if ((signature ?? throw new InvalidOperationException()).GetSignedHeaderCount() > options.GetMaximumSignedHeaderCount())
            throw new EmailLimitException("MaxArcSignedHeaderCount", options.GetMaximumSignedHeaderCount(), signature.GetSignedHeaderCount());
        string syntaxReason = ValidateMessageSignature(signature);
        if (syntaxReason.Length > 0)
            return Result(field, EmailArcSignatureVerificationStatus.SignatureSyntaxError, syntaxReason, null, null, null, null, false, false);
        if (HasProhibitedSignedHeader(signature))
            return Result(field, EmailArcSignatureVerificationStatus.ProhibitedSignedHeader, "An ARC-Message-Signature must not sign Authentication-Results or ARC header fields.", null, null, null, null, false, false);
        if (!IsAlgorithmEnabled(signature.GetAlgorithmKind(), options.GetAllowLegacyRsaSha1()))
            return Result(field, EmailArcSignatureVerificationStatus.UnsupportedAlgorithm, "The ARC-Message-Signature algorithm is not enabled for verification.", null, null, null, null, false, false);
        EmailHashAlgorithm hashAlgorithm = GetHashAlgorithm(signature.GetAlgorithmKind());
        int expectedHashBytes = hashAlgorithm == EmailHashAlgorithm.Sha1 ? 20 : 32;
        if (Convert.FromBase64String(signature.GetBodyHash()).Length != expectedHashBytes)
            return Result(field, EmailArcSignatureVerificationStatus.SignatureSyntaxError, "The AMS body hash length does not match its algorithm.", null, null, null, null, false, false);
        if (signature.GetHeaderCanonicalizationKind() == EmailDkimCanonicalizationKind.Extension || signature.GetBodyCanonicalizationKind() == EmailDkimCanonicalizationKind.Extension)
            return Result(field, EmailArcSignatureVerificationStatus.UnsupportedCanonicalization, "The AMS canonicalization algorithm is unsupported.", null, null, null, null, false, false);
        if (!HasDnsTextQuery(signature))
            return Result(field, EmailArcSignatureVerificationStatus.UnsupportedQueryMethod, "The AMS does not specify a supported key query method.", null, null, null, null, false, false);
        EmailDkimVerificationKey? binding = EmailDkimKeyResolution.Resolve(keyResolver, signature.GetSigningDomain(), signature.GetSelector());
        EmailArcSignatureVerificationStatus keyStatus = ValidateKey(binding, hashAlgorithm);
        if (keyStatus != EmailArcSignatureVerificationStatus.Pass)
            return Result(field, keyStatus, KeyFailureReason(keyStatus), binding, null, null, null, false, false);
        if (signature.HasTimestamp() && IsBeyondSkew(signature.GetTimestamp(), verificationTime, options.GetAllowedClockSkewSeconds()))
            return Result(field, EmailArcSignatureVerificationStatus.TimestampInFuture, "The AMS timestamp is in the future.", binding, null, null, null, false, false);
        if (signature.HasExpiration() && IsBeyondSkew(verificationTime, signature.GetExpiration(), options.GetAllowedClockSkewSeconds()))
            return Result(field, EmailArcSignatureVerificationStatus.SignatureExpired, "The AMS has expired.", binding, null, null, null, false, false);
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
            return Result(field, EmailArcSignatureVerificationStatus.BodyLengthInvalid, "The AMS body-length tag exceeds the canonicalized body length.", binding, bodyResult, null, null, false, false);
        bool bodyHashValid = bodyResult.GetHash().Matches(Convert.FromBase64String(signature.GetBodyHash()));
        if (!bodyHashValid)
            return Result(field, EmailArcSignatureVerificationStatus.BodyHashMismatch, "The canonicalized body hash does not match the AMS bh= value.", binding, bodyResult, null, null, false, false);
        EmailDkimHeaderCanonicalizationOptions headerOptions = new EmailDkimHeaderCanonicalizationOptions();
        headerOptions.SetKind(signature.GetHeaderCanonicalizationKind());
        headerOptions.SetMaximumSelectedHeaderCount(options.GetMaximumSignedHeaderCount());
        headerOptions.SetMaximumInputBytes(options.GetMaximumCanonicalizedHeaderBytes());
        headerOptions.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedHeaderBytes());
        EmailDkimHeaderCanonicalizationResult headerResult = new EmailArcCanonicalizer().CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeaderAndOptions(headers, signature, header, headerOptions);
        byte[] headerHash = Hash(headerResult.GetCanonicalizedHeaders(), hashAlgorithm);
        bool signatureValid = VerifyRsa((binding ?? throw new InvalidOperationException()).GetKey(), headerHash, Convert.FromBase64String(signature.GetSignatureData()), hashAlgorithm);
        if (!signatureValid)
            return Result(field, EmailArcSignatureVerificationStatus.SignatureMismatch, "The AMS signature does not match the canonicalized message headers.", binding, bodyResult, headerResult, null, true, false);
        return Result(field, EmailArcSignatureVerificationStatus.Pass, "The ARC-Message-Signature verified successfully.", binding, bodyResult, headerResult, null, true, true);
    }

    static EmailArcSignatureVerificationResult VerifySeal(EmailArcSetCollectionResult collection, EmailArcHeaderField field, IEmailDkimKeyResolver keyResolver, EmailArcVerificationOptions options, long verificationTime)
    {
        EmailHeader header = field.GetHeader();
        EnforceHeaderLimit(header, options);
        if (!field.IsParseSuccess() || !field.HasSeal())
            return Result(field, EmailArcSignatureVerificationStatus.SignatureSyntaxError, "The ARC-Seal field is malformed.", null, null, null, null, false, false);
        EmailArcSeal? seal = field.GetSeal();
        if (!EmailDkimVerificationValidation.IsDomain((seal ?? throw new InvalidOperationException()).GetSigningDomain()) || !EmailDkimVerificationValidation.IsDomain(seal.GetSelector()))
            return Result(field, EmailArcSignatureVerificationStatus.SignatureSyntaxError, "The ARC-Seal signing domain or selector is invalid.", null, null, null, null, false, false);
        if (!IsAlgorithmEnabled(seal.GetAlgorithmKind(), options.GetAllowLegacyRsaSha1()))
            return Result(field, EmailArcSignatureVerificationStatus.UnsupportedAlgorithm, "The ARC-Seal algorithm is not enabled for verification.", null, null, null, null, false, false);
        EmailHashAlgorithm hashAlgorithm = GetHashAlgorithm(seal.GetAlgorithmKind());
        EmailDkimVerificationKey? binding = EmailDkimKeyResolution.Resolve(keyResolver, seal.GetSigningDomain(), seal.GetSelector());
        EmailArcSignatureVerificationStatus keyStatus = ValidateKey(binding, hashAlgorithm);
        if (keyStatus != EmailArcSignatureVerificationStatus.Pass)
            return Result(field, keyStatus, KeyFailureReason(keyStatus), binding, null, null, null, false, false);
        if (seal.GetTimestamp() >= 0 && IsBeyondSkew(seal.GetTimestamp(), verificationTime, options.GetAllowedClockSkewSeconds()))
            return Result(field, EmailArcSignatureVerificationStatus.TimestampInFuture, "The ARC-Seal timestamp is in the future.", binding, null, null, null, false, false);
        EmailArcSealCanonicalizationOptions canonicalizationOptions = new EmailArcSealCanonicalizationOptions();
        canonicalizationOptions.SetMaximumHeaderCount(options.GetCollectionOptions().GetMaximumArcHeaderCount());
        canonicalizationOptions.SetMaximumInputBytes(options.GetMaximumCanonicalizedHeaderBytes());
        canonicalizationOptions.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedHeaderBytes());
        EmailArcSealCanonicalizationResult canonicalization = new EmailArcCanonicalizer().CanonicalizeSealWithCollectionAndSealInstanceAndOptions(collection, field.GetInstance(), canonicalizationOptions);
        byte[] hash = Hash(canonicalization.GetCanonicalizedHeaders(), hashAlgorithm);
        bool signatureValid = VerifyRsa((binding ?? throw new InvalidOperationException()).GetKey(), hash, Convert.FromBase64String(seal.GetSignatureData()), hashAlgorithm);
        if (!signatureValid)
            return Result(field, EmailArcSignatureVerificationStatus.SignatureMismatch, "The ARC-Seal signature does not match its ARC chain scope.", binding, null, null, canonicalization, false, false);
        return Result(field, EmailArcSignatureVerificationStatus.Pass, "The ARC-Seal verified successfully.", binding, null, null, canonicalization, false, true);
    }

    static string ValidateMessageSignature(EmailDkimSignature signature)
    {
        if (!EmailDkimVerificationValidation.IsDomain(signature.GetSigningDomain()) || !EmailDkimVerificationValidation.IsDomain(signature.GetSelector()))
            return "The AMS signing domain or selector is invalid.";
        if (!HasSignedHeader(signature, "From"))
            return "The AMS signed-header list does not include From.";
        if (signature.HasTimestamp() && signature.HasExpiration() && signature.GetExpiration() <= signature.GetTimestamp())
            return "The AMS expiration is not later than its timestamp.";
        if (signature.HasBodyLength() && signature.GetBodyLength() < 0)
            return "The AMS body-length tag is too large to process.";
        return "";
    }

    static bool HasProhibitedSignedHeader(EmailDkimSignature signature)
    {
        List<string> names = signature.GetSignedHeaders();
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            if (string.Equals(name, "Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Message-Signature", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Seal", StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
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

    static bool IsAlgorithmEnabled(EmailDkimAlgorithmKind algorithm, bool allowLegacyRsaSha1)
    {
        return algorithm == EmailDkimAlgorithmKind.RsaSha256 || algorithm == EmailDkimAlgorithmKind.RsaSha1 && allowLegacyRsaSha1;
    }

    static EmailHashAlgorithm GetHashAlgorithm(EmailDkimAlgorithmKind algorithm)
    {
        return algorithm == EmailDkimAlgorithmKind.RsaSha1 ? EmailHashAlgorithm.Sha1 : EmailHashAlgorithm.Sha256;
    }

    static EmailArcSignatureVerificationStatus ValidateKey(EmailDkimVerificationKey? binding, EmailHashAlgorithm hashAlgorithm)
    {
        if (binding == null)
            return EmailArcSignatureVerificationStatus.KeyNotFound;
        EmailDkimKey key = binding.GetKey();
        if (!key.IsStructurallyValid())
            return EmailArcSignatureVerificationStatus.KeySyntaxError;
        if (key.IsRevoked() || !key.HasPublicKey())
            return EmailArcSignatureVerificationStatus.KeyRevoked;
        if (!key.IsSizePolicyCompliant())
            return EmailArcSignatureVerificationStatus.KeySizePolicyViolation;
        if (!key.AllowsService("email"))
            return EmailArcSignatureVerificationStatus.KeyServiceNotAllowed;
        string hashName = hashAlgorithm == EmailHashAlgorithm.Sha1 ? "sha1" : "sha256";
        if (!key.AllowsHashAlgorithm(hashName))
            return EmailArcSignatureVerificationStatus.KeyHashNotAllowed;
        if (key.GetAlgorithmKind() != EmailDkimKeyAlgorithmKind.Rsa || !key.HasRsaPublicKey())
            return EmailArcSignatureVerificationStatus.KeyAlgorithmMismatch;
        return EmailArcSignatureVerificationStatus.Pass;
    }

    static string KeyFailureReason(EmailArcSignatureVerificationStatus status)
    {
        if (status == EmailArcSignatureVerificationStatus.KeyNotFound)
            return "No caller-supplied key matches the ARC signing domain and selector.";
        if (status == EmailArcSignatureVerificationStatus.KeySyntaxError)
            return "The supplied ARC verification key is structurally invalid.";
        if (status == EmailArcSignatureVerificationStatus.KeyRevoked)
            return "The supplied ARC verification key is revoked.";
        if (status == EmailArcSignatureVerificationStatus.KeySizePolicyViolation)
            return "The supplied ARC verification key violates its key-size policy.";
        if (status == EmailArcSignatureVerificationStatus.KeyServiceNotAllowed)
            return "The supplied ARC verification key is not authorized for email.";
        if (status == EmailArcSignatureVerificationStatus.KeyHashNotAllowed)
            return "The supplied ARC verification key does not allow the signature hash algorithm.";
        return "The supplied ARC verification key does not match the signature algorithm.";
    }

    static byte[] Hash(byte[] value, EmailHashAlgorithm algorithm)
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
            HashAlgorithmName name = algorithm == EmailHashAlgorithm.Sha1 ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
            return rsa.VerifyHash(hash, signature, name, RSASignaturePadding.Pkcs1);
        }
    }

    static bool IsBeyondSkew(long later, long earlier, long skew)
    {
        if (later <= earlier)
            return false;
        return later - earlier > skew;
    }

    static void EnforceHeaderLimit(EmailHeader header, EmailArcVerificationOptions options)
    {
        int length = header.GetRawValue().Length;
        if (length > options.GetMaximumSignatureHeaderBytes())
            throw new EmailLimitException("MaxArcSignatureHeaderBytes", options.GetMaximumSignatureHeaderBytes(), length);
    }

    static void InitializeResults(EmailArcSetCollectionResult collection, EmailArcSignatureVerificationResult[] messageSignatures, EmailArcSignatureVerificationResult[] seals)
    {
        int index = 0;
        while (index < collection.GetSetCount())
        {
            EmailArcSet set = collection.GetSet(index);
            int instance = set.GetInstance();
            if (set.GetMessageSignatureFieldCount() == 1)
                messageSignatures[instance] = Result(set.GetMessageSignatureField(0), EmailArcSignatureVerificationStatus.NotEvaluated, "The ARC-Message-Signature was not evaluated.", null, null, null, null, false, false);
            if (set.GetSealFieldCount() == 1)
                seals[instance] = Result(set.GetSealField(0), EmailArcSignatureVerificationStatus.NotEvaluated, "The ARC-Seal was not evaluated.", null, null, null, null, false, false);
            index++;
        }
    }

    static EmailArcSignatureVerificationResult Result(EmailArcHeaderField field, EmailArcSignatureVerificationStatus status, string reason, EmailDkimVerificationKey? key, EmailDkimBodyCanonicalizationResult? body, EmailDkimHeaderCanonicalizationResult? messageHeaders, EmailArcSealCanonicalizationResult? sealHeaders, bool bodyHashValid, bool signatureValid)
    {
        return new EmailArcSignatureVerificationResult(field, status, reason, key, body, messageHeaders, sealHeaders, bodyHashValid, signatureValid);
    }

    static EmailArcVerificationResult CreateResult(EmailArcVerificationStatus status, EmailArcVerificationFailureKind failureKind, int failureInstance, string reason, EmailArcSetCollectionResult collection, EmailArcSignatureVerificationResult[] messageSignatures, EmailArcSignatureVerificationResult[] seals, bool hasOldestPass, int oldestPass)
    {
        List<EmailArcSetVerificationResult> sets = new List<EmailArcSetVerificationResult>();
        int index = 0;
        while (index < collection.GetSetCount())
        {
            EmailArcSet set = collection.GetSet(index);
            int instance = set.GetInstance();
            sets.Add(new EmailArcSetVerificationResult(set, messageSignatures[instance], seals[instance]));
            index++;
        }

        return new EmailArcVerificationResult(status, failureKind, failureInstance, reason, collection, sets, hasOldestPass, oldestPass);
    }
}
