using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Represents a policy.
/// </summary>
public class EmailPolicy : IEmailPolicyContract
{
    EmailParseOptions parseOptions;
    EmailWriteOptions writeOptions;
    EmailValidationOptions validationOptions;
    EmailNormalizationOptions normalizationOptions;
    EmailGenerationOptions generationOptions;
    EmailRedactionOptions redactionOptions;
    EmailDisplayOptions displayOptions;
    EmailCanonicalComparisonOptions comparisonOptions;
    EmailPolicy(EmailParseOptions parseOptions, EmailWriteOptions writeOptions, EmailValidationOptions validationOptions, EmailNormalizationOptions normalizationOptions, EmailGenerationOptions generationOptions, EmailRedactionOptions redactionOptions, EmailDisplayOptions displayOptions, EmailCanonicalComparisonOptions comparisonOptions)
    {
        this.parseOptions = parseOptions.Clone();
        this.writeOptions = writeOptions.Clone();
        this.validationOptions = validationOptions.Clone();
        this.normalizationOptions = normalizationOptions.Clone();
        this.generationOptions = generationOptions.Clone();
        this.redactionOptions = redactionOptions.Clone();
        this.displayOptions = displayOptions.Clone();
        this.comparisonOptions = comparisonOptions.Clone();
    }

    /// <summary>
    /// Creates a parse options configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreateParseOptions()
    {
        return parseOptions.Clone();
    }

    /// <summary>
    /// Creates a write options configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    public EmailWriteOptions CreateWriteOptions()
    {
        return writeOptions.Clone();
    }

    /// <summary>
    /// Creates a validation options configured for the requested behavior.
    /// </summary>
    /// <returns>The validation options produced by the operation.</returns>
    public EmailValidationOptions CreateValidationOptions()
    {
        return validationOptions.Clone();
    }

    /// <summary>
    /// Creates a normalization options configured for the requested behavior.
    /// </summary>
    /// <returns>The normalization options produced by the operation.</returns>
    public EmailNormalizationOptions CreateNormalizationOptions()
    {
        return normalizationOptions.Clone();
    }

    /// <summary>
    /// Creates a generation options configured for the requested behavior.
    /// </summary>
    /// <returns>The generation options produced by the operation.</returns>
    public EmailGenerationOptions CreateGenerationOptions()
    {
        return generationOptions.Clone();
    }

    /// <summary>
    /// Creates a redaction options configured for the requested behavior.
    /// </summary>
    /// <returns>The redaction options produced by the operation.</returns>
    public EmailRedactionOptions CreateRedactionOptions()
    {
        return redactionOptions.Clone();
    }

    /// <summary>
    /// Creates a display options configured for the requested behavior.
    /// </summary>
    /// <returns>The display options produced by the operation.</returns>
    public EmailDisplayOptions CreateDisplayOptions()
    {
        return displayOptions.Clone();
    }

    /// <summary>
    /// Creates a canonical comparison options configured for the requested behavior.
    /// </summary>
    /// <returns>The canonical comparison options produced by the operation.</returns>
    public EmailCanonicalComparisonOptions CreateCanonicalComparisonOptions()
    {
        return comparisonOptions.Clone();
    }

    internal static EmailPolicy CreatePreserveImplementation()
    {
        return CreatePolicy(EmailParseMode.ConformingTolerant, true, false, false, false, false, EmailWriteMode.ExactPreservation, EmailTransferEncodingConstraint.SevenBit, false, false, false, false, false, false);
    }

    internal static EmailPolicy CreateStrictInternetImplementation()
    {
        return CreatePolicy(EmailParseMode.Strict, false, false, true, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.SevenBit, false, false, true, false, false, false);
    }

    internal static EmailPolicy CreateTolerantInternetImplementation()
    {
        return CreatePolicy(EmailParseMode.ConformingTolerant, true, false, false, false, false, EmailWriteMode.MinimalRegeneration, EmailTransferEncodingConstraint.SevenBit, false, false, false, false, false, false);
    }

    internal static EmailPolicy CreateForensicImplementation()
    {
        return CreatePolicy(EmailParseMode.Forensic, true, true, false, true, true, EmailWriteMode.ExactPreservation, EmailTransferEncodingConstraint.Binary, true, false, false, false, false, false);
    }

    internal static EmailPolicy CreateSevenBitTransportImplementation()
    {
        return CreatePolicy(EmailParseMode.ConformingTolerant, true, false, false, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.SevenBit, false, false, true, true, false, false);
    }

    internal static EmailPolicy CreateSmtpUtf8Implementation()
    {
        return CreatePolicy(EmailParseMode.ConformingTolerant, true, true, false, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.EightBit, true, false, true, true, false, false);
    }

    internal static EmailPolicy CreateSafeDisplayImplementation()
    {
        return CreatePolicy(EmailParseMode.ConformingTolerant, false, false, false, false, false, EmailWriteMode.MinimalRegeneration, EmailTransferEncodingConstraint.SevenBit, false, true, false, false, true, false);
    }

    internal static EmailPolicy CreateDeterministicTestImplementation()
    {
        return CreatePolicy(EmailParseMode.ConformingTolerant, true, true, false, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.EightBit, true, false, true, false, false, true);
    }

    static EmailPolicy CreatePolicy(EmailParseMode parseMode, bool preserveSource, bool allowSmtpUtf8, bool strictCompatibility, bool brokenBoundaries, bool hashBodies, EmailWriteMode writeMode, EmailTransferEncodingConstraint transportConstraint, bool writeSmtpUtf8, bool bounded, bool strictValidation, bool requireBccStripped, bool safeDisplay, bool deterministic)
    {
        EmailParseOptions parse = CreateParseOptionsWithModeAndPreserveSourceAndAllowSmtpUtf8AndStrictCompatibilityAndBrokenBoundariesAndHashBodiesAndBounded(parseMode, preserveSource, allowSmtpUtf8, strictCompatibility, brokenBoundaries, hashBodies, bounded);
        EmailWriteOptions write = CreateWriteOptionsWithModeAndConstraintAndAllowSmtpUtf8AndDeterministic(writeMode, transportConstraint, writeSmtpUtf8, deterministic);
        EmailValidationOptions validation = CreateValidationOptionsWithAllowSmtpUtf8AndStrictAndRequireBccStripped(allowSmtpUtf8, strictValidation, requireBccStripped);
        EmailNormalizationOptions normalization = CreateNormalizationOptionsWithDeterministic(deterministic);
        EmailGenerationOptions generation = CreateGenerationOptionsWithDeterministic(deterministic);
        EmailRedactionOptions redaction = BuildRedactionOptions();
        EmailDisplayOptions display = CreateDisplayOptionsWithSafeAndBounded(safeDisplay, bounded);
        EmailCanonicalComparisonOptions comparison = CreateComparisonOptions(deterministic);
        return new EmailPolicy(parse, write, validation, normalization, generation, redaction, display, comparison);
    }

    static EmailParseOptions CreateParseOptionsWithModeAndPreserveSourceAndAllowSmtpUtf8AndStrictCompatibilityAndBrokenBoundariesAndHashBodiesAndBounded(EmailParseMode mode, bool preserveSource, bool allowSmtpUtf8, bool strictCompatibility, bool brokenBoundaries, bool hashBodies, bool bounded)
    {
        EmailParseOptions options = new EmailParseOptions();
        options.SetMode(mode);
        options.SetLimits(CreateLimits(bounded));
        options.SetStorageOptions(CreateStorageOptions(bounded));
        options.SetBodyIngestOptions(CreateBodyIngestOptions(hashBodies));
        options.SetCompatibilityOptions(CreateCompatibilityOptions(strictCompatibility, brokenBoundaries));
        options.SetPreserveSource(preserveSource);
        options.SetAllowSmtpUtf8(allowSmtpUtf8);
        return options;
    }

    static EmailLimits CreateLimits(bool bounded)
    {
        EmailLimits limits = new EmailLimits();
        limits.SetParsingLimits(CreateParsingLimits(bounded));
        limits.SetDecodingLimits(CreateDecodingLimits(bounded));
        limits.SetHtmlLimits(CreateHtmlLimits());
        limits.SetArchiveLimits(CreateArchiveLimits());
        limits.SetTemporaryStorageLimits(CreateTemporaryStorageLimits(bounded));
        limits.SetCryptographicLimits(CreateCryptographicLimits());
        return limits;
    }

    static EmailParsingLimits CreateParsingLimits(bool bounded)
    {
        EmailParsingLimits limits = new EmailParsingLimits();
        limits.SetMaxMessageBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxMessageBytes : EmailParsingLimitsConstants.DefaultMaxMessageBytes);
        limits.SetMaxHeaderBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxHeaderBytes : EmailParsingLimitsConstants.DefaultMaxHeaderBytes);
        limits.SetMaxHeaderFieldCount(bounded ? EmailParseOptionsConstants.UntrustedMaxHeaderFieldCount : EmailParsingLimitsConstants.DefaultMaxHeaderFieldCount);
        limits.SetMaxLineBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxLineBytes : EmailParsingLimitsConstants.DefaultMaxLineBytes);
        limits.SetMaxNestingDepth(bounded ? EmailParseOptionsConstants.UntrustedMaxNestingDepth : EmailParsingLimitsConstants.DefaultMaxNestingDepth);
        limits.SetMaxPartCount(bounded ? EmailParseOptionsConstants.UntrustedMaxPartCount : EmailParsingLimitsConstants.DefaultMaxPartCount);
        limits.SetMaxParametersPerHeader(bounded ? EmailParseOptionsConstants.UntrustedMaxParametersPerHeader : EmailParsingLimitsConstants.DefaultMaxParametersPerHeader);
        limits.SetMaxAddressesPerHeader(bounded ? EmailParseOptionsConstants.UntrustedMaxAddressesPerHeader : EmailParsingLimitsConstants.DefaultMaxAddressesPerHeader);
        limits.SetMaxEncodedWordsPerHeader(bounded ? EmailParseOptionsConstants.UntrustedMaxEncodedWordsPerHeader : EmailParsingLimitsConstants.DefaultMaxEncodedWordsPerHeader);
        return limits;
    }

    static EmailDecodingLimits CreateDecodingLimits(bool bounded)
    {
        EmailDecodingLimits limits = new EmailDecodingLimits();
        limits.SetMaxDecodedBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxDecodedBytes : EmailDecodingLimitsConstants.DefaultMaxDecodedBytes);
        limits.SetMaxDecompressedBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxDecodedBytes : EmailDecodingLimitsConstants.DefaultMaxDecompressedBytes);
        limits.SetMaxExpansionRatio(bounded ? EmailParseOptionsConstants.UntrustedMaxExpansionRatio : EmailDecodingLimitsConstants.DefaultMaxExpansionRatio);
        limits.SetMaxTextCharacters(EmailDecodingLimitsConstants.DefaultMaxTextCharacters);
        limits.SetMaxCharsetNameLength(EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength);
        return limits;
    }

    static EmailHtmlLimits CreateHtmlLimits()
    {
        EmailHtmlLimits limits = new EmailHtmlLimits();
        limits.SetMaxInputCharacters(EmailHtmlLimitsConstants.DefaultMaxInputCharacters);
        limits.SetMaxOutputCharacters(EmailHtmlLimitsConstants.DefaultMaxOutputCharacters);
        limits.SetMaxTokenCount(EmailHtmlLimitsConstants.DefaultMaxTokenCount);
        limits.SetMaxNestingDepth(EmailHtmlLimitsConstants.DefaultMaxNestingDepth);
        limits.SetMaxAttributesPerTag(EmailHtmlLimitsConstants.DefaultMaxAttributesPerTag);
        limits.SetMaxUrlCharacters(EmailHtmlLimitsConstants.DefaultMaxUrlCharacters);
        limits.SetMaxDataUriCount(EmailHtmlLimitsConstants.DefaultMaxDataUriCount);
        limits.SetMaxDataUriBytes(EmailHtmlLimitsConstants.DefaultMaxDataUriBytes);
        return limits;
    }

    static EmailArchiveLimits CreateArchiveLimits()
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();
        limits.SetMaxArchiveBytes(EmailArchiveLimitsConstants.DefaultMaxArchiveBytes);
        limits.SetMaxEntryBytes(EmailArchiveLimitsConstants.DefaultMaxEntryBytes);
        limits.SetMaxEntryCount(EmailArchiveLimitsConstants.DefaultMaxEntryCount);
        limits.SetMaxPartialFragmentCount(EmailArchiveLimitsConstants.DefaultMaxPartialFragmentCount);
        limits.SetMaxReassembledBytes(EmailArchiveLimitsConstants.DefaultMaxReassembledBytes);
        limits.SetMaxTnefAttributeCount(EmailArchiveLimitsConstants.DefaultMaxTnefAttributeCount);
        limits.SetMaxPathCharacters(EmailArchiveLimitsConstants.DefaultMaxPathCharacters);
        return limits;
    }

    static EmailTemporaryStorageLimits CreateTemporaryStorageLimits(bool bounded)
    {
        EmailTemporaryStorageLimits limits = new EmailTemporaryStorageLimits();
        limits.SetMemoryThresholdBytes(bounded ? EmailParseOptionsConstants.UntrustedMemoryThresholdBytes : EmailTemporaryStorageLimitsConstants.DefaultMemoryThresholdBytes);
        limits.SetMaxTemporaryBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxTemporaryBytes : EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryBytes);
        limits.SetMaxTemporaryFileCount(bounded ? EmailParseOptionsConstants.UntrustedMaxTemporaryFileCount : EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryFileCount);
        limits.SetMaxOpenStreamCount(bounded ? EmailParseOptionsConstants.UntrustedMaxOpenStreamCount : EmailTemporaryStorageLimitsConstants.DefaultMaxOpenStreamCount);
        limits.SetMaxMaterializedBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxMaterializedBytes : EmailTemporaryStorageLimitsConstants.DefaultMaxMaterializedBytes);
        return limits;
    }

    static EmailCryptographicLimits CreateCryptographicLimits()
    {
        EmailCryptographicLimits limits = new EmailCryptographicLimits();
        limits.SetMaxInputBytes(EmailCryptographicLimitsConstants.DefaultMaxInputBytes);
        limits.SetMaxSignatureCount(EmailCryptographicLimitsConstants.DefaultMaxSignatureCount);
        limits.SetMaxRecipientCount(EmailCryptographicLimitsConstants.DefaultMaxRecipientCount);
        limits.SetMaxCertificateBytes(EmailCryptographicLimitsConstants.DefaultMaxCertificateBytes);
        limits.SetMaxKeyBytes(EmailCryptographicLimitsConstants.DefaultMaxKeyBytes);
        limits.SetMaxAsn1Depth(EmailCryptographicLimitsConstants.DefaultMaxAsn1Depth);
        limits.SetMaxAsn1ElementCount(EmailCryptographicLimitsConstants.DefaultMaxAsn1ElementCount);
        limits.SetMaxOpenPgpPacketCount(EmailCryptographicLimitsConstants.DefaultMaxOpenPgpPacketCount);
        limits.SetMaxDecompressedBytes(EmailCryptographicLimitsConstants.DefaultMaxDecompressedBytes);
        limits.SetMaxOperationCount(EmailCryptographicLimitsConstants.DefaultMaxOperationCount);
        return limits;
    }

    static EmailStorageOptions CreateStorageOptions(bool bounded)
    {
        EmailStorageOptions options = new EmailStorageOptions();
        options.SetLimits(CreateTemporaryStorageLimits(bounded));
        options.SetTemporaryDirectory(Path.GetTempPath());
        options.SetClearsSensitiveBytes(bounded);
        options.SetCopyBufferBytes(EmailStorageOptionsConstants.DefaultCopyBufferBytes);
        return options;
    }

    static EmailBodyIngestOptions CreateBodyIngestOptions(bool hashBodies)
    {
        EmailBodyIngestOptions options = new EmailBodyIngestOptions();
        options.SetStorageMode(EmailBodyStorageMode.Lazy);
        options.SetDecodeOnIngest(false);
        options.SetComputeRawHash(hashBodies);
        options.SetComputeDecodedHash(hashBodies);
        options.SetHashAlgorithm(EmailHashAlgorithm.Sha256);
        return options;
    }

    static EmailCompatibilityOptions CreateCompatibilityOptions(bool strict, bool brokenBoundaries)
    {
        EmailCompatibilityOptions options = new EmailCompatibilityOptions();
        options.SetAllowBareLineEndings(!strict);
        options.SetAllowMissingHeaderBodySeparator(!strict);
        options.SetAllowMalformedHeaderNames(!strict);
        options.SetAllowOrphanHeaderContinuations(!strict);
        options.SetAllowBrokenMimeBoundaries(brokenBoundaries);
        options.SetAllowTruncatedInput(!strict);
        options.SetAllowDuplicateSingletonHeaders(!strict);
        options.SetReturnPartialResults(!strict);
        return options;
    }

    static EmailWriteOptions CreateWriteOptionsWithModeAndConstraintAndAllowSmtpUtf8AndDeterministic(EmailWriteMode mode, EmailTransferEncodingConstraint constraint, bool allowSmtpUtf8, bool deterministic)
    {
        EmailWriteOptions options = new EmailWriteOptions();
        options.SetMode(mode);
        options.SetTransportConstraint(constraint);
        options.SetHeaderWriteOptions(CreateHeaderWriteOptions(allowSmtpUtf8));
        options.SetDeterministicOutput(deterministic);
        options.SetDeterministicSeed(deterministic ? CreateDeterministicSeed() : new byte[0]);
        options.SetIncludeBlindRecipientHeaders(mode == EmailWriteMode.ExactPreservation);
        options.SetMaximumOutputBytes(EmailParsingLimitsConstants.DefaultMaxMessageBytes);
        options.SetMaximumNestingDepth(EmailParsingLimitsConstants.DefaultMaxNestingDepth);
        options.SetMaximumPartCount(EmailParsingLimitsConstants.DefaultMaxPartCount);
        return options;
    }

    static EmailHeaderWriteOptions CreateHeaderWriteOptions(bool allowSmtpUtf8)
    {
        EmailHeaderWriteOptions options = new EmailHeaderWriteOptions();
        options.SetPreferredLineLength(78);
        options.SetMaximumLineLength(998);
        options.SetMaximumOutputBytes((int)EmailParsingLimitsConstants.DefaultMaxHeaderBytes);
        options.SetMaximumEncodedWords(EmailEncodedWordConstants.DefaultMaximumEncodedWords);
        options.SetAllowInternationalized(allowSmtpUtf8);
        options.SetIncludeSeparator(true);
        options.SetCheckCardinality(true);
        options.SetCanonicalizeFieldNames(true);
        options.SetEncodedWordEncoding(EmailEncodedWordEncodingChoice.Automatic);
        return options;
    }

    static EmailValidationOptions CreateValidationOptionsWithAllowSmtpUtf8AndStrictAndRequireBccStripped(bool allowSmtpUtf8, bool strict, bool requireBccStripped)
    {
        EmailValidationOptions options = new EmailValidationOptions();
        options.SetValidateSyntax(true);
        options.SetValidateSemantics(true);
        options.SetRequireMimeVersion(strict);
        options.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        options.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        options.SetMaximumContentBytes(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes);
        options.SetMessageHeaderOptions(CreateHeaderValidationOptions(EmailHeaderValidationContext.Message, allowSmtpUtf8, strict, requireBccStripped));
        options.SetEntityHeaderOptions(CreateHeaderValidationOptions(EmailHeaderValidationContext.MimeEntity, allowSmtpUtf8, strict, false));
        return options;
    }

    static EmailHeaderValidationOptions CreateHeaderValidationOptions(EmailHeaderValidationContext context, bool allowSmtpUtf8, bool strict, bool requireBccStripped)
    {
        EmailHeaderValidationOptions options = new EmailHeaderValidationOptions();
        options.SetContext(context);
        options.SetAllowSmtpUtf8(allowSmtpUtf8);
        options.SetAllowObsoleteSyntax(!strict);
        options.SetRequireDate(context == EmailHeaderValidationContext.Message && strict);
        options.SetRequireFrom(context == EmailHeaderValidationContext.Message && strict);
        options.SetRequireMessageId(false);
        options.SetRequireBccStripped(context == EmailHeaderValidationContext.Message && requireBccStripped);
        options.SetValidateStructuredValues(true);
        options.SetMaximumLineLength(998);
        return options;
    }

    static EmailNormalizationOptions CreateNormalizationOptionsWithDeterministic(bool deterministic)
    {
        EmailNormalizationOptions options = new EmailNormalizationOptions();
        options.SetNormalizeLineEndings(deterministic);
        options.SetRefoldHeaders(deterministic);
        options.SetRepairMimeVersion(deterministic);
        options.SetRepairContentTypes(deterministic);
        options.SetRepairTransferEncodings(deterministic);
        options.SetRepairBoundaries(deterministic);
        options.SetNormalizeCharsets(deterministic);
        options.SetNormalizeFilenames(deterministic);
        options.SetPreferredHeaderLineLength(78);
        options.SetMaximumContentBytes(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes);
        options.SetDeterministicBoundarySeed(deterministic ? CreateDeterministicSeed() : new byte[0]);
        return options;
    }

    static EmailGenerationOptions CreateGenerationOptionsWithDeterministic(bool deterministic)
    {
        EmailGenerationOptions options = new EmailGenerationOptions();
        options.SetGenerateDate(deterministic);
        options.SetGenerateMessageId(deterministic);
        options.SetGenerateContentIds(deterministic);
        options.SetGenerateMimeVersion(deterministic);
        options.SetGenerateBoundaries(deterministic);
        options.SetDeterministicOutput(deterministic);
        options.SetIdentifierDomain(deterministic ? "test.invalid" : "local.invalid");
        options.SetDeterministicSeed(deterministic ? CreateDeterministicSeed() : new byte[0]);
        options.ClearGenerationDate();
        options.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        options.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        return options;
    }

    static EmailRedactionOptions BuildRedactionOptions()
    {
        EmailRedactionOptions options = new EmailRedactionOptions();
        options.SetRedactAddresses(false);
        options.SetRedactMessageIds(false);
        options.SetRemoveReceivedHeaders(false);
        options.SetRedactIpAddresses(false);
        options.SetRemoveAuthenticationHeaders(false);
        options.SetRedactSubjects(false);
        options.SetRedactBodies(false);
        options.SetRedactAttachmentNames(false);
        options.SetRedactAttachmentContent(false);
        options.SetPseudonymDomain("redacted.invalid");
        options.SetSubjectReplacement("[redacted]");
        options.SetBodyReplacement("[redacted body]\r\n");
        options.SetAttachmentNamePrefix("attachment");
        options.SetAttachmentContentReplacement(new byte[0]);
        options.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        options.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        return options;
    }

    static EmailDisplayOptions CreateDisplayOptionsWithSafeAndBounded(bool safe, bool bounded)
    {
        EmailDisplayOptions options = new EmailDisplayOptions();
        options.SetBodyPreference(EmailBodyPreference.Html);
        options.SetMaximumDepth(bounded ? EmailParseOptionsConstants.UntrustedMaxNestingDepth : EmailTraversalConstants.DefaultMaximumDepth);
        options.SetMaximumEntityCount(bounded ? EmailParseOptionsConstants.UntrustedMaxPartCount : EmailTraversalConstants.DefaultMaximumEntityCount);
        options.SetTextOptions(CreateTextDecodeOptions(safe, bounded));
        options.SetFlowedOptions(CreateFlowedTextOptions(bounded));
        options.SetHtmlTextOptions(CreateHtmlTextOptions(safe));
        options.SetHtmlSanitizeOptions(CreateHtmlSanitizeOptions());
        options.SetAnalysisOptions(CreateTextAnalysisOptions(bounded));
        options.SetRelatedIndexOptions(CreateRelatedIndexOptions(bounded));
        return options;
    }

    static EmailTextDecodeOptions CreateTextDecodeOptions(bool safe, bool bounded)
    {
        EmailTextDecodeOptions options = new EmailTextDecodeOptions();
        options.SetFallbackCharset("us-ascii");
        options.SetMaximumDecodedBytes(bounded ? EmailParseOptionsConstants.UntrustedMaxDecodedBytes : EmailDecodingLimitsConstants.DefaultMaxDecodedBytes);
        options.SetNewlinePolicy(EmailTextNewlinePolicy.Preserve);
        options.SetUnicodeNormalization(safe ? EmailTextUnicodeNormalization.Nfkc : EmailTextUnicodeNormalization.None);
        options.SetRemoveControlCharacters(safe);
        options.SetRemoveBidiControls(safe);
        options.SetCharsetOptions(CreateCharsetOptions(safe));
        return options;
    }

    static EmailCharsetDecodeOptions CreateCharsetOptions(bool safe)
    {
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();
        options.SetFallbackPolicy(EmailCharsetFallbackPolicy.Replacement);
        options.SetBomPolicy(safe ? EmailCharsetBomPolicy.RequireMatch : EmailCharsetBomPolicy.Detect);
        options.SetReplacement("\uFFFD");
        options.SetMaximumCharacters(EmailDecodingLimitsConstants.DefaultMaxTextCharacters);
        options.SetMaximumCharsetNameLength(EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength);
        return options;
    }

    static EmailFlowedTextOptions CreateFlowedTextOptions(bool bounded)
    {
        EmailFlowedTextOptions options = new EmailFlowedTextOptions();
        options.SetDeleteSpace(true);
        options.SetMaximumLineLength(78);
        options.SetMaximumCharacters(bounded ? EmailDecodingLimitsConstants.DefaultMaxTextCharacters : 16 * 1024 * 1024);
        return options;
    }

    static EmailHtmlTextOptions CreateHtmlTextOptions(bool safe)
    {
        EmailHtmlTextOptions options = new EmailHtmlTextOptions();
        options.SetLimits(CreateHtmlLimits());
        options.SetIncludeLinkTargets(!safe);
        options.SetIncludeImageAlternativeText(true);
        return options;
    }

    static EmailHtmlSanitizeOptions CreateHtmlSanitizeOptions()
    {
        EmailHtmlSanitizeOptions options = new EmailHtmlSanitizeOptions();
        options.SetLimits(CreateHtmlLimits());
        options.SetAllowRemoteResources(false);
        options.SetAllowCidResources(true);
        options.SetAllowSafeDataImages(false);
        options.SetPreserveComments(false);
        options.SetMaximumMutationCount(EmailHtmlSanitizeOptionsConstants.DefaultMaximumMutationCount);
        return options;
    }

    static EmailTextAnalysisOptions CreateTextAnalysisOptions(bool bounded)
    {
        EmailTextAnalysisOptions options = new EmailTextAnalysisOptions();
        options.SetMaximumCharacters(bounded ? EmailDecodingLimitsConstants.DefaultMaxTextCharacters : EmailTextAnalysisOptionsConstants.DefaultMaximumCharacters);
        options.SetMaximumLines(EmailTextAnalysisOptionsConstants.DefaultMaximumLines);
        options.SetMaximumSnippetCharacters(EmailTextAnalysisOptionsConstants.DefaultMaximumSnippetCharacters);
        options.SetIncludeQuotedTextInSnippet(false);
        options.SetIncludeSignatureInSnippet(false);
        return options;
    }

    static EmailRelatedIndexOptions CreateRelatedIndexOptions(bool bounded)
    {
        EmailRelatedIndexOptions options = new EmailRelatedIndexOptions();
        options.SetMaximumDepth(bounded ? EmailParseOptionsConstants.UntrustedMaxNestingDepth : EmailTraversalConstants.DefaultMaximumDepth);
        options.SetMaximumEntityCount(bounded ? EmailParseOptionsConstants.UntrustedMaxPartCount : EmailTraversalConstants.DefaultMaximumEntityCount);
        options.SetMaximumHtmlBytes(EmailHtmlLimitsConstants.DefaultMaxInputCharacters);
        options.SetMaximumReferences(EmailRelatedIndexOptionsConstants.DefaultMaximumReferences);
        options.SetMaximumUrlCharacters(EmailHtmlLimitsConstants.DefaultMaxUrlCharacters);
        return options;
    }

    static EmailCanonicalComparisonOptions CreateComparisonOptions(bool deterministic)
    {
        EmailCanonicalComparisonOptions options = new EmailCanonicalComparisonOptions();
        options.SetHashAlgorithm(EmailHashAlgorithm.Sha256);
        options.SetIgnoreReceivedHeaders(false);
        options.SetIgnoreAuthenticationHeaders(false);
        options.SetIgnoreDateHeaders(false);
        options.SetIgnoreMessageIdentifiers(false);
        options.SetIgnoreSubjectHeaders(false);
        options.SetIgnoreHeaderOrder(false);
        options.ClearIgnoredHeaderNames();
        options.SetDeterministicSeed(deterministic ? CreateDeterministicSeed() : Encoding.ASCII.GetBytes("email-semantic-fingerprint-v1"));
        options.SetMaximumOutputBytes(EmailParsingLimitsConstants.DefaultMaxMessageBytes);
        options.SetMaximumDepth(EmailTraversalConstants.DefaultMaximumDepth);
        options.SetMaximumEntityCount(EmailTraversalConstants.DefaultMaximumEntityCount);
        return options;
    }

    static byte[] CreateDeterministicSeed()
    {
        return Encoding.ASCII.GetBytes("email-policy-deterministic-test-v1");
    }
}
