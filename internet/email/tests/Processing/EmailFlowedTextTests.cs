using System.Text;
using email;

namespace email.Tests;

public class EmailFlowedTextTests
{
    [Test]
    public void DecodeJoinsFlowedLinesAccordingToDelSp()
    {
        string encoded = "one two \r\nthree\r\nnext \r\nline\r\n";

        Assert.That(new EmailFlowedText().DecodeWithValueAndDeleteSpace(encoded, false), Is.EqualTo("one two three\r\nnext line\r\n"));
        Assert.That(new EmailFlowedText().DecodeWithValueAndDeleteSpace(encoded, true), Is.EqualTo("one twothree\r\nnextline\r\n"));
    }

    [Test]
    public void DecodeUnstuffsAfterQuoteMarkersAndPreservesQuoteDepth()
    {
        string encoded =
            ">> quoted text  \r\n" +
            ">>continues\r\n" +
            "> > literal angle\r\n" +
            " From sender\r\n" +
            " >unquoted angle\r\n";

        EmailFlowedTextDecodeResult result = new EmailFlowedText().DecodeResultWithValueAndDeleteSpace(encoded, true);
        string decoded = result.GetText();

        Assert.That(decoded, Is.EqualTo(
            ">>quoted text continues\r\n" +
            ">> literal angle\r\n" +
            "From sender\r\n" +
            ">unquoted angle\r\n"));
        Assert.That(result.GetLineCount(), Is.EqualTo(4));
        Assert.That(result.GetLine(0).GetQuoteDepth(), Is.EqualTo(2));
        Assert.That(result.GetLine(0).GetContent(), Is.EqualTo("quoted text continues"));
        Assert.That(result.GetLine(1).GetQuoteDepth(), Is.EqualTo(1));
        Assert.That(result.GetLine(1).GetContent(), Is.EqualTo("> literal angle"));
        Assert.That(result.GetLine(3).GetQuoteDepth(), Is.Zero);
        Assert.That(result.GetLine(3).GetContent(), Is.EqualTo(">unquoted angle"));

        EmailFlowedTextDecodeResult roundTrip = new EmailFlowedText().DecodeResultWithValueAndDeleteSpace(new EmailFlowedText().EncodeWithValueUsingEmailFlowedTextDecodeResult(result), true);
        Assert.That(roundTrip.GetLines().Select(line => (line.GetQuoteDepth(), line.GetContent())),
            Is.EqualTo(result.GetLines().Select(line => (line.GetQuoteDepth(), line.GetContent()))));
    }

    [Test]
    public void QuoteDepthChangesAndSignatureSeparatorsEndParagraphs()
    {
        string encoded =
            ">first  \r\n" +
            ">second \r\n" +
            ">>deeper\r\n" +
            "body \r\n" +
            "-- \r\n" +
            "signature\r\n";

        string decoded = new EmailFlowedText().DecodeWithValueAndDeleteSpace(encoded, true);

        Assert.That(decoded, Is.EqualTo(
            ">first second\r\n" +
            ">>deeper\r\n" +
            "body\r\n" +
            "-- \r\n" +
            "signature\r\n"));
    }

    [Test]
    public void EncodeWrapsAtNaturalSpacesAndRoundTripsWithEitherDelSpMode()
    {
        string logical = "This paragraph has enough words to wrap cleanly.\r\n>Quoted words also wrap cleanly.\r\n";
        EmailFlowedTextOptions delSpYes = new EmailFlowedTextOptions();
        delSpYes.SetMaximumLineLength(24);
        EmailFlowedTextOptions delSpNo = new EmailFlowedTextOptions();
        delSpNo.SetDeleteSpace(false);
        delSpNo.SetMaximumLineLength(24);

        string yesEncoded = new EmailFlowedText().EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(logical, delSpYes);
        string noEncoded = new EmailFlowedText().EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(logical, delSpNo);

        Assert.That(yesEncoded, Does.Contain("  \r\n"));
        Assert.That(noEncoded, Does.Contain(" \r\n"));
        Assert.That(noEncoded, Does.Not.Contain("  \r\n"));
        Assert.That(new EmailFlowedText().DecodeWithValueAndDeleteSpace(yesEncoded, true), Is.EqualTo(logical));
        Assert.That(new EmailFlowedText().DecodeWithValueAndDeleteSpace(noEncoded, false), Is.EqualTo(logical));
        Assert.That(PhysicalLinesAreAtMost(yesEncoded, 24), Is.True);
        Assert.That(PhysicalLinesAreAtMost(noEncoded, 24), Is.True);
    }

    [Test]
    public void EncodeSpaceStuffsProtectedPrefixesAndKeepsSignatureFixed()
    {
        string logical = "From sender\r\n leading\r\n>quoted\r\n-- \r\nname\r\n";

        string encoded = new EmailFlowedText().EncodeWithValueUsingString(logical);

        Assert.That(encoded, Is.EqualTo(
            " From sender\r\n" +
            "  leading\r\n" +
            ">quoted\r\n" +
            "-- \r\n" +
            "name\r\n"));
        Assert.That(new EmailFlowedText().DecodeWithValueAndDeleteSpace(encoded, true), Is.EqualTo(logical));
    }

    [Test]
    public void MimeProcessorDetectsCaseInsensitiveParametersAfterTransferDecoding()
    {
        string physical = ">caf\u00e9  \r\n>continues\r\n";
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(physical));
        MimeEntity flowed = ParseEntity(
            "Content-Type: text/plain; charset=utf-8; FORMAT=FLOWED; DELSP=YES\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\n",
            Encoding.ASCII.GetBytes(base64));
        MimeEntity fixedText = ParseEntity(
            "Content-Type: text/plain; charset=utf-8\r\n\r\n",
            Encoding.UTF8.GetBytes(physical));
        MimeEntity defaultDelSp = ParseEntity(
            "Content-Type: text/plain; charset=utf-8; format=flowed\r\n\r\n",
            Encoding.UTF8.GetBytes("one \r\ntwo\r\n"));

        EmailTextDecodeResult flowedResult = new EmailTextProcessor().DecodeWithEntity(flowed);
        EmailTextDecodeResult fixedResult = new EmailTextProcessor().DecodeWithEntity(fixedText);
        EmailTextDecodeResult defaultDelSpResult = new EmailTextProcessor().DecodeWithEntity(defaultDelSp);
        EmailAttachmentView attachment = new EmailAttachmentView(flowed, new EmailPathOperations().CreateRoot(), EmailAttachmentKind.Body, null);

        Assert.That(flowedResult.GetText(), Is.EqualTo(">café continues\r\n"));
        Assert.That(flowedResult.GetSelectedCharset(), Is.EqualTo("utf-8"));
        Assert.That(new EmailTextProcessor().IsFlowed(flowed.GetMediaType()!), Is.True);
        Assert.That(new EmailTextProcessor().UsesDelSp(flowed.GetMediaType()!), Is.True);
        Assert.That(fixedResult.GetText(), Is.EqualTo(physical));
        Assert.That(new EmailTextProcessor().IsFlowed(fixedText.GetMediaType()!), Is.False);
        Assert.That(defaultDelSpResult.GetText(), Is.EqualTo("one two\r\n"));
        Assert.That(new EmailTextProcessor().UsesDelSp(defaultDelSp.GetMediaType()!), Is.False);
        Assert.That(attachment.ProcessText().GetText(), Is.EqualTo(">café continues\r\n"));
    }

    [Test]
    public void OverlongWordsRemainIntactAndLimitsAndOptionsAreValidated()
    {
        EmailFlowedTextOptions narrow = new EmailFlowedTextOptions();
        narrow.SetMaximumLineLength(10);
        string encoded = new EmailFlowedText().EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions("abcdefghijklmnop words", narrow);
        Assert.That(encoded, Does.StartWith("abcdefghijklmnop  \r\n"));
        Assert.That(new EmailFlowedText().DecodeWithValueAndDeleteSpace(encoded, true), Is.EqualTo("abcdefghijklmnop words\r\n"));

        EmailFlowedTextOptions limited = new EmailFlowedTextOptions();
        limited.SetMaximumCharacters(3);
        Assert.Throws<EmailLimitException>(() => new EmailFlowedText().DecodeWithValueAndOptions("four", limited));
        Assert.Throws<EmailLimitException>(() => new EmailFlowedText().EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions("four", limited));
        Assert.Throws<ArgumentNullException>(() => new EmailFlowedText().DecodeWithValueAndDeleteSpace(null!, true));
        Assert.Throws<ArgumentNullException>(() => new EmailFlowedText().EncodeWithValueUsingString((string)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailFlowedText().EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions("text", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => narrow.SetMaximumLineLength(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => narrow.SetMaximumLineLength(999));
        Assert.Throws<ArgumentOutOfRangeException>(() => narrow.SetMaximumCharacters(0));

        EmailFlowedTextOptions clone = narrow.Clone();
        narrow.SetDeleteSpace(false);
        narrow.SetMaximumLineLength(20);
        Assert.That(clone.GetDeleteSpace(), Is.True);
        Assert.That(clone.GetMaximumLineLength(), Is.EqualTo(10));
    }

    static bool PhysicalLinesAreAtMost(string value, int maximum)
    {
        string[] lines = value.Split("\r\n", StringSplitOptions.None);
        return lines.Take(lines.Length - 1).All(line => line.Length <= maximum);
    }

    static MimeEntity ParseEntity(string headers, byte[] body)
    {
        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        byte[] source = new byte[headerBytes.Length + body.Length];
        Array.Copy(headerBytes, source, headerBytes.Length);
        Array.Copy(body, 0, source, headerBytes.Length, body.Length);
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(source);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}
