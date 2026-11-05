using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with ARC sealer.
/// </summary>
public class EmailArcSealer : IEmailArcSealerContract
{
    internal static EmailArcSealingResult SealImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA privateKey, EmailArcSealingOptions options)
    {
        return SealImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, privateKey, privateKey, options);
    }

    /// <summary>
    /// Seals the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    public EmailArcSealingResult SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA privateKey, EmailArcSealingOptions options)
    {
        return EmailArcSealer.SealImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, privateKey, options);
    }

    internal static EmailArcSealingResult SealImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (authenticationResults == null)
            throw new ArgumentNullException("authenticationResults");
        if (messageSignaturePrivateKey == null)
            throw new ArgumentNullException("messageSignaturePrivateKey");
        if (sealPrivateKey == null)
            throw new ArgumentNullException("sealPrivateKey");
        if (options == null)
            throw new ArgumentNullException("options");
        if (chainStatus < EmailArcChainStatus.None || chainStatus > EmailArcChainStatus.Fail)
            throw new ArgumentOutOfRangeException("chainStatus");
        EmailArcSealingOptions snapshot = options.Clone();
        ValidateSigningInputs(headers, messageSignaturePrivateKey, sealPrivateKey, snapshot);
        EmailArcSetCollectionResult priorCollection = new EmailArcSetCollector().CollectWithHeadersAndOptions(headers, snapshot.GetCollectionOptions());
        int instance = GetNextInstance(priorCollection, chainStatus);
        long timestamp = snapshot.HasTimestamp() ? snapshot.GetTimestamp() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        EmailDkimSigningValidation.RequireTime(timestamp, "timestamp");
        List<string> signedHeaders = GetSignedHeaders(headers, snapshot);
        if (signedHeaders.Count > snapshot.GetMaximumSignedHeaderCount())
            throw new EmailLimitException("MaxArcSignedHeaderCount", snapshot.GetMaximumSignedHeaderCount(), signedHeaders.Count);
        EmailHeaders working = new EmailHeadersOperations().CreateWithPreservedHeaders(headers.GetAll());
        EmailHeader authenticationResultsHeader = CreateAuthenticationResultsHeader(authenticationResults, instance, snapshot);
        working.Insert(0, authenticationResultsHeader);
        EmailDkimBodyCanonicalizationResult bodyResult = CanonicalizeBody(body, snapshot);
        string unsignedMessageValue = BuildMessageSignatureValue(instance, snapshot, timestamp, signedHeaders, bodyResult, "");
        EmailHeader unsignedMessageHeader = CreateHeader("ARC-Message-Signature", unsignedMessageValue, EmailHeaderValueKind.ArcMessageSignature);
        EnforceHeaderLimit(unsignedMessageHeader, snapshot);
        EmailDkimSignature? unsignedMessageSignature = ParseUnsignedMessageSignature(unsignedMessageHeader, snapshot);
        EmailDkimHeaderCanonicalizationOptions messageCanonicalizationOptions = CreateMessageCanonicalizationOptions(snapshot);
        EmailDkimHeaderCanonicalizationResult messageCanonicalization = new EmailArcCanonicalizer().CanonicalizeMessageSignatureWithHeadersAndSignatureAndSignatureHeaderAndOptions(working, unsignedMessageSignature ?? throw new InvalidOperationException(), unsignedMessageHeader, messageCanonicalizationOptions);
        byte[] messageHeaderHash = SHA256.HashData(messageCanonicalization.GetCanonicalizedHeaders());
        byte[] messageSignatureBytes = messageSignaturePrivateKey.SignHash(messageHeaderHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string finalMessageValue = BuildMessageSignatureValue(instance, snapshot, timestamp, signedHeaders, bodyResult, Convert.ToBase64String(messageSignatureBytes));
        EmailHeader messageSignatureHeader = CreateHeader("ARC-Message-Signature", finalMessageValue, EmailHeaderValueKind.ArcMessageSignature);
        EnforceHeaderLimit(messageSignatureHeader, snapshot);
        EmailDkimSignature? messageSignature = ParseMessageSignature(messageSignatureHeader, snapshot);
        working.Insert(0, messageSignatureHeader);
        string unsignedSealValue = BuildSealValue(instance, chainStatus, snapshot, timestamp, "");
        EmailHeader unsignedSealHeader = CreateHeader("ARC-Seal", unsignedSealValue, EmailHeaderValueKind.ArcSeal);
        EnforceHeaderLimit(unsignedSealHeader, snapshot);
        ParseUnsignedSeal(unsignedSealHeader, snapshot);
        working.Insert(0, unsignedSealHeader);
        EmailArcSetCollectionResult unsignedCollection = new EmailArcSetCollector().CollectWithHeadersAndOptions(working, snapshot.GetCollectionOptions());
        EmailArcSealCanonicalizationOptions sealCanonicalizationOptions = CreateSealCanonicalizationOptions(snapshot);
        EmailArcSealCanonicalizationResult sealCanonicalization = new EmailArcCanonicalizer().CanonicalizeSealWithCollectionAndSealInstanceAndOptions(unsignedCollection, instance, sealCanonicalizationOptions);
        byte[] sealHeaderHash = SHA256.HashData(sealCanonicalization.GetCanonicalizedHeaders());
        byte[] sealSignatureBytes = sealPrivateKey.SignHash(sealHeaderHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string finalSealValue = BuildSealValue(instance, chainStatus, snapshot, timestamp, Convert.ToBase64String(sealSignatureBytes));
        EmailHeader sealHeader = CreateHeader("ARC-Seal", finalSealValue, EmailHeaderValueKind.ArcSeal);
        EnforceHeaderLimit(sealHeader, snapshot);
        EmailArcSeal? seal = ParseSeal(sealHeader, snapshot);
        working.ReplaceAt(0, sealHeader);
        EmailArcSetCollectionResult finalCollection = new EmailArcSetCollector().CollectWithHeadersAndOptions(working, snapshot.GetCollectionOptions());
        EmailArcAuthenticationResults? arcAuthenticationResults = ParseAuthenticationResults(authenticationResultsHeader, snapshot);
        return new EmailArcSealingResult(instance, chainStatus, authenticationResultsHeader, arcAuthenticationResults, messageSignatureHeader, messageSignature, sealHeader, seal, bodyResult, messageCanonicalization, sealCanonicalization, messageHeaderHash, messageSignatureBytes, sealHeaderHash, sealSignatureBytes, finalCollection, priorCollection.GetFieldCount(), timestamp);
    }

    /// <summary>
    /// Seals the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "messageSignaturePrivateKey">The message signature private key used by the operation.</param>
    /// <param name = "sealPrivateKey">The seal private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailArcSealingResult SealWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options)
    {
        return EmailArcSealer.SealImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, messageSignaturePrivateKey, sealPrivateKey, options);
    }

    internal static EmailArcSealingResult SealAndInsertImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA privateKey, EmailArcSealingOptions options)
    {
        return SealAndInsertImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, privateKey, privateKey, options);
    }

    /// <summary>
    /// Seals the and insert.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    public EmailArcSealingResult SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA privateKey, EmailArcSealingOptions options)
    {
        return EmailArcSealer.SealAndInsertImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, privateKey, options);
    }

    internal static EmailArcSealingResult SealAndInsertImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options)
    {
        EmailArcSealingResult result = SealImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, messageSignaturePrivateKey, sealPrivateKey, options);
        headers.Insert(0, result.GetAuthenticationResultsHeader());
        headers.Insert(0, result.GetMessageSignatureHeader());
        headers.Insert(0, result.GetSealHeader());
        return result;
    }

    /// <summary>
    /// Seals the and insert.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "authenticationResults">The authentication results used by the operation.</param>
    /// <param name = "chainStatus">The chain status used by the operation.</param>
    /// <param name = "messageSignaturePrivateKey">The message signature private key used by the operation.</param>
    /// <param name = "sealPrivateKey">The seal private key used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC sealing result produced by the operation.</returns>
    public EmailArcSealingResult SealAndInsertWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(EmailHeaders headers, ReadOnlySpan<byte> body, EmailAuthenticationResults authenticationResults, EmailArcChainStatus chainStatus, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options)
    {
        return EmailArcSealer.SealAndInsertImplementationWithHeadersAndBodyAndAuthenticationResultsAndChainStatusAndMessageSignaturePrivateKeyAndSealPrivateKeyAndOptions(headers, body, authenticationResults, chainStatus, messageSignaturePrivateKey, sealPrivateKey, options);
    }

    static int GetNextInstance(EmailArcSetCollectionResult collection, EmailArcChainStatus chainStatus)
    {
        if (!collection.HasArcHeaders())
        {
            if (chainStatus != EmailArcChainStatus.None)
                throw new ArgumentException("A new ARC chain must be sealed with cv=none.", "chainStatus");
            return 1;
        }

        if (chainStatus == EmailArcChainStatus.None)
            throw new ArgumentException("An existing ARC chain cannot be extended with cv=none.", "chainStatus");
        int maximumInstance = collection.GetMaximumInstance();
        if (maximumInstance == 0)
            throw new InvalidOperationException("The existing ARC fields do not provide an instance that can be extended safely.");
        if (maximumInstance >= 50)
            throw new EmailLimitException("MaxArcSetCount", 50, maximumInstance + 1);
        EmailArcSet? highest = collection.GetSetByInstance(maximumInstance);
        if (highest != null && highest.GetDeclaredChainStatus() == EmailArcChainStatus.Fail)
            throw new InvalidOperationException("An ARC chain whose newest seal reports cv=fail cannot be extended.");
        if (collection.GetStatus() != EmailArcSetCollectionStatus.Valid && chainStatus != EmailArcChainStatus.Fail)
            throw new ArgumentException("A structurally invalid ARC chain must be sealed with cv=fail.", "chainStatus");
        return maximumInstance + 1;
    }

    static void ValidateSigningInputs(EmailHeaders headers, RSA messageSignaturePrivateKey, RSA sealPrivateKey, EmailArcSealingOptions options)
    {
        if (options.GetMessageSignatureDomain().Length == 0 || options.GetMessageSignatureSelector().Length == 0)
            throw new ArgumentException("An AMS signing domain and selector are required.", "options");
        if (options.GetSealDomain().Length == 0 || options.GetSealSelector().Length == 0)
            throw new ArgumentException("An ARC-Seal signing domain and selector are required.", "options");
        if (!headers.Contains("From"))
            throw new ArgumentException("A message must contain a From header before it can be ARC-sealed.", "headers");
        if (!Contains(options.GetSignedHeaders(), "From"))
            throw new ArgumentException("The AMS signed-header list must include From.", "options");
        ValidateKeySize(messageSignaturePrivateKey, options);
        ValidateKeySize(sealPrivateKey, options);
        int index = 0;
        while (index < options.GetSignedHeaderCount())
        {
            string name = options.GetSignedHeader(index);
            if (IsProhibitedSignedHeader(name))
                throw new ArgumentException("AMS cannot sign Authentication-Results or ARC header fields.", "options");
            if (name.Length + 4 > 998)
                throw new ArgumentException("A signed header name cannot fit on an Internet header line.", "options");
            index++;
        }
    }

    static void ValidateKeySize(RSA key, EmailArcSealingOptions options)
    {
        if (key.KeySize < options.GetMinimumRsaKeyBits() || key.KeySize > options.GetMaximumRsaKeyBits())
            throw new ArgumentException("An RSA key size is outside the configured ARC signing policy.", "key");
    }

    static List<string> GetSignedHeaders(EmailHeaders headers, EmailArcSealingOptions options)
    {
        List<string> names = options.GetSignedHeaders();
        if (!options.GetIncludeExistingDkimSignatures())
            return names;
        int required = headers.GetCountNamed("DKIM-Signature");
        int included = Count(names, "DKIM-Signature");
        while (included < required)
        {
            names.Add("DKIM-Signature");
            included++;
        }

        return names;
    }

    static EmailDkimBodyCanonicalizationResult CanonicalizeBody(ReadOnlySpan<byte> body, EmailArcSealingOptions options)
    {
        EmailDkimBodyCanonicalizationOptions bodyOptions = new EmailDkimBodyCanonicalizationOptions();
        bodyOptions.SetKind(options.GetBodyCanonicalizationKind());
        bodyOptions.SetHashAlgorithm(EmailHashAlgorithm.Sha256);
        bodyOptions.SetMaximumInputBytes(options.GetMaximumBodyInputBytes());
        bodyOptions.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedBodyBytes());
        bodyOptions.SetMaximumHashedBytes(options.GetMaximumCanonicalizedBodyBytes());
        if (options.HasBodyLengthLimit())
            bodyOptions.SetBodyLengthLimit(options.GetBodyLengthLimit());
        return new EmailDkimCanonicalizer().CanonicalizeBodyWithInputAndOptions(body, bodyOptions).GetResult();
    }

    static EmailHeader CreateAuthenticationResultsHeader(EmailAuthenticationResults value, int instance, EmailArcSealingOptions options)
    {
        string prefix = " i=" + instance + "; ";
        int minimumHeaderBytes = "ARC-Authentication-Results:".Length + prefix.Length + 2;
        if (minimumHeaderBytes > options.GetMaximumSignatureHeaderBytes())
            throw new EmailLimitException("MaxArcSignatureHeaderBytes", options.GetMaximumSignatureHeaderBytes(), minimumHeaderBytes);
        EmailAuthenticationFormatOptions formatOptions = new EmailAuthenticationFormatOptions();
        formatOptions.SetFirstLineColumn("ARC-Authentication-Results:".Length + prefix.Length);
        formatOptions.SetMaximumLineLength(options.GetPreferredLineLength());
        formatOptions.SetMaximumOutputBytes(options.GetMaximumSignatureHeaderBytes());
        EmailAuthenticationFormatResult formatted = new EmailAuthenticationFormatter().FormatWithValueAndOptions(value, formatOptions);
        if (!formatted.IsSuccess())
            throw new ArgumentException("The authentication assessment cannot be formatted as ARC-Authentication-Results.", "value");
        List<byte> rawValue = new List<byte>();
        rawValue.AddRange(Encoding.ASCII.GetBytes(prefix));
        rawValue.AddRange(formatted.GetBytes());
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("ARC-Authentication-Results", rawValue.ToArray(), EmailHeaderValueKind.ArcAuthenticationResults);
        header.SetLineEnding(EmailLineEnding.CrLf);
        EnforceHeaderLimit(header, options);
        ParseAuthenticationResults(header, options);
        return header;
    }

    static string BuildMessageSignatureValue(int instance, EmailArcSealingOptions options, long timestamp, List<string> signedHeaders, EmailDkimBodyCanonicalizationResult body, string signature)
    {
        StringBuilder value = new StringBuilder();
        value.Append(" i=");
        value.Append(instance);
        value.Append("; a=rsa-sha256;");
        AppendTagLine(value, "c", CanonicalizationName(options.GetHeaderCanonicalizationKind()) + "/" + CanonicalizationName(options.GetBodyCanonicalizationKind()));
        AppendTagLine(value, "d", options.GetMessageSignatureDomain());
        AppendTagLine(value, "s", options.GetMessageSignatureSelector());
        AppendTagLine(value, "t", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (options.HasBodyLengthLimit())
            AppendTagLine(value, "l", body.GetHashedByteCount().ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendSignedHeaders(value, signedHeaders, options.GetPreferredLineLength());
        AppendTagLine(value, "bh", Convert.ToBase64String(body.GetHash().GetDigestBytes()));
        AppendSignature(value, signature, options.GetPreferredLineLength());
        return value.ToString();
    }

    static string BuildSealValue(int instance, EmailArcChainStatus chainStatus, EmailArcSealingOptions options, long timestamp, string signature)
    {
        StringBuilder value = new StringBuilder();
        value.Append(" i=");
        value.Append(instance);
        value.Append("; a=rsa-sha256; cv=");
        value.Append(ChainStatusName(chainStatus));
        value.Append(';');
        AppendTagLine(value, "d", options.GetSealDomain());
        AppendTagLine(value, "s", options.GetSealSelector());
        AppendTagLine(value, "t", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendSignature(value, signature, options.GetPreferredLineLength());
        return value.ToString();
    }

    static void AppendTagLine(StringBuilder output, string name, string value)
    {
        if ((long)name.Length + value.Length + 3 > 998)
            throw new ArgumentException("An ARC tag cannot fit on an Internet header line.", "value");
        output.Append("\r\n\t");
        output.Append(name);
        output.Append('=');
        output.Append(value);
        output.Append(';');
    }

    static void AppendSignedHeaders(StringBuilder output, List<string> names, int preferredLineLength)
    {
        output.Append("\r\n\th=");
        int lineLength = 3;
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            if (index > 0)
            {
                if (lineLength + 1 + name.Length > preferredLineLength)
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

    static EmailHeader CreateHeader(string name, string value, EmailHeaderValueKind kind)
    {
        EmailHeader header = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.ASCII.GetBytes(value), kind);
        header.SetLineEnding(EmailLineEnding.CrLf);
        return header;
    }

    static EmailDkimHeaderCanonicalizationOptions CreateMessageCanonicalizationOptions(EmailArcSealingOptions options)
    {
        EmailDkimHeaderCanonicalizationOptions result = new EmailDkimHeaderCanonicalizationOptions();
        result.SetKind(options.GetHeaderCanonicalizationKind());
        result.SetMaximumSelectedHeaderCount(options.GetMaximumSignedHeaderCount());
        result.SetMaximumInputBytes(options.GetMaximumCanonicalizedHeaderBytes());
        result.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedHeaderBytes());
        return result;
    }

    static EmailArcSealCanonicalizationOptions CreateSealCanonicalizationOptions(EmailArcSealingOptions options)
    {
        EmailArcSealCanonicalizationOptions result = new EmailArcSealCanonicalizationOptions();
        result.SetMaximumHeaderCount(options.GetCollectionOptions().GetMaximumArcHeaderCount());
        result.SetMaximumInputBytes(options.GetMaximumCanonicalizedHeaderBytes());
        result.SetMaximumCanonicalizedBytes(options.GetMaximumCanonicalizedHeaderBytes());
        return result;
    }

    static EmailDkimSignature? ParseMessageSignature(EmailHeader header, EmailArcSealingOptions options)
    {
        EmailSignatureParserOptions parserOptions = CreateParserOptions(header, options);
        EmailArcMessageSignatureParseResult parsed = new EmailArcMessageSignatureParser().ParseWithInputAndOptions(header.GetRawValue(), parserOptions);
        if (!parsed.IsSuccess() || !parsed.HasValue())
            throw new InvalidOperationException("The generated ARC-Message-Signature did not pass strict parsing.");
        return parsed.GetValue();
    }

    static EmailDkimSignature? ParseUnsignedMessageSignature(EmailHeader header, EmailArcSealingOptions options)
    {
        EmailSignatureParserOptions parserOptions = CreateParserOptions(header, options);
        parserOptions.SetMode(EmailStructuredValueParseMode.Tolerant);
        EmailArcMessageSignatureParseResult parsed = new EmailArcMessageSignatureParser().ParseWithInputAndOptions(header.GetRawValue(), parserOptions);
        if (!parsed.HasValue())
            throw new InvalidOperationException("The generated unsigned ARC-Message-Signature could not be parsed.");
        return parsed.GetValue();
    }

    static EmailArcSeal? ParseSeal(EmailHeader header, EmailArcSealingOptions options)
    {
        EmailSignatureParserOptions parserOptions = CreateParserOptions(header, options);
        EmailArcSealParseResult parsed = new EmailArcSealParser().ParseWithInputAndOptions(header.GetRawValue(), parserOptions);
        if (!parsed.IsSuccess() || !parsed.HasValue())
            throw new InvalidOperationException("The generated ARC-Seal did not pass strict parsing.");
        return parsed.GetValue();
    }

    static EmailArcSeal? ParseUnsignedSeal(EmailHeader header, EmailArcSealingOptions options)
    {
        EmailSignatureParserOptions parserOptions = CreateParserOptions(header, options);
        parserOptions.SetMode(EmailStructuredValueParseMode.Tolerant);
        EmailArcSealParseResult parsed = new EmailArcSealParser().ParseWithInputAndOptions(header.GetRawValue(), parserOptions);
        if (!parsed.HasValue())
            throw new InvalidOperationException("The generated unsigned ARC-Seal could not be parsed.");
        return parsed.GetValue();
    }

    static EmailArcAuthenticationResults? ParseAuthenticationResults(EmailHeader header, EmailArcSealingOptions options)
    {
        EmailSignatureParserOptions parserOptions = CreateParserOptions(header, options);
        EmailArcAuthenticationResultsParseResult parsed = new EmailArcAuthenticationResultsParser().ParseWithInputAndOptions(header.GetRawValue(), parserOptions);
        if (!parsed.IsSuccess() || !parsed.HasValue())
            throw new InvalidOperationException("The generated ARC-Authentication-Results did not pass strict parsing.");
        return parsed.GetValue();
    }

    static EmailSignatureParserOptions CreateParserOptions(EmailHeader header, EmailArcSealingOptions options)
    {
        EmailSignatureParserOptions parserOptions = new EmailSignatureParserOptions();
        parserOptions.SetMode(EmailStructuredValueParseMode.Strict);
        parserOptions.SetMaximumInputBytes(header.GetRawValue().Length);
        parserOptions.SetMaximumTags(options.GetCollectionOptions().GetMaximumTags());
        return parserOptions;
    }

    static void EnforceHeaderLimit(EmailHeader header, EmailArcSealingOptions options)
    {
        int length = header.GetRawFieldBytes().Length;
        if (length > options.GetMaximumSignatureHeaderBytes())
            throw new EmailLimitException("MaxArcSignatureHeaderBytes", options.GetMaximumSignatureHeaderBytes(), length);
    }

    static bool IsProhibitedSignedHeader(string name)
    {
        return string.Equals(name, "Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Authentication-Results", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Message-Signature", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Seal", StringComparison.OrdinalIgnoreCase);
    }

    static bool Contains(List<string> names, string expected)
    {
        return Count(names, expected) > 0;
    }

    static int Count(List<string> names, string expected)
    {
        int count = 0;
        int index = 0;
        while (index < names.Count)
        {
            if (string.Equals(names[index], expected, StringComparison.OrdinalIgnoreCase))
                count++;
            index++;
        }

        return count;
    }

    static string CanonicalizationName(EmailDkimCanonicalizationKind value)
    {
        return value == EmailDkimCanonicalizationKind.Simple ? "simple" : "relaxed";
    }

    static string ChainStatusName(EmailArcChainStatus value)
    {
        if (value == EmailArcChainStatus.None)
            return "none";
        if (value == EmailArcChainStatus.Pass)
            return "pass";
        return "fail";
    }
}
