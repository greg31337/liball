using email;

namespace email.Tests;

public class EmailLimitsCloneTests
{
    [Test]
    public void CloneDeepCopiesEveryLimitGroupAndSetting()
    {
        EmailLimits original = new EmailLimits();
        EmailLimits copy = original.Clone();

        MutateEverySetting(original);

        Assert.That(copy.GetParsingLimits().GetMaxMessageBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        Assert.That(copy.GetParsingLimits().GetMaxHeaderBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxHeaderBytes));
        Assert.That(copy.GetParsingLimits().GetMaxHeaderFieldCount(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxHeaderFieldCount));
        Assert.That(copy.GetParsingLimits().GetMaxLineBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxLineBytes));
        Assert.That(copy.GetParsingLimits().GetMaxNestingDepth(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(copy.GetParsingLimits().GetMaxPartCount(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxPartCount));
        Assert.That(copy.GetParsingLimits().GetMaxParametersPerHeader(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxParametersPerHeader));
        Assert.That(copy.GetParsingLimits().GetMaxAddressesPerHeader(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxAddressesPerHeader));
        Assert.That(copy.GetParsingLimits().GetMaxEncodedWordsPerHeader(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxEncodedWordsPerHeader));

        Assert.That(copy.GetDecodingLimits().GetMaxDecodedBytes(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes));
        Assert.That(copy.GetDecodingLimits().GetMaxDecompressedBytes(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxDecompressedBytes));
        Assert.That(copy.GetDecodingLimits().GetMaxExpansionRatio(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxExpansionRatio));
        Assert.That(copy.GetDecodingLimits().GetMaxTextCharacters(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxTextCharacters));
        Assert.That(copy.GetDecodingLimits().GetMaxCharsetNameLength(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength));

        Assert.That(copy.GetHtmlLimits().GetMaxInputCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxInputCharacters));
        Assert.That(copy.GetHtmlLimits().GetMaxOutputCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxOutputCharacters));
        Assert.That(copy.GetHtmlLimits().GetMaxTokenCount(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxTokenCount));
        Assert.That(copy.GetHtmlLimits().GetMaxNestingDepth(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(copy.GetHtmlLimits().GetMaxAttributesPerTag(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxAttributesPerTag));
        Assert.That(copy.GetHtmlLimits().GetMaxUrlCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxUrlCharacters));
        Assert.That(copy.GetHtmlLimits().GetMaxDataUriCount(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxDataUriCount));
        Assert.That(copy.GetHtmlLimits().GetMaxDataUriBytes(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxDataUriBytes));

        Assert.That(copy.GetArchiveLimits().GetMaxArchiveBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxArchiveBytes));
        Assert.That(copy.GetArchiveLimits().GetMaxEntryBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxEntryBytes));
        Assert.That(copy.GetArchiveLimits().GetMaxEntryCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxEntryCount));
        Assert.That(copy.GetArchiveLimits().GetMaxPartialFragmentCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPartialFragmentCount));
        Assert.That(copy.GetArchiveLimits().GetMaxReassembledBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxReassembledBytes));
        Assert.That(copy.GetArchiveLimits().GetMaxTnefAttributeCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxTnefAttributeCount));
        Assert.That(copy.GetArchiveLimits().GetMaxPathCharacters(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPathCharacters));

        Assert.That(copy.GetTemporaryStorageLimits().GetMemoryThresholdBytes(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMemoryThresholdBytes));
        Assert.That(copy.GetTemporaryStorageLimits().GetMaxTemporaryBytes(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryBytes));
        Assert.That(copy.GetTemporaryStorageLimits().GetMaxTemporaryFileCount(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryFileCount));
        Assert.That(copy.GetTemporaryStorageLimits().GetMaxOpenStreamCount(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxOpenStreamCount));
        Assert.That(copy.GetTemporaryStorageLimits().GetMaxMaterializedBytes(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxMaterializedBytes));

        Assert.That(copy.GetCryptographicLimits().GetMaxInputBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxInputBytes));
        Assert.That(copy.GetCryptographicLimits().GetMaxSignatureCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxSignatureCount));
        Assert.That(copy.GetCryptographicLimits().GetMaxRecipientCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxRecipientCount));
        Assert.That(copy.GetCryptographicLimits().GetMaxCertificateBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxCertificateBytes));
        Assert.That(copy.GetCryptographicLimits().GetMaxKeyBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxKeyBytes));
        Assert.That(copy.GetCryptographicLimits().GetMaxAsn1Depth(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxAsn1Depth));
        Assert.That(copy.GetCryptographicLimits().GetMaxAsn1ElementCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxAsn1ElementCount));
        Assert.That(copy.GetCryptographicLimits().GetMaxOpenPgpPacketCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxOpenPgpPacketCount));
        Assert.That(copy.GetCryptographicLimits().GetMaxDecompressedBytes(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxDecompressedBytes));
        Assert.That(copy.GetCryptographicLimits().GetMaxOperationCount(), Is.EqualTo(EmailCryptographicLimitsConstants.DefaultMaxOperationCount));
    }

    [Test]
    public void AggregateAcceptsReplacementGroupsAndRejectsNull()
    {
        EmailLimits limits = new EmailLimits();
        EmailParsingLimits parsing = new EmailParsingLimits();
        EmailDecodingLimits decoding = new EmailDecodingLimits();
        EmailHtmlLimits html = new EmailHtmlLimits();
        EmailArchiveLimits archive = new EmailArchiveLimits();
        EmailTemporaryStorageLimits storage = new EmailTemporaryStorageLimits();
        EmailCryptographicLimits cryptographic = new EmailCryptographicLimits();

        limits.SetParsingLimits(parsing);
        limits.SetDecodingLimits(decoding);
        limits.SetHtmlLimits(html);
        limits.SetArchiveLimits(archive);
        limits.SetTemporaryStorageLimits(storage);
        limits.SetCryptographicLimits(cryptographic);

        Assert.That(limits.GetParsingLimits(), Is.SameAs(parsing));
        Assert.That(limits.GetDecodingLimits(), Is.SameAs(decoding));
        Assert.That(limits.GetHtmlLimits(), Is.SameAs(html));
        Assert.That(limits.GetArchiveLimits(), Is.SameAs(archive));
        Assert.That(limits.GetTemporaryStorageLimits(), Is.SameAs(storage));
        Assert.That(limits.GetCryptographicLimits(), Is.SameAs(cryptographic));
        Assert.Throws<ArgumentNullException>(() => limits.SetParsingLimits(null!));
        Assert.Throws<ArgumentNullException>(() => limits.SetDecodingLimits(null!));
        Assert.Throws<ArgumentNullException>(() => limits.SetHtmlLimits(null!));
        Assert.Throws<ArgumentNullException>(() => limits.SetArchiveLimits(null!));
        Assert.Throws<ArgumentNullException>(() => limits.SetTemporaryStorageLimits(null!));
        Assert.Throws<ArgumentNullException>(() => limits.SetCryptographicLimits(null!));
    }

    static void MutateEverySetting(EmailLimits limits)
    {
        EmailParsingLimits parsing = limits.GetParsingLimits();
        parsing.SetMaxMessageBytes(1);
        parsing.SetMaxHeaderBytes(1);
        parsing.SetMaxHeaderFieldCount(1);
        parsing.SetMaxLineBytes(1);
        parsing.SetMaxNestingDepth(1);
        parsing.SetMaxPartCount(1);
        parsing.SetMaxParametersPerHeader(1);
        parsing.SetMaxAddressesPerHeader(1);
        parsing.SetMaxEncodedWordsPerHeader(1);

        EmailDecodingLimits decoding = limits.GetDecodingLimits();
        decoding.SetMaxDecodedBytes(1);
        decoding.SetMaxDecompressedBytes(1);
        decoding.SetMaxExpansionRatio(1);
        decoding.SetMaxTextCharacters(1);
        decoding.SetMaxCharsetNameLength(1);

        EmailHtmlLimits html = limits.GetHtmlLimits();
        html.SetMaxInputCharacters(1);
        html.SetMaxOutputCharacters(1);
        html.SetMaxTokenCount(1);
        html.SetMaxNestingDepth(1);
        html.SetMaxAttributesPerTag(1);
        html.SetMaxUrlCharacters(1);
        html.SetMaxDataUriCount(1);
        html.SetMaxDataUriBytes(1);

        EmailArchiveLimits archive = limits.GetArchiveLimits();
        archive.SetMaxArchiveBytes(1);
        archive.SetMaxEntryBytes(1);
        archive.SetMaxEntryCount(1);
        archive.SetMaxPartialFragmentCount(1);
        archive.SetMaxReassembledBytes(1);
        archive.SetMaxTnefAttributeCount(1);
        archive.SetMaxPathCharacters(1);

        EmailTemporaryStorageLimits storage = limits.GetTemporaryStorageLimits();
        storage.SetMemoryThresholdBytes(0);
        storage.SetMaxTemporaryBytes(1);
        storage.SetMaxTemporaryFileCount(1);
        storage.SetMaxOpenStreamCount(1);
        storage.SetMaxMaterializedBytes(1);

        EmailCryptographicLimits cryptographic = limits.GetCryptographicLimits();
        cryptographic.SetMaxInputBytes(1);
        cryptographic.SetMaxSignatureCount(1);
        cryptographic.SetMaxRecipientCount(1);
        cryptographic.SetMaxCertificateBytes(1);
        cryptographic.SetMaxKeyBytes(1);
        cryptographic.SetMaxAsn1Depth(1);
        cryptographic.SetMaxAsn1ElementCount(1);
        cryptographic.SetMaxOpenPgpPacketCount(1);
        cryptographic.SetMaxDecompressedBytes(1);
        cryptographic.SetMaxOperationCount(1);
    }
}
