using System;

namespace diameter.Tests;

public class MessageHeaderDecoderTests
{
    [Test]
    public void HeaderDecodingReadsEveryWireField()
    {
        byte[] source = new byte[]
        {
            1, 0, 0, 20,
            208, 18, 52, 86,
            120, 154, 188, 222,
            1, 35, 69, 103,
            137, 171, 205, 239
        };

        DiameterDecodeResult result = MessageDecoder.DecodeHeader(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(20));
            Assert.That(result.ConsumedCount, Is.EqualTo(20));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.Version, Is.EqualTo(1));
            Assert.That(
                result.Message.Flags,
                Is.EqualTo(
                    DiameterMessageFlags.Request |
                    DiameterMessageFlags.Proxyable |
                    DiameterMessageFlags.PotentiallyRetransmitted));
            Assert.That(result.Message.CommandCode, Is.EqualTo(1193046U));
            Assert.That(result.Message.ApplicationId, Is.EqualTo(2023406814U));
            Assert.That(result.Message.HopByHopIdentifier, Is.EqualTo(19088743U));
            Assert.That(result.Message.EndToEndIdentifier, Is.EqualTo(2309737967U));
            Assert.That(result.Message.Avps, Is.Empty);
        });
    }

    [Test]
    public void HeaderDecodingReportsTheDeclaredTotalMessageLength()
    {
        byte[] source = CreateHeader(100);
        byte[] inputWithTrailingData = new byte[24];
        Array.Copy(source, inputWithTrailingData, source.Length);

        DiameterDecodeResult result = MessageDecoder.DecodeHeader(inputWithTrailingData);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(100));
            Assert.That(result.ConsumedCount, Is.EqualTo(20));
            Assert.That(result.Message, Is.Not.Null);
        });
    }

    [Test]
    public void ShortHeadersReturnIncompleteWithoutReadingPastInput()
    {
        DiameterDecodeResult empty = MessageDecoder.DecodeHeader(new byte[0]);
        DiameterDecodeResult versionOnly = MessageDecoder.DecodeHeader(new byte[] { 2 });
        DiameterDecodeResult almostComplete = MessageDecoder.DecodeHeader(new byte[19]);

        Assert.Multiple(() =>
        {
            AssertFailure(empty, DiameterCodecStatus.Incomplete, 0, "incomplete-header");
            AssertFailure(versionOnly, DiameterCodecStatus.Incomplete, 1, "incomplete-header");
            AssertFailure(almostComplete, DiameterCodecStatus.Incomplete, 19, "incomplete-header");
            Assert.That(empty.RequiredCount, Is.EqualTo(20));
            Assert.That(versionOnly.RequiredCount, Is.EqualTo(20));
            Assert.That(almostComplete.RequiredCount, Is.EqualTo(20));
            Assert.That(empty.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Information));
        });
    }

    [Test]
    public void HeaderDecodingRejectsUnsupportedVersionAndInvalidLengths()
    {
        byte[] unsupportedVersion = CreateHeader(20);
        unsupportedVersion[0] = 2;
        byte[] tooShort = CreateHeader(20);
        SetLength(tooShort, 19);
        byte[] misaligned = CreateHeader(20);
        SetLength(misaligned, 21);
        byte[] oversized = CreateHeader(20);
        SetLength(oversized, 16777215);

        DiameterDecodeResult versionResult = MessageDecoder.DecodeHeader(unsupportedVersion);
        DiameterDecodeResult tooShortResult = MessageDecoder.DecodeHeader(tooShort);
        DiameterDecodeResult misalignedResult = MessageDecoder.DecodeHeader(misaligned);
        DiameterDecodeResult oversizedResult = MessageDecoder.DecodeHeader(oversized);

        Assert.Multiple(() =>
        {
            AssertFailure(versionResult, DiameterCodecStatus.Invalid, 0, "unsupported-version");
            AssertFailure(tooShortResult, DiameterCodecStatus.Invalid, 1, "invalid-message-length");
            AssertFailure(misalignedResult, DiameterCodecStatus.Invalid, 1, "invalid-message-length");
            AssertFailure(oversizedResult, DiameterCodecStatus.LimitExceeded, 1, "message-length-limit");
        });
    }

    [Test]
    public void HeaderDecodingRejectsReservedAndIllegalFlagCombinations()
    {
        byte[] reserved = CreateHeader(20);
        reserved[4] = 1;
        byte[] requestError = CreateHeader(20);
        requestError[4] = 160;
        byte[] answerRetransmission = CreateHeader(20);
        answerRetransmission[4] = 16;
        byte[] errorAnswer = CreateHeader(20);
        errorAnswer[4] = 96;

        DiameterDecodeResult reservedResult = MessageDecoder.DecodeHeader(reserved);
        DiameterDecodeResult requestErrorResult = MessageDecoder.DecodeHeader(requestError);
        DiameterDecodeResult retransmissionResult = MessageDecoder.DecodeHeader(answerRetransmission);
        DiameterDecodeResult errorAnswerResult = MessageDecoder.DecodeHeader(errorAnswer);

        Assert.Multiple(() =>
        {
            AssertFailure(reservedResult, DiameterCodecStatus.Invalid, 4, "invalid-message-flags");
            AssertFailure(requestErrorResult, DiameterCodecStatus.Invalid, 4, "invalid-message-flags");
            AssertFailure(retransmissionResult, DiameterCodecStatus.Invalid, 4, "invalid-message-flags");
            Assert.That(errorAnswerResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(errorAnswerResult.Message, Is.Not.Null);
            Assert.That(
                errorAnswerResult.Message!.Flags,
                Is.EqualTo(DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error));
        });
    }

    private static byte[] CreateHeader(int messageLength)
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.ApplicationId = DiameterApplicationIds.Common;
        message.HopByHopIdentifier = 1U;
        message.EndToEndIdentifier = 2U;
        byte[] header = new byte[DiameterProtocolConstants.MessageHeaderSize];
        DiameterEncodeResult result = MessageEncoder.EncodeHeader(header, message, messageLength);
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return header;
    }

    private static void SetLength(byte[] header, int messageLength)
    {
        header[1] = (byte)(messageLength >> 16);
        header[2] = (byte)((messageLength >> 8) & 255);
        header[3] = (byte)(messageLength & 255);
    }

    private static void AssertFailure(
        DiameterDecodeResult result,
        DiameterCodecStatus status,
        int offset,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Message, Is.Null);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }
}
