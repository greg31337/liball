using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with openPGP MIME verifier.
/// </summary>
public class EmailOpenPgpMimeVerifier : IEmailOpenPgpMimeVerifierContract
{
    internal static EmailOpenPgpVerificationResult VerifyImplementationWithRootAndKeys(MimeEntity root, List<EmailOpenPgpVerificationKey> keys)
    {
        return VerifyImplementationWithRootAndKeysAndOptions(root, keys, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    public EmailOpenPgpVerificationResult VerifyWithRootAndKeys(MimeEntity root, List<EmailOpenPgpVerificationKey> keys)
    {
        return EmailOpenPgpMimeVerifier.VerifyImplementationWithRootAndKeys(root, keys);
    }

    internal static EmailOpenPgpVerificationResult VerifyImplementationWithRootAndKeysAndOptions(MimeEntity root, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        ValidateKeys(keys);
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailIssueList issues = new EmailIssueList();
        EmailSecurityMultipartParseResult parsed = new EmailSecurityMultipartParser().ParseWithRoot(root);
        if (!parsed.IsSuccess() || !(parsed.GetSecurityMultipart() ?? throw new InvalidOperationException()).IsSigned() || !string.Equals((parsed.GetSecurityMultipart() ?? throw new InvalidOperationException()).GetProtocol(), "application/pgp-signature", StringComparison.OrdinalIgnoreCase))
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.PgpMime, new byte[0], new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpMimeMalformed, "The entity is not a valid OpenPGP multipart/signed object.");
        EmailSecurityMultipart? security = parsed.GetSecurityMultipart();
        byte[] content = (security ?? throw new InvalidOperationException()).MaterializeExactSignedOctets(snapshot.GetLimits().GetMaxInputBytes());
        byte[]? signatureBytes = EmailOpenPgpMimeUtility.EntityContent(security.GetSignaturePart(), snapshot.GetLimits());
        if (signatureBytes == null)
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.PgpMime, content, new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpMimeMalformed, "The PGP/MIME signature part has no decodable content.");
        EmailOpenPgpSignaturePacket? signature = ParseSingleSignature(signatureBytes);
        if (signature == null || signature.GetSignatureType() != 0)
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.PgpMime, content, new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpMimeMalformed, "The PGP/MIME signature part is not one detached binary-document signature packet.");
        if (security.GetMicAlgorithmCount() != 1 || !string.Equals(security.GetMicAlgorithmAt(0), EmailOpenPgpMimeUtility.MicAlgorithm(signature.GetHashAlgorithm()), StringComparison.OrdinalIgnoreCase))
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.PgpMime, content, EmailOpenPgpMimeUtility.SignerIdentifier(signature), signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpMimeMalformed, "The PGP/MIME micalg parameter does not match the signature packet.");
        return VerifySignatureWithHashContentAndReturnedContentAndSignatureAndFormatAndKeysAndOptionsAndIssues(content, content, signature, EmailOpenPgpSignedFormat.PgpMime, keys, snapshot, issues);
    }

    /// <summary>
    /// Verifies the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpVerificationResult VerifyWithRootAndKeysAndOptions(MimeEntity root, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeVerifier.VerifyImplementationWithRootAndKeysAndOptions(root, keys, options);
    }

    internal static EmailOpenPgpVerificationResult VerifyCleartextImplementationWithInputAndKeys(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys)
    {
        return VerifyCleartextImplementationWithInputAndKeysAndOptions(input, keys, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Verifies the cleartext.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    public EmailOpenPgpVerificationResult VerifyCleartextWithInputAndKeys(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys)
    {
        return EmailOpenPgpMimeVerifier.VerifyCleartextImplementationWithInputAndKeys(input, keys);
    }

    internal static EmailOpenPgpVerificationResult VerifyCleartextImplementationWithInputAndKeysAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options)
    {
        ValidateKeys(keys);
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailOpenPgpMimeUtility.Bound(input.Length, snapshot.GetLimits().GetMaxInputBytes(), "MaxOpenPgpCleartextBytes");
        EmailIssueList issues = new EmailIssueList();
        EmailCryptographicWorkBudget work = new EmailCryptographicWorkBudget(snapshot.GetLimits());
        byte[] bytes = input.ToArray();
        List<PgpTextLine> lines = SplitLines(bytes);
        if (lines.Count < 3 || !AsciiEquals(lines[0].GetData(), "-----BEGIN PGP SIGNED MESSAGE-----"))
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Cleartext, new byte[0], new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpCleartextMalformed, "The cleartext signed message header is missing.");
        int index = 1;
        while (index < lines.Count && lines[index].GetData().Length != 0)
        {
            if (!IsHashHeader(lines[index].GetData()))
                return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Cleartext, new byte[0], new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpCleartextMalformed, "A cleartext signature contains a prohibited or malformed armor header.");
            index++;
        }

        if (index >= lines.Count)
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Cleartext, new byte[0], new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpCleartextMalformed, "The cleartext signature has no header separator.");
        index++;
        List<byte[]> displayLines = new List<byte[]>();
        List<byte[]> hashLines = new List<byte[]>();
        int signatureOffset = -1;
        while (index < lines.Count)
        {
            byte[] line = lines[index].GetData();
            if (AsciiEquals(line, "-----BEGIN PGP SIGNATURE-----"))
            {
                signatureOffset = lines[index].GetOffset();
                break;
            }

            if (line.Length > 0 && line[0] == (byte)'-')
            {
                if (line.Length < 2 || line[1] != (byte)' ')
                    return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Cleartext, JoinLines(displayLines, false), new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpCleartextMalformed, "A dash-prefixed cleartext line is not correctly dash-escaped.");
                line = EmailOpenPgpMimeUtility.Slice(line, 2, line.Length - 2);
            }

            displayLines.Add(line);
            hashLines.Add(TrimTrailingWhitespace(line));
            index++;
        }

        byte[] display = JoinLines(displayLines, false);
        if (signatureOffset < 0)
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Cleartext, display, new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpCleartextMalformed, "The armored signature following the cleartext is missing.");
        EmailOpenPgpSignaturePacket? signature = ParseSingleSignature(EmailOpenPgpMimeUtility.Slice(bytes, signatureOffset, bytes.Length - signatureOffset));
        if (signature == null || signature.GetSignatureType() != 1)
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Cleartext, display, new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpCleartextMalformed, "The cleartext framework does not contain one canonical-text signature packet.");
        return VerifySignatureWithHashContentAndReturnedContentAndSignatureAndFormatAndKeysAndOptionsAndIssuesAndIdentifierOverrideAndWork(JoinLines(hashLines, false), display, signature, EmailOpenPgpSignedFormat.Cleartext, keys, snapshot, issues, null, work);
    }

    /// <summary>
    /// Verifies the cleartext.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpVerificationResult VerifyCleartextWithInputAndKeysAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeVerifier.VerifyCleartextImplementationWithInputAndKeysAndOptions(input, keys, options);
    }

    internal static EmailOpenPgpVerificationResult VerifyInlineImplementationWithInputAndKeys(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys)
    {
        return VerifyInlineImplementationWithInputAndKeysAndOptions(input, keys, new EmailOpenPgpMimeOptions());
    }

    /// <summary>
    /// Verifies the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    public EmailOpenPgpVerificationResult VerifyInlineWithInputAndKeys(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys)
    {
        return EmailOpenPgpMimeVerifier.VerifyInlineImplementationWithInputAndKeys(input, keys);
    }

    internal static EmailOpenPgpVerificationResult VerifyInlineImplementationWithInputAndKeysAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options)
    {
        ValidateKeys(keys);
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpMimeOptions snapshot = options.Clone();
        EmailOpenPgpMimeUtility.Bound(input.Length, snapshot.GetLimits().GetMaxInputBytes(), "MaxOpenPgpInlineBytes");
        EmailIssueList issues = new EmailIssueList();
        EmailCryptographicWorkBudget work = new EmailCryptographicWorkBudget(snapshot.GetLimits());
        EmailOpenPgpReadResult? message = new EmailOpenPgpReader().ParseWithInputAndOptions(input, ReaderOptions(snapshot));
        message = UnwrapCompression(message, snapshot, work);
        if (message == null || !message.IsComplete() || message.GetPacketCount() != 3 || message.GetPacketAt(0).GetTypeIdentifier() != 4 || message.GetPacketAt(1).GetLiteralData() == null || message.GetPacketAt(2).GetSignature() == null)
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Inline, new byte[0], new byte[0], -1, -1, issues, EmailIssueCode.OpenPgpInlineUnsupported, "Only a complete one-pass inline signed OpenPGP message is supported.");
        EmailOpenPgpOnePassData? onePass = ParseOnePass(message.GetPacketAt(0).GetBody());
        EmailOpenPgpSignaturePacket? signature = message.GetPacketAt(2).GetSignature();
        byte[] content = (message.GetPacketAt(1).GetLiteralData() ?? throw new InvalidOperationException()).GetData();
        if (onePass == null || onePass.GetNested() != 1 || onePass.GetSignatureType() != (signature ?? throw new InvalidOperationException()).GetSignatureType() || onePass.GetHashAlgorithm() != signature.GetHashAlgorithm() || onePass.GetPublicKeyAlgorithm() != signature.GetPublicKeyAlgorithm() || signature.GetSignatureType() != 0 && signature.GetSignatureType() != 1 || onePass.GetSalt().Length > 0 && !onePass.GetSalt().AsSpan().SequenceEqual(signature.GetSalt()))
            return Failure(EmailOpenPgpVerificationStatus.Malformed, EmailOpenPgpSignedFormat.Inline, content, new byte[0], (signature ?? throw new InvalidOperationException()).GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpInlineUnsupported, "The one-pass and trailing signature packets are inconsistent or unsupported.");
        byte[] identifier = EmailOpenPgpMimeUtility.SignerIdentifier(signature);
        if (identifier.Length == 0)
            identifier = onePass.GetIdentifier();
        byte[] hashContent = signature.GetSignatureType() == 1 ? NormalizeText(content) : content;
        return VerifySignatureWithHashContentAndReturnedContentAndSignatureAndFormatAndKeysAndOptionsAndIssuesAndIdentifierOverrideAndWork(hashContent, content, signature, EmailOpenPgpSignedFormat.Inline, keys, snapshot, issues, identifier, work);
    }

    /// <summary>
    /// Verifies the inline.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "keys">The keys used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The openPGP verification result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpVerificationResult VerifyInlineWithInputAndKeysAndOptions(ReadOnlySpan<byte> input, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options)
    {
        return EmailOpenPgpMimeVerifier.VerifyInlineImplementationWithInputAndKeysAndOptions(input, keys, options);
    }

    static EmailOpenPgpVerificationResult VerifySignatureWithHashContentAndReturnedContentAndSignatureAndFormatAndKeysAndOptionsAndIssues(byte[] hashContent, byte[] returnedContent, EmailOpenPgpSignaturePacket signature, EmailOpenPgpSignedFormat format, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options, EmailIssueList issues)
    {
        return VerifySignatureWithHashContentAndReturnedContentAndSignatureAndFormatAndKeysAndOptionsAndIssuesAndIdentifierOverrideAndWork(hashContent, returnedContent, signature, format, keys, options, issues, null, null);
    }

    static EmailOpenPgpVerificationResult VerifySignatureWithHashContentAndReturnedContentAndSignatureAndFormatAndKeysAndOptionsAndIssuesAndIdentifierOverrideAndWork(byte[] hashContent, byte[] returnedContent, EmailOpenPgpSignaturePacket signature, EmailOpenPgpSignedFormat format, List<EmailOpenPgpVerificationKey> keys, EmailOpenPgpMimeOptions options, EmailIssueList issues, byte[]? identifierOverride, EmailCryptographicWorkBudget? work)
    {
        byte[] identifier = identifierOverride ?? EmailOpenPgpMimeUtility.SignerIdentifier(signature);
        EmailOpenPgpAlgorithmSupport support = new EmailOpenPgpAlgorithms().GetSupport(EmailOpenPgpAlgorithmCategory.PublicKey, signature.GetPublicKeyAlgorithm());
        if (!support.IsSupported())
            return Failure(EmailOpenPgpVerificationStatus.UnsupportedAlgorithm, format, returnedContent, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpAlgorithmUnsupported, "The OpenPGP signature public-key algorithm is unsupported.");
        EmailOpenPgpVerificationKey? key = EmailOpenPgpMimeUtility.FindVerificationKey(keys, identifier, signature.GetPublicKeyAlgorithm());
        if (key == null)
            return Failure(EmailOpenPgpVerificationStatus.KeyNotFound, format, returnedContent, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpVerificationKeyMissing, "No caller-supplied public key matches the OpenPGP signature issuer.");
        if (work == null)
            work = new EmailCryptographicWorkBudget(options.GetLimits());
        work.Charge(2);
        byte[]? hashInput = EmailOpenPgpMimeUtility.SignatureHashInput(hashContent, signature);
        if (hashInput == null)
            return Failure(EmailOpenPgpVerificationStatus.UnsupportedAlgorithm, format, returnedContent, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpAlgorithmUnsupported, "The OpenPGP signature version is unsupported.");
        EmailOpenPgpOperationResult digest = new EmailOpenPgpAlgorithms().ComputeHashForVerification(hashInput, signature.GetHashAlgorithm(), options.GetAlgorithmOptions());
        if (!digest.IsSuccess())
        {
            EmailOpenPgpVerificationResult failed = OperationFailure(digest, format, returnedContent, identifier, signature);
            digest.Dispose();
            return failed;
        }

        byte[] hash = digest.GetOutput();
        EmailOpenPgpVerificationResult result;
        if (signature.GetHashPrefix().Length != 2 || !CryptographicOperations.FixedTimeEquals(new ReadOnlySpan<byte>(hash, 0, 2), signature.GetHashPrefix()))
        {
            result = Failure(EmailOpenPgpVerificationStatus.SignatureMismatch, format, returnedContent, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpSignatureMismatch, "The OpenPGP signature hash prefix does not match the content.");
        }
        else
        {
            EmailOpenPgpOperationResult? verification = new EmailOpenPgpAlgorithms().VerifyHashWithHashAndSignatureMaterialAndHashAlgorithmAndPublicKeyAlgorithmAndKeyAndOptions(hash, signature.GetSignatureMaterial(), signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), key.GetPublicKey(), options.GetAlgorithmOptions());
            if (!(verification ?? throw new InvalidOperationException()).IsSuccess())
                result = OperationFailure(verification, format, returnedContent, identifier, signature);
            else if (!verification.IsVerified())
                result = Failure(EmailOpenPgpVerificationStatus.SignatureMismatch, format, returnedContent, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues, EmailIssueCode.OpenPgpSignatureMismatch, "The OpenPGP signature does not match the content.");
            else
                result = new EmailOpenPgpVerificationResult(EmailOpenPgpVerificationStatus.Pass, format, returnedContent, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), issues);
            verification.Dispose();
        }

        CryptographicOperations.ZeroMemory(hash);
        digest.Dispose();
        return result;
    }

    static EmailOpenPgpVerificationResult OperationFailure(EmailOpenPgpOperationResult operation, EmailOpenPgpSignedFormat format, byte[] content, byte[] identifier, EmailOpenPgpSignaturePacket signature)
    {
        EmailOpenPgpVerificationStatus status = operation.GetStatus() == EmailOpenPgpOperationStatus.UnsupportedAlgorithm ? EmailOpenPgpVerificationStatus.UnsupportedAlgorithm : operation.GetStatus() == EmailOpenPgpOperationStatus.PolicyRejected ? EmailOpenPgpVerificationStatus.PolicyRejected : EmailOpenPgpVerificationStatus.Malformed;
        return new EmailOpenPgpVerificationResult(status, format, content, identifier, signature.GetHashAlgorithm(), signature.GetPublicKeyAlgorithm(), operation.GetIssues());
    }

    static EmailOpenPgpSignaturePacket? ParseSingleSignature(ReadOnlySpan<byte> input)
    {
        EmailOpenPgpReadResult parsed = new EmailOpenPgpReader().ParseWithInput(input);
        if (!parsed.IsComplete() || parsed.GetPacketCount() != 1 || parsed.GetPacketAt(0).GetKind() != EmailOpenPgpPacketKind.Signature)
            return null;
        return parsed.GetPacketAt(0).GetSignature();
    }

    static EmailOpenPgpReadResult? UnwrapCompression(EmailOpenPgpReadResult value, EmailOpenPgpMimeOptions options, EmailCryptographicWorkBudget work)
    {
        int depth = 0;
        while (value != null && value.IsComplete() && value.GetPacketCount() == 1 && value.GetPacketAt(0).GetCompressedData() != null)
        {
            if (depth++ >= options.GetLimits().GetMaxAsn1Depth())
                return null;
            work.Charge(1);
            EmailOpenPgpCompressedData? compressed = value.GetPacketAt(0).GetCompressedData();
            EmailOpenPgpOperationResult? decompressed = new EmailOpenPgpAlgorithms().DecompressWithDataAndCompressionAlgorithmAndOptions((compressed ?? throw new InvalidOperationException()).GetCompressedData(), compressed.GetAlgorithm(), options.GetAlgorithmOptions());
            if (!(decompressed ?? throw new InvalidOperationException()).IsSuccess())
            {
                decompressed.Dispose();
                return null;
            }

            value = new EmailOpenPgpReader().ParseWithInputAndOptions(decompressed.GetOutput(), ReaderOptions(options));
            decompressed.Dispose();
        }

        return value;
    }

    static EmailOpenPgpOnePassData? ParseOnePass(byte[] body)
    {
        if (body.Length == 13 && body[0] == 3)
            return new EmailOpenPgpOnePassData(body[1], body[2], body[3], EmailOpenPgpMimeUtility.Slice(body, 4, 8), new byte[0], body[12]);
        if (body.Length < 38 || body[0] != 6)
            return null;
        int saltLength = body[4];
        if (body.Length != 38 + saltLength)
            return null;
        return new EmailOpenPgpOnePassData(body[1], body[2], body[3], EmailOpenPgpMimeUtility.Slice(body, 5 + saltLength, 32), EmailOpenPgpMimeUtility.Slice(body, 5, saltLength), body[body.Length - 1]);
    }

    static EmailOpenPgpReaderOptions ReaderOptions(EmailOpenPgpMimeOptions options)
    {
        EmailOpenPgpReaderOptions value = new EmailOpenPgpReaderOptions();
        value.SetLimits(options.GetLimits());
        return value;
    }

    static byte[] NormalizeText(byte[] input)
    {
        return JoinLines(SplitLineData(input), input.Length > 0 && (input[input.Length - 1] == 13 || input[input.Length - 1] == 10));
    }

    static List<byte[]> SplitLineData(byte[] input)
    {
        List<PgpTextLine> lines = SplitLines(input);
        List<byte[]> output = new List<byte[]>();
        int index = 0;
        while (index < lines.Count)
        {
            output.Add(lines[index].GetData());
            index++;
        }

        return output;
    }

    static List<PgpTextLine> SplitLines(byte[] input)
    {
        List<PgpTextLine> values = new List<PgpTextLine>();
        int start = 0;
        int index = 0;
        while (index < input.Length)
        {
            if (input[index] != 13 && input[index] != 10)
            {
                index++;
                continue;
            }

            values.Add(new PgpTextLine(start, EmailOpenPgpMimeUtility.Slice(input, start, index - start)));
            if (input[index] == 13 && index + 1 < input.Length && input[index + 1] == 10)
                index++;
            index++;
            start = index;
        }

        if (start < input.Length)
            values.Add(new PgpTextLine(start, EmailOpenPgpMimeUtility.Slice(input, start, input.Length - start)));
        return values;
    }

    static byte[] JoinLines(List<byte[]> lines, bool finalCrLf)
    {
        if (lines.Count == 0)
            return new byte[0];
        int length = finalCrLf ? lines.Count * 2 : (lines.Count - 1) * 2;
        int index = 0;
        while (index < lines.Count)
        {
            length += lines[index].Length;
            index++;
        }

        byte[] result = new byte[length];
        int position = 0;
        index = 0;
        while (index < lines.Count)
        {
            lines[index].CopyTo(result, position);
            position += lines[index].Length;
            if (index + 1 < lines.Count || finalCrLf)
            {
                result[position++] = 13;
                result[position++] = 10;
            }

            index++;
        }

        return result;
    }

    static byte[] TrimTrailingWhitespace(byte[] value)
    {
        int length = value.Length;
        while (length > 0 && (value[length - 1] == 32 || value[length - 1] == 9))
            length--;
        return length == value.Length ? value : EmailOpenPgpMimeUtility.Slice(value, 0, length);
    }

    static bool AsciiEquals(byte[] value, string expected)
    {
        return value.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes(expected));
    }

    static bool IsHashHeader(byte[] value)
    {
        if (value.Length <= 6 || !value.AsSpan(0, 6).SequenceEqual("Hash: "u8))
            return false;
        int index = 6;
        while (index < value.Length)
        {
            byte current = value[index];
            if (!(current >= (byte)'A' && current <= (byte)'Z' || current >= (byte)'a' && current <= (byte)'z' || current >= (byte)'0' && current <= (byte)'9' || current == (byte)'-' || current == (byte)',' || current == (byte)' '))
                return false;
            index++;
        }

        return true;
    }

    static void ValidateKeys(List<EmailOpenPgpVerificationKey> keys)
    {
        if (keys == null)
            throw new ArgumentNullException("keys");
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index] == null)
                throw new ArgumentException("The verification key list cannot contain null entries.", "keys");
            index++;
        }
    }

    static EmailOpenPgpVerificationResult Failure(EmailOpenPgpVerificationStatus status, EmailOpenPgpSignedFormat format, byte[] content, byte[] identifier, int hashAlgorithm, int publicKeyAlgorithm, EmailIssueList issues, EmailIssueCode code, string description)
    {
        issues.Add(EmailOpenPgpMimeUtility.Issue(code, description, "The content was retained for inspection but was not reported as authenticated."));
        return new EmailOpenPgpVerificationResult(status, format, content, identifier, hashAlgorithm, publicKeyAlgorithm, issues);
    }
}
