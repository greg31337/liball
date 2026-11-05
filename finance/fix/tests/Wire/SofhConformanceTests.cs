using System;
using fix;

namespace fix.Tests;

[TestFixture]
public class SofhConformanceTests
{
    [Test]
    public void PublishedSbeExampleVectorHasTheStandardHeaderAndLength()
    {
        byte[] vector = Convert.FromHexString(
            "00000044EB50360063005B0000004F52" +
            "44303030303141434354303100004745" +
            "4D3400000000318016B33B1365291507" +
            "000000321A8501000000000000000000" +
            "00000080");

        FrameResult result = new SofhMessageFramer().ReadFrame(vector, new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(vector, Has.Length.EqualTo(68));
            AssertComplete(result, 68, FixEncodingTypes.SbeVersion10LittleEndian);
            Assert.That(vector.AsSpan(0, FixDefaults.SofhHeaderSize).ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 68, 235, 80 }));
        });
    }

    [TestCase(FixEncodingTypes.PrivateUseMinimum)]
    [TestCase(FixEncodingTypes.PrivateUseMaximum)]
    [TestCase(FixEncodingTypes.GoogleProtocolBuffersVersion10)]
    [TestCase(FixEncodingTypes.SbeVersion10BigEndian)]
    [TestCase(FixEncodingTypes.SbeVersion20BigEndian)]
    [TestCase(FixEncodingTypes.Asn1PerVersion10)]
    [TestCase(FixEncodingTypes.Asn1BerVersion10)]
    [TestCase(FixEncodingTypes.Asn1OerVersion10)]
    [TestCase(FixEncodingTypes.SbeVersion10LittleEndian)]
    [TestCase(FixEncodingTypes.SbeVersion20LittleEndian)]
    [TestCase(FixEncodingTypes.TagValue)]
    [TestCase(FixEncodingTypes.FixmlVersion10)]
    [TestCase(FixEncodingTypes.Json)]
    [TestCase(FixEncodingTypes.FastMinimum)]
    [TestCase(FixEncodingTypes.FastMaximum)]
    [TestCase(FixEncodingTypes.Bson)]
    public void EveryRegisteredEncodingIdentifierIsAccepted(int encoding)
    {
        byte[] frame = BuildFrame([1, 2, 3], (ushort)encoding, false);

        FrameResult result = new SofhMessageFramer().ReadFrame(frame, new FrameOptions());

        AssertComplete(result, frame.Length, (ushort)encoding);
    }

    [Test]
    public void NetworkAndBilateralLittleEndianHeadersDecodeToTheSameValues()
    {
        byte[] payload = [10, 20, 30, 40, 50, 60];
        byte[] network = BuildFrame(payload, FixEncodingTypes.TagValue, false);
        byte[] little = BuildFrame(payload, FixEncodingTypes.TagValue, true);
        SofhMessageFramer framer = new SofhMessageFramer();

        FrameResult networkResult = framer.ReadFrame(network, new FrameOptions());
        FrameResult littleResult = framer.ReadFrame(little, new FrameOptions());

        Assert.Multiple(() =>
        {
            Assert.That(network.AsSpan(0, 6).ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 12, 240, 0 }));
            Assert.That(little.AsSpan(0, 6).ToArray(), Is.EqualTo(new byte[] { 12, 0, 0, 0, 0, 240 }));
            AssertComplete(networkResult, 12, FixEncodingTypes.TagValue);
            AssertComplete(littleResult, 12, FixEncodingTypes.TagValue);
        });
    }

    [Test]
    public void EveryPrefixOfBothByteOrdersReportsTheExactRequirement()
    {
        byte[] network = BuildFrame([1, 2, 3, 4, 5], FixEncodingTypes.SbeVersion10LittleEndian, false);
        byte[] little = BuildFrame([6, 7, 8, 9], FixEncodingTypes.SbeVersion20LittleEndian, true);

        AssertEveryPrefix(network);
        AssertEveryPrefix(little);
    }

    [Test]
    public void MixedEndianConcatenatedFramesAreConsumedIndividually()
    {
        byte[][] frames =
        [
            BuildFrame([1, 2, 3], FixEncodingTypes.SbeVersion10BigEndian, false),
            BuildFrame([4, 5, 6, 7], FixEncodingTypes.SbeVersion10LittleEndian, true),
            BuildFrame([8, 9], FixEncodingTypes.Json, false)
        ];
        byte[] source = Combine(Combine(frames[0], frames[1]), frames[2]);
        ushort[] encodings = [FixEncodingTypes.SbeVersion10BigEndian, FixEncodingTypes.SbeVersion10LittleEndian, FixEncodingTypes.Json];
        SofhMessageFramer framer = new SofhMessageFramer();
        int offset = 0;

        Assert.Multiple(() =>
        {
            for (int index = 0; index < frames.Length; index++)
            {
                FrameResult result = framer.ReadFrame(source.AsSpan(offset), new FrameOptions());
                AssertComplete(result, frames[index].Length, encodings[index]);
                offset += result.Consumed;
            }
            Assert.That(offset, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void UnknownEncodingIsRejectedInEitherHeaderByteOrder()
    {
        byte[] network = BuildFrame([1], 0x1234, false);
        byte[] little = BuildFrame([2], 0x1234, true);
        SofhMessageFramer framer = new SofhMessageFramer();

        FrameResult networkResult = framer.ReadFrame(network, new FrameOptions());
        FrameResult littleResult = framer.ReadFrame(little, new FrameOptions());

        Assert.Multiple(() =>
        {
            AssertUnknownEncoding(networkResult);
            AssertUnknownEncoding(littleResult);
            Assert.That(networkResult.EncodingType, Is.EqualTo(0x1234));
            Assert.That(littleResult.EncodingType, Is.EqualTo(0x3412));
        });
    }

    [Test]
    public void FrameLimitsAreInclusiveAndRejectInvalidUnsignedLengths()
    {
        byte[] frame = BuildFrame([1, 2, 3, 4], FixEncodingTypes.SbeVersion10BigEndian, false);
        byte[] minimum = BuildHeader(FixDefaults.SofhHeaderSize, FixEncodingTypes.SbeVersion10BigEndian, false);
        byte[] belowMinimum = BuildHeader(FixDefaults.SofhHeaderSize - 1, FixEncodingTypes.SbeVersion10BigEndian, false);
        byte[] unsignedMaximum = [255, 255, 255, 255, 91, 224];
        SofhMessageFramer framer = new SofhMessageFramer();

        FrameResult exact = framer.ReadFrame(frame, new FrameOptions { MaxFrameSize = frame.Length });
        FrameResult oneShort = framer.ReadFrame(frame.AsSpan(0, FixDefaults.SofhHeaderSize), new FrameOptions { MaxFrameSize = frame.Length - 1 });
        FrameResult headerOnly = framer.ReadFrame(minimum, new FrameOptions { MaxFrameSize = FixDefaults.SofhHeaderSize });
        FrameResult invalidLength = framer.ReadFrame(belowMinimum, new FrameOptions());
        FrameResult unsupportedLength = framer.ReadFrame(unsignedMaximum, new FrameOptions { MaxFrameSize = int.MaxValue });
        FrameResult invalidLimit = framer.ReadFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { MaxFrameSize = -1 });
        FrameResult headerLimit = framer.ReadFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { MaxFrameSize = FixDefaults.SofhHeaderSize - 1 });

        Assert.Multiple(() =>
        {
            AssertComplete(exact, frame.Length, FixEncodingTypes.SbeVersion10BigEndian);
            AssertComplete(headerOnly, FixDefaults.SofhHeaderSize, FixEncodingTypes.SbeVersion10BigEndian);
            Assert.That(oneShort.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(oneShort.Required, Is.EqualTo(frame.Length));
            Assert.That(invalidLength.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(invalidLength.ErrorCode, Is.EqualTo("invalid-message-length"));
            Assert.That(unsupportedLength.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(unsupportedLength.Required, Is.EqualTo(int.MaxValue));
            Assert.That(invalidLimit.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(headerLimit.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(headerLimit.Required, Is.EqualTo(FixDefaults.SofhHeaderSize));
        });
    }

    private static void AssertEveryPrefix(byte[] frame)
    {
        SofhMessageFramer framer = new SofhMessageFramer();
        Assert.Multiple(() =>
        {
            for (int length = 0; length < frame.Length; length++)
            {
                FrameResult result = framer.ReadFrame(frame.AsSpan(0, length), new FrameOptions());
                int required = length < FixDefaults.SofhHeaderSize ? FixDefaults.SofhHeaderSize : frame.Length;

                Assert.That(result.Status, Is.EqualTo(FrameStatus.Incomplete), $"prefix {length}");
                Assert.That(result.Consumed, Is.Zero, $"prefix {length}");
                Assert.That(result.Required, Is.EqualTo(required), $"prefix {length}");
                Assert.That(result.ErrorOffset, Is.EqualTo(length), $"prefix {length}");
                Assert.That(result.ErrorCode, Is.EqualTo("incomplete"), $"prefix {length}");
            }
        });
    }

    private static void AssertComplete(FrameResult result, int length, ushort encoding)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Complete), result.ErrorCode);
            Assert.That(result.FrameOffset, Is.Zero);
            Assert.That(result.FrameLength, Is.EqualTo(length));
            Assert.That(result.Consumed, Is.EqualTo(length));
            Assert.That(result.Required, Is.EqualTo(length));
            Assert.That(result.EncodingType, Is.EqualTo(encoding));
            Assert.That(result.IsSofh, Is.True);
            Assert.That(result.ErrorCode, Is.Empty);
        });
    }

    private static void AssertUnknownEncoding(FrameResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(result.IsSofh, Is.True);
            Assert.That(result.ErrorOffset, Is.EqualTo(4));
            Assert.That(result.Required, Is.EqualTo(FixDefaults.SofhHeaderSize));
            Assert.That(result.ErrorCode, Is.EqualTo("unknown-encoding-type"));
        });
    }

    private static byte[] BuildFrame(byte[] payload, ushort encoding, bool littleEndian)
    {
        return Combine(BuildHeader(payload.Length + FixDefaults.SofhHeaderSize, encoding, littleEndian), payload);
    }

    private static byte[] BuildHeader(int length, ushort encoding, bool littleEndian)
    {
        byte[] header = new byte[FixDefaults.SofhHeaderSize];
        if (littleEndian)
        {
            header[0] = (byte)length;
            header[1] = (byte)(length >> 8);
            header[2] = (byte)(length >> 16);
            header[3] = (byte)(length >> 24);
            header[4] = (byte)encoding;
            header[5] = (byte)(encoding >> 8);
        }
        else
        {
            header[0] = (byte)(length >> 24);
            header[1] = (byte)(length >> 16);
            header[2] = (byte)(length >> 8);
            header[3] = (byte)length;
            header[4] = (byte)(encoding >> 8);
            header[5] = (byte)encoding;
        }
        return header;
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
