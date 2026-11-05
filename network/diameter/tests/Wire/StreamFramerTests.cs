using System;

namespace diameter.Tests;

public class StreamFramerTests
{
    [Test]
    public void FragmentedHeadersAndBodiesCompleteFromOwnedRemainders()
    {
        int[] splitPoints = new int[] { 1, 3, 19, 20, 21, 35 };
        int splitIndex = 0;
        while (splitIndex < splitPoints.Length)
        {
            byte[] frame = CreateFrame(1U, 5);
            int split = splitPoints[splitIndex];

            DiameterFramingResult first = StreamFramer.FrameStreamData(
                new byte[0],
                frame.AsSpan(0, split),
                false,
                1024);
            DiameterFramingResult second = StreamFramer.FrameStreamData(
                first.Remainder,
                frame.AsSpan(split),
                false,
                1024);
            frame[0] = 9;

            Assert.That(first.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(first.ConsumedCount, Is.Zero);
            Assert.That(first.Frames, Is.Empty);
            Assert.That(first.Remainder, Has.Length.EqualTo(split));
            if (split < DiameterProtocolConstants.MessageHeaderSize)
            {
                Assert.That(
                    first.RequiredCount,
                    Is.EqualTo(DiameterProtocolConstants.MessageHeaderSize));
            }
            else
            {
                Assert.That(first.RequiredCount, Is.EqualTo(36));
            }

            Assert.That(second.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(second.RequiredCount, Is.Zero);
            Assert.That(second.ConsumedCount, Is.EqualTo(36));
            Assert.That(second.Frames, Has.Count.EqualTo(1));
            Assert.That(second.Frames[0], Has.Length.EqualTo(36));
            Assert.That(second.Frames[0][0], Is.EqualTo(DiameterProtocolConstants.Version));
            Assert.That(second.Remainder, Is.Empty);
            splitIndex = splitIndex + 1;
        }
    }

    [Test]
    public void CoalescedFramesAreEmittedInOrderAndPartialTailIsRetained()
    {
        byte[] first = CreateFrame(1U, 0);
        byte[] second = CreateFrame(2U, 1);
        byte[] third = CreateFrame(3U, 5);
        byte[] input = new byte[first.Length + second.Length + 10];
        Array.Copy(first, 0, input, 0, first.Length);
        Array.Copy(second, 0, input, first.Length, second.Length);
        Array.Copy(third, 0, input, first.Length + second.Length, 10);

        DiameterFramingResult initial = StreamFramer.FrameStreamData(
            new byte[0],
            input,
            false,
            1024);
        DiameterFramingResult completed = StreamFramer.FrameStreamData(
            initial.Remainder,
            third.AsSpan(10),
            false,
            1024);

        Assert.Multiple(() =>
        {
            Assert.That(initial.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(initial.RequiredCount, Is.EqualTo(20));
            Assert.That(initial.ConsumedCount, Is.EqualTo(first.Length + second.Length));
            Assert.That(initial.Frames, Has.Count.EqualTo(2));
            Assert.That(initial.Frames[0], Is.EqualTo(first));
            Assert.That(initial.Frames[1], Is.EqualTo(second));
            Assert.That(initial.Remainder, Is.EqualTo(third.AsSpan(0, 10).ToArray()));
            Assert.That(completed.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(completed.ConsumedCount, Is.EqualTo(third.Length));
            Assert.That(completed.Frames, Has.Count.EqualTo(1));
            Assert.That(completed.Frames[0], Is.EqualTo(third));
            Assert.That(completed.Remainder, Is.Empty);
        });
    }

    [Test]
    public void EndOfStreamDistinguishesCleanAndTruncatedInput()
    {
        byte[] frame = CreateFrame(1U, 5);

        DiameterFramingResult empty = StreamFramer.FrameStreamData(
            new byte[0],
            new byte[0],
            true,
            1024);
        DiameterFramingResult clean = StreamFramer.FrameStreamData(
            new byte[0],
            frame,
            true,
            1024);
        DiameterFramingResult header = StreamFramer.FrameStreamData(
            new byte[0],
            frame.AsSpan(0, 10),
            true,
            1024);
        DiameterFramingResult body = StreamFramer.FrameStreamData(
            new byte[0],
            frame.AsSpan(0, 25),
            true,
            1024);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(empty.EndOfStream, Is.True);
            Assert.That(empty.CleanEndOfStream, Is.True);
            Assert.That(clean.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(clean.EndOfStream, Is.True);
            Assert.That(clean.CleanEndOfStream, Is.True);
            Assert.That(clean.Frames, Has.Count.EqualTo(1));
            AssertIncompleteEnd(header, 20, "truncated-frame-header");
            AssertIncompleteEnd(body, frame.Length, "truncated-frame");
        });
    }

    [Test]
    public void ConfiguredAndProtocolFrameLimitsAreRejectedFromTheHeader()
    {
        byte[] frame = CreateFrame(1U, 5);
        byte[] protocolOversized = CreateHeaderWithLength(16777215);

        DiameterFramingResult configured = StreamFramer.FrameStreamData(
            new byte[0],
            frame.AsSpan(0, 20),
            false,
            32);
        DiameterFramingResult exact = StreamFramer.FrameStreamData(
            new byte[0],
            frame,
            false,
            frame.Length);
        DiameterFramingResult protocol = StreamFramer.FrameStreamData(
            new byte[0],
            protocolOversized,
            false,
            DiameterProtocolConstants.MaximumMessageLength);

        Assert.Multiple(() =>
        {
            AssertFailure(
                configured,
                DiameterCodecStatus.LimitExceeded,
                1,
                "configured-frame-size-limit");
            Assert.That(configured.RequiredCount, Is.EqualTo(frame.Length));
            Assert.That(configured.Remainder, Has.Length.EqualTo(20));
            Assert.That(exact.Status, Is.EqualTo(DiameterCodecStatus.Success));
            AssertFailure(
                protocol,
                DiameterCodecStatus.LimitExceeded,
                1,
                "message-length-limit");
            Assert.That(protocol.RequiredCount, Is.EqualTo(16777215));
        });
    }

    [Test]
    public void StructuralHeaderErrorsRetainTheUnconsumedBytes()
    {
        byte[] unsupported = CreateHeaderWithLength(20);
        unsupported[0] = 2;
        byte[] shortLength = CreateHeaderWithLength(19);
        byte[] misaligned = CreateHeaderWithLength(21);

        DiameterFramingResult version = StreamFramer.FrameStreamData(
            new byte[0],
            unsupported,
            false,
            1024);
        DiameterFramingResult shortResult = StreamFramer.FrameStreamData(
            new byte[0],
            shortLength,
            false,
            1024);
        DiameterFramingResult alignment = StreamFramer.FrameStreamData(
            new byte[0],
            misaligned,
            false,
            1024);

        Assert.Multiple(() =>
        {
            AssertFailure(version, DiameterCodecStatus.Invalid, 0, "unsupported-version");
            Assert.That(version.Remainder, Is.EqualTo(unsupported));
            AssertFailure(
                shortResult,
                DiameterCodecStatus.Invalid,
                1,
                "invalid-message-length");
            AssertFailure(
                alignment,
                DiameterCodecStatus.Invalid,
                1,
                "invalid-message-length");
        });
    }

    [Test]
    public void CompleteFramesBeforeAnErrorAreReturnedForRecovery()
    {
        byte[] valid = CreateFrame(1U, 0);
        byte[] invalid = CreateHeaderWithLength(20);
        invalid[0] = 2;
        byte[] input = new byte[valid.Length + invalid.Length];
        Array.Copy(valid, 0, input, 0, valid.Length);
        Array.Copy(invalid, 0, input, valid.Length, invalid.Length);

        DiameterFramingResult result = StreamFramer.FrameStreamData(
            new byte[0],
            input,
            false,
            1024);
        DiameterFramingResult recovery = StreamFramer.FrameStreamData(
            new byte[0],
            valid,
            false,
            1024);

        Assert.Multiple(() =>
        {
            AssertFailure(
                result,
                DiameterCodecStatus.Invalid,
                valid.Length,
                "unsupported-version");
            Assert.That(result.ConsumedCount, Is.EqualTo(valid.Length));
            Assert.That(result.Frames, Has.Count.EqualTo(1));
            Assert.That(result.Frames[0], Is.EqualTo(valid));
            Assert.That(result.Remainder, Is.EqualTo(invalid));
            Assert.That(recovery.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(recovery.Frames, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void FramingDoesNotApplyStrictMessageFlagPolicy()
    {
        byte[] frame = CreateFrame(1U, 0);
        frame[4] = 1;

        DiameterFramingResult result = StreamFramer.FrameStreamData(
            new byte[0],
            frame,
            false,
            1024);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.Frames, Has.Count.EqualTo(1));
            Assert.That(result.Frames[0][4], Is.EqualTo(1));
        });
    }

    [Test]
    public void EmptyReadsAreNoOpsAndInvalidLimitsAreReported()
    {
        DiameterFramingResult empty = StreamFramer.FrameStreamData(
            new byte[0],
            new byte[0],
            false,
            1024);
        DiameterFramingResult invalid = StreamFramer.FrameStreamData(
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5 },
            false,
            21);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(empty.RequiredCount, Is.Zero);
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(empty.Frames, Is.Empty);
            Assert.That(empty.Remainder, Is.Empty);
            AssertFailure(
                invalid,
                DiameterCodecStatus.Invalid,
                0,
                "invalid-frame-limit");
        });
    }

    [Test]
    public void RetainedAndEmittedBytesAreOwnedByTheResult()
    {
        byte[] frame = CreateFrame(1U, 5);
        byte[] prefix = new byte[10];
        Array.Copy(frame, prefix, prefix.Length);
        DiameterFramingResult partial = StreamFramer.FrameStreamData(
            new byte[0],
            prefix,
            false,
            1024);
        prefix[0] = 9;

        DiameterFramingResult complete = StreamFramer.FrameStreamData(
            partial.Remainder,
            frame.AsSpan(10),
            false,
            1024);
        partial.Remainder[0] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(complete.Frames[0][0], Is.EqualTo(DiameterProtocolConstants.Version));
            Assert.That(complete.Frames[0], Is.EqualTo(frame));
        });
    }

    private static byte[] CreateFrame(uint identifier, int payloadLength)
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

        DiameterEncodeResult result = MessageEncoder.EncodeMessageOwned(message);
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return result.Data;
    }

    private static byte[] CreateHeaderWithLength(int length)
    {
        byte[] header = new byte[DiameterProtocolConstants.MessageHeaderSize];
        header[0] = DiameterProtocolConstants.Version;
        header[1] = (byte)(length >> 16);
        header[2] = (byte)((length >> 8) & 255);
        header[3] = (byte)(length & 255);
        return header;
    }

    private static void AssertIncompleteEnd(
        DiameterFramingResult result,
        int requiredCount,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.EndOfStream, Is.True);
        Assert.That(result.CleanEndOfStream, Is.False);
        Assert.That(result.Remainder, Is.Not.Empty);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Information));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }

    private static void AssertFailure(
        DiameterFramingResult result,
        DiameterCodecStatus status,
        int offset,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.CleanEndOfStream, Is.False);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Error));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }
}
