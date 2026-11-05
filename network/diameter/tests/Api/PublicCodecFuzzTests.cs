using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class PublicCodecFuzzTests
{
    [Test]
    public void SeededRandomInputsNeverThrowOverconsumeOrRetainUnsuppliedBytes()
    {
        IDiameterMessageCodec messageCodec = new DiameterMessageCodec();
        IDiameterAvpCodec avpCodec = new DiameterAvpCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumMessageSize = 1024;
        limits.MaximumAvpCount = 64;
        limits.MaximumCodecIssueCount = 16;
        limits.MaximumGroupedDepth = 4;
        limits.MaximumGroupedAvpCount = 32;
        limits.MaximumGroupedBytes = 1024;
        List<DiameterAvp> groupedTypes = new List<DiameterAvp>();
        DiameterAvp groupedType = new DiameterAvp();
        groupedType.Code = 1000U;
        groupedTypes.Add(groupedType);
        Random random = new Random(6733);

        int iteration = 0;
        while (iteration < 2048)
        {
            int length = random.Next(0, 257);
            byte[] source = new byte[length];
            random.NextBytes(source);

            DiameterDecodeResult strictMessage = messageCodec.DecodeMessage(
                source,
                false,
                limits);
            DiameterDecodeResult permissiveMessage = messageCodec.DecodeMessage(
                source,
                true,
                limits);
            AssertMessageDecodeBounds(strictMessage, source.Length);
            AssertMessageDecodeBounds(permissiveMessage, source.Length);

            DiameterAvpDecodeResult strictAvp = avpCodec.DecodeAvp(source, false);
            DiameterAvpDecodeResult permissiveAvp = avpCodec.DecodeAvp(source, true);
            AssertAvpDecodeBounds(strictAvp, source.Length);
            AssertAvpDecodeBounds(permissiveAvp, source.Length);

            AssertValueDecodeBounds(avpCodec.DecodeInteger32(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeInteger64(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeUnsigned32(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeUnsigned64(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeFloat32(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeFloat64(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeEnumerated(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeOctetString(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeUtf8String(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeAddress(source), source.Length);
            AssertValueDecodeBounds(
                avpCodec.DecodeTime(source, DateTimeOffset.UnixEpoch),
                source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeDiameterIdentity(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeDiameterUri(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeIpFilterRule(source), source.Length);
            AssertValueDecodeBounds(avpCodec.DecodeQosFilterRule(source), source.Length);
            AssertValueDecodeBounds(
                avpCodec.DecodeGrouped(source, groupedTypes, limits),
                source.Length);

            int retainedLength = random.Next(0, 65);
            int inputLength = random.Next(0, 193);
            byte[] retained = new byte[retainedLength];
            byte[] input = new byte[inputLength];
            random.NextBytes(retained);
            random.NextBytes(input);
            DiameterFramingResult framing = messageCodec.FrameMessageStream(
                retained,
                input,
                false,
                256);
            AssertFramingBounds(framing, retained.Length + input.Length, 256);
            iteration = iteration + 1;
        }
    }

    [Test]
    public void EverySingleByteMutationOfValidMessagesAndAvpsRemainsBounded()
    {
        IDiameterMessageCodec messageCodec = new DiameterMessageCodec();
        IDiameterAvpCodec avpCodec = new DiameterAvpCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumMessageSize = 1024;
        DiameterMessage message = CreateTwoAvpMessage();
        byte[] validMessage = messageCodec.EncodeMessageToArray(message).Data;
        byte[] validAvp = avpCodec.EncodeAvpToArray(message.Avps[0]).Data;
        int[] masks = new int[] { 1, 2, 4, 8, 16, 32, 64, 128, 255 };

        int byteIndex = 0;
        while (byteIndex < validMessage.Length)
        {
            int maskIndex = 0;
            while (maskIndex < masks.Length)
            {
                byte[] mutated = Copy(validMessage);
                mutated[byteIndex] = (byte)(mutated[byteIndex] ^ masks[maskIndex]);
                DiameterDecodeResult strict = messageCodec.DecodeMessage(
                    mutated,
                    false,
                    limits);
                DiameterDecodeResult permissive = messageCodec.DecodeMessage(
                    mutated,
                    true,
                    limits);
                DiameterFramingResult framing = messageCodec.FrameMessageStream(
                    new byte[0],
                    mutated,
                    false,
                    1024);
                AssertMessageDecodeBounds(strict, mutated.Length);
                AssertMessageDecodeBounds(permissive, mutated.Length);
                AssertFramingBounds(framing, mutated.Length, 1024);
                maskIndex = maskIndex + 1;
            }

            byteIndex = byteIndex + 1;
        }

        byteIndex = 0;
        while (byteIndex < validAvp.Length)
        {
            int maskIndex = 0;
            while (maskIndex < masks.Length)
            {
                byte[] mutated = Copy(validAvp);
                mutated[byteIndex] = (byte)(mutated[byteIndex] ^ masks[maskIndex]);
                AssertAvpDecodeBounds(avpCodec.DecodeAvp(mutated, false), mutated.Length);
                AssertAvpDecodeBounds(avpCodec.DecodeAvp(mutated, true), mutated.Length);
                maskIndex = maskIndex + 1;
            }

            byteIndex = byteIndex + 1;
        }
    }

    [Test]
    public void MessageDecoderAcceptsOnlyCompleteAvpBoundariesAsDeclaredLengths()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        byte[] encoded = codec.EncodeMessageToArray(CreateTwoAvpMessage()).Data;
        int declaredLength = DiameterProtocolConstants.MessageHeaderSize;
        while (declaredLength <= encoded.Length)
        {
            byte[] mutated = Copy(encoded);
            WriteUInt24(mutated, 1, declaredLength);
            DiameterDecodeResult result = codec.DecodeMessage(mutated, false, limits);
            bool completeBoundary = declaredLength == 20 ||
                declaredLength == 32 ||
                declaredLength == 44;
            if (completeBoundary)
            {
                Assert.That(
                    result.Status,
                    Is.EqualTo(DiameterCodecStatus.Success),
                    $"declared message length {declaredLength}");
                Assert.That(result.ConsumedCount, Is.EqualTo(declaredLength));
            }
            else
            {
                Assert.That(
                    result.Status,
                    Is.EqualTo(DiameterCodecStatus.Invalid),
                    $"declared message length {declaredLength}");
                Assert.That(result.Issues, Has.Count.EqualTo(1));
                Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-message-avp-sequence"));
            }

            declaredLength = declaredLength + 4;
        }

        declaredLength = encoded.Length + 4;
        while (declaredLength <= encoded.Length + 20)
        {
            byte[] mutated = Copy(encoded);
            WriteUInt24(mutated, 1, declaredLength);
            DiameterDecodeResult result = codec.DecodeMessage(mutated, false, limits);
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(result.RequiredCount, Is.EqualTo(declaredLength));
            Assert.That(result.ConsumedCount, Is.Zero);
            declaredLength = declaredLength + 4;
        }
    }

    [Test]
    public void MessageDecoderRejectsEveryInconsistentFirstAvpLengthMutation()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        byte[] encoded = codec.EncodeMessageToArray(CreateTwoAvpMessage()).Data;
        int declaredLength = 0;
        while (declaredLength <= 20)
        {
            byte[] mutated = Copy(encoded);
            WriteUInt24(mutated, DiameterProtocolConstants.MessageHeaderSize + 5, declaredLength);
            DiameterDecodeResult result = codec.DecodeMessage(mutated, false, limits);
            if (declaredLength == 12)
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.Message!.Avps, Has.Count.EqualTo(2));
            }
            else
            {
                Assert.That(
                    result.Status,
                    Is.EqualTo(DiameterCodecStatus.Invalid),
                    $"declared AVP length {declaredLength}");
                Assert.That(result.Message, Is.Null);
                Assert.That(result.Issues, Has.Count.EqualTo(1));
            }

            declaredLength = declaredLength + 1;
        }
    }

    [Test]
    public void FramingRetentionIsBoundedByActualInputAndConfiguredFrameSize()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        int maximumFrameSize = 256;
        byte[] frame = new byte[maximumFrameSize];
        frame[0] = DiameterProtocolConstants.Version;
        WriteUInt24(frame, 1, maximumFrameSize);
        byte[] remainder = new byte[0];
        int index = 0;
        while (index < frame.Length)
        {
            DiameterFramingResult result = codec.FrameMessageStream(
                remainder,
                new ReadOnlySpan<byte>(frame, index, 1),
                false,
                maximumFrameSize);
            AssertFramingBounds(result, remainder.Length + 1, maximumFrameSize);
            if (index < frame.Length - 1)
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
                Assert.That(result.Remainder, Has.Length.EqualTo(index + 1));
                Assert.That(result.Remainder.Length, Is.LessThan(maximumFrameSize));
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
                Assert.That(result.Frames, Has.Count.EqualTo(1));
                Assert.That(result.Frames[0], Has.Length.EqualTo(maximumFrameSize));
                Assert.That(result.Remainder, Is.Empty);
            }

            remainder = result.Remainder;
            index = index + 1;
        }

        byte[] hostileHeader = new byte[20];
        hostileHeader[0] = DiameterProtocolConstants.Version;
        WriteUInt24(hostileHeader, 1, 16777215);
        DiameterFramingResult hostile = codec.FrameMessageStream(
            new byte[0],
            hostileHeader,
            false,
            maximumFrameSize);
        Assert.That(hostile.Status, Is.EqualTo(DiameterCodecStatus.LimitExceeded));
        Assert.That(hostile.RequiredCount, Is.EqualTo(16777215));
        Assert.That(hostile.Remainder, Has.Length.EqualTo(hostileHeader.Length));
        Assert.That(hostile.Frames, Is.Empty);
    }

    private static DiameterMessage CreateTwoAvpMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.HopByHopIdentifier = 1U;
        message.EndToEndIdentifier = 2U;
        DiameterAvp first = new DiameterAvp();
        first.Code = 1U;
        first.Flags = DiameterAvpFlags.Mandatory;
        first.Data = new byte[] { 1, 2, 3, 4 };
        DiameterAvp second = new DiameterAvp();
        second.Code = 2U;
        second.Flags = DiameterAvpFlags.Mandatory;
        second.Data = new byte[] { 5, 6, 7, 8 };
        message.Avps.Add(first);
        message.Avps.Add(second);
        return message;
    }

    private static byte[] Copy(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    private static void WriteUInt24(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 16);
        destination[offset + 1] = (byte)(value >> 8);
        destination[offset + 2] = (byte)value;
    }

    private static void AssertMessageDecodeBounds(
        DiameterDecodeResult result,
        int sourceLength)
    {
        Assert.That(result.RequiredCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.ConsumedCount, Is.InRange(0, sourceLength));
        if (result.Status == DiameterCodecStatus.Success)
        {
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.ConsumedCount, Is.EqualTo(result.RequiredCount));
        }
        else
        {
            Assert.That(result.Message, Is.Null);
            Assert.That(result.Issues, Is.Not.Empty);
        }
    }

    private static void AssertAvpDecodeBounds(
        DiameterAvpDecodeResult result,
        int sourceLength)
    {
        Assert.That(result.RequiredCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.ConsumedCount, Is.InRange(0, sourceLength));
        if (result.Status == DiameterCodecStatus.Success)
        {
            Assert.That(result.Avp, Is.Not.Null);
            Assert.That(result.ConsumedCount, Is.EqualTo(result.RequiredCount));
            Assert.That(result.Avp!.Data.Length, Is.LessThanOrEqualTo(sourceLength));
        }
        else
        {
            Assert.That(result.Avp, Is.Null);
            Assert.That(result.Issues, Is.Not.Empty);
        }
    }

    private static void AssertValueDecodeBounds(
        DiameterValueResult result,
        int sourceLength)
    {
        Assert.That(result.RequiredCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.ConsumedCount, Is.InRange(0, sourceLength));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Data.Length, Is.LessThanOrEqualTo(sourceLength));
        if (result.Status != DiameterCodecStatus.Success)
        {
            Assert.That(result.Issues, Is.Not.Empty);
        }
    }

    private static void AssertFramingBounds(
        DiameterFramingResult result,
        int suppliedLength,
        int maximumFrameSize)
    {
        Assert.That(result.RequiredCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.ConsumedCount, Is.InRange(0, suppliedLength));
        Assert.That(result.Remainder.Length, Is.LessThanOrEqualTo(suppliedLength));
        Assert.That(
            result.ConsumedCount + result.Remainder.Length,
            Is.EqualTo(suppliedLength));
        int emittedLength = 0;
        int index = 0;
        while (index < result.Frames.Count)
        {
            Assert.That(result.Frames[index].Length, Is.InRange(20, maximumFrameSize));
            emittedLength = emittedLength + result.Frames[index].Length;
            index = index + 1;
        }

        Assert.That(emittedLength, Is.EqualTo(result.ConsumedCount));
        if (result.Status != DiameterCodecStatus.Success)
        {
            Assert.That(result.Issues, Is.Not.Empty);
        }
    }
}
