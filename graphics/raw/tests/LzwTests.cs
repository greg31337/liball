using raw;

namespace raw.Tests;

public class LzwTests
{
    [Test]
    public void TiffSpecificationExampleMatchesKnownCodeStream()
    {
        byte[] source = new byte[] { 7, 7, 7, 8, 8, 7, 7, 6, 6 };
        byte[] expected = new byte[] { 0x80, 0x01, 0xE0, 0x40, 0x80, 0x44, 0x08, 0x0C, 0x06, 0x80, 0x80 };

        byte[] encoded = Encode(source, 1);
        byte[] decoded = Decode(expected, source.Length, 1);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(decoded, Is.EqualTo(source));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    public void EmptyAndSpecialCaseStringsRoundTrip(int earlyChange)
    {
        byte[][] sources = new byte[][]
        {
            new byte[0],
            new byte[] { 65 },
            Enumerable.Repeat((byte)65, 300).ToArray(),
            System.Text.Encoding.ASCII.GetBytes("TOBEORNOTTOBEORTOBEORNOT")
        };

        foreach (byte[] source in sources)
        {
            byte[] first = Encode(source, earlyChange);
            byte[] second = Encode(source, earlyChange);
            Assert.Multiple(() =>
            {
                Assert.That(second, Is.EqualTo(first), $"determinism length {source.Length}");
                Assert.That(Decode(first, source.Length, earlyChange), Is.EqualTo(source), $"round trip length {source.Length}");
            });
        }
    }

    [Test]
    public void EarlyChangeOneReadsEndCodeAtTenBitsAfterEntry510()
    {
        int[] codes = new int[256];
        int[] widths = Enumerable.Repeat(9, codes.Length).ToArray();
        codes[0] = LzwCodec.ClearCode;
        byte[] expected = new byte[254];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = (byte)index;
            codes[index + 1] = expected[index];
        }
        codes[codes.Length - 1] = LzwCodec.EndCode;
        widths[widths.Length - 1] = 10;

        byte[] encoded = PackCodes(codes, widths);

        Assert.That(Decode(encoded, expected.Length, 1), Is.EqualTo(expected));
    }

    [Test]
    public void EarlyChangeZeroPostponesTenBitCodesByOneDictionaryEntry()
    {
        int[] codes = new int[257];
        int[] widths = Enumerable.Repeat(9, codes.Length).ToArray();
        codes[0] = LzwCodec.ClearCode;
        byte[] expected = new byte[255];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = (byte)index;
            codes[index + 1] = expected[index];
        }
        codes[codes.Length - 1] = LzwCodec.EndCode;
        widths[widths.Length - 1] = 10;

        byte[] encoded = PackCodes(codes, widths);

        Assert.That(Decode(encoded, expected.Length, 0), Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void EncoderChangesWidthAtTheSpecifiedIndependentBoundary(int earlyChange)
    {
        byte[] source = Enumerable.Range(0, 255).Select(value => (byte)value).ToArray();
        int[] codes = new int[257];
        int[] widths = Enumerable.Repeat(9, codes.Length).ToArray();
        codes[0] = LzwCodec.ClearCode;
        for (int index = 0; index < source.Length; index++)
        {
            codes[index + 1] = source[index];
        }
        codes[codes.Length - 1] = LzwCodec.EndCode;
        widths[widths.Length - 1] = 10;
        if (earlyChange == 1)
        {
            widths[widths.Length - 2] = 10;
        }

        byte[] expected = PackCodes(codes, widths);

        Assert.That(Encode(source, earlyChange), Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void LongIncompressibleDataCrossesEveryWidthAndClearsTheTable(int earlyChange)
    {
        Random random = new Random(7711 + earlyChange);
        byte[] source = new byte[24000];
        random.NextBytes(source);

        byte[] encoded = Encode(source, earlyChange);
        byte[] decoded = Decode(encoded, source.Length, earlyChange);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(source));
            Assert.That(encoded.Length, Is.GreaterThan(4096));
        });
    }

    [Test]
    public void ClearCodeResetsDictionaryAndCodeWidth()
    {
        int[] codes = new int[] { 256, 65, 66, 256, 67, 257 };
        int[] widths = Enumerable.Repeat(9, codes.Length).ToArray();

        byte[] decoded = Decode(PackCodes(codes, widths), 3, 1);

        Assert.That(decoded, Is.EqualTo(new byte[] { 65, 66, 67 }));
    }

    [Test]
    public void CorruptControlCodesReferencesAndPaddingAreRejected()
    {
        RawFormatException? missingClear = Assert.Throws<RawFormatException>(() => Decode(PackCodes(new int[] { 65, 257 }, new int[] { 9, 9 }), 1, 1));
        RawFormatException? futureCode = Assert.Throws<RawFormatException>(() => Decode(PackCodes(new int[] { 256, 300, 257 }, new int[] { 9, 9, 9 }), 1, 1));
        RawFormatException? earlyEnd = Assert.Throws<RawFormatException>(() => Decode(PackCodes(new int[] { 256, 257 }, new int[] { 9, 9 }), 1, 1));
        RawFormatException? overflow = Assert.Throws<RawFormatException>(() => Decode(PackCodes(new int[] { 256, 65, 257 }, new int[] { 9, 9, 9 }), 0, 1));
        byte[] missingEndBytes = PackCodes(new int[] { 256, 65 }, new int[] { 9, 9 });
        RawFormatException? missingEnd = Assert.Throws<RawFormatException>(() => Decode(missingEndBytes, 1, 1));
        byte[] badPadding = PackCodes(new int[] { 256, 257 }, new int[] { 9, 9 });
        badPadding[badPadding.Length - 1] |= 1;
        RawFormatException? padding = Assert.Throws<RawFormatException>(() => Decode(badPadding, 0, 1));
        byte[] trailingBytes = PackCodes(new int[] { 256, 257 }, new int[] { 9, 9 }).Concat(new byte[] { 0 }).ToArray();
        RawFormatException? trailing = Assert.Throws<RawFormatException>(() => Decode(trailingBytes, 0, 1));

        Assert.Multiple(() =>
        {
            Assert.That((missingClear ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((futureCode ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((earlyEnd ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((overflow ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((missingEnd ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((padding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((trailing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void HorizontalEightBitPredictorUsesMatchingComponentsAndRowBoundaries()
    {
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.Horizontal, 4, 2, 3, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);
        byte[] source = new byte[]
        {
            10, 20, 30, 12, 25, 40, 15, 30, 42, 17, 31, 50,
            200, 100, 50, 190, 110, 55, 180, 120, 65, 170, 130, 80
        };
        byte[] original = source.ToArray();

        byte[] encoded = LzwCodec.EncodeTiff(source, 1, parameters, new RawLimits(), CancellationToken.None);
        byte[] decoded = LzwCodec.DecodeTiff(encoded, 1, parameters, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(source));
            Assert.That(source, Is.EqualTo(original));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void HorizontalSixteenBitPredictorPreservesEndianWordsAndWraparound(RawByteOrder byteOrder)
    {
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.Horizontal, 4, 2, 2, 16, TiffSampleFormat.UnsignedInteger, byteOrder);
        ushort[] values = new ushort[] { 65000, 1, 10, 65530, 500, 4, 65535, 20, 0, 65535, 1, 0, 2, 1, 3, 2 };
        byte[] source = UInt16Bytes(values, byteOrder);

        byte[] encoded = LzwCodec.EncodeTiff(source, 1, parameters, new RawLimits(), CancellationToken.None);
        byte[] decoded = LzwCodec.DecodeTiff(encoded, 1, parameters, new RawLimits(), CancellationToken.None);

        Assert.That(decoded, Is.EqualTo(source));
    }

    [Test]
    public void HorizontalPackedFourBitPredictorRoundTripsOddRows()
    {
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.Horizontal, 5, 2, 1, 4, TiffSampleFormat.UnsignedInteger, RawByteOrder.BigEndian);
        uint[][] rows = new uint[][]
        {
            new uint[] { 1, 3, 2, 15, 0 },
            new uint[] { 15, 0, 14, 1, 13 }
        };
        byte[] source = new byte[6];
        BitPacking.PackUnsigned(rows[0], 4, RawBitOrder.MostSignificantBitFirst, source.AsSpan(0, 3));
        BitPacking.PackUnsigned(rows[1], 4, RawBitOrder.MostSignificantBitFirst, source.AsSpan(3, 3));

        byte[] encoded = LzwCodec.EncodeTiff(source, 1, parameters, new RawLimits(), CancellationToken.None);
        byte[] decoded = LzwCodec.DecodeTiff(encoded, 1, parameters, new RawLimits(), CancellationToken.None);

        Assert.That(decoded, Is.EqualTo(source));
    }

    [TestCase(RawByteOrder.LittleEndian, 32)]
    [TestCase(RawByteOrder.BigEndian, 32)]
    [TestCase(RawByteOrder.LittleEndian, 64)]
    [TestCase(RawByteOrder.BigEndian, 64)]
    public void FloatingPointPredictorShufflesBytePlanesAndRoundTrips(RawByteOrder byteOrder, int bitsPerSample)
    {
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.FloatingPoint, 4, 2, 2, bitsPerSample, TiffSampleFormat.FloatingPoint, byteOrder);
        byte[] source = CreateFloatingBytes(bitsPerSample, byteOrder);

        byte[] encoded = LzwCodec.EncodeTiff(source, 1, parameters, new RawLimits(), CancellationToken.None);
        byte[] decoded = LzwCodec.DecodeTiff(encoded, 1, parameters, new RawLimits(), CancellationToken.None);

        Assert.That(decoded, Is.EqualTo(source));
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void FloatingPointPredictorMatchesKnownBytePlaneVector(RawByteOrder byteOrder)
    {
        TiffPredictorParameters parameters = new TiffPredictorParameters(TiffPredictor.FloatingPoint, 2, 1, 1, 32, TiffSampleFormat.FloatingPoint, byteOrder);
        byte[] source = new byte[8];
        EndianWriter.WriteSingle(source.AsSpan(0, 4), 1.0F, byteOrder);
        EndianWriter.WriteSingle(source.AsSpan(4, 4), 2.0F, byteOrder);
        byte[] predicted = source.ToArray();

        TiffPredictorCodec.Encode(predicted, parameters, new RawLimits(), CancellationToken.None);

        Assert.That(predicted, Is.EqualTo(new byte[] { 0x3F, 0x01, 0x40, 0x80, 0, 0, 0, 0 }));
        TiffPredictorCodec.Decode(predicted, parameters, new RawLimits(), CancellationToken.None);
        Assert.That(predicted, Is.EqualTo(source));
    }

    [Test]
    public void EncoderPreflightsShortDestinationWithoutWriting()
    {
        byte[] source = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        byte[] destination = Enumerable.Repeat((byte)0xCC, 8).ToArray();

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => LzwCodec.Encode(source, destination, 1, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That(destination, Is.All.EqualTo(0xCC));
        });
    }

    [Test]
    public void InvalidEarlyChangePredictorGeometryAndSampleKindsAreRejected()
    {
        TiffPredictorParameters valid = new TiffPredictorParameters(TiffPredictor.Horizontal, 2, 1, 1, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);
        TiffPredictorParameters badFloatKind = new TiffPredictorParameters(TiffPredictor.FloatingPoint, 2, 1, 1, 32, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);
        TiffPredictorParameters badFloatWidth = new TiffPredictorParameters(TiffPredictor.FloatingPoint, 2, 1, 1, 8, TiffSampleFormat.FloatingPoint, RawByteOrder.LittleEndian);
        TiffPredictorParameters badDimensions = new TiffPredictorParameters(TiffPredictor.Horizontal, 0, 1, 1, 8, TiffSampleFormat.UnsignedInteger, RawByteOrder.LittleEndian);

        RawFormatException? earlyChange = Assert.Throws<RawFormatException>(() => LzwCodec.Encode(new byte[0], 2, new RawLimits(), CancellationToken.None));
        RawFormatException? length = Assert.Throws<RawFormatException>(() => LzwCodec.EncodeTiff(new byte[1], 1, valid, new RawLimits(), CancellationToken.None));
        RawFormatException? floatKind = Assert.Throws<RawFormatException>(() => LzwCodec.EncodeTiff(new byte[8], 1, badFloatKind, new RawLimits(), CancellationToken.None));
        RawFormatException? floatWidth = Assert.Throws<RawFormatException>(() => LzwCodec.EncodeTiff(new byte[2], 1, badFloatWidth, new RawLimits(), CancellationToken.None));
        RawFormatException? dimensions = Assert.Throws<RawFormatException>(() => TiffPredictorCodec.GetDecodedLength(badDimensions, new RawLimits()));

        Assert.Multiple(() =>
        {
            Assert.That((earlyChange ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((floatKind ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((floatWidth ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((dimensions ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void AllocationLimitsAndCancellationAreEnforcedBeforeResultsEscape()
    {
        RawLimits limits = new RawLimits();
        limits.MaxDecodedBytes = 4;
        limits.MaxAllocationBytes = 4;
        byte[] validFive = Encode(new byte[] { 1, 2, 3, 4, 5 }, 1);

        RawLimitException? decodeLimit = Assert.Throws<RawLimitException>(() => LzwCodec.Decode(validFive, 5, 1, limits, CancellationToken.None));
        RawLimitException? encodeLimit = Assert.Throws<RawLimitException>(() => LzwCodec.Encode(new byte[] { 1, 2, 3, 4 }, 1, limits, CancellationToken.None));
        RawLimitException? missingLimits = Assert.Throws<RawLimitException>(() => LzwCodec.Decode(new byte[0], 0, 1, null, CancellationToken.None));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => LzwCodec.Encode(new byte[] { 1, 2, 3 }, 1, new RawLimits(), cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That((decodeLimit ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((encodeLimit ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((missingLimits ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
        });
    }

    static byte[] Encode(byte[] source, int earlyChange)
    {
        return LzwCodec.Encode(source, earlyChange, new RawLimits(), CancellationToken.None);
    }

    static byte[] Decode(byte[] source, int decodedLength, int earlyChange)
    {
        return LzwCodec.Decode(source, decodedLength, earlyChange, new RawLimits(), CancellationToken.None);
    }

    static byte[] PackCodes(int[] codes, int[] widths)
    {
        Assert.That(widths.Length, Is.EqualTo(codes.Length));
        int bitCount = widths.Sum();
        byte[] bytes = new byte[(bitCount + 7) / 8];
        int bitOffset = 0;
        for (int index = 0; index < codes.Length; index++)
        {
            for (int bit = widths[index] - 1; bit >= 0; bit--)
            {
                if (((codes[index] >> bit) & 1) != 0)
                {
                    bytes[bitOffset / 8] |= (byte)(1 << (7 - bitOffset % 8));
                }
                bitOffset++;
            }
        }
        return bytes;
    }

    static byte[] UInt16Bytes(ushort[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[values.Length * 2];
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
        }
        return bytes;
    }

    static byte[] CreateFloatingBytes(int bitsPerSample, RawByteOrder byteOrder)
    {
        if (bitsPerSample == 32)
        {
            float[] values = new float[] { 0, 1, -1, 12.5F, float.PositiveInfinity, float.NegativeInfinity, BitConverter.UInt32BitsToSingle(0x7FC12345), -0.0F, 1000, -12, 0.25F, 0.5F, 0.75F, 1.25F, -3.5F, 8 };
            byte[] bytes = new byte[values.Length * 4];
            for (int index = 0; index < values.Length; index++)
            {
                EndianWriter.WriteSingle(bytes.AsSpan(index * 4, 4), values[index], byteOrder);
            }
            return bytes;
        }

        double[] doubles = new double[] { 0, 1, -1, 12.5, double.PositiveInfinity, double.NegativeInfinity, BitConverter.UInt64BitsToDouble(0x7FF8123456789ABC), -0.0, 1000, -12, 0.25, 0.5, 0.75, 1.25, -3.5, 8 };
        byte[] result = new byte[doubles.Length * 8];
        for (int index = 0; index < doubles.Length; index++)
        {
            EndianWriter.WriteDouble(result.AsSpan(index * 8, 8), doubles[index], byteOrder);
        }
        return result;
    }
}
