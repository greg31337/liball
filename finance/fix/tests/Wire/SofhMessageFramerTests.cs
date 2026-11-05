using System;
using fix;

namespace fix.Tests;

[TestFixture]
public class SofhMessageFramerTests
{
    [Test]
    public void ScannerReadsVersion10BigEndianAndVersion11LittleEndianHeaders()
    {
        byte[] bigEndian = BuildFrame([1, 2, 3, 4], FixEncodingTypes.SbeVersion10LittleEndian, false);
        byte[] littleEndian = BuildFrame([5, 6, 7], FixEncodingTypes.SbeVersion20LittleEndian, true);

        FrameResult big = SofhFrameScanner.ScanFrame(bigEndian, new FrameOptions());
        FrameResult little = SofhFrameScanner.ScanFrame(littleEndian, new FrameOptions());

        Assert.Multiple(() =>
        {
            AssertComplete(big, bigEndian.Length, FixEncodingTypes.SbeVersion10LittleEndian);
            AssertComplete(little, littleEndian.Length, FixEncodingTypes.SbeVersion20LittleEndian);
        });
    }

    [Test]
    public void TagValueEncodingAmbiguityIsResolvedByTheDeclaredSizeInEitherByteOrder()
    {
        byte[] bigEndian = BuildFrame([65], FixEncodingTypes.TagValue, false);
        byte[] littleEndian = BuildFrame([66, 67], FixEncodingTypes.TagValue, true);

        FrameResult big = SofhFrameScanner.ScanFrame(bigEndian, new FrameOptions());
        FrameResult little = SofhFrameScanner.ScanFrame(littleEndian, new FrameOptions());

        Assert.Multiple(() =>
        {
            AssertComplete(big, bigEndian.Length, FixEncodingTypes.TagValue);
            AssertComplete(little, littleEndian.Length, FixEncodingTypes.TagValue);
        });
    }

    [Test]
    public void EveryPrefixReportsTheHeaderOrExactFrameRequirement()
    {
        byte[] bigEndian = BuildFrame([1, 2, 3, 4, 5], FixEncodingTypes.SbeVersion10BigEndian, false);
        byte[] littleEndian = BuildFrame([6, 7, 8, 9], FixEncodingTypes.SbeVersion10LittleEndian, true);

        AssertPrefixes(bigEndian);
        AssertPrefixes(littleEndian);
    }

    [Test]
    public void PublicFramerReturnsOnlyTheFirstConcatenatedFrame()
    {
        byte[] first = BuildFrame([1, 2, 3], FixEncodingTypes.GoogleProtocolBuffersVersion10, false);
        byte[] second = BuildFrame([4, 5], FixEncodingTypes.Json, false);
        byte[] source = Combine(first, second);
        IMessageFramer framer = new SofhMessageFramer();

        FrameResult result = framer.ReadFrame(source, new FrameOptions());

        Assert.Multiple(() =>
        {
            AssertComplete(result, first.Length, FixEncodingTypes.GoogleProtocolBuffersVersion10);
            Assert.That(result.Consumed, Is.EqualTo(first.Length));
            Assert.That(result.Required, Is.EqualTo(first.Length));
            Assert.That(source.AsSpan(result.Consumed, second.Length).ToArray(), Is.EqualTo(second));
        });
    }

    [Test]
    public void RegisteredPrivateEncodingsAreAcceptedAndUnknownEncodingsAreRejected()
    {
        byte[] privateBig = BuildFrame([1], FixEncodingTypes.PrivateUseMinimum, false);
        byte[] privateLittle = BuildFrame([2], FixEncodingTypes.PrivateUseMaximum, true);
        byte[] fast = BuildFrame([3], FixEncodingTypes.FastMaximum, false);
        byte[] unknown = BuildFrame([4], 0x1234, false);

        FrameResult firstPrivate = SofhFrameScanner.ScanFrame(privateBig, new FrameOptions());
        FrameResult lastPrivate = SofhFrameScanner.ScanFrame(privateLittle, new FrameOptions());
        FrameResult fastResult = SofhFrameScanner.ScanFrame(fast, new FrameOptions());
        FrameResult unknownResult = SofhFrameScanner.ScanFrame(unknown, new FrameOptions());

        Assert.Multiple(() =>
        {
            AssertComplete(firstPrivate, privateBig.Length, FixEncodingTypes.PrivateUseMinimum);
            AssertComplete(lastPrivate, privateLittle.Length, FixEncodingTypes.PrivateUseMaximum);
            AssertComplete(fastResult, fast.Length, FixEncodingTypes.FastMaximum);
            Assert.That(unknownResult.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(unknownResult.IsSofh, Is.True);
            Assert.That(unknownResult.EncodingType, Is.EqualTo(0x1234));
            Assert.That(unknownResult.ErrorOffset, Is.EqualTo(4));
            Assert.That(unknownResult.ErrorCode, Is.EqualTo("unknown-encoding-type"));
        });
    }

    [Test]
    public void InvalidAndOversizedLengthsFailAsSoonAsTheHeaderIsAvailable()
    {
        byte[] tooSmall = BuildHeader(5, FixEncodingTypes.SbeVersion10BigEndian, false);
        byte[] oversized = BuildHeader(100, FixEncodingTypes.SbeVersion10BigEndian, false);
        byte[] beyondSignedRange = [255, 255, 255, 255, 91, 224];

        FrameResult invalid = SofhFrameScanner.ScanFrame(tooSmall, new FrameOptions());
        FrameResult limited = SofhFrameScanner.ScanFrame(oversized, new FrameOptions { MaxFrameSize = 99 });
        FrameResult signedLimit = SofhFrameScanner.ScanFrame(beyondSignedRange, new FrameOptions { MaxFrameSize = int.MaxValue });
        FrameResult negativeLimit = SofhFrameScanner.ScanFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { MaxFrameSize = -1 });
        FrameResult smallerThanHeader = SofhFrameScanner.ScanFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { MaxFrameSize = 5 });

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(invalid.ErrorCode, Is.EqualTo("invalid-message-length"));
            Assert.That(invalid.Required, Is.EqualTo(FixDefaults.SofhHeaderSize));
            Assert.That(limited.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(limited.Required, Is.EqualTo(100));
            Assert.That(signedLimit.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(signedLimit.Required, Is.EqualTo(int.MaxValue));
            Assert.That(negativeLimit.Status, Is.EqualTo(FrameStatus.Invalid));
            Assert.That(negativeLimit.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(smallerThanHeader.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(smallerThanHeader.Required, Is.EqualTo(FixDefaults.SofhHeaderSize));
        });
    }

    [Test]
    public void RecoveryFindsCompleteAndPartialFramesAndRetainsPossibleHeaderBytes()
    {
        byte[] frame = BuildFrame([1, 2, 3, 4, 5], FixEncodingTypes.SbeVersion10LittleEndian, false);
        byte[] junk = [255, 255, 255];
        byte[] completeSource = Combine(junk, frame);
        byte[] partialSource = Combine(junk, frame.AsSpan(0, frame.Length - 2).ToArray());
        SofhMessageFramer framer = new SofhMessageFramer();
        FrameOptions options = new FrameOptions { RecoverFromJunk = true };

        FrameResult complete = framer.ReadFrame(completeSource, options);
        FrameResult partial = framer.ReadFrame(partialSource, options);
        FrameResult noFrame = framer.ReadFrame(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, options);

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(FrameStatus.Recovered));
            Assert.That(complete.FrameOffset, Is.EqualTo(junk.Length));
            Assert.That(complete.FrameLength, Is.EqualTo(frame.Length));
            Assert.That(complete.Consumed, Is.EqualTo(completeSource.Length));
            Assert.That(complete.Required, Is.EqualTo(completeSource.Length));
            Assert.That(partial.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(partial.FrameOffset, Is.EqualTo(junk.Length));
            Assert.That(partial.Consumed, Is.EqualTo(junk.Length));
            Assert.That(partial.Required, Is.EqualTo(junk.Length + frame.Length));
            Assert.That(noFrame.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(noFrame.FrameOffset, Is.EqualTo(3));
            Assert.That(noFrame.Consumed, Is.EqualTo(3));
            Assert.That(noFrame.Required, Is.EqualTo(9));
        });
    }

    [Test]
    public void RecoveryPreservesInvalidLimitsOversizedHeadersAndShortPrefixes()
    {
        SofhMessageFramer framer = new SofhMessageFramer();
        FrameResult invalidLimit = framer.ReadFrame(ReadOnlySpan<byte>.Empty, new FrameOptions { RecoverFromJunk = true, MaxFrameSize = 5 });
        byte[] oversizedHeader = BuildHeader(100, FixEncodingTypes.SbeVersion10BigEndian, false);
        FrameResult oversized = framer.ReadFrame(oversizedHeader, new FrameOptions { RecoverFromJunk = true, MaxFrameSize = 99 });
        FrameResult shortPrefix = framer.ReadFrame(new byte[] { 1, 2, 3 }, new FrameOptions { RecoverFromJunk = true });

        Assert.Multiple(() =>
        {
            Assert.That(invalidLimit.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(oversized.Status, Is.EqualTo(FrameStatus.LimitExceeded));
            Assert.That(oversized.EncodingType, Is.EqualTo(FixEncodingTypes.SbeVersion10BigEndian));
            Assert.That(shortPrefix.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(shortPrefix.FrameOffset, Is.Zero);
            Assert.That(shortPrefix.Consumed, Is.Zero);
            Assert.That(shortPrefix.Required, Is.EqualTo(FixDefaults.SofhHeaderSize));
        });
    }

    private static void AssertPrefixes(byte[] frame)
    {
        Assert.Multiple(() =>
        {
            for (int length = 0; length < frame.Length; length++)
            {
                FrameResult result = SofhFrameScanner.ScanFrame(frame.AsSpan(0, length), new FrameOptions());
                Assert.That(result.Status, Is.EqualTo(FrameStatus.Incomplete), length.ToString());
                Assert.That(result.Consumed, Is.Zero, length.ToString());
                Assert.That(result.ErrorOffset, Is.EqualTo(length), length.ToString());
                Assert.That(result.ErrorCode, Is.EqualTo("incomplete"), length.ToString());
                int expected = length < FixDefaults.SofhHeaderSize ? FixDefaults.SofhHeaderSize : frame.Length;
                Assert.That(result.Required, Is.EqualTo(expected), length.ToString());
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
            Assert.That(result.IsSofh, Is.True);
            Assert.That(result.EncodingType, Is.EqualTo(encoding));
            Assert.That(result.ErrorCode, Is.Empty);
        });
    }

    private static byte[] BuildFrame(byte[] payload, ushort encoding, bool littleEndian)
    {
        byte[] header = BuildHeader(payload.Length + FixDefaults.SofhHeaderSize, encoding, littleEndian);
        return Combine(header, payload);
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
