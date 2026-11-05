using System.Text;
using email;

namespace email.Tests;

public class EmailCodecConformanceTests
{
    [TestCase("", "")]
    [TestCase("f", "Zg==")]
    [TestCase("fo", "Zm8=")]
    [TestCase("foo", "Zm9v")]
    [TestCase("foob", "Zm9vYg==")]
    [TestCase("fooba", "Zm9vYmE=")]
    [TestCase("foobar", "Zm9vYmFy")]
    public void Rfc4648Section10Base64VectorsEncodeAndDecode(string plain, string encoded)
    {
        byte[] plainBytes = Encoding.ASCII.GetBytes(plain);
        byte[] encodedBytes = Encoding.ASCII.GetBytes(encoded);

        Assert.That(new EmailBase64().EncodeWithInput(plainBytes), Is.EqualTo(encodedBytes));
        EmailBase64DecodeResult decoded = new EmailBase64().Decode(encodedBytes, EmailBase64DecodeMode.Strict, plainBytes.Length);
        Assert.That(decoded.GetDecodedBytes(), Is.EqualTo(plainBytes));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [TestCase("14FB9C03D97E", "FPucA9l+")]
    [TestCase("14FB9C03D9", "FPucA9k=")]
    [TestCase("14FB9C03", "FPucAw==")]
    public void Rfc4648Section9BinaryIllustrationsMatch(string hexadecimal, string encoded)
    {
        byte[] input = Convert.FromHexString(hexadecimal);

        Assert.That(Encoding.ASCII.GetString(new EmailBase64().EncodeWithInput(input)), Is.EqualTo(encoded));
        Assert.That(
            new EmailBase64().Decode(Encoding.ASCII.GetBytes(encoded), EmailBase64DecodeMode.Strict, input.Length).GetDecodedBytes(),
            Is.EqualTo(input));
    }

    [Test]
    public void Rfc2045Section67SoftBreakExampleDecodesToItsSingleRawLine()
    {
        string encoded = "Now's the time =\r\nfor all folk to come=\r\n to the aid of their country.";
        string expected = "Now's the time for all folk to come to the aid of their country.";

        EmailQuotedPrintableDecodeResult decoded = new EmailQuotedPrintable().Decode(
            Encoding.ASCII.GetBytes(encoded),
            EmailQuotedPrintableDecodeMode.Strict,
            expected.Length);

        Assert.That(Encoding.ASCII.GetString(decoded.GetDecodedBytes()), Is.EqualTo(expected));
        Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
    }

    [TestCase("=?US-ASCII?Q?Keith_Moore?=", "Keith Moore")]
    [TestCase("=?ISO-8859-1?Q?Keld_J=F8rn_Simonsen?=", "Keld Jørn Simonsen")]
    [TestCase("=?ISO-8859-1?Q?Andr=E9?=", "André")]
    [TestCase("=?ISO-8859-1?Q?Olle_J=E4rnefors?=", "Olle Järnefors")]
    [TestCase("=?ISO-8859-1?Q?Patrik_F=E4ltstr=F6m?=", "Patrik Fältström")]
    public void Rfc2047Section8QuotedPrintableNameExamplesDecode(string encoded, string expected)
    {
        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes(encoded),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.Phrase);

        Assert.That(Encoding.Latin1.GetString(result.GetCombinedBytes()), Is.EqualTo(expected));
        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void Rfc2047Section8SplitSubjectExampleSuppressesFoldingWhitespace()
    {
        string encoded = "=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?=\r\n =?ISO-8859-2?B?dSB1bmRlcnN0YW5kIHRoZSBleGFtcGxlLg==?=";

        EmailEncodedWordParseResult result = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes(encoded),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.UnstructuredText);

        Assert.That(Encoding.ASCII.GetString(result.GetCombinedBytes()), Is.EqualTo("If you can read this you understand the example."));
        Assert.That(result.GetEncodedWordCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void Rfc2231OfficialLanguageEncodedWordAndUrlContinuationExamplesDecode()
    {
        EmailEncodedWordParseResult word = new EmailEncodedWord().ParseWithInputAndModeAndContext(
            Encoding.ASCII.GetBytes("=?US-ASCII*EN?Q?Keith_Moore?="),
            EmailEncodedWordParseMode.Strict,
            EmailEncodedWordContext.Phrase);
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>
        {
            new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted("URL*0", Encoding.ASCII.GetBytes("ftp://"), true),
            new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted("URL*1", Encoding.ASCII.GetBytes("cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar"), true)
        };

        EmailRfc2231DecodeResult continuation = new EmailRfc2231().DecodeWithParametersAndMode(parameters, EmailRfc2231DecodeMode.Strict);

        Assert.That(Encoding.ASCII.GetString(word.GetCombinedBytes()), Is.EqualTo("Keith Moore"));
        Assert.That(word.GetPart(0).GetCharset(), Is.EqualTo("US-ASCII"));
        Assert.That(word.GetPart(0).GetLanguage(), Is.EqualTo("EN"));
        Assert.That(
            Encoding.ASCII.GetString(continuation.GetParameter(0).GetDecodedBytes()),
            Is.EqualTo("ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar"));
        Assert.That(continuation.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void Rfc2045IllegalRawQuotedPrintableBytesArePreservedAndDiagnosedByMode()
    {
        byte[] input = new byte[] { 65, 1, 127, 128, 10, 66, 13, 67 };

        EmailQuotedPrintableDecodeResult strict = new EmailQuotedPrintable().Decode(input, EmailQuotedPrintableDecodeMode.Strict, input.Length);
        EmailQuotedPrintableDecodeResult tolerant = new EmailQuotedPrintable().Decode(input, EmailQuotedPrintableDecodeMode.Tolerant, input.Length);

        Assert.That(strict.GetDecodedBytes(), Is.EqualTo(input));
        Assert.That(tolerant.GetDecodedBytes(), Is.EqualTo(input));
        Assert.That(strict.GetIssues().GetCount(), Is.EqualTo(5));
        Assert.That(tolerant.GetIssues().GetCount(), Is.EqualTo(5));
        long[] offsets = new long[] { 1, 2, 3, 4, 6 };
        int index = 0;
        while (index < offsets.Length)
        {
            Assert.That(strict.GetIssues().GetAt(index).GetCode(), Is.EqualTo(EmailIssueCode.QuotedPrintableInvalidCharacter));
            Assert.That(strict.GetIssues().GetAt(index).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
            Assert.That(strict.GetIssues().GetAt(index).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(offsets[index]));
            Assert.That(tolerant.GetIssues().GetAt(index).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
            index++;
        }
    }

    [Test]
    public void Base64DecoderIsInvariantAcrossEveryChunkPartitionForValidAndMalformedInput()
    {
        AssertBase64DecoderPartitions("Zm9vYmFy", EmailBase64DecodeMode.Strict);
        AssertBase64DecoderPartitions("T!Q===", EmailBase64DecodeMode.Tolerant);
    }

    [Test]
    public void QuotedPrintableDecoderIsInvariantAcrossEveryChunkPartitionForValidAndMalformedInput()
    {
        AssertQuotedPrintableDecoderPartitions("a=3D=\r\nb", EmailQuotedPrintableDecodeMode.Strict);
        AssertQuotedPrintableDecoderPartitions("x=QZ=af=", EmailQuotedPrintableDecodeMode.Tolerant);
    }

    [Test]
    public void EncodersAreInvariantAcrossEveryChunkPartitionIncludingEmptyFeeds()
    {
        byte[] base64Input = new byte[] { 0, 1, 2, 3, 254, 255 };
        byte[] quotedPrintableInput = Encoding.ASCII.GetBytes("From \r\n=");

        AssertBase64EncoderPartitions(base64Input);
        AssertQuotedPrintableEncoderPartitions(quotedPrintableInput);
    }

    [Test]
    public void BoundedDecodersAcceptExactExpansionLimitAndStopBeforeTheNextOutputUnit()
    {
        byte[] base64Input = Encoding.ASCII.GetBytes("TWFuTQ==");
        EmailBase64DecodeResult base64Exact = new EmailBase64().Decode(base64Input, EmailBase64DecodeMode.Strict, 4);
        EmailBase64DecodeResult base64Limited = new EmailBase64().Decode(base64Input, EmailBase64DecodeMode.Strict, 3);
        byte[] quotedPrintableInput = Encoding.ASCII.GetBytes("ab=63d");
        EmailQuotedPrintableDecodeResult quotedPrintableExact = new EmailQuotedPrintable().Decode(quotedPrintableInput, EmailQuotedPrintableDecodeMode.Strict, 4);
        EmailQuotedPrintableDecodeResult quotedPrintableLimited = new EmailQuotedPrintable().Decode(quotedPrintableInput, EmailQuotedPrintableDecodeMode.Strict, 3);
        byte[] uuencoded = new EmailUuencode().Encode(Encoding.ASCII.GetBytes("Cat"), 420, "cat.txt");
        EmailUuencodeDecodeResult uuencodeExact = new EmailUuencode().Decode(uuencoded, 3);
        EmailUuencodeDecodeResult uuencodeLimited = new EmailUuencode().Decode(uuencoded, 2);

        Assert.That(Encoding.ASCII.GetString(base64Exact.GetDecodedBytes()), Is.EqualTo("ManM"));
        Assert.That(base64Exact.HasFatalError(), Is.False);
        Assert.That(Encoding.ASCII.GetString(base64Limited.GetDecodedBytes()), Is.EqualTo("Man"));
        Assert.That(base64Limited.HasFatalError(), Is.True);
        Assert.That(Encoding.ASCII.GetString(quotedPrintableExact.GetDecodedBytes()), Is.EqualTo("abcd"));
        Assert.That(quotedPrintableExact.HasFatalError(), Is.False);
        Assert.That(Encoding.ASCII.GetString(quotedPrintableLimited.GetDecodedBytes()), Is.EqualTo("abc"));
        Assert.That(quotedPrintableLimited.HasFatalError(), Is.True);
        Assert.That(Encoding.ASCII.GetString(uuencodeExact.GetDecodedBytes()), Is.EqualTo("Cat"));
        Assert.That(uuencodeExact.HasFatalError(), Is.False);
        Assert.That(Encoding.ASCII.GetString(uuencodeLimited.GetDecodedBytes()), Is.EqualTo("Ca"));
        Assert.That(uuencodeLimited.HasFatalError(), Is.True);
    }

    [Test]
    public void EveryAutomaticSelectionKindRoundTripsThroughItsMatchingCodec()
    {
        byte[] sevenBit = Encoding.ASCII.GetBytes("plain\r\ntext");
        byte[] eightBit = Encoding.UTF8.GetBytes("日本語\r\n");
        byte[] binary = new byte[] { 0, 1, 2, 128, 255 };
        byte[] quotedPrintable = Encoding.UTF8.GetBytes("A longer café description in mostly ASCII.");
        byte[] base64 = Encoding.UTF8.GetBytes("日本語の本文です");

        AssertSelectionRoundTrip(sevenBit, "text/plain", EmailTransferEncodingConstraint.SevenBit, EmailTransferEncodingKind.SevenBit);
        AssertSelectionRoundTrip(eightBit, "text/plain", EmailTransferEncodingConstraint.EightBit, EmailTransferEncodingKind.EightBit);
        AssertSelectionRoundTrip(binary, "application/octet-stream", EmailTransferEncodingConstraint.Binary, EmailTransferEncodingKind.Binary);
        AssertSelectionRoundTrip(quotedPrintable, "text/plain", EmailTransferEncodingConstraint.SevenBit, EmailTransferEncodingKind.QuotedPrintable);
        AssertSelectionRoundTrip(base64, "text/plain", EmailTransferEncodingConstraint.SevenBit, EmailTransferEncodingKind.Base64);
    }

    static void AssertBase64DecoderPartitions(string value, EmailBase64DecodeMode mode)
    {
        byte[] input = Encoding.ASCII.GetBytes(value);
        EmailBase64DecodeResult baseline = new EmailBase64().Decode(input, mode, 100);
        int partitionCount = 1 << (input.Length - 1);
        int mask = 0;
        while (mask < partitionCount)
        {
            EmailBase64Decoder decoder = new EmailBase64Decoder(mode, 100);
            List<byte> output = new List<byte>();
            output.AddRange(decoder.Feed(new byte[0]));
            int start = 0;
            int boundary = 1;
            while (boundary < input.Length)
            {
                if ((mask & 1 << boundary - 1) != 0)
                {
                    output.AddRange(decoder.Feed(new ReadOnlySpan<byte>(input, start, boundary - start)));
                    output.AddRange(decoder.Feed(new byte[0]));
                    start = boundary;
                }

                boundary++;
            }

            output.AddRange(decoder.Feed(new ReadOnlySpan<byte>(input, start, input.Length - start)));
            output.AddRange(decoder.Feed(new byte[0]));
            output.AddRange(decoder.Complete());
            Assert.That(output.ToArray(), Is.EqualTo(baseline.GetDecodedBytes()), "partition " + mask);
            AssertIssuesEqual(decoder.GetIssues(), baseline.GetIssues(), "partition " + mask);
            mask++;
        }
    }

    static void AssertQuotedPrintableDecoderPartitions(string value, EmailQuotedPrintableDecodeMode mode)
    {
        byte[] input = Encoding.ASCII.GetBytes(value);
        EmailQuotedPrintableDecodeResult baseline = new EmailQuotedPrintable().Decode(input, mode, 100);
        int partitionCount = 1 << (input.Length - 1);
        int mask = 0;
        while (mask < partitionCount)
        {
            EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(mode, 100);
            List<byte> output = new List<byte>();
            output.AddRange(decoder.Feed(new byte[0]));
            int start = 0;
            int boundary = 1;
            while (boundary < input.Length)
            {
                if ((mask & 1 << boundary - 1) != 0)
                {
                    output.AddRange(decoder.Feed(new ReadOnlySpan<byte>(input, start, boundary - start)));
                    output.AddRange(decoder.Feed(new byte[0]));
                    start = boundary;
                }

                boundary++;
            }

            output.AddRange(decoder.Feed(new ReadOnlySpan<byte>(input, start, input.Length - start)));
            output.AddRange(decoder.Feed(new byte[0]));
            output.AddRange(decoder.Complete());
            Assert.That(output.ToArray(), Is.EqualTo(baseline.GetDecodedBytes()), "partition " + mask);
            AssertIssuesEqual(decoder.GetIssues(), baseline.GetIssues(), "partition " + mask);
            mask++;
        }
    }

    static void AssertBase64EncoderPartitions(byte[] input)
    {
        byte[] baseline = new EmailBase64().EncodeWithInputAndLineLength(input, 8);
        int partitionCount = 1 << (input.Length - 1);
        int mask = 0;
        while (mask < partitionCount)
        {
            EmailBase64Encoder encoder = new EmailBase64Encoder(8);
            List<byte> output = new List<byte>();
            output.AddRange(encoder.Feed(new byte[0]));
            FeedBase64EncoderPartitions(encoder, input, mask, output);
            output.AddRange(encoder.Feed(new byte[0]));
            output.AddRange(encoder.Complete());
            Assert.That(output.ToArray(), Is.EqualTo(baseline), "partition " + mask);
            mask++;
        }
    }

    static void FeedBase64EncoderPartitions(EmailBase64Encoder encoder, byte[] input, int mask, List<byte> output)
    {
        int start = 0;
        int boundary = 1;
        while (boundary < input.Length)
        {
            if ((mask & 1 << boundary - 1) != 0)
            {
                output.AddRange(encoder.Feed(new ReadOnlySpan<byte>(input, start, boundary - start)));
                output.AddRange(encoder.Feed(new byte[0]));
                start = boundary;
            }

            boundary++;
        }

        output.AddRange(encoder.Feed(new ReadOnlySpan<byte>(input, start, input.Length - start)));
    }

    static void AssertQuotedPrintableEncoderPartitions(byte[] input)
    {
        byte[] baseline = new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(input, 12, EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf, true);
        int partitionCount = 1 << (input.Length - 1);
        int mask = 0;
        while (mask < partitionCount)
        {
            EmailQuotedPrintableEncoder encoder = new EmailQuotedPrintableEncoder(12, EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf, true);
            List<byte> output = new List<byte>();
            output.AddRange(encoder.Feed(new byte[0]));
            int start = 0;
            int boundary = 1;
            while (boundary < input.Length)
            {
                if ((mask & 1 << boundary - 1) != 0)
                {
                    output.AddRange(encoder.Feed(new ReadOnlySpan<byte>(input, start, boundary - start)));
                    output.AddRange(encoder.Feed(new byte[0]));
                    start = boundary;
                }

                boundary++;
            }

            output.AddRange(encoder.Feed(new ReadOnlySpan<byte>(input, start, input.Length - start)));
            output.AddRange(encoder.Feed(new byte[0]));
            output.AddRange(encoder.Complete());
            Assert.That(output.ToArray(), Is.EqualTo(baseline), "partition " + mask);
            mask++;
        }
    }

    static void AssertIssuesEqual(EmailIssueList actual, EmailIssueList expected, string message)
    {
        Assert.That(actual.GetCount(), Is.EqualTo(expected.GetCount()), message);
        int index = 0;
        while (index < expected.GetCount())
        {
            Assert.That(actual.GetAt(index).GetCode(), Is.EqualTo(expected.GetAt(index).GetCode()), message + ", issue " + index);
            Assert.That(actual.GetAt(index).GetSeverity(), Is.EqualTo(expected.GetAt(index).GetSeverity()), message + ", issue " + index);
            Assert.That(
                actual.GetAt(index).GetSourcePosition()!.GetByteOffset(),
                Is.EqualTo(expected.GetAt(index).GetSourcePosition()!.GetByteOffset()),
                message + ", issue " + index);
            index++;
        }
    }

    static void AssertSelectionRoundTrip(byte[] input, string mediaType, EmailTransferEncodingConstraint constraint, EmailTransferEncodingKind expectedKind)
    {
        EmailEncodingSelectionResult selection = new EmailEncodingSelector().SelectWithInputAndMediaTypeAndConstraint(input, mediaType, constraint);
        byte[] decoded = input;
        if (selection.GetEncoding() == EmailTransferEncodingKind.Base64)
        {
            byte[] encoded = new EmailBase64().EncodeWithInput(input);
            decoded = new EmailBase64().Decode(encoded, EmailBase64DecodeMode.Strict, input.Length).GetDecodedBytes();
        }
        else if (selection.GetEncoding() == EmailTransferEncodingKind.QuotedPrintable)
        {
            byte[] encoded = new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(input, EmailQuotedPrintableNewlinePolicy.Encode, false);
            decoded = new EmailQuotedPrintable().Decode(encoded, EmailQuotedPrintableDecodeMode.Strict, input.Length).GetDecodedBytes();
        }
        else
        {
            EmailTransferValidationResult validation = new EmailTransferEncoding().ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(input, selection.GetEncoding(), input.Length, 998);
            Assert.That(validation.IsSuccess(), Is.True);
        }

        Assert.That(selection.GetEncoding(), Is.EqualTo(expectedKind));
        Assert.That(decoded, Is.EqualTo(input));
    }
}
