using email;

namespace email.Tests;

public class EmailLimitValidationTests
{
    [Test]
    public void ParsingLimitsRejectNonPositiveValues()
    {
        EmailParsingLimits limits = new EmailParsingLimits();
        TestDelegate[] invalidSetters = new TestDelegate[]
        {
            () => limits.SetMaxMessageBytes(0),
            () => limits.SetMaxHeaderBytes(0),
            () => limits.SetMaxHeaderFieldCount(0),
            () => limits.SetMaxLineBytes(0),
            () => limits.SetMaxNestingDepth(0),
            () => limits.SetMaxPartCount(0),
            () => limits.SetMaxParametersPerHeader(0),
            () => limits.SetMaxAddressesPerHeader(0),
            () => limits.SetMaxEncodedWordsPerHeader(0)
        };

        AssertAllOutOfRange(invalidSetters);
    }

    [Test]
    public void DecodingLimitsRejectNonPositiveValues()
    {
        EmailDecodingLimits limits = new EmailDecodingLimits();
        TestDelegate[] invalidSetters = new TestDelegate[]
        {
            () => limits.SetMaxDecodedBytes(0),
            () => limits.SetMaxDecompressedBytes(0),
            () => limits.SetMaxExpansionRatio(0),
            () => limits.SetMaxTextCharacters(0),
            () => limits.SetMaxCharsetNameLength(0)
        };

        AssertAllOutOfRange(invalidSetters);
    }

    [Test]
    public void HtmlLimitsRejectNonPositiveValues()
    {
        EmailHtmlLimits limits = new EmailHtmlLimits();
        TestDelegate[] invalidSetters = new TestDelegate[]
        {
            () => limits.SetMaxInputCharacters(0),
            () => limits.SetMaxOutputCharacters(0),
            () => limits.SetMaxTokenCount(0),
            () => limits.SetMaxNestingDepth(0),
            () => limits.SetMaxAttributesPerTag(0),
            () => limits.SetMaxUrlCharacters(0),
            () => limits.SetMaxDataUriCount(0),
            () => limits.SetMaxDataUriBytes(0)
        };

        AssertAllOutOfRange(invalidSetters);
    }

    [Test]
    public void ArchiveLimitsRejectNonPositiveValues()
    {
        EmailArchiveLimits limits = new EmailArchiveLimits();
        TestDelegate[] invalidSetters = new TestDelegate[]
        {
            () => limits.SetMaxArchiveBytes(0),
            () => limits.SetMaxEntryBytes(0),
            () => limits.SetMaxEntryCount(0),
            () => limits.SetMaxPartialFragmentCount(0),
            () => limits.SetMaxReassembledBytes(0),
            () => limits.SetMaxTnefAttributeCount(0),
            () => limits.SetMaxPathCharacters(0)
        };

        AssertAllOutOfRange(invalidSetters);
    }

    [Test]
    public void TemporaryStorageAllowsZeroThresholdButRejectsOtherNonPositiveValues()
    {
        EmailTemporaryStorageLimits limits = new EmailTemporaryStorageLimits();
        limits.SetMemoryThresholdBytes(0);
        Assert.That(limits.GetMemoryThresholdBytes(), Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMemoryThresholdBytes(-1));

        TestDelegate[] invalidSetters = new TestDelegate[]
        {
            () => limits.SetMaxTemporaryBytes(0),
            () => limits.SetMaxTemporaryFileCount(0),
            () => limits.SetMaxOpenStreamCount(0),
            () => limits.SetMaxMaterializedBytes(0)
        };

        AssertAllOutOfRange(invalidSetters);
    }

    [Test]
    public void CryptographicLimitsRejectNonPositiveValues()
    {
        EmailCryptographicLimits limits = new EmailCryptographicLimits();
        TestDelegate[] invalidSetters = new TestDelegate[]
        {
            () => limits.SetMaxInputBytes(0),
            () => limits.SetMaxSignatureCount(0),
            () => limits.SetMaxRecipientCount(0),
            () => limits.SetMaxCertificateBytes(0),
            () => limits.SetMaxKeyBytes(0),
            () => limits.SetMaxAsn1Depth(0),
            () => limits.SetMaxAsn1ElementCount(0),
            () => limits.SetMaxOpenPgpPacketCount(0),
            () => limits.SetMaxDecompressedBytes(0),
            () => limits.SetMaxOperationCount(0)
        };

        AssertAllOutOfRange(invalidSetters);
    }

    [Test]
    public void PositiveCustomLimitsAreRetained()
    {
        EmailParsingLimits parsing = new EmailParsingLimits();
        parsing.SetMaxMessageBytes(1);
        parsing.SetMaxNestingDepth(2);
        EmailDecodingLimits decoding = new EmailDecodingLimits();
        decoding.SetMaxExpansionRatio(3);
        EmailHtmlLimits html = new EmailHtmlLimits();
        html.SetMaxTokenCount(4);
        EmailArchiveLimits archive = new EmailArchiveLimits();
        archive.SetMaxEntryCount(5);
        EmailTemporaryStorageLimits storage = new EmailTemporaryStorageLimits();
        storage.SetMaxTemporaryFileCount(6);
        EmailCryptographicLimits cryptographic = new EmailCryptographicLimits();
        cryptographic.SetMaxOperationCount(7);

        Assert.That(parsing.GetMaxMessageBytes(), Is.EqualTo(1));
        Assert.That(parsing.GetMaxNestingDepth(), Is.EqualTo(2));
        Assert.That(decoding.GetMaxExpansionRatio(), Is.EqualTo(3));
        Assert.That(html.GetMaxTokenCount(), Is.EqualTo(4));
        Assert.That(archive.GetMaxEntryCount(), Is.EqualTo(5));
        Assert.That(storage.GetMaxTemporaryFileCount(), Is.EqualTo(6));
        Assert.That(cryptographic.GetMaxOperationCount(), Is.EqualTo(7));
    }

    static void AssertAllOutOfRange(TestDelegate[] actions)
    {
        int index = 0;
        while (index < actions.Length)
        {
            Assert.Throws<ArgumentOutOfRangeException>(actions[index]);
            index++;
        }
    }
}
