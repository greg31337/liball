using System.Text;

namespace http.Tests;

public class HpackHuffmanCodecTests
{
    [Test]
    public void EncoderMatchesRfcRequestExamples()
    {
        AssertEncodedText("www.example.com", "f1e3c2e5f23a6ba0ab90f4ff");
        AssertEncodedText("no-cache", "a8eb10649cbf");
        AssertEncodedText("custom-key", "25a849e95ba97d7f");
        AssertEncodedText("custom-value", "25a849e95bb8e8b4bf");
    }

    [Test]
    public void EncoderHandlesEveryOctetSymbolAndRoundTrips()
    {
        byte[] input = new byte[256];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)index;
            index = index + 1;
        }

        HpackHuffmanResult encoded = HpackHuffmanCodec.Encode(input);
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            encoded.Bytes, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.UsedHuffman, Is.True);
            Assert.That(encoded.RawLength, Is.EqualTo(input.Length));
            Assert.That(encoded.HuffmanByteLength, Is.EqualTo(encoded.Bytes.Length));
            Assert.That(encoded.RequiredSize, Is.EqualTo(encoded.Bytes.Length));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Bytes, Is.EqualTo(input));
        });
    }

    [Test]
    public void EncoderUsesCanonicalLeadingEosPadding()
    {
        HpackHuffmanResult asciiA = HpackHuffmanCodec.Encode("a"u8);
        HpackHuffmanResult zero = HpackHuffmanCodec.Encode(new byte[] { 0x00 });
        HpackHuffmanResult empty = HpackHuffmanCodec.Encode(
            ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(asciiA.Bytes, Is.EqualTo(new byte[] { 0x1f }));
            Assert.That(asciiA.HuffmanBitLength, Is.EqualTo(5));
            Assert.That(zero.Bytes, Is.EqualTo(new byte[] { 0xff, 0xc7 }));
            Assert.That(zero.HuffmanBitLength, Is.EqualTo(13));
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Bytes, Is.Empty);
            Assert.That(empty.UsedHuffman, Is.True);
        });
    }

    [Test]
    public void EncodedLengthCalculationIsExact()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HpackHuffmanCodec.CalculateEncodedLength(
                ReadOnlySpan<byte>.Empty), Is.Zero);
            Assert.That(HpackHuffmanCodec.CalculateEncodedLength("a"u8),
                Is.EqualTo(1));
            Assert.That(HpackHuffmanCodec.CalculateEncodedLength(
                new byte[] { 0x00 }), Is.EqualTo(2));
            Assert.That(HpackHuffmanCodec.CalculateEncodedLength(
                "www.example.com"u8), Is.EqualTo(12));
            Assert.That(HpackHuffmanCodec.CalculateEncodedLength(
                "custom-value"u8), Is.EqualTo(9));
        });
    }

    [Test]
    public void BoundedEncoderIsExactAndAllOrNothing()
    {
        byte[] exact = new byte[14];
        byte[] insufficient = new byte[12];
        Array.Fill(exact, (byte)0xcc);
        Array.Fill(insufficient, (byte)0xcc);

        HpackHuffmanResult written = HpackHuffmanCodec.Write(
            "www.example.com"u8, exact, 1);
        HpackHuffmanResult needOutput = HpackHuffmanCodec.Write(
            "www.example.com"u8, insufficient, 1);
        HpackHuffmanResult invalidOffset = HpackHuffmanCodec.Write(
            "a"u8, insufficient, -1);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(12));
            Assert.That(written.RequiredSize, Is.EqualTo(13));
            Assert.That(exact, Is.EqualTo(Convert.FromHexString(
                "ccf1e3c2e5f23a6ba0ab90f4ffcc")));
            Assert.That(needOutput.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(13));
            Assert.That(insufficient,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 12).ToArray()));
            Assert.That(invalidOffset.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalidOffset.ErrorCode,
                Is.EqualTo("invalid-hpack-huffman-offset"));
        });
    }

    [Test]
    public void StringSelectionFollowsTheConfiguredDeterministicPolicy()
    {
        HpackOptions disabledOptions = new HpackOptions();
        disabledOptions.UseHuffman = false;
        HpackOptions smallerOptions = new HpackOptions();
        HpackOptions forcedOptions = new HpackOptions();
        forcedOptions.UseHuffmanOnlyWhenSmaller = false;

        HpackHuffmanResult disabled = HpackHuffmanCodec.SelectString(
            "www.example.com"u8, disabledOptions);
        HpackHuffmanResult smaller = HpackHuffmanCodec.SelectString(
            "www.example.com"u8, smallerOptions);
        HpackHuffmanResult tied = HpackHuffmanCodec.SelectString(
            "a"u8, smallerOptions);
        HpackHuffmanResult forced = HpackHuffmanCodec.SelectString(
            new byte[] { 0x00 }, forcedOptions);
        HpackHuffmanResult repeated = HpackHuffmanCodec.SelectString(
            "www.example.com"u8, smallerOptions);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.UsedHuffman, Is.False);
            Assert.That(disabled.Bytes, Is.EqualTo("www.example.com"u8.ToArray()));
            Assert.That(smaller.UsedHuffman, Is.True);
            Assert.That(smaller.Bytes,
                Is.EqualTo(Convert.FromHexString("f1e3c2e5f23a6ba0ab90f4ff")));
            Assert.That(tied.UsedHuffman, Is.False);
            Assert.That(tied.Bytes, Is.EqualTo("a"u8.ToArray()));
            Assert.That(forced.UsedHuffman, Is.True);
            Assert.That(forced.Bytes, Is.EqualTo(new byte[] { 0xff, 0xc7 }));
            Assert.That(repeated.UsedHuffman, Is.EqualTo(smaller.UsedHuffman));
            Assert.That(repeated.Bytes, Is.EqualTo(smaller.Bytes));
        });
    }

    [Test]
    public void DecoderMatchesRfcRequestExamples()
    {
        AssertDecodedText("f1e3c2e5f23a6ba0ab90f4ff", "www.example.com");
        AssertDecodedText("a8eb10649cbf", "no-cache");
        AssertDecodedText("25a849e95ba97d7f", "custom-key");
        AssertDecodedText("25a849e95bb8e8b4bf", "custom-value");
    }

    [Test]
    public void DecoderHandlesEveryOctetSymbol()
    {
        byte[] expected = new byte[256];
        int index = 0;
        while (index < expected.Length)
        {
            expected[index] = (byte)index;
            index = index + 1;
        }

        byte[] encoded = EncodeForTest(expected);
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            encoded, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Bytes, Is.EqualTo(expected));
            Assert.That(decoded.Consumed, Is.EqualTo(encoded.Length));
            Assert.That(decoded.Written, Is.EqualTo(256));
            Assert.That(decoded.RequiredSize, Is.EqualTo(256));
        });
    }

    [Test]
    public void DecoderMeasuresBeforeAllocatingBoundedOutput()
    {
        HpackHuffmanResult encoded = HpackHuffmanCodec.Encode("aaaaaa"u8);

        HpackHuffmanResult exact = HpackHuffmanCodec.Decode(encoded.Bytes, 6);
        HpackHuffmanResult limited = HpackHuffmanCodec.Decode(encoded.Bytes, 5);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exact.Bytes, Is.EqualTo("aaaaaa"u8.ToArray()));
            Assert.That(limited.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(limited.Bytes, Is.Empty);
            Assert.That(limited.Written, Is.EqualTo(6));
            Assert.That(limited.IsMalformed, Is.False);
            Assert.That(limited.ErrorCode,
                Is.EqualTo("hpack-string-limit-exceeded"));
        });
    }

    [Test]
    public void DecoderAcceptsEveryLegalTerminalPaddingLength()
    {
        for (int paddingLength = 0; paddingLength <= 7; paddingLength++)
        {
            int symbol = FindSymbolForPaddingLength(paddingLength);
            Assert.That(symbol, Is.GreaterThanOrEqualTo(0),
                $"No symbol found for padding length {paddingLength}.");

            byte[] expected = [(byte)symbol];
            byte[] encoded = EncodeForTest(expected);
            HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
                encoded, int.MaxValue);

            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done),
                $"Padding length {paddingLength} was rejected.");
            Assert.That(decoded.Bytes, Is.EqualTo(expected));
        }
    }

    [Test]
    public void DecoderAcceptsEmptyInput()
    {
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            ReadOnlySpan<byte>.Empty, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Bytes, Is.Empty);
            Assert.That(decoded.Consumed, Is.Zero);
            Assert.That(decoded.Written, Is.Zero);
        });
    }

    [Test]
    public void DecoderRejectsTheReservedEosSymbol()
    {
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            new byte[] { 0xff, 0xff, 0xff, 0xfc }, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(decoded.IsMalformed, Is.True);
            Assert.That(decoded.IsEos, Is.True);
            Assert.That(decoded.IsInvalidPadding, Is.False);
            Assert.That(decoded.ErrorCode, Is.EqualTo("hpack-huffman-eos-symbol"));
            Assert.That(decoded.HasErrorPosition, Is.True);
            Assert.That(decoded.ErrorPosition, Is.EqualTo(3));
            Assert.That(decoded.ErrorBitPosition, Is.EqualTo(29));
            Assert.That(decoded.Consumed, Is.EqualTo(4));
        });
    }

    [Test]
    public void DecoderRejectsPaddingThatIsNotAnEosPrefix()
    {
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            new byte[] { 0x18 }, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(decoded.IsMalformed, Is.True);
            Assert.That(decoded.IsInvalidPadding, Is.True);
            Assert.That(decoded.IsEos, Is.False);
            Assert.That(decoded.ErrorCode,
                Is.EqualTo("invalid-hpack-huffman-padding"));
            Assert.That(decoded.ErrorPosition, Is.Zero);
            Assert.That(decoded.ErrorBitPosition, Is.EqualTo(5));
            Assert.That(decoded.Written, Is.EqualTo(1));
            Assert.That(decoded.Bytes, Is.Empty);
        });
    }

    [Test]
    public void DecoderRejectsPaddingLongerThanSevenBits()
    {
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            new byte[] { 0xff }, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(decoded.IsInvalidPadding, Is.True);
            Assert.That(decoded.ErrorCode,
                Is.EqualTo("invalid-hpack-huffman-padding"));
            Assert.That(decoded.ErrorPosition, Is.Zero);
            Assert.That(decoded.ErrorBitPosition, Is.Zero);
            Assert.That(decoded.Consumed, Is.EqualTo(1));
        });
    }

    static void AssertDecodedText(string hexadecimal, string expected)
    {
        byte[] encoded = Convert.FromHexString(hexadecimal);
        HpackHuffmanResult decoded = HpackHuffmanCodec.Decode(
            encoded, int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(decoded.Bytes), Is.EqualTo(expected));
            Assert.That(decoded.Consumed, Is.EqualTo(encoded.Length));
            Assert.That(decoded.Written, Is.EqualTo(expected.Length));
        });
    }

    static void AssertEncodedText(string input, string expectedHexadecimal)
    {
        byte[] source = Encoding.ASCII.GetBytes(input);
        byte[] expected = Convert.FromHexString(expectedHexadecimal);
        HpackHuffmanResult encoded = HpackHuffmanCodec.Encode(source);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(encoded.Bytes, Is.EqualTo(expected));
            Assert.That(encoded.Consumed, Is.EqualTo(source.Length));
            Assert.That(encoded.Written, Is.EqualTo(expected.Length));
            Assert.That(encoded.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(encoded.HuffmanByteLength, Is.EqualTo(expected.Length));
        });
    }

    static int FindSymbolForPaddingLength(int paddingLength)
    {
        int symbol = 0;
        while (symbol < 256)
        {
            int length = HpackStaticTable.HuffmanCodeLengths[symbol];
            int candidatePadding = (8 - length % 8) % 8;
            if (candidatePadding == paddingLength)
            {
                return symbol;
            }

            symbol = symbol + 1;
        }

        return -1;
    }

    static byte[] EncodeForTest(byte[] input)
    {
        List<byte> encoded = new List<byte>();
        int current = 0;
        int currentLength = 0;
        int inputIndex = 0;
        while (inputIndex < input.Length)
        {
            int symbol = input[inputIndex];
            uint code = HpackStaticTable.HuffmanCodes[symbol];
            int codeLength = HpackStaticTable.HuffmanCodeLengths[symbol];
            int bitIndex = codeLength - 1;
            while (bitIndex >= 0)
            {
                current = current << 1;
                if ((code & (1u << bitIndex)) != 0)
                {
                    current = current | 1;
                }

                currentLength = currentLength + 1;
                if (currentLength == 8)
                {
                    encoded.Add((byte)current);
                    current = 0;
                    currentLength = 0;
                }

                bitIndex = bitIndex - 1;
            }

            inputIndex = inputIndex + 1;
        }

        if (currentLength != 0)
        {
            int paddingLength = 8 - currentLength;
            current = current << paddingLength;
            current = current | (1 << paddingLength) - 1;
            encoded.Add((byte)current);
        }

        return encoded.ToArray();
    }
}
