using System.IO;
using System.Text;
using email;

namespace email.Tests;

public class EmailPolicyTests
{
    [Test]
    public void NamedPresetsExposeCompleteExplicitSettings()
    {
        AssertPolicy(new EmailPolicyOperations().CreatePreserve(), EmailParseMode.ConformingTolerant, true, false, false, false, false, EmailWriteMode.ExactPreservation, EmailTransferEncodingConstraint.SevenBit, false, false, false, false, false, false);
        AssertPolicy(new EmailPolicyOperations().CreateStrictInternet(), EmailParseMode.Strict, false, false, true, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.SevenBit, false, false, true, false, false, false);
        AssertPolicy(new EmailPolicyOperations().CreateTolerantInternet(), EmailParseMode.ConformingTolerant, true, false, false, false, false, EmailWriteMode.MinimalRegeneration, EmailTransferEncodingConstraint.SevenBit, false, false, false, false, false, false);
        AssertPolicy(new EmailPolicyOperations().CreateForensic(), EmailParseMode.Forensic, true, true, false, true, true, EmailWriteMode.ExactPreservation, EmailTransferEncodingConstraint.Binary, true, false, false, false, false, false);
        AssertPolicy(new EmailPolicyOperations().CreateSevenBitTransport(), EmailParseMode.ConformingTolerant, true, false, false, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.SevenBit, false, false, true, true, false, false);
        AssertPolicy(new EmailPolicyOperations().CreateSmtpUtf8(), EmailParseMode.ConformingTolerant, true, true, false, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.EightBit, true, false, true, true, false, false);
        AssertPolicy(new EmailPolicyOperations().CreateSafeDisplay(), EmailParseMode.ConformingTolerant, false, false, false, false, false, EmailWriteMode.MinimalRegeneration, EmailTransferEncodingConstraint.SevenBit, false, true, false, false, true, false);
        AssertPolicy(new EmailPolicyOperations().CreateDeterministicTest(), EmailParseMode.ConformingTolerant, true, true, false, false, false, EmailWriteMode.Canonical, EmailTransferEncodingConstraint.EightBit, true, false, true, false, false, true);
    }

    [Test]
    public void OptionFactoriesReturnIndependentSnapshots()
    {
        EmailPolicy policy = new EmailPolicyOperations().CreateDeterministicTest();
        EmailParseOptions firstParse = policy.CreateParseOptions();
        EmailWriteOptions firstWrite = policy.CreateWriteOptions();
        EmailGenerationOptions firstGeneration = policy.CreateGenerationOptions();
        EmailDisplayOptions firstDisplay = policy.CreateDisplayOptions();

        firstParse.SetMode(EmailParseMode.Strict);
        firstWrite.SetDeterministicSeed(new byte[] { 1 });
        firstGeneration.SetIdentifierDomain("changed.invalid");
        EmailTextDecodeOptions changedText = firstDisplay.GetTextOptions();
        changedText.SetRemoveBidiControls(true);
        firstDisplay.SetTextOptions(changedText);

        Assert.That(policy.CreateParseOptions(), Is.Not.SameAs(firstParse));
        Assert.That(policy.CreateParseOptions().GetMode(), Is.EqualTo(EmailParseMode.ConformingTolerant));
        Assert.That(policy.CreateWriteOptions().GetDeterministicSeed(), Is.EqualTo(Encoding.ASCII.GetBytes("email-policy-deterministic-test-v1")));
        Assert.That(policy.CreateGenerationOptions().GetIdentifierDomain(), Is.EqualTo("test.invalid"));
        Assert.That(policy.CreateDisplayOptions().GetTextOptions().GetRemoveBidiControls(), Is.False);

        EmailPolicy safe = new EmailPolicyOperations().CreateSafeDisplay();
        EmailDisplayOptions safeFirst = safe.CreateDisplayOptions();
        EmailTextDecodeOptions safeText = safeFirst.GetTextOptions();
        safeText.SetRemoveBidiControls(false);
        safeFirst.SetTextOptions(safeText);
        Assert.That(safe.CreateDisplayOptions().GetTextOptions().GetRemoveBidiControls(), Is.True);
    }

    [Test]
    public void DeterministicTestPresetProducesRepeatableGeneratedOutput()
    {
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>
        {
            new EmailEntityFactory().CreateTextWithValue("one"),
            new EmailEntityFactory().CreateTextWithValue("two")
        });
        EmailMessage original = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWith(), root, new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
        EmailPolicy firstPolicy = new EmailPolicyOperations().CreateDeterministicTest();
        EmailPolicy secondPolicy = new EmailPolicyOperations().CreateDeterministicTest();

        EmailMessage first = new EmailGenerator().GenerateWithMessageAndOptions(original, firstPolicy.CreateGenerationOptions()).GetMessage()!;
        EmailMessage second = new EmailGenerator().GenerateWithMessageAndOptions(original, secondPolicy.CreateGenerationOptions()).GetMessage()!;
        byte[] firstBytes = new EmailWriter().ToArrayWithMessageAndOptions(first!, firstPolicy.CreateWriteOptions());
        byte[] secondBytes = new EmailWriter().ToArrayWithMessageAndOptions(second!, secondPolicy.CreateWriteOptions());

        Assert.That(firstBytes, Is.EqualTo(secondBytes));
        Assert.That(Encoding.ASCII.GetString(firstBytes), Does.Contain("@test.invalid>"));
    }

    static void AssertPolicy(EmailPolicy policy, EmailParseMode parseMode, bool preserveSource, bool allowSmtpUtf8, bool strictCompatibility, bool brokenBoundaries, bool hashBodies, EmailWriteMode writeMode, EmailTransferEncodingConstraint transport, bool writeSmtpUtf8, bool bounded, bool strictValidation, bool requireBccStripped, bool safeDisplay, bool deterministic)
    {
        AssertParse(policy.CreateParseOptions(), parseMode, preserveSource, allowSmtpUtf8, strictCompatibility, brokenBoundaries, hashBodies, bounded);
        AssertWrite(policy.CreateWriteOptions(), writeMode, transport, writeSmtpUtf8, deterministic);
        AssertValidation(policy.CreateValidationOptions(), allowSmtpUtf8, strictValidation, requireBccStripped);
        AssertNormalization(policy.CreateNormalizationOptions(), deterministic);
        AssertGeneration(policy.CreateGenerationOptions(), deterministic);
        AssertRedaction(policy.CreateRedactionOptions());
        AssertDisplay(policy.CreateDisplayOptions(), safeDisplay, bounded);
        AssertComparison(policy.CreateCanonicalComparisonOptions(), deterministic);
    }

    static void AssertParse(EmailParseOptions options, EmailParseMode mode, bool preserveSource, bool allowSmtpUtf8, bool strict, bool brokenBoundaries, bool hashBodies, bool bounded)
    {
        Assert.That(options.GetMode(), Is.EqualTo(mode));
        Assert.That(options.GetPreserveSource(), Is.EqualTo(preserveSource));
        Assert.That(options.GetAllowSmtpUtf8(), Is.EqualTo(allowSmtpUtf8));

        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        Assert.That(compatibility.GetAllowBareLineEndings(), Is.EqualTo(!strict));
        Assert.That(compatibility.GetAllowMissingHeaderBodySeparator(), Is.EqualTo(!strict));
        Assert.That(compatibility.GetAllowMalformedHeaderNames(), Is.EqualTo(!strict));
        Assert.That(compatibility.GetAllowOrphanHeaderContinuations(), Is.EqualTo(!strict));
        Assert.That(compatibility.GetAllowBrokenMimeBoundaries(), Is.EqualTo(brokenBoundaries));
        Assert.That(compatibility.GetAllowTruncatedInput(), Is.EqualTo(!strict));
        Assert.That(compatibility.GetAllowDuplicateSingletonHeaders(), Is.EqualTo(!strict));
        Assert.That(compatibility.GetReturnPartialResults(), Is.EqualTo(!strict));

        EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
        Assert.That(ingest.GetStorageMode(), Is.EqualTo(EmailBodyStorageMode.Lazy));
        Assert.That(ingest.GetDecodeOnIngest(), Is.False);
        Assert.That(ingest.GetComputeRawHash(), Is.EqualTo(hashBodies));
        Assert.That(ingest.GetComputeDecodedHash(), Is.EqualTo(hashBodies));
        Assert.That(ingest.GetHashAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha256));

        AssertLimits(options.GetLimits(), bounded);
        AssertStorage(options.GetStorageOptions(), bounded);
    }

    static void AssertLimits(EmailLimits limits, bool bounded)
    {
        EmailParsingLimits parsing = limits.GetParsingLimits();
        Assert.That(parsing.GetMaxMessageBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxMessageBytes : EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        Assert.That(parsing.GetMaxHeaderBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxHeaderBytes : EmailParsingLimitsConstants.DefaultMaxHeaderBytes));
        Assert.That(parsing.GetMaxHeaderFieldCount(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxHeaderFieldCount : EmailParsingLimitsConstants.DefaultMaxHeaderFieldCount));
        Assert.That(parsing.GetMaxLineBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxLineBytes : EmailParsingLimitsConstants.DefaultMaxLineBytes));
        Assert.That(parsing.GetMaxNestingDepth(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxNestingDepth : EmailParsingLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(parsing.GetMaxPartCount(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxPartCount : EmailParsingLimitsConstants.DefaultMaxPartCount));
        Assert.That(parsing.GetMaxParametersPerHeader(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxParametersPerHeader : EmailParsingLimitsConstants.DefaultMaxParametersPerHeader));
        Assert.That(parsing.GetMaxAddressesPerHeader(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxAddressesPerHeader : EmailParsingLimitsConstants.DefaultMaxAddressesPerHeader));
        Assert.That(parsing.GetMaxEncodedWordsPerHeader(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxEncodedWordsPerHeader : EmailParsingLimitsConstants.DefaultMaxEncodedWordsPerHeader));

        EmailDecodingLimits decoding = limits.GetDecodingLimits();
        Assert.That(decoding.GetMaxDecodedBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxDecodedBytes : EmailDecodingLimitsConstants.DefaultMaxDecodedBytes));
        Assert.That(decoding.GetMaxDecompressedBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxDecodedBytes : EmailDecodingLimitsConstants.DefaultMaxDecompressedBytes));
        Assert.That(decoding.GetMaxExpansionRatio(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxExpansionRatio : EmailDecodingLimitsConstants.DefaultMaxExpansionRatio));
        Assert.That(decoding.GetMaxTextCharacters(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxTextCharacters));
        Assert.That(decoding.GetMaxCharsetNameLength(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength));

        AssertHtmlLimits(limits.GetHtmlLimits());
        AssertArchiveLimits(limits.GetArchiveLimits());
        AssertTemporaryLimits(limits.GetTemporaryStorageLimits(), bounded);
        AssertCryptographicLimits(limits.GetCryptographicLimits());
    }

    static void AssertHtmlLimits(EmailHtmlLimits limits)
    {
        Assert.That(limits.GetMaxInputCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxInputCharacters));
        Assert.That(limits.GetMaxOutputCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxOutputCharacters));
        Assert.That(limits.GetMaxTokenCount(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxTokenCount));
        Assert.That(limits.GetMaxNestingDepth(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(limits.GetMaxAttributesPerTag(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxAttributesPerTag));
        Assert.That(limits.GetMaxUrlCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxUrlCharacters));
        Assert.That(limits.GetMaxDataUriCount(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxDataUriCount));
        Assert.That(limits.GetMaxDataUriBytes(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxDataUriBytes));
    }

    static void AssertArchiveLimits(EmailArchiveLimits limits)
    {
        Assert.That(limits.GetMaxArchiveBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxArchiveBytes));
        Assert.That(limits.GetMaxEntryBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxEntryBytes));
        Assert.That(limits.GetMaxEntryCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxEntryCount));
        Assert.That(limits.GetMaxPartialFragmentCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPartialFragmentCount));
        Assert.That(limits.GetMaxReassembledBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxReassembledBytes));
        Assert.That(limits.GetMaxTnefAttributeCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxTnefAttributeCount));
        Assert.That(limits.GetMaxPathCharacters(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPathCharacters));
    }

    static void AssertTemporaryLimits(EmailTemporaryStorageLimits limits, bool bounded)
    {
        Assert.That(limits.GetMemoryThresholdBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMemoryThresholdBytes : EmailTemporaryStorageLimitsConstants.DefaultMemoryThresholdBytes));
        Assert.That(limits.GetMaxTemporaryBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxTemporaryBytes : EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryBytes));
        Assert.That(limits.GetMaxTemporaryFileCount(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxTemporaryFileCount : EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryFileCount));
        Assert.That(limits.GetMaxOpenStreamCount(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxOpenStreamCount : EmailTemporaryStorageLimitsConstants.DefaultMaxOpenStreamCount));
        Assert.That(limits.GetMaxMaterializedBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxMaterializedBytes : EmailTemporaryStorageLimitsConstants.DefaultMaxMaterializedBytes));
    }

    static void AssertCryptographicLimits(EmailCryptographicLimits limits)
    {
        Assert.That(limits.GetMaxInputBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxInputBytes));
        Assert.That(limits.GetMaxSignatureCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxSignatureCount));
        Assert.That(limits.GetMaxRecipientCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxRecipientCount));
        Assert.That(limits.GetMaxCertificateBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxCertificateBytes));
        Assert.That(limits.GetMaxKeyBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxKeyBytes));
        Assert.That(limits.GetMaxAsn1Depth(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxAsn1Depth));
        Assert.That(limits.GetMaxAsn1ElementCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxAsn1ElementCount));
        Assert.That(limits.GetMaxOpenPgpPacketCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxOpenPgpPacketCount));
        Assert.That(limits.GetMaxDecompressedBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxDecompressedBytes));
        Assert.That(limits.GetMaxOperationCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxOperationCount));
    }

    static void AssertStorage(EmailStorageOptions storage, bool bounded)
    {
        AssertTemporaryLimits(storage.GetLimits(), bounded);
        Assert.That(storage.GetTemporaryDirectory(), Is.EqualTo(Path.GetFullPath(Path.GetTempPath())));
        Assert.That(storage.GetClearsSensitiveBytes(), Is.EqualTo(bounded));
        Assert.That(storage.GetCopyBufferBytes(), Is.EqualTo(EmailStorageOptionsConstants.DefaultCopyBufferBytes));
    }

    static void AssertWrite(EmailWriteOptions options, EmailWriteMode mode, EmailTransferEncodingConstraint transport, bool allowSmtpUtf8, bool deterministic)
    {
        Assert.That(options.GetMode(), Is.EqualTo(mode));
        Assert.That(options.GetTransportConstraint(), Is.EqualTo(transport));
        Assert.That(options.GetAllowSmtpUtf8(), Is.EqualTo(allowSmtpUtf8));
        Assert.That(options.GetDeterministicOutput(), Is.EqualTo(deterministic));
        Assert.That(options.GetDeterministicSeed(), Is.EqualTo(deterministic ? Encoding.ASCII.GetBytes("email-policy-deterministic-test-v1") : new byte[0]));
        Assert.That(options.GetIncludeBlindRecipientHeaders(), Is.EqualTo(mode == EmailWriteMode.ExactPreservation));
        Assert.That(options.GetMaximumOutputBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        Assert.That(options.GetMaximumNestingDepth(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(options.GetMaximumPartCount(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxPartCount));

        EmailHeaderWriteOptions header = options.GetHeaderWriteOptions();
        Assert.That(header.GetPreferredLineLength(), Is.EqualTo(78));
        Assert.That(header.GetMaximumLineLength(), Is.EqualTo(998));
        Assert.That(header.GetMaximumOutputBytes(), Is.EqualTo((int)EmailParsingLimitsConstants.DefaultMaxHeaderBytes));
        Assert.That(header.GetMaximumEncodedWords(), Is.EqualTo(EmailEncodedWordConstants.DefaultMaximumEncodedWords));
        Assert.That(header.GetAllowInternationalized(), Is.EqualTo(allowSmtpUtf8));
        Assert.That(header.GetIncludeSeparator(), Is.True);
        Assert.That(header.GetCheckCardinality(), Is.True);
        Assert.That(header.GetCanonicalizeFieldNames(), Is.True);
        Assert.That(header.GetEncodedWordEncoding(), Is.EqualTo(EmailEncodedWordEncodingChoice.Automatic));
    }

    static void AssertValidation(EmailValidationOptions options, bool allowSmtpUtf8, bool strict, bool requireBccStripped)
    {
        Assert.That(options.GetValidateSyntax(), Is.True);
        Assert.That(options.GetValidateSemantics(), Is.True);
        Assert.That(options.GetRequireMimeVersion(), Is.EqualTo(strict));
        Assert.That(options.GetMaximumDepth(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumDepth));
        Assert.That(options.GetMaximumEntityCount(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumEntityCount));
        Assert.That(options.GetMaximumContentBytes(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes));
        AssertHeaderValidation(options.GetMessageHeaderOptions(), EmailHeaderValidationContext.Message, allowSmtpUtf8, strict, requireBccStripped);
        AssertHeaderValidation(options.GetEntityHeaderOptions(), EmailHeaderValidationContext.MimeEntity, allowSmtpUtf8, strict, false);
    }

    static void AssertHeaderValidation(EmailHeaderValidationOptions options, EmailHeaderValidationContext context, bool allowSmtpUtf8, bool strict, bool requireBccStripped)
    {
        Assert.That(options.GetContext(), Is.EqualTo(context));
        Assert.That(options.GetAllowSmtpUtf8(), Is.EqualTo(allowSmtpUtf8));
        Assert.That(options.GetAllowObsoleteSyntax(), Is.EqualTo(!strict));
        Assert.That(options.GetRequireDate(), Is.EqualTo(context == EmailHeaderValidationContext.Message && strict));
        Assert.That(options.GetRequireFrom(), Is.EqualTo(context == EmailHeaderValidationContext.Message && strict));
        Assert.That(options.GetRequireMessageId(), Is.False);
        Assert.That(options.GetRequireBccStripped(), Is.EqualTo(context == EmailHeaderValidationContext.Message && requireBccStripped));
        Assert.That(options.GetValidateStructuredValues(), Is.True);
        Assert.That(options.GetMaximumLineLength(), Is.EqualTo(998));
    }

    static void AssertNormalization(EmailNormalizationOptions options, bool deterministic)
    {
        Assert.That(options.GetNormalizeLineEndings(), Is.EqualTo(deterministic));
        Assert.That(options.GetRefoldHeaders(), Is.EqualTo(deterministic));
        Assert.That(options.GetRepairMimeVersion(), Is.EqualTo(deterministic));
        Assert.That(options.GetRepairContentTypes(), Is.EqualTo(deterministic));
        Assert.That(options.GetRepairTransferEncodings(), Is.EqualTo(deterministic));
        Assert.That(options.GetRepairBoundaries(), Is.EqualTo(deterministic));
        Assert.That(options.GetNormalizeCharsets(), Is.EqualTo(deterministic));
        Assert.That(options.GetNormalizeFilenames(), Is.EqualTo(deterministic));
        Assert.That(options.GetPreferredHeaderLineLength(), Is.EqualTo(78));
        Assert.That(options.GetMaximumContentBytes(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes));
        Assert.That(options.GetDeterministicBoundarySeed(), Is.EqualTo(deterministic ? Encoding.ASCII.GetBytes("email-policy-deterministic-test-v1") : new byte[0]));
    }

    static void AssertGeneration(EmailGenerationOptions options, bool deterministic)
    {
        Assert.That(options.GetGenerateDate(), Is.EqualTo(deterministic));
        Assert.That(options.GetGenerateMessageId(), Is.EqualTo(deterministic));
        Assert.That(options.GetGenerateContentIds(), Is.EqualTo(deterministic));
        Assert.That(options.GetGenerateMimeVersion(), Is.EqualTo(deterministic));
        Assert.That(options.GetGenerateBoundaries(), Is.EqualTo(deterministic));
        Assert.That(options.GetDeterministicOutput(), Is.EqualTo(deterministic));
        Assert.That(options.GetIdentifierDomain(), Is.EqualTo(deterministic ? "test.invalid" : "local.invalid"));
        Assert.That(options.GetDeterministicSeed(), Is.EqualTo(deterministic ? Encoding.ASCII.GetBytes("email-policy-deterministic-test-v1") : new byte[0]));
        Assert.That(options.HasGenerationDate(), Is.False);
        Assert.That(options.GetGenerationDate(), Is.Null);
        Assert.That(options.GetMaximumDepth(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumDepth));
        Assert.That(options.GetMaximumEntityCount(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumEntityCount));
    }

    static void AssertRedaction(EmailRedactionOptions options)
    {
        Assert.That(options.GetRedactAddresses(), Is.False);
        Assert.That(options.GetRedactMessageIds(), Is.False);
        Assert.That(options.GetRemoveReceivedHeaders(), Is.False);
        Assert.That(options.GetRedactIpAddresses(), Is.False);
        Assert.That(options.GetRemoveAuthenticationHeaders(), Is.False);
        Assert.That(options.GetRedactSubjects(), Is.False);
        Assert.That(options.GetRedactBodies(), Is.False);
        Assert.That(options.GetRedactAttachmentNames(), Is.False);
        Assert.That(options.GetRedactAttachmentContent(), Is.False);
        Assert.That(options.GetPseudonymDomain(), Is.EqualTo("redacted.invalid"));
        Assert.That(options.GetSubjectReplacement(), Is.EqualTo("[redacted]"));
        Assert.That(options.GetBodyReplacement(), Is.EqualTo("[redacted body]\r\n"));
        Assert.That(options.GetAttachmentNamePrefix(), Is.EqualTo("attachment"));
        Assert.That(options.GetAttachmentContentReplacement(), Is.Empty);
        Assert.That(options.GetMaximumDepth(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumDepth));
        Assert.That(options.GetMaximumEntityCount(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumEntityCount));
    }

    static void AssertDisplay(EmailDisplayOptions options, bool safe, bool bounded)
    {
        Assert.That(options.GetBodyPreference(), Is.EqualTo(EmailBodyPreference.Html));
        Assert.That(options.GetMaximumDepth(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxNestingDepth : EmailTraversalConstants.DefaultMaximumDepth));
        Assert.That(options.GetMaximumEntityCount(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxPartCount : EmailTraversalConstants.DefaultMaximumEntityCount));

        EmailTextDecodeOptions text = options.GetTextOptions();
        Assert.That(text.HasFallbackCharset(), Is.True);
        Assert.That(text.GetFallbackCharset(), Is.EqualTo("us-ascii"));
        Assert.That(text.GetMaximumDecodedBytes(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxDecodedBytes : EmailDecodingLimitsConstants.DefaultMaxDecodedBytes));
        Assert.That(text.GetNewlinePolicy(), Is.EqualTo(EmailTextNewlinePolicy.Preserve));
        Assert.That(text.GetUnicodeNormalization(), Is.EqualTo(safe ? EmailTextUnicodeNormalization.Nfkc : EmailTextUnicodeNormalization.None));
        Assert.That(text.GetRemoveControlCharacters(), Is.EqualTo(safe));
        Assert.That(text.GetRemoveBidiControls(), Is.EqualTo(safe));
        AssertCharset(text.GetCharsetOptions(), safe);

        EmailFlowedTextOptions flowed = options.GetFlowedOptions();
        Assert.That(flowed.GetDeleteSpace(), Is.True);
        Assert.That(flowed.GetMaximumLineLength(), Is.EqualTo(78));
        Assert.That(flowed.GetMaximumCharacters(), Is.EqualTo(bounded ? EmailDecodingLimitsConstants.DefaultMaxTextCharacters : 16 * 1024 * 1024));

        EmailHtmlTextOptions htmlText = options.GetHtmlTextOptions();
        AssertHtmlLimits(htmlText.GetLimits());
        Assert.That(htmlText.GetIncludeLinkTargets(), Is.EqualTo(!safe));
        Assert.That(htmlText.GetIncludeImageAlternativeText(), Is.True);

        EmailHtmlSanitizeOptions sanitize = options.GetHtmlSanitizeOptions();
        AssertHtmlLimits(sanitize.GetLimits());
        Assert.That(sanitize.GetAllowRemoteResources(), Is.False);
        Assert.That(sanitize.GetAllowCidResources(), Is.True);
        Assert.That(sanitize.GetAllowSafeDataImages(), Is.False);
        Assert.That(sanitize.GetPreserveComments(), Is.False);
        Assert.That(sanitize.GetMaximumMutationCount(), Is.EqualTo(EmailHtmlSanitizeOptionsConstants.DefaultMaximumMutationCount));

        EmailTextAnalysisOptions analysis = options.GetAnalysisOptions();
        Assert.That(analysis.GetMaximumCharacters(), Is.EqualTo(bounded ? EmailDecodingLimitsConstants.DefaultMaxTextCharacters : EmailTextAnalysisOptionsConstants.DefaultMaximumCharacters));
        Assert.That(analysis.GetMaximumLines(), Is.EqualTo(EmailTextAnalysisOptionsConstants.DefaultMaximumLines));
        Assert.That(analysis.GetMaximumSnippetCharacters(), Is.EqualTo(EmailTextAnalysisOptionsConstants.DefaultMaximumSnippetCharacters));
        Assert.That(analysis.GetIncludeQuotedTextInSnippet(), Is.False);
        Assert.That(analysis.GetIncludeSignatureInSnippet(), Is.False);

        EmailRelatedIndexOptions related = options.GetRelatedIndexOptions();
        Assert.That(related.GetMaximumDepth(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxNestingDepth : EmailTraversalConstants.DefaultMaximumDepth));
        Assert.That(related.GetMaximumEntityCount(), Is.EqualTo(bounded ? EmailParseOptionsConstants.UntrustedMaxPartCount : EmailTraversalConstants.DefaultMaximumEntityCount));
        Assert.That(related.GetMaximumHtmlBytes(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxInputCharacters));
        Assert.That(related.GetMaximumReferences(), Is.EqualTo(EmailRelatedIndexOptionsConstants.DefaultMaximumReferences));
        Assert.That(related.GetMaximumUrlCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxUrlCharacters));
    }

    static void AssertCharset(EmailCharsetDecodeOptions options, bool safe)
    {
        Assert.That(options.GetFallbackPolicy(), Is.EqualTo(EmailCharsetFallbackPolicy.Replacement));
        Assert.That(options.GetBomPolicy(), Is.EqualTo(safe ? EmailCharsetBomPolicy.RequireMatch : EmailCharsetBomPolicy.Detect));
        Assert.That(options.GetReplacement(), Is.EqualTo("\uFFFD"));
        Assert.That(options.GetMaximumCharacters(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxTextCharacters));
        Assert.That(options.GetMaximumCharsetNameLength(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength));
    }

    static void AssertComparison(EmailCanonicalComparisonOptions options, bool deterministic)
    {
        Assert.That(options.GetHashAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha256));
        Assert.That(options.GetIgnoreReceivedHeaders(), Is.False);
        Assert.That(options.GetIgnoreAuthenticationHeaders(), Is.False);
        Assert.That(options.GetIgnoreDateHeaders(), Is.False);
        Assert.That(options.GetIgnoreMessageIdentifiers(), Is.False);
        Assert.That(options.GetIgnoreSubjectHeaders(), Is.False);
        Assert.That(options.GetIgnoreHeaderOrder(), Is.False);
        Assert.That(options.GetIgnoredHeaderNameCount(), Is.Zero);
        Assert.That(options.GetIgnoredHeaderNames(), Is.Empty);
        Assert.That(options.GetDeterministicSeed(), Is.EqualTo(Encoding.ASCII.GetBytes(deterministic ? "email-policy-deterministic-test-v1" : "email-semantic-fingerprint-v1")));
        Assert.That(options.GetMaximumOutputBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        Assert.That(options.GetMaximumDepth(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumDepth));
        Assert.That(options.GetMaximumEntityCount(), Is.EqualTo(EmailTraversalConstants.DefaultMaximumEntityCount));
    }
}
