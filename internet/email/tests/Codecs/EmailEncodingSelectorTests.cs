using System.Text;
using email;

namespace email.Tests;

public class EmailEncodingSelectorTests
{
    [Test]
    public void OptionsDefaultToSafeSevenBitBinaryMediaPolicy()
    {
        EmailEncodingSelectionOptions options = new EmailEncodingSelectionOptions();

        Assert.That(options.GetMediaType(), Is.EqualTo("application/octet-stream"));
        Assert.That(options.GetConstraint(), Is.EqualTo(EmailTransferEncodingConstraint.SevenBit));
        Assert.That(options.GetMaximumLineBytes(), Is.EqualTo(998));
        Assert.That(options.GetMinimumTextPercentage(), Is.EqualTo(80));
    }

    [TestCase(EmailTransferEncodingConstraint.SevenBit)]
    [TestCase(EmailTransferEncodingConstraint.EightBit)]
    [TestCase(EmailTransferEncodingConstraint.Binary)]
    public void CanonicalAsciiUsesSevenBitForEveryTransportConstraint(EmailTransferEncodingConstraint constraint)
    {
        byte[] input = Encoding.ASCII.GetBytes("hello\r\nworld");

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(input, "text/plain", constraint);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
        Assert.That(result.GetReason(), Is.EqualTo(EmailEncodingSelectionReason.SevenBitSafe));
        Assert.That(result.GetMaximumLineByteCount(), Is.EqualTo(5));
        Assert.That(result.HasCanonicalLineBreaks(), Is.True);
    }

    [Test]
    public void MostlyAsciiUtf8TextUsesQuotedPrintableOnSevenBitTransport()
    {
        byte[] input = Encoding.UTF8.GetBytes("A longer café description with mostly ASCII text.\r\n");

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "text/plain; charset=utf-8",
            EmailTransferEncodingConstraint.SevenBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.QuotedPrintable));
        Assert.That(result.GetReason(), Is.EqualTo(EmailEncodingSelectionReason.MostlyText));
        Assert.That(result.IsTextualMediaType(), Is.True);
        Assert.That(result.IsMostlyText(), Is.True);
        Assert.That(result.GetHighBitByteCount(), Is.EqualTo(2));
    }

    [Test]
    public void DenseNonAsciiTextUsesBase64WhenSevenBitIsRequired()
    {
        byte[] input = Encoding.UTF8.GetBytes("日本語の本文です");

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "text/plain",
            EmailTransferEncodingConstraint.SevenBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(result.GetReason(), Is.EqualTo(EmailEncodingSelectionReason.BinaryContent));
        Assert.That(result.IsTextualMediaType(), Is.True);
        Assert.That(result.IsMostlyText(), Is.False);
    }

    [Test]
    public void EightBitTransportKeepsDenseUnicodeTextUnencoded()
    {
        byte[] input = Encoding.UTF8.GetBytes("日本語の本文です\r\n");

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "text/plain",
            EmailTransferEncodingConstraint.EightBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.EightBit));
        Assert.That(result.GetReason(), Is.EqualTo(EmailEncodingSelectionReason.EightBitSafe));
    }

    [Test]
    public void EightBitTransportStillBase64EncodesBinaryMedia()
    {
        byte[] input = new byte[] { 0x80, 0x81, 0x82, 0x83 };

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "application/octet-stream",
            EmailTransferEncodingConstraint.EightBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(result.GetReason(), Is.EqualTo(EmailEncodingSelectionReason.BinaryContent));
        Assert.That(result.IsTextualMediaType(), Is.False);
    }

    [Test]
    public void BinaryTransportUsesBinaryForArbitraryBytes()
    {
        byte[] input = new byte[] { 0, 10, 13, 128, 255 };

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "application/octet-stream",
            EmailTransferEncodingConstraint.Binary);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.Binary));
        Assert.That(result.GetReason(), Is.EqualTo(EmailEncodingSelectionReason.BinaryTransportAvailable));
    }

    [Test]
    public void TextWithAnOccasionalNullUsesQuotedPrintableWhenIdentityIsUnsafe()
    {
        byte[] input = Encoding.ASCII.GetBytes("mostly ordinary text here");
        input[10] = 0;

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "text/plain",
            EmailTransferEncodingConstraint.EightBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.QuotedPrintable));
        Assert.That(result.GetNullByteCount(), Is.EqualTo(1));
        Assert.That(result.GetControlByteCount(), Is.EqualTo(1));
    }

    [Test]
    public void ConfiguredLineLimitForcesTextOrBinaryEncodingByMediaType()
    {
        byte[] input = Encoding.ASCII.GetBytes("123456");
        EmailEncodingSelectionOptions textOptions = Options("text/plain", EmailTransferEncodingConstraint.EightBit, 5, 80);
        EmailEncodingSelectionOptions binaryOptions = Options("application/octet-stream", EmailTransferEncodingConstraint.EightBit, 5, 80);

        EmailEncodingSelectionResult text = new EmailEncodingSelector().SelectWithInputAndOptions(input, textOptions);
        EmailEncodingSelectionResult binary = new EmailEncodingSelector().SelectWithInputAndOptions(input, binaryOptions);

        Assert.That(text.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.QuotedPrintable));
        Assert.That(binary.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(text.GetMaximumLineByteCount(), Is.EqualTo(6));
    }

    [TestCase("one\ntwo")]
    [TestCase("one\rtwo")]
    public void NonCanonicalTextLineBreaksPreventIdentitySelection(string value)
    {
        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            Encoding.ASCII.GetBytes(value),
            "text/plain",
            EmailTransferEncodingConstraint.EightBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.QuotedPrintable));
        Assert.That(result.HasCanonicalLineBreaks(), Is.False);
    }

    [Test]
    public void MinimumTextPercentageControlsTheQuotedPrintableBoundary()
    {
        byte[] input = new byte[] { 65, 65, 65, 65, 65, 65, 65, 65, 0x80, 0x81 };
        EmailEncodingSelectionOptions eighty = Options("text/plain", EmailTransferEncodingConstraint.SevenBit, 998, 80);
        EmailEncodingSelectionOptions eightyOne = Options("text/plain", EmailTransferEncodingConstraint.SevenBit, 998, 81);

        EmailEncodingSelectionResult atBoundary = new EmailEncodingSelector().SelectWithInputAndOptions(input, eighty);
        EmailEncodingSelectionResult aboveBoundary = new EmailEncodingSelector().SelectWithInputAndOptions(input, eightyOne);

        Assert.That(atBoundary.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.QuotedPrintable));
        Assert.That(atBoundary.IsMostlyText(), Is.True);
        Assert.That(aboveBoundary.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(aboveBoundary.IsMostlyText(), Is.False);
    }

    [TestCase("application/json")]
    [TestCase("APPLICATION/PROBLEM+JSON; charset=utf-8")]
    [TestCase("image/svg+xml")]
    [TestCase("message/rfc822")]
    public void StructuredTextMediaTypesAreRecognized(string mediaType)
    {
        byte[] input = Encoding.UTF8.GetBytes("A longer café value represented as structured text.");

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            mediaType,
            EmailTransferEncodingConstraint.SevenBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.QuotedPrintable));
        Assert.That(result.IsTextualMediaType(), Is.True);
    }

    [Test]
    public void EmptyContentUsesSevenBitEvenForBinaryMedia()
    {
        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            new byte[0],
            "application/octet-stream",
            EmailTransferEncodingConstraint.SevenBit);

        Assert.That(result.GetEncoding(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
        Assert.That(result.GetSourceByteCount(), Is.Zero);
        Assert.That(result.IsMostlyText(), Is.True);
    }

    [Test]
    public void ResultReportsDeterministicByteAndLineStatistics()
    {
        byte[] input = new byte[] { 65, 13, 10, 0, 31, 128 };

        EmailEncodingSelectionResult result = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(
            input,
            "application/octet-stream",
            EmailTransferEncodingConstraint.Binary);

        Assert.That(result.GetSourceByteCount(), Is.EqualTo(6));
        Assert.That(result.GetTextByteCount(), Is.EqualTo(3));
        Assert.That(result.GetHighBitByteCount(), Is.EqualTo(1));
        Assert.That(result.GetNullByteCount(), Is.EqualTo(1));
        Assert.That(result.GetControlByteCount(), Is.EqualTo(2));
        Assert.That(result.GetMaximumLineByteCount(), Is.EqualTo(3));
        Assert.That(result.HasCanonicalLineBreaks(), Is.True);
    }

    [Test]
    public void OptionsCloneIsIndependentAndSettersValidateConfiguration()
    {
        EmailEncodingSelectionOptions original = Options("text/plain", EmailTransferEncodingConstraint.EightBit, 120, 75);
        EmailEncodingSelectionOptions clone = original.Clone();
        clone.SetMediaType("application/octet-stream");
        clone.SetConstraint(EmailTransferEncodingConstraint.Binary);
        clone.SetMaximumLineBytes(10);
        clone.SetMinimumTextPercentage(20);

        Assert.That(original.GetMediaType(), Is.EqualTo("text/plain"));
        Assert.That(original.GetConstraint(), Is.EqualTo(EmailTransferEncodingConstraint.EightBit));
        Assert.That(original.GetMaximumLineBytes(), Is.EqualTo(120));
        Assert.That(original.GetMinimumTextPercentage(), Is.EqualTo(75));
        Assert.That(() => original.SetMediaType(" "), Throws.ArgumentException);
        Assert.That(() => original.SetConstraint((EmailTransferEncodingConstraint)10), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => original.SetMaximumLineBytes(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => original.SetMinimumTextPercentage(101), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => new EmailEncodingSelector().SelectWithInputAndOptions(new byte[0], (EmailEncodingSelectionOptions)null!), Throws.ArgumentNullException);
    }

    [Test]
    public void EncodedKindsAreKnownButRejectedByIdentityValidation()
    {
        Assert.That(new EmailTransferEncoding().IsIdentityKind(EmailTransferEncodingKind.SevenBit), Is.True);
        Assert.That(new EmailTransferEncoding().IsIdentityKind(EmailTransferEncodingKind.QuotedPrintable), Is.False);
        Assert.DoesNotThrow(() => new EmailTransferEncoding().ValidateKind(EmailTransferEncodingKind.Base64));
        Assert.That(
            () => new EmailTransferEncoding().ValidateWithInputAndKind(new byte[0], EmailTransferEncodingKind.QuotedPrintable),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => new EmailTransferEncoding().CopyWithInputAndOutputAndKindAndMaximumBytes(
                new MemoryStream(),
                new MemoryStream(),
                EmailTransferEncodingKind.Base64,
                0),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => new EmailTransferValidationResult(
                EmailTransferEncodingKind.QuotedPrintable,
                0,
                0,
                false,
                new EmailIssueList()),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    static EmailEncodingSelectionOptions Options(string mediaType, EmailTransferEncodingConstraint constraint, int maximumLineBytes, int minimumTextPercentage)
    {
        EmailEncodingSelectionOptions options = new EmailEncodingSelectionOptions();
        options.SetMediaType(mediaType);
        options.SetConstraint(constraint);
        options.SetMaximumLineBytes(maximumLineBytes);
        options.SetMinimumTextPercentage(minimumTextPercentage);
        return options;
    }
}
