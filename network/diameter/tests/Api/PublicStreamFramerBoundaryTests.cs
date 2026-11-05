using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class PublicStreamFramerBoundaryTests
{
    [Test]
    public void EverySingleFrameSplitPointProducesExactlyOneOwnedFrame()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        byte[] frame = CreateFrame(codec, 1U, 13);
        int split = 0;
        while (split <= frame.Length)
        {
            DiameterFramingResult first = codec.FrameMessageStream(
                new byte[0],
                new ReadOnlySpan<byte>(frame, 0, split),
                false,
                1024);
            DiameterFramingResult second = codec.FrameMessageStream(
                first.Remainder,
                new ReadOnlySpan<byte>(frame, split, frame.Length - split),
                false,
                1024);

            Assert.That(
                first.Frames.Count + second.Frames.Count,
                Is.EqualTo(1),
                $"split {split}");
            byte[] emitted = first.Frames.Count == 1 ? first.Frames[0] : second.Frames[0];
            Assert.That(emitted, Is.EqualTo(frame), $"split {split}");
            Assert.That(emitted, Is.Not.SameAs(frame), $"split {split}");
            Assert.That(second.Status, Is.EqualTo(DiameterCodecStatus.Success), $"split {split}");
            Assert.That(second.Remainder, Is.Empty, $"split {split}");

            if (split == 0)
            {
                Assert.That(first.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(first.Remainder, Is.Empty);
            }
            else if (split < frame.Length)
            {
                Assert.That(
                    first.Status,
                    Is.EqualTo(DiameterCodecStatus.Incomplete),
                    $"split {split}");
                Assert.That(first.Remainder, Has.Length.EqualTo(split));
                Assert.That(first.ConsumedCount, Is.Zero);
                Assert.That(first.Issues, Has.Count.EqualTo(1));
                if (split < DiameterProtocolConstants.MessageHeaderSize)
                {
                    Assert.That(first.RequiredCount, Is.EqualTo(20));
                    Assert.That(first.Issues[0].Code, Is.EqualTo("incomplete-frame-header"));
                }
                else
                {
                    Assert.That(first.RequiredCount, Is.EqualTo(frame.Length));
                    Assert.That(first.Issues[0].Code, Is.EqualTo("incomplete-frame"));
                }
            }
            else
            {
                Assert.That(first.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(first.Frames, Has.Count.EqualTo(1));
                Assert.That(second.Frames, Is.Empty);
            }

            split = split + 1;
        }
    }

    [Test]
    public void OneByteReadsAndInterspersedEmptyReadsRetainAllDataUntilCompletion()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        byte[] frame = CreateFrame(codec, 2U, 9);
        byte[] remainder = new byte[0];
        int index = 0;
        while (index < frame.Length)
        {
            DiameterFramingResult empty = codec.FrameMessageStream(
                remainder,
                new byte[0],
                false,
                1024);
            Assert.That(empty.Remainder, Is.EqualTo(remainder), $"empty read {index}");
            Assert.That(empty.Remainder, Is.Not.SameAs(remainder), $"empty ownership {index}");

            DiameterFramingResult next = codec.FrameMessageStream(
                empty.Remainder,
                new ReadOnlySpan<byte>(frame, index, 1),
                false,
                1024);
            if (index < frame.Length - 1)
            {
                Assert.That(next.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
                Assert.That(next.Frames, Is.Empty);
                Assert.That(next.Remainder, Has.Length.EqualTo(index + 1));
            }
            else
            {
                Assert.That(next.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(next.Frames, Has.Count.EqualTo(1));
                Assert.That(next.Frames[0], Is.EqualTo(frame));
                Assert.That(next.Remainder, Is.Empty);
            }

            remainder = next.Remainder;
            index = index + 1;
        }
    }

    [Test]
    public void CoalescedReadsEmitEveryFrameInOrderForMultipleCounts()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        int frameCount = 1;
        while (frameCount <= 8)
        {
            List<byte[]> frames = new List<byte[]>();
            int totalLength = 0;
            int index = 0;
            while (index < frameCount)
            {
                byte[] frame = CreateFrame(codec, (uint)(index + 1), index % 7);
                frames.Add(frame);
                totalLength = totalLength + frame.Length;
                index = index + 1;
            }

            byte[] input = new byte[totalLength];
            int offset = 0;
            index = 0;
            while (index < frames.Count)
            {
                Array.Copy(frames[index], 0, input, offset, frames[index].Length);
                offset = offset + frames[index].Length;
                index = index + 1;
            }

            DiameterFramingResult result = codec.FrameMessageStream(
                new byte[0],
                input,
                false,
                4096);

            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.Zero);
            Assert.That(result.ConsumedCount, Is.EqualTo(input.Length));
            Assert.That(result.Frames, Has.Count.EqualTo(frameCount));
            Assert.That(result.Remainder, Is.Empty);
            index = 0;
            while (index < frames.Count)
            {
                Assert.That(result.Frames[index], Is.EqualTo(frames[index]));
                Assert.That(result.Frames[index], Is.Not.SameAs(frames[index]));
                index = index + 1;
            }

            frameCount = frameCount + 1;
        }
    }

    [Test]
    public void EveryEndOfStreamPositionIsClassifiedAsCleanOrTruncated()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        byte[] frame = CreateFrame(codec, 3U, 17);
        int length = 0;
        while (length <= frame.Length)
        {
            DiameterFramingResult result = codec.FrameMessageStream(
                new byte[0],
                new ReadOnlySpan<byte>(frame, 0, length),
                true,
                1024);

            Assert.That(result.EndOfStream, Is.True, $"length {length}");
            if (length == 0)
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.CleanEndOfStream, Is.True);
                Assert.That(result.Frames, Is.Empty);
                Assert.That(result.Remainder, Is.Empty);
            }
            else if (length < DiameterProtocolConstants.MessageHeaderSize)
            {
                AssertTruncated(result, 20, "truncated-frame-header", length);
            }
            else if (length < frame.Length)
            {
                AssertTruncated(result, frame.Length, "truncated-frame", length);
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.CleanEndOfStream, Is.True);
                Assert.That(result.Frames, Has.Count.EqualTo(1));
                Assert.That(result.Frames[0], Is.EqualTo(frame));
                Assert.That(result.Remainder, Is.Empty);
            }

            length = length + 1;
        }
    }

    [Test]
    public void EndOfStreamReturnsCompleteFramesBeforeATruncatedTail()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        byte[] first = CreateFrame(codec, 4U, 0);
        byte[] second = CreateFrame(codec, 5U, 5);
        int tailLength = 1;
        while (tailLength < second.Length)
        {
            byte[] input = new byte[first.Length + tailLength];
            Array.Copy(first, 0, input, 0, first.Length);
            Array.Copy(second, 0, input, first.Length, tailLength);
            DiameterFramingResult result = codec.FrameMessageStream(
                new byte[0],
                input,
                true,
                1024);

            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(result.EndOfStream, Is.True);
            Assert.That(result.CleanEndOfStream, Is.False);
            Assert.That(result.ConsumedCount, Is.EqualTo(first.Length));
            Assert.That(result.Frames, Has.Count.EqualTo(1));
            Assert.That(result.Frames[0], Is.EqualTo(first));
            Assert.That(result.Remainder, Is.EqualTo(second.AsSpan(0, tailLength).ToArray()));
            Assert.That(
                result.Issues[0].Code,
                Is.EqualTo(tailLength < 20 ? "truncated-frame-header" : "truncated-frame"));
            tailLength = tailLength + 1;
        }
    }

    [Test]
    public void ConfiguredAndProtocolOversizedDeclarationsFailWithoutBodyAllocation()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        int configuredLength = 68;
        byte[] configuredHeader = CreateHeader(configuredLength);
        DiameterFramingResult configured = codec.FrameMessageStream(
            new byte[0],
            configuredHeader,
            false,
            64);
        DiameterFramingResult exact = codec.FrameMessageStream(
            new byte[0],
            configuredHeader,
            false,
            configuredLength);

        AssertLimitFailure(
            configured,
            configuredLength,
            "configured-frame-size-limit");
        Assert.That(configured.Remainder, Is.EqualTo(configuredHeader));
        Assert.That(exact.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
        Assert.That(exact.RequiredCount, Is.EqualTo(configuredLength));

        int protocolLength = DiameterProtocolConstants.MaximumMessageLength + 1;
        while (protocolLength <= 16777215)
        {
            byte[] header = CreateHeader(protocolLength);
            DiameterFramingResult result = codec.FrameMessageStream(
                new byte[0],
                header,
                false,
                DiameterProtocolConstants.MaximumMessageLength);
            AssertLimitFailure(result, protocolLength, "message-length-limit");
            Assert.That(result.Remainder, Is.EqualTo(header));
            protocolLength = protocolLength + 1;
        }
    }

    [Test]
    public void CallerCanRecoverValidFramesAfterEachStructuralHeaderError()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        byte[] validBefore = CreateFrame(codec, 6U, 0);
        byte[] validAfter = CreateFrame(codec, 7U, 0);
        byte[][] invalidHeaders = new byte[][]
        {
            CreateInvalidVersionHeader(),
            CreateHeader(19),
            CreateHeader(21)
        };
        string[] codes = new string[]
        {
            "unsupported-version",
            "invalid-message-length",
            "invalid-message-length"
        };

        int index = 0;
        while (index < invalidHeaders.Length)
        {
            byte[] input = Combine(validBefore, invalidHeaders[index], validAfter);
            DiameterFramingResult failed = codec.FrameMessageStream(
                new byte[0],
                input,
                false,
                1024);

            Assert.That(failed.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(failed.ConsumedCount, Is.EqualTo(validBefore.Length));
            Assert.That(failed.Frames, Has.Count.EqualTo(1));
            Assert.That(failed.Frames[0], Is.EqualTo(validBefore));
            Assert.That(failed.Remainder, Has.Length.EqualTo(20 + validAfter.Length));
            Assert.That(failed.Issues, Has.Count.EqualTo(1));
            Assert.That(failed.Issues[0].Offset, Is.EqualTo(validBefore.Length +
                (codes[index] == "unsupported-version" ? 0 : 1)));
            Assert.That(failed.Issues[0].Code, Is.EqualTo(codes[index]));

            DiameterFramingResult recovered = codec.FrameMessageStream(
                new byte[0],
                new ReadOnlySpan<byte>(failed.Remainder, 20, validAfter.Length),
                false,
                1024);
            Assert.That(recovered.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(recovered.Frames, Has.Count.EqualTo(1));
            Assert.That(recovered.Frames[0], Is.EqualTo(validAfter));
            index = index + 1;
        }
    }

    private static byte[] CreateFrame(
        IDiameterMessageCodec codec,
        uint identifier,
        int payloadLength)
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.HopByHopIdentifier = identifier;
        message.EndToEndIdentifier = identifier;
        if (payloadLength > 0)
        {
            DiameterAvp avp = new DiameterAvp();
            avp.Code = DiameterAvpCodes.UserName;
            avp.Flags = DiameterAvpFlags.Mandatory;
            avp.Data = new byte[payloadLength];
            int index = 0;
            while (index < payloadLength)
            {
                avp.Data[index] = (byte)(index + 1);
                index = index + 1;
            }

            message.Avps.Add(avp);
        }

        DiameterEncodeResult result = codec.EncodeMessageToArray(message);
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return result.Data;
    }

    private static byte[] CreateHeader(int length)
    {
        byte[] header = new byte[DiameterProtocolConstants.MessageHeaderSize];
        header[0] = DiameterProtocolConstants.Version;
        header[1] = (byte)(length >> 16);
        header[2] = (byte)(length >> 8);
        header[3] = (byte)length;
        return header;
    }

    private static byte[] CreateInvalidVersionHeader()
    {
        byte[] header = CreateHeader(20);
        header[0] = 2;
        return header;
    }

    private static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        byte[] result = new byte[first.Length + second.Length + third.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        Array.Copy(third, 0, result, first.Length + second.Length, third.Length);
        return result;
    }

    private static void AssertTruncated(
        DiameterFramingResult result,
        int requiredCount,
        string code,
        int retainedCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.EndOfStream, Is.True);
        Assert.That(result.CleanEndOfStream, Is.False);
        Assert.That(result.Frames, Is.Empty);
        Assert.That(result.Remainder, Has.Length.EqualTo(retainedCount));
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Information));
        Assert.That(result.Issues[0].Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(retainedCount));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }

    private static void AssertLimitFailure(
        DiameterFramingResult result,
        int requiredCount,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.LimitExceeded));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Frames, Is.Empty);
        Assert.That(result.CleanEndOfStream, Is.False);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Error));
        Assert.That(result.Issues[0].Status, Is.EqualTo(DiameterCodecStatus.LimitExceeded));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(1));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }
}
