using email;

namespace email.Tests;

public class EmailParseOptionsTests
{
    [Test]
    public void DefaultOptionsAreTolerantPreservingAndBounded()
    {
        EmailParseOptions options = new EmailParseOptions();

        Assert.That(options.GetMode(), Is.EqualTo(EmailParseMode.ConformingTolerant));
        Assert.That(options.GetPreserveSource(), Is.True);
        Assert.That(options.GetAllowSmtpUtf8(), Is.False);
        Assert.That(options.GetLimits().GetParsingLimits().GetMaxMessageBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        AssertTolerant(options.GetCompatibilityOptions());
    }

    [Test]
    public void StrictAndForensicPresetsSelectOppositeRecoveryPolicies()
    {
        EmailParseOptions strict = new EmailParseOptionsOperations().CreateStrict();
        EmailParseOptions forensic = new EmailParseOptionsOperations().CreateForensic();

        Assert.That(strict.GetMode(), Is.EqualTo(EmailParseMode.Strict));
        Assert.That(strict.GetPreserveSource(), Is.False);
        Assert.That(strict.GetAllowSmtpUtf8(), Is.False);
        AssertAllRecovery(strict.GetCompatibilityOptions(), false);

        Assert.That(forensic.GetMode(), Is.EqualTo(EmailParseMode.Forensic));
        Assert.That(forensic.GetPreserveSource(), Is.True);
        Assert.That(forensic.GetAllowSmtpUtf8(), Is.True);
        AssertAllRecovery(forensic.GetCompatibilityOptions(), true);
    }

    [Test]
    public void TolerantPreservingAndSmtpUtf8PresetsComposeIndependentPolicies()
    {
        EmailParseOptions tolerant = new EmailParseOptionsOperations().CreateConformingTolerant();
        EmailParseOptions preserving = new EmailParseOptionsOperations().CreatePreserving();
        EmailParseOptions smtpUtf8 = new EmailParseOptionsOperations().CreateSmtpUtf8();

        Assert.That(tolerant.GetMode(), Is.EqualTo(EmailParseMode.ConformingTolerant));
        Assert.That(tolerant.GetPreserveSource(), Is.False);
        Assert.That(tolerant.GetAllowSmtpUtf8(), Is.False);
        AssertTolerant(tolerant.GetCompatibilityOptions());
        Assert.That(preserving.GetMode(), Is.EqualTo(EmailParseMode.ConformingTolerant));
        Assert.That(preserving.GetPreserveSource(), Is.True);
        Assert.That(preserving.GetAllowSmtpUtf8(), Is.False);
        Assert.That(smtpUtf8.GetMode(), Is.EqualTo(EmailParseMode.ConformingTolerant));
        Assert.That(smtpUtf8.GetPreserveSource(), Is.False);
        Assert.That(smtpUtf8.GetAllowSmtpUtf8(), Is.True);
    }

    [Test]
    public void BoundedUntrustedPresetTightensParsingDecodingAndStorageCeilings()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreateBoundedUntrusted();
        EmailLimits limits = options.GetLimits();
        EmailParsingLimits parsing = limits.GetParsingLimits();
        EmailDecodingLimits decoding = limits.GetDecodingLimits();
        EmailTemporaryStorageLimits temporary = limits.GetTemporaryStorageLimits();
        EmailTemporaryStorageLimits storage = options.GetStorageOptions().GetLimits();

        Assert.That(parsing.GetMaxMessageBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxMessageBytes));
        Assert.That(parsing.GetMaxHeaderBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxHeaderBytes));
        Assert.That(parsing.GetMaxHeaderFieldCount(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxHeaderFieldCount));
        Assert.That(parsing.GetMaxLineBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxLineBytes));
        Assert.That(parsing.GetMaxNestingDepth(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxNestingDepth));
        Assert.That(parsing.GetMaxPartCount(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxPartCount));
        Assert.That(parsing.GetMaxParametersPerHeader(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxParametersPerHeader));
        Assert.That(parsing.GetMaxAddressesPerHeader(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxAddressesPerHeader));
        Assert.That(parsing.GetMaxEncodedWordsPerHeader(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxEncodedWordsPerHeader));
        Assert.That(decoding.GetMaxDecodedBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxDecodedBytes));
        Assert.That(decoding.GetMaxDecompressedBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxDecodedBytes));
        Assert.That(decoding.GetMaxExpansionRatio(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxExpansionRatio));
        AssertTemporaryLimits(temporary);
        AssertTemporaryLimits(storage);
        Assert.That(options.GetPreserveSource(), Is.False);
        Assert.That(options.GetAllowSmtpUtf8(), Is.False);
    }

    [Test]
    public void OptionsAndClonesOwnDefensivePolicySnapshots()
    {
        EmailLimits suppliedLimits = new EmailLimits();
        suppliedLimits.GetParsingLimits().SetMaxPartCount(12);
        EmailStorageOptions suppliedStorage = new EmailStorageOptions();
        suppliedStorage.SetCopyBufferBytes(1234);
        EmailCompatibilityOptions suppliedCompatibility = new EmailCompatibilityOptions();
        suppliedCompatibility.SetAllowBareLineEndings(false);
        EmailParseOptions options = new EmailParseOptions();
        options.SetLimits(suppliedLimits);
        options.SetStorageOptions(suppliedStorage);
        options.SetCompatibilityOptions(suppliedCompatibility);
        EmailParseOptions clone = options.Clone();

        suppliedLimits.GetParsingLimits().SetMaxPartCount(1);
        suppliedStorage.SetCopyBufferBytes(1);
        suppliedCompatibility.SetAllowBareLineEndings(true);
        EmailLimits returnedLimits = options.GetLimits();
        returnedLimits.GetParsingLimits().SetMaxPartCount(2);
        EmailStorageOptions returnedStorage = options.GetStorageOptions();
        returnedStorage.SetCopyBufferBytes(2);
        EmailCompatibilityOptions returnedCompatibility = options.GetCompatibilityOptions();
        returnedCompatibility.SetAllowBareLineEndings(true);

        Assert.That(options.GetLimits().GetParsingLimits().GetMaxPartCount(), Is.EqualTo(12));
        Assert.That(options.GetStorageOptions().GetCopyBufferBytes(), Is.EqualTo(1234));
        Assert.That(options.GetCompatibilityOptions().GetAllowBareLineEndings(), Is.False);
        Assert.That(clone.GetLimits().GetParsingLimits().GetMaxPartCount(), Is.EqualTo(12));
        Assert.That(clone.GetStorageOptions().GetCopyBufferBytes(), Is.EqualTo(1234));
        Assert.That(clone.GetCompatibilityOptions().GetAllowBareLineEndings(), Is.False);
    }

    [Test]
    public void InvalidModesAndNullPoliciesAreRejected()
    {
        EmailParseOptions options = new EmailParseOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailParseMode)99));
        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetStorageOptions(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetCompatibilityOptions(null!));
    }

    static void AssertTolerant(EmailCompatibilityOptions compatibility)
    {
        Assert.That(compatibility.GetAllowBareLineEndings(), Is.True);
        Assert.That(compatibility.GetAllowMissingHeaderBodySeparator(), Is.True);
        Assert.That(compatibility.GetAllowMalformedHeaderNames(), Is.True);
        Assert.That(compatibility.GetAllowOrphanHeaderContinuations(), Is.True);
        Assert.That(compatibility.GetAllowBrokenMimeBoundaries(), Is.False);
        Assert.That(compatibility.GetAllowOutlookDeviations(), Is.True);
        Assert.That(compatibility.GetAllowExchangeDeviations(), Is.True);
        Assert.That(compatibility.GetAllowGmailDeviations(), Is.True);
        Assert.That(compatibility.GetAllowCommonMimeTypeAliases(), Is.True);
        Assert.That(compatibility.GetAllowBrokenEncodedWords(), Is.True);
        Assert.That(compatibility.GetAllowBrokenRfc2231Parameters(), Is.True);
        Assert.That(compatibility.GetAllowLegacyAttachmentRecovery(), Is.True);
        Assert.That(compatibility.GetAllowTruncatedInput(), Is.True);
        Assert.That(compatibility.GetAllowDuplicateSingletonHeaders(), Is.True);
        Assert.That(compatibility.GetReturnPartialResults(), Is.True);
    }

    static void AssertAllRecovery(EmailCompatibilityOptions compatibility, bool expected)
    {
        Assert.That(compatibility.GetAllowBareLineEndings(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowMissingHeaderBodySeparator(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowMalformedHeaderNames(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowOrphanHeaderContinuations(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowBrokenMimeBoundaries(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowOutlookDeviations(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowExchangeDeviations(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowGmailDeviations(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowCommonMimeTypeAliases(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowBrokenEncodedWords(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowBrokenRfc2231Parameters(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowLegacyAttachmentRecovery(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowTruncatedInput(), Is.EqualTo(expected));
        Assert.That(compatibility.GetAllowDuplicateSingletonHeaders(), Is.EqualTo(expected));
        Assert.That(compatibility.GetReturnPartialResults(), Is.EqualTo(expected));
    }

    static void AssertTemporaryLimits(EmailTemporaryStorageLimits limits)
    {
        Assert.That(limits.GetMemoryThresholdBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMemoryThresholdBytes));
        Assert.That(limits.GetMaxTemporaryBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxTemporaryBytes));
        Assert.That(limits.GetMaxTemporaryFileCount(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxTemporaryFileCount));
        Assert.That(limits.GetMaxOpenStreamCount(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxOpenStreamCount));
        Assert.That(limits.GetMaxMaterializedBytes(), Is.EqualTo(EmailParseOptionsConstants.UntrustedMaxMaterializedBytes));
    }
}
