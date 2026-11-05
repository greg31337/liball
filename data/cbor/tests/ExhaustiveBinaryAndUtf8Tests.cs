using cbor;

namespace cbor.Tests;

[TestFixture]
public class ExhaustiveBinaryAndUtf8Tests
{
    [Test]
    public void EveryInitialByteHasTheExpectedHeadBehavior()
    {
        byte[] source = [0, 1, 2, 3, 4, 5, 6, 7, 8];
        int accepted = 0;
        int rejected = 0;

        for (int initial = 0; initial <= byte.MaxValue; initial++)
        {
            source[0] = (byte)initial;
            int majorType = initial >> 5;
            int additionalInformation = initial & 31;

            if (additionalInformation >= 28 && additionalInformation <= 30)
            {
                AssertDecodeFailure(source, "CBOR_RESERVED_ADDITIONAL_INFORMATION", initial);
                rejected++;
                continue;
            }
            if (additionalInformation == 31 && (majorType < 2 || majorType == 6))
            {
                AssertDecodeFailure(source, "CBOR_ILLEGAL_INDEFINITE_HEAD", initial);
                rejected++;
                continue;
            }

            CborHead head = CborBinaryPrimitives.ReadHead(source);
            int expectedWidth = GetExpectedArgumentWidth(additionalInformation);
            ulong expectedArgument = GetExpectedArgument(additionalInformation);
            if (head.MajorType != majorType
                || head.AdditionalInformation != additionalInformation
                || head.ArgumentWidth != expectedWidth
                || head.HeadLength != expectedWidth + 1
                || head.Argument != expectedArgument
                || head.IsIndefinite != (additionalInformation == 31 && majorType >= 2 && majorType <= 5)
                || head.IsBreak != (initial == byte.MaxValue))
            {
                Assert.Fail($"Unexpected decoded head for initial byte 0x{initial:x2}.");
            }
            accepted++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.EqualTo(229));
            Assert.That(rejected, Is.EqualTo(27));
        });
    }

    [Test]
    public void EveryMajorTypeRoundTripsAllIntegerAndLengthBoundaries()
    {
        ulong[] arguments =
        [
            0, 1, 22, 23, 24, 25,
            254, 255, 256, 257,
            65534, 65535, 65536, 65537,
            (ulong)uint.MaxValue - 1, uint.MaxValue, (ulong)uint.MaxValue + 1,
            ulong.MaxValue - 1, ulong.MaxValue
        ];
        int roundTrips = 0;

        for (int majorType = 0; majorType <= 7; majorType++)
        {
            foreach (ulong argument in arguments)
            {
                byte[] destination = [0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5];
                int expectedSize = CborBinaryPrimitives.GetHeadSize(argument);
                int written = CborBinaryPrimitives.WriteHead(destination, majorType, argument);
                CborHead decoded = CborBinaryPrimitives.ReadHead(destination.AsSpan(0, written));

                if (written != expectedSize
                    || decoded.HeadLength != expectedSize
                    || decoded.MajorType != majorType
                    || decoded.Argument != argument
                    || destination[written] != 0xa5)
                {
                    Assert.Fail($"Major type {majorType} did not round-trip argument {argument}.");
                }
                roundTrips++;
            }
        }

        Assert.That(roundTrips, Is.EqualTo(arguments.Length * 8));
    }

    [Test]
    public void EveryTruncatedHeadAndUndersizedDestinationIsRejectedAtomically()
    {
        int[] additionalInformationValues = [24, 25, 26, 27];
        ulong[] arguments = [24, 256, 65536, (ulong)uint.MaxValue + 1, ulong.MaxValue];
        int truncations = 0;
        int undersizedDestinations = 0;

        foreach (int additionalInformation in additionalInformationValues)
        {
            int width = GetExpectedArgumentWidth(additionalInformation);
            byte[] source = new byte[width + 1];
            source[0] = (byte)additionalInformation;
            for (int length = 0; length <= width; length++)
            {
                CborDecodeException? failure = Assert.Throws<CborDecodeException>(
                    () => CborBinaryPrimitives.ReadHead(source.AsSpan(0, length)));
                Assert.Multiple(() =>
                {
                    Assert.That(failure?.ErrorCode, Is.EqualTo("CBOR_TRUNCATED_HEAD"));
                    Assert.That(failure?.ErrorOffset, Is.EqualTo(length));
                });
                truncations++;
            }
        }

        foreach (ulong argument in arguments)
        {
            int requiredLength = CborBinaryPrimitives.GetHeadSize(argument);
            for (int length = 0; length < requiredLength; length++)
            {
                byte[] destination = [0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5];
                CborEncodeException? failure = Assert.Throws<CborEncodeException>(
                    () => CborBinaryPrimitives.WriteHead(destination.AsSpan(0, length), 0, argument));
                Assert.Multiple(() =>
                {
                    Assert.That(failure?.ErrorCode, Is.EqualTo("CBOR_DESTINATION_TOO_SMALL"));
                    Assert.That(failure?.ErrorOffset, Is.EqualTo(length));
                    Assert.That(destination, Is.All.EqualTo(0xa5));
                });
                undersizedDestinations++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(truncations, Is.EqualTo(19));
            Assert.That(undersizedDestinations, Is.EqualTo(28));
        });
    }

    [Test]
    public void Utf8AbsoluteOffsetOverflowIsRejectedWithoutConsumingInput()
    {
        CborUtf8 decoder = new CborUtf8();
        decoder.TotalBytes = long.MaxValue - 1;

        CborUtf8Result boundary = decoder.ProcessChunk([0x61], false);
        CborUtf8Result overflow = decoder.ProcessChunk([0x62], false);
        CborUtf8Result repeated = decoder.ProcessChunk([0x63], true);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(boundary.TotalBytes, Is.EqualTo(long.MaxValue));
            Assert.That(overflow.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(overflow.ErrorCode, Is.EqualTo("CBOR_UTF8_OFFSET_OVERFLOW"));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(long.MaxValue));
            Assert.That(overflow.Consumed, Is.Zero);
            Assert.That(overflow.Text, Is.EqualTo("a"));
            Assert.That(repeated.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(repeated.Consumed, Is.Zero);
            Assert.That(repeated.ErrorOffset, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void EveryHalfPrecisionBitPatternRoundTripsExactly()
    {
        byte[] encoded = new byte[2];
        int patterns = 0;

        for (int raw = 0; raw <= ushort.MaxValue; raw++)
        {
            ushort bits = (ushort)raw;
            bool expectedNaN = (bits & 0x7c00) == 0x7c00 && (bits & 0x03ff) != 0;
            bool expectedSignaling = expectedNaN && (bits & 0x0200) == 0;
            CborFloatValue value = CborHalfPrecision.CreateHalfFloat(bits);
            int written = CborHalfPrecision.WriteFloatRepresentation(encoded, value);
            CborFloatValue restored = CborHalfPrecision.ReadHalfPrecision(encoded);

            if (value.Width != CborFloatWidth.Half
                || value.RawBits != bits
                || value.IsNaN != expectedNaN
                || value.IsSignalingNaN != expectedSignaling
                || written != 2
                || restored.RawBits != bits
                || (!expectedNaN && CborHalfPrecision.ConvertDoubleToHalfBits(value.Value) != bits))
            {
                Assert.Fail($"Half-precision pattern 0x{raw:x4} did not round-trip exactly.");
            }
            patterns++;
        }

        Assert.That(patterns, Is.EqualTo(65536));
    }

    [Test]
    public void EveryHalfNanPayloadMapsAndRoundTripsAcrossAllFloatWidths()
    {
        byte[] singleEncoded = new byte[4];
        byte[] doubleEncoded = new byte[8];
        int payloads = 0;

        for (uint sign = 0; sign <= 1; sign++)
        {
            for (uint payload = 1; payload <= 0x03ff; payload++)
            {
                ushort halfBits = (ushort)((sign << 15) | 0x7c00U | payload);
                uint singleBits = (sign << 31) | 0x7f800000U | (payload << 13);
                ulong doubleBits = ((ulong)sign << 63) | 0x7ff0000000000000UL | ((ulong)payload << 42);
                bool expectedSignaling = (payload & 0x0200U) == 0;
                CborFloatValue half = CborHalfPrecision.CreateHalfFloat(halfBits);
                CborFloatValue single = CborHalfPrecision.CreateSingleFloat(singleBits);
                CborFloatValue doubleValue = CborHalfPrecision.CreateDoubleFloat(doubleBits);

                CborHalfPrecision.WriteFloatRepresentation(singleEncoded, single);
                CborHalfPrecision.WriteFloatRepresentation(doubleEncoded, doubleValue);
                CborFloatValue restoredSingle = CborHalfPrecision.ReadSinglePrecision(singleEncoded);
                CborFloatValue restoredDouble = CborHalfPrecision.ReadDoublePrecision(doubleEncoded);

                if (!half.IsNaN
                    || !single.IsNaN
                    || !doubleValue.IsNaN
                    || half.IsSignalingNaN != expectedSignaling
                    || single.IsSignalingNaN != expectedSignaling
                    || doubleValue.IsSignalingNaN != expectedSignaling
                    || half.RawBits != halfBits
                    || restoredSingle.RawBits != singleBits
                    || restoredDouble.RawBits != doubleBits)
                {
                    Assert.Fail($"NaN payload 0x{payload:x3} with sign {sign} was not preserved.");
                }
                payloads++;
            }
        }

        Assert.That(payloads, Is.EqualTo(2046));
    }

    [Test]
    public void EveryFragmentationPartitionProducesTheSameUtf8Text()
    {
        byte[] bytes = [0x24, 0xc2, 0xa2, 0xe2, 0x82, 0xac, 0xf0, 0x90, 0x8d, 0x88];
        int partitions = 1 << (bytes.Length - 1);

        for (int partition = 0; partition < partitions; partition++)
        {
            CborUtf8 decoder = new CborUtf8();
            int start = 0;
            for (int boundary = 1; boundary < bytes.Length; boundary++)
            {
                if ((partition & (1 << (boundary - 1))) != 0)
                {
                    CborUtf8Result fragment = decoder.ProcessChunk(bytes.AsSpan(start, boundary - start), false);
                    if (fragment.Status != CborReadStatus.NeedMoreData || fragment.ErrorCode != null)
                    {
                        Assert.Fail($"Valid UTF-8 failed at partition {partition}, boundary {boundary}.");
                    }
                    start = boundary;
                }
            }

            CborUtf8Result completed = decoder.ProcessChunk(bytes.AsSpan(start), true);
            if (completed.Status != CborReadStatus.Done
                || completed.Text != "$¢€𐍈"
                || completed.TotalBytes != bytes.Length
                || completed.ErrorCode != null)
            {
                Assert.Fail($"Valid UTF-8 produced an unexpected result for partition {partition}.");
            }
        }

        Assert.That(partitions, Is.EqualTo(512));
    }

    [Test]
    public void MalformedUtf8HasStableErrorsAtEveryFragmentBoundary()
    {
        byte[][] inputs =
        [
            [0xc0, 0x80],
            [0xe0, 0x80, 0x80],
            [0xed, 0xa0, 0x80],
            [0xf4, 0x90, 0x80, 0x80],
            [0xe2, 0x28, 0xa1],
            [0x61, 0xe2, 0x28],
            [0x80],
            [0xe2, 0x82]
        ];
        string[] expectedCodes =
        [
            "CBOR_UTF8_NON_SHORTEST",
            "CBOR_UTF8_NON_SHORTEST",
            "CBOR_UTF8_SURROGATE",
            "CBOR_UTF8_OUT_OF_RANGE",
            "CBOR_UTF8_INVALID_CONTINUATION",
            "CBOR_UTF8_INVALID_CONTINUATION",
            "CBOR_UTF8_INVALID_LEADING_BYTE",
            "CBOR_UTF8_INCOMPLETE_SEQUENCE"
        ];
        long[] expectedOffsets = [0, 0, 0, 0, 1, 2, 0, 0];
        int fragmentations = 0;

        for (int caseIndex = 0; caseIndex < inputs.Length; caseIndex++)
        {
            byte[] input = inputs[caseIndex];
            for (int split = 0; split <= input.Length; split++)
            {
                CborUtf8 decoder = new CborUtf8();
                CborUtf8Result result = decoder.ProcessChunk(input.AsSpan(0, split), false);
                if (result.Status != CborReadStatus.Error)
                {
                    result = decoder.ProcessChunk(input.AsSpan(split), true);
                }
                if (result.Status != CborReadStatus.Error
                    || result.ErrorCode != expectedCodes[caseIndex]
                    || result.ErrorOffset != expectedOffsets[caseIndex])
                {
                    Assert.Fail($"Malformed UTF-8 case {caseIndex} changed at split {split}.");
                }
                fragmentations++;
            }
        }

        Assert.That(fragmentations, Is.EqualTo(29));
    }

    private static void AssertDecodeFailure(byte[] source, string expectedCode, int initial)
    {
        CborDecodeException? failure = Assert.Throws<CborDecodeException>(
            () => CborBinaryPrimitives.ReadHead(source));
        if (failure?.ErrorCode != expectedCode)
        {
            Assert.Fail($"Initial byte 0x{initial:x2} produced {failure?.ErrorCode} instead of {expectedCode}.");
        }
    }

    private static int GetExpectedArgumentWidth(int additionalInformation)
    {
        if (additionalInformation < 24 || additionalInformation == 31)
        {
            return 0;
        }
        if (additionalInformation == 24)
        {
            return 1;
        }
        if (additionalInformation == 25)
        {
            return 2;
        }
        if (additionalInformation == 26)
        {
            return 4;
        }
        return 8;
    }

    private static ulong GetExpectedArgument(int additionalInformation)
    {
        if (additionalInformation < 24)
        {
            return (ulong)additionalInformation;
        }
        if (additionalInformation == 24)
        {
            return 0x01;
        }
        if (additionalInformation == 25)
        {
            return 0x0102;
        }
        if (additionalInformation == 26)
        {
            return 0x01020304;
        }
        if (additionalInformation == 27)
        {
            return 0x0102030405060708;
        }
        return 0;
    }
}
