using email;

namespace email.Tests;

public class EmailLimitDefaultsTests
{
    [Test]
    public void ParsingDefaultsAreExplicitAndBounded()
    {
        EmailParsingLimits limits = new EmailParsingLimits();

        Assert.That(limits.GetMaxMessageBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxMessageBytes));
        Assert.That(limits.GetMaxHeaderBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxHeaderBytes));
        Assert.That(limits.GetMaxHeaderFieldCount(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxHeaderFieldCount));
        Assert.That(limits.GetMaxLineBytes(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxLineBytes));
        Assert.That(limits.GetMaxNestingDepth(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(limits.GetMaxPartCount(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxPartCount));
        Assert.That(limits.GetMaxParametersPerHeader(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxParametersPerHeader));
        Assert.That(limits.GetMaxAddressesPerHeader(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxAddressesPerHeader));
        Assert.That(limits.GetMaxEncodedWordsPerHeader(), Is.EqualTo(EmailParsingLimitsConstants.DefaultMaxEncodedWordsPerHeader));
    }

    [Test]
    public void DecodingDefaultsAreExplicitAndBounded()
    {
        EmailDecodingLimits limits = new EmailDecodingLimits();

        Assert.That(limits.GetMaxDecodedBytes(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxDecodedBytes));
        Assert.That(limits.GetMaxDecompressedBytes(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxDecompressedBytes));
        Assert.That(limits.GetMaxExpansionRatio(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxExpansionRatio));
        Assert.That(limits.GetMaxTextCharacters(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxTextCharacters));
        Assert.That(limits.GetMaxCharsetNameLength(), Is.EqualTo(EmailDecodingLimitsConstants.DefaultMaxCharsetNameLength));
    }

    [Test]
    public void HtmlDefaultsAreExplicitAndBounded()
    {
        EmailHtmlLimits limits = new EmailHtmlLimits();

        Assert.That(limits.GetMaxInputCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxInputCharacters));
        Assert.That(limits.GetMaxOutputCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxOutputCharacters));
        Assert.That(limits.GetMaxTokenCount(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxTokenCount));
        Assert.That(limits.GetMaxNestingDepth(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxNestingDepth));
        Assert.That(limits.GetMaxAttributesPerTag(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxAttributesPerTag));
        Assert.That(limits.GetMaxUrlCharacters(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxUrlCharacters));
        Assert.That(limits.GetMaxDataUriCount(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxDataUriCount));
        Assert.That(limits.GetMaxDataUriBytes(), Is.EqualTo(EmailHtmlLimitsConstants.DefaultMaxDataUriBytes));
    }

    [Test]
    public void ArchiveDefaultsAreExplicitAndBounded()
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();

        Assert.That(limits.GetMaxArchiveBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxArchiveBytes));
        Assert.That(limits.GetMaxEntryBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxEntryBytes));
        Assert.That(limits.GetMaxEntryCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxEntryCount));
        Assert.That(limits.GetMaxPartialFragmentCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPartialFragmentCount));
        Assert.That(limits.GetMaxReassembledBytes(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxReassembledBytes));
        Assert.That(limits.GetMaxTnefAttributeCount(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxTnefAttributeCount));
        Assert.That(limits.GetMaxPathCharacters(), Is.EqualTo(EmailArchiveLimitsConstants.DefaultMaxPathCharacters));
    }

    [Test]
    public void TemporaryStorageDefaultsAreExplicitAndBounded()
    {
        EmailTemporaryStorageLimits limits = new EmailTemporaryStorageLimits();

        Assert.That(limits.GetMemoryThresholdBytes(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMemoryThresholdBytes));
        Assert.That(limits.GetMaxTemporaryBytes(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryBytes));
        Assert.That(limits.GetMaxTemporaryFileCount(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxTemporaryFileCount));
        Assert.That(limits.GetMaxOpenStreamCount(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxOpenStreamCount));
        Assert.That(limits.GetMaxMaterializedBytes(), Is.EqualTo(EmailTemporaryStorageLimitsConstants.DefaultMaxMaterializedBytes));
    }

    [Test]
    public void CryptographicDefaultsAreExplicitAndBounded()
    {
        EmailCryptographicLimits limits = new EmailCryptographicLimits();

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

    [Test]
    public void AggregateCreatesEveryLimitGroup()
    {
        EmailLimits limits = new EmailLimits();

        Assert.That(limits.GetParsingLimits(), Is.Not.Null);
        Assert.That(limits.GetDecodingLimits(), Is.Not.Null);
        Assert.That(limits.GetHtmlLimits(), Is.Not.Null);
        Assert.That(limits.GetArchiveLimits(), Is.Not.Null);
        Assert.That(limits.GetTemporaryStorageLimits(), Is.Not.Null);
        Assert.That(limits.GetCryptographicLimits(), Is.Not.Null);
    }
}
