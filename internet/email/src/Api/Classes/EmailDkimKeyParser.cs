using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses DKIM key into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailDkimKeyParser : IEmailDkimKeyParserContract
{
    internal static EmailDkimKeyParseResult ParseImplementationWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        EmailDkimKeyParserOptions options = new EmailDkimKeyParserOptions();
        options.SetMode(mode);
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDkimKeyParseResult ParseWithInputAndMode(ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode)
    {
        return EmailDkimKeyParser.ParseImplementationWithInputAndMode(input, mode);
    }

    internal static EmailDkimKeyParseResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options)
    {
        return ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDkimKeyParseResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options)
    {
        return EmailDkimKeyParser.ParseImplementationWithInputAndOptions(input, options);
    }

    internal static EmailDkimKeyParseResult ParseImplementationWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options, EmailSourcePosition initialPosition)
    {
        ValidateArguments(options, initialPosition);
        EmailDkimKeyParserOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumInputBytes())
            return InputLimitResult(input.Length, initialPosition);
        byte[] source = input.ToArray();
        EmailSignatureParserOptions tagOptions = new EmailSignatureParserOptions();
        tagOptions.SetMode(snapshot.GetMode());
        tagOptions.SetMaximumTags(snapshot.GetMaximumTags());
        tagOptions.SetMaximumInputBytes(snapshot.GetMaximumInputBytes());
        EmailSignatureParserWorker worker = new EmailSignatureParserWorker(source, tagOptions, initialPosition);
        EmailSignatureTagList tags = worker.ParseTags(false);
        EmailIssueList issues = worker.GetIssues();
        if (issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal))
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DkimKeyInputLimitExceeded, "The DKIM key record exceeds the configured tag limit.", initialPosition, null, null, "Tags parsed before the limit remain available for diagnostics."));
        bool structurallyValid = ValidateRecordTags(tags, snapshot, initialPosition, issues);
        EmailSignatureTag? keyTag = tags.GetFirst("k");
        string keyType = keyTag == null ? "rsa" : keyTag.GetCompactValue().ToLowerInvariant();
        EmailDkimKeyAlgorithmKind algorithmKind = GetAlgorithmKind(keyType);
        EmailSignatureTag? publicTag = tags.GetFirst("p");
        byte[] publicKeyData = new byte[0];
        EmailDkimParsedKeyMaterial? material = null;
        bool revoked = publicTag != null && publicTag.GetCompactValue().Length == 0;
        bool sizeCompliant = true;
        if (revoked)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Information, EmailIssueCode.DkimKeyRevoked, "The DKIM key record explicitly revokes this selector.", (publicTag ?? throw new InvalidOperationException()).GetSourcePosition(), null, null, "The revoked state and original record were retained."));
        }
        else if (publicTag != null)
        {
            EmailBase64DecodeResult decoded = DecodeBase64(publicTag.GetCompactValue(), snapshot.GetMaximumDecodedKeyBytes());
            if (!decoded.IsSuccess())
            {
                structurallyValid = false;
                AddDecodeIssue(issues, decoded, publicTag.GetSourcePosition());
            }
            else
            {
                publicKeyData = decoded.GetDecodedBytes();
                if (algorithmKind == EmailDkimKeyAlgorithmKind.Rsa)
                {
                    material = ParseRsa(publicKeyData);
                    if (material == null)
                    {
                        structurallyValid = false;
                        AddError(issues, EmailIssueCode.DkimKeyMaterialMalformed, "The RSA DKIM public key is not a complete DER SubjectPublicKeyInfo or RSAPublicKey value.", publicTag.GetSourcePosition(), "The decoded bytes and original key record were retained without a usable key.");
                    }
                }
                else if (algorithmKind == EmailDkimKeyAlgorithmKind.Ed25519)
                {
                    if (publicKeyData.Length == 32)
                        material = new EmailDkimParsedKeyMaterial(EmailDkimKeyAlgorithmKind.Ed25519, "ed25519", null, null, publicKeyData, 256);
                    else
                    {
                        structurallyValid = false;
                        AddError(issues, EmailIssueCode.DkimKeyMaterialMalformed, "An Ed25519 DKIM public key must contain exactly 32 decoded octets.", publicTag.GetSourcePosition(), "The decoded bytes and original key record were retained without a usable key.");
                    }
                }
                else
                {
                    issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.DkimKeyAlgorithmUnsupported, "The DKIM key record uses the unsupported key type '" + keyType + "'.", keyTag == null ? initialPosition : keyTag.GetSourcePosition(), null, null, "The extension key type, decoded bytes, and original tags were retained."));
                }
            }
        }

        if (material != null && material.GetAlgorithmKind() == EmailDkimKeyAlgorithmKind.Rsa)
            sizeCompliant = ValidateRsaSize(material.GetKeyBitCount(), snapshot, issues, publicTag == null ? initialPosition : publicTag.GetSourcePosition());
        EmailDkimKey value = CreateKey(tags, source, initialPosition, EmailDkimKeySourceKind.DnsRecord, algorithmKind, keyType, publicKeyData, material, revoked, structurallyValid, sizeCompliant);
        return new EmailDkimKeyParseResult(value, issues, worker.GetConsumedByteCount(), source.Length);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailDkimKeyParseResult ParseWithInputAndOptionsAndInitialPosition(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options, EmailSourcePosition initialPosition)
    {
        return EmailDkimKeyParser.ParseImplementationWithInputAndOptionsAndInitialPosition(input, options, initialPosition);
    }

    internal static EmailDkimKeyParseResult ImportSubjectPublicKeyInfoImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return ImportSubjectPublicKeyInfoImplementationWithInputAndOptions(input, new EmailDkimKeyParserOptions());
    }

    /// <summary>
    /// Imports the subject public key info.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    public EmailDkimKeyParseResult ImportSubjectPublicKeyInfoWithInput(ReadOnlySpan<byte> input)
    {
        return EmailDkimKeyParser.ImportSubjectPublicKeyInfoImplementationWithInput(input);
    }

    internal static EmailDkimKeyParseResult ImportSubjectPublicKeyInfoImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDkimKeyParserOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumInputBytes() || input.Length > snapshot.GetMaximumDecodedKeyBytes())
            return InputLimitResult(input.Length, new EmailSourcePosition(0, 1, 1));
        byte[] source = input.ToArray();
        EmailIssueList issues = new EmailIssueList();
        EmailDkimParsedKeyMaterial? material = EmailDkimKeyDer.ParseSubjectPublicKeyInfo(source);
        if (material == null)
        {
            AddError(issues, EmailIssueCode.DkimKeyMaterialMalformed, "The imported SubjectPublicKeyInfo is malformed or uses an unsupported algorithm.", new EmailSourcePosition(0, 1, 1), "No DKIM key was produced.");
            return new EmailDkimKeyParseResult(null, issues, source.Length, source.Length);
        }

        bool sizeCompliant = material.GetAlgorithmKind() != EmailDkimKeyAlgorithmKind.Rsa || ValidateRsaSize(material.GetKeyBitCount(), snapshot, issues, new EmailSourcePosition(0, 1, 1));
        EmailDkimKey key = CreateImportedKey(source, EmailDkimKeySourceKind.SubjectPublicKeyInfo, material, sizeCompliant);
        return new EmailDkimKeyParseResult(key, issues, source.Length, source.Length);
    }

    /// <summary>
    /// Imports the subject public key info.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimKeyParseResult ImportSubjectPublicKeyInfoWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options)
    {
        return EmailDkimKeyParser.ImportSubjectPublicKeyInfoImplementationWithInputAndOptions(input, options);
    }

    internal static EmailDkimKeyParseResult ImportRsaPublicKeyImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return ImportRsaPublicKeyImplementationWithInputAndOptions(input, new EmailDkimKeyParserOptions());
    }

    /// <summary>
    /// Imports the RSA public key.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    public EmailDkimKeyParseResult ImportRsaPublicKeyWithInput(ReadOnlySpan<byte> input)
    {
        return EmailDkimKeyParser.ImportRsaPublicKeyImplementationWithInput(input);
    }

    internal static EmailDkimKeyParseResult ImportRsaPublicKeyImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDkimKeyParserOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumInputBytes() || input.Length > snapshot.GetMaximumDecodedKeyBytes())
            return InputLimitResult(input.Length, new EmailSourcePosition(0, 1, 1));
        byte[] source = input.ToArray();
        EmailIssueList issues = new EmailIssueList();
        EmailDkimParsedKeyMaterial? material = EmailDkimKeyDer.ParseRsaPublicKey(source);
        if (material == null)
        {
            AddError(issues, EmailIssueCode.DkimKeyMaterialMalformed, "The imported RSAPublicKey is malformed.", new EmailSourcePosition(0, 1, 1), "No DKIM key was produced.");
            return new EmailDkimKeyParseResult(null, issues, source.Length, source.Length);
        }

        bool sizeCompliant = ValidateRsaSize(material.GetKeyBitCount(), snapshot, issues, new EmailSourcePosition(0, 1, 1));
        EmailDkimKey key = CreateImportedKey(source, EmailDkimKeySourceKind.RsaPublicKey, material, sizeCompliant);
        return new EmailDkimKeyParseResult(key, issues, source.Length, source.Length);
    }

    /// <summary>
    /// Imports the RSA public key.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimKeyParseResult ImportRsaPublicKeyWithInputAndOptions(ReadOnlySpan<byte> input, EmailDkimKeyParserOptions options)
    {
        return EmailDkimKeyParser.ImportRsaPublicKeyImplementationWithInputAndOptions(input, options);
    }

    internal static EmailDkimKeyParseResult ImportPemImplementationWithInput(string input)
    {
        return ImportPemImplementationWithInputAndOptions(input, new EmailDkimKeyParserOptions());
    }

    /// <summary>
    /// Imports the pem.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    public EmailDkimKeyParseResult ImportPemWithInput(string input)
    {
        return EmailDkimKeyParser.ImportPemImplementationWithInput(input);
    }

    internal static EmailDkimKeyParseResult ImportPemImplementationWithInputAndOptions(string input, EmailDkimKeyParserOptions options)
    {
        if (input == null)
            throw new ArgumentNullException("input");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDkimKeyParserOptions snapshot = options.Clone();
        if (input.Length > snapshot.GetMaximumInputBytes())
            return InputLimitResult(input.Length, new EmailSourcePosition(0, 1, 1));
        EmailIssueList issues = new EmailIssueList();
        if (!IsAscii(input))
        {
            AddError(issues, EmailIssueCode.DkimKeyPemMalformed, "A PEM public key must contain only ASCII characters.", new EmailSourcePosition(0, 1, 1), "No DKIM key was produced.");
            return new EmailDkimKeyParseResult(null, issues, input.Length, input.Length);
        }

        string trimmed = input.Trim();
        string publicHeader = "-----BEGIN PUBLIC KEY-----";
        string rsaHeader = "-----BEGIN RSA PUBLIC KEY-----";
        string footer;
        EmailDkimKeySourceKind sourceKind;
        int headerLength;
        bool subjectPublicKeyInfo;
        if (trimmed.StartsWith(publicHeader, StringComparison.Ordinal))
        {
            footer = "-----END PUBLIC KEY-----";
            sourceKind = EmailDkimKeySourceKind.PemSubjectPublicKeyInfo;
            headerLength = publicHeader.Length;
            subjectPublicKeyInfo = true;
        }
        else if (trimmed.StartsWith(rsaHeader, StringComparison.Ordinal))
        {
            footer = "-----END RSA PUBLIC KEY-----";
            sourceKind = EmailDkimKeySourceKind.PemRsaPublicKey;
            headerLength = rsaHeader.Length;
            subjectPublicKeyInfo = false;
        }
        else
        {
            AddError(issues, EmailIssueCode.DkimKeyPemMalformed, "PEM import requires a PUBLIC KEY or RSA PUBLIC KEY block.", new EmailSourcePosition(0, 1, 1), "No DKIM key was produced.");
            return new EmailDkimKeyParseResult(null, issues, input.Length, input.Length);
        }

        int footerIndex = trimmed.IndexOf(footer, headerLength, StringComparison.Ordinal);
        bool headerLine = headerLength < trimmed.Length && (trimmed[headerLength] == '\r' || trimmed[headerLength] == '\n');
        bool footerLine = footerIndex > headerLength && (trimmed[footerIndex - 1] == '\r' || trimmed[footerIndex - 1] == '\n');
        if (footerIndex < 0 || !headerLine || !footerLine || trimmed.Substring(footerIndex + footer.Length).Trim().Length != 0)
        {
            AddError(issues, EmailIssueCode.DkimKeyPemMalformed, "The PEM public-key block is incomplete or contains trailing data.", new EmailSourcePosition(0, 1, 1), "No DKIM key was produced.");
            return new EmailDkimKeyParseResult(null, issues, input.Length, input.Length);
        }

        string payload = trimmed.Substring(headerLength, footerIndex - headerLength);
        EmailBase64DecodeResult decoded = DecodeBase64(payload, snapshot.GetMaximumDecodedKeyBytes());
        if (!decoded.IsSuccess())
        {
            AddDecodeIssue(issues, decoded, new EmailSourcePosition(headerLength, 1, headerLength + 1));
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.DkimKeyPemMalformed, "The PEM public-key payload is not strict Base64.", new EmailSourcePosition(headerLength, 1, headerLength + 1), null, null, "No DKIM key was produced."));
            return new EmailDkimKeyParseResult(null, issues, input.Length, input.Length);
        }

        byte[] der = decoded.GetDecodedBytes();
        EmailDkimParsedKeyMaterial? material = subjectPublicKeyInfo ? EmailDkimKeyDer.ParseSubjectPublicKeyInfo(der) : EmailDkimKeyDer.ParseRsaPublicKey(der);
        if (material == null)
        {
            AddError(issues, EmailIssueCode.DkimKeyMaterialMalformed, "The PEM block does not contain a valid supported public key.", new EmailSourcePosition(0, 1, 1), "No DKIM key was produced.");
            return new EmailDkimKeyParseResult(null, issues, input.Length, input.Length);
        }

        bool sizeCompliant = material.GetAlgorithmKind() != EmailDkimKeyAlgorithmKind.Rsa || ValidateRsaSize(material.GetKeyBitCount(), snapshot, issues, new EmailSourcePosition(0, 1, 1));
        byte[] source = Encoding.ASCII.GetBytes(input);
        EmailDkimKey key = CreateImportedKeyWithRaw(source, der, sourceKind, material, sizeCompliant);
        return new EmailDkimKeyParseResult(key, issues, input.Length, input.Length);
    }

    /// <summary>
    /// Imports the pem.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The DKIM key parse result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimKeyParseResult ImportPemWithInputAndOptions(string input, EmailDkimKeyParserOptions options)
    {
        return EmailDkimKeyParser.ImportPemImplementationWithInputAndOptions(input, options);
    }

    static bool ValidateRecordTags(EmailSignatureTagList tags, EmailDkimKeyParserOptions options, EmailSourcePosition position, EmailIssueList issues)
    {
        bool valid = true;
        List<EmailSignatureTag> all = tags.GetAll();
        int index = 0;
        while (index < all.Count)
        {
            if (all[index].IsMalformed() || tags.HasDuplicate(all[index].GetName()))
                valid = false;
            index++;
        }

        EmailSignatureTag? version = tags.GetFirst("v");
        if (version != null && (version.GetCompactValue() != "DKIM1" || tags.GetCount() == 0 || tags.GetAt(0) != version))
        {
            valid = false;
            AddModeIssue(issues, options, "The DKIM key version must be exactly DKIM1 and must be the first tag.", version.GetSourcePosition());
        }

        EmailSignatureTag? publicKey = tags.GetFirst("p");
        if (publicKey == null)
        {
            valid = false;
            AddModeIssue(issues, options, "The required DKIM public-key tag 'p' is missing.", position);
        }

        if (!ValidateWord(tags.GetFirst("k"), false) || !ValidateList(tags.GetFirst("h"), false) || !ValidateList(tags.GetFirst("s"), true) || !ValidateList(tags.GetFirst("t"), false) || !ValidateNotes(tags.GetFirst("n")))
        {
            valid = false;
            AddModeIssue(issues, options, "A DKIM key algorithm, hash, service, or flag tag is malformed.", position);
        }

        return valid;
    }

    static bool ValidateWord(EmailSignatureTag? tag, bool allowStar)
    {
        if (tag == null)
            return true;
        string value = tag.GetCompactValue();
        if (allowStar && value == "*")
            return true;
        if (value.Length == 0 || !IsAlpha(value[0]))
            return false;
        if (value.Length > 1 && value[value.Length - 1] == '-')
            return false;
        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsAlpha(current) && !IsDigit(current) && current != '-')
                return false;
            index++;
        }

        return true;
    }

    static bool ValidateNotes(EmailSignatureTag? tag)
    {
        if (tag == null)
            return true;
        string value = tag.GetValue();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '=')
            {
                if (index + 2 >= value.Length || !IsUpperHex(value[index + 1]) || !IsUpperHex(value[index + 2]))
                    return false;
                index += 3;
            }
            else
            {
                if (current < 32 || current > 126 || current == ';')
                    return false;
                index++;
            }
        }

        return true;
    }

    static bool ValidateList(EmailSignatureTag? tag, bool allowStar)
    {
        if (tag == null)
            return true;
        string[] values = tag.GetCompactValue().Split(':');
        if (values.Length == 0)
            return false;
        int index = 0;
        while (index < values.Length)
        {
            EmailSignatureTag item = new EmailSignatureTag("x", values[index], values[index], new byte[0], new byte[0], tag.GetSourcePosition(), false);
            if (!ValidateWord(item, allowStar))
                return false;
            index++;
        }

        return true;
    }

    static EmailDkimParsedKeyMaterial? ParseRsa(byte[] value)
    {
        EmailDkimParsedKeyMaterial? material = EmailDkimKeyDer.ParseSubjectPublicKeyInfo(value);
        if (material != null && material.GetAlgorithmKind() == EmailDkimKeyAlgorithmKind.Rsa)
            return material;
        return EmailDkimKeyDer.ParseRsaPublicKey(value);
    }

    static EmailDkimKeyAlgorithmKind GetAlgorithmKind(string value)
    {
        if (string.Equals(value, "rsa", StringComparison.OrdinalIgnoreCase))
            return EmailDkimKeyAlgorithmKind.Rsa;
        if (string.Equals(value, "ed25519", StringComparison.OrdinalIgnoreCase))
            return EmailDkimKeyAlgorithmKind.Ed25519;
        return value.Length == 0 ? EmailDkimKeyAlgorithmKind.Unknown : EmailDkimKeyAlgorithmKind.Extension;
    }

    static bool ValidateRsaSize(int bits, EmailDkimKeyParserOptions options, EmailIssueList issues, EmailSourcePosition position)
    {
        if (bits >= options.GetMinimumRsaKeyBits() && bits <= options.GetMaximumRsaKeyBits())
            return true;
        AddError(issues, EmailIssueCode.DkimKeySizePolicyViolation, "The RSA DKIM key has " + bits + " bits; policy permits " + options.GetMinimumRsaKeyBits() + " through " + options.GetMaximumRsaKeyBits() + " bits.", position, "The key metadata was retained but the key is not policy-compliant.");
        return false;
    }

    static EmailBase64DecodeResult DecodeBase64(string value, long maximumBytes)
    {
        if (!IsAscii(value))
        {
            EmailIssueList issues = new EmailIssueList();
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.Base64InvalidCharacter, "Base64 key data contains a non-ASCII character.", null, null, null, "No key bytes were decoded."));
            return new EmailBase64DecodeResult(new byte[0], issues);
        }

        return new EmailBase64().Decode(Encoding.ASCII.GetBytes(value), EmailBase64DecodeMode.Strict, maximumBytes);
    }

    static void AddDecodeIssue(EmailIssueList issues, EmailBase64DecodeResult decoded, EmailSourcePosition position)
    {
        if (decoded.HasFatalError())
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DkimKeyInputLimitExceeded, "Decoded DKIM public-key data exceeds the configured key-byte limit.", position, null, null, "Partial decoded data was discarded."));
        else
            AddError(issues, EmailIssueCode.DkimKeyMaterialMalformed, "DKIM public-key data is not strict Base64.", position, "The original record was retained without a usable key.");
    }

    static EmailDkimKey CreateKey(EmailSignatureTagList tags, byte[] raw, EmailSourcePosition position, EmailDkimKeySourceKind sourceKind, EmailDkimKeyAlgorithmKind algorithmKind, string keyType, byte[] publicKeyData, EmailDkimParsedKeyMaterial? material, bool revoked, bool structurallyValid, bool sizeCompliant)
    {
        byte[]? spki = material == null ? null : material.GetSubjectPublicKeyInfo();
        byte[]? rsa = material == null ? null : material.GetRsaPublicKey();
        byte[]? ed = material == null ? null : material.GetEd25519PublicKey();
        int bits = material == null ? 0 : material.GetKeyBitCount();
        return new EmailDkimKey(tags, raw, position, sourceKind, algorithmKind, keyType, publicKeyData, spki, rsa, ed, bits, revoked, structurallyValid, sizeCompliant);
    }

    static EmailDkimKey CreateImportedKey(byte[] source, EmailDkimKeySourceKind sourceKind, EmailDkimParsedKeyMaterial material, bool sizeCompliant)
    {
        return CreateImportedKeyWithRaw(source, source, sourceKind, material, sizeCompliant);
    }

    static EmailDkimKey CreateImportedKeyWithRaw(byte[] raw, byte[] publicKeyData, EmailDkimKeySourceKind sourceKind, EmailDkimParsedKeyMaterial material, bool sizeCompliant)
    {
        EmailSignatureTagList tags = new EmailSignatureTagList(new List<EmailSignatureTag>(), new byte[0]);
        return new EmailDkimKey(tags, raw, new EmailSourcePosition(0, 1, 1), sourceKind, material.GetAlgorithmKind(), material.GetKeyType(), publicKeyData, material.GetSubjectPublicKeyInfo(), material.GetRsaPublicKey(), material.GetEd25519PublicKey(), material.GetKeyBitCount(), false, true, sizeCompliant);
    }

    static EmailDkimKeyParseResult InputLimitResult(int sourceLength, EmailSourcePosition position)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DkimKeyInputLimitExceeded, "DKIM key input exceeds the configured input-byte limit.", position, null, null, "No DKIM key was produced."));
        return new EmailDkimKeyParseResult(null, issues, 0, sourceLength);
    }

    static void ValidateArguments(EmailDkimKeyParserOptions options, EmailSourcePosition position)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        if (position == null)
            throw new ArgumentNullException("initialPosition");
    }

    static void AddModeIssue(EmailIssueList issues, EmailDkimKeyParserOptions options, string description, EmailSourcePosition position)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailStructuredValueParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.DkimKeyRecordMalformed, description, position, null, null, "The original key tags were retained for diagnostics."));
    }

    static void AddError(EmailIssueList issues, EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, position, null, null, recovery));
    }

    static bool IsAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
                return false;
            index++;
        }

        return true;
    }

    static bool IsAlpha(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool IsUpperHex(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F';
    }
}
