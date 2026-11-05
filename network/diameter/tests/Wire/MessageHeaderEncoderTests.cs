using System;

namespace diameter.Tests;

public class MessageHeaderEncoderTests
{
    [Test]
    public void HeaderMeasurementReturnsTheFixedHeaderSize()
    {
        DiameterMessage message = CreateMessage();

        DiameterEncodeResult result = MessageEncoder.MeasureHeader(
            message,
            DiameterProtocolConstants.MessageHeaderSize);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(DiameterProtocolConstants.MessageHeaderSize));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.Data, Is.Empty);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void HeaderEncodingProducesTheExpectedWireLayout()
    {
        DiameterMessage message = CreateMessage();
        byte[] destination = new byte[DiameterProtocolConstants.MessageHeaderSize];

        DiameterEncodeResult result = MessageEncoder.EncodeHeader(
            destination,
            message,
            DiameterProtocolConstants.MessageHeaderSize);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(20));
            Assert.That(result.WrittenCount, Is.EqualTo(20));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(
                destination,
                Is.EqualTo(new byte[]
                {
                    1, 0, 0, 20,
                    208, 18, 52, 86,
                    120, 154, 188, 222,
                    1, 35, 69, 103,
                    137, 171, 205, 239
                }));
        });
    }

    [Test]
    public void HeaderEncodingAcceptsErrorAnswersAndMaximum24BitFields()
    {
        DiameterMessage errorAnswer = CreateMessage();
        errorAnswer.Flags = DiameterMessageFlags.Proxyable | DiameterMessageFlags.Error;
        errorAnswer.CommandCode = DiameterProtocolConstants.MaximumCommandCode;
        byte[] destination = new byte[DiameterProtocolConstants.MessageHeaderSize];

        DiameterEncodeResult result = MessageEncoder.EncodeHeader(
            destination,
            errorAnswer,
            DiameterProtocolConstants.MaximumMessageLength);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(destination[1], Is.EqualTo(255));
            Assert.That(destination[2], Is.EqualTo(255));
            Assert.That(destination[3], Is.EqualTo(252));
            Assert.That(destination[4], Is.EqualTo(96));
            Assert.That(destination[5], Is.EqualTo(255));
            Assert.That(destination[6], Is.EqualTo(255));
            Assert.That(destination[7], Is.EqualTo(255));
        });
    }

    [Test]
    public void HeaderValidationRejectsReservedAndIllegalFlagCombinations()
    {
        DiameterMessage reserved = CreateMessage();
        reserved.Flags = (DiameterMessageFlags)1;
        DiameterMessage requestError = CreateMessage();
        requestError.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Error;
        DiameterMessage answerRetransmission = CreateMessage();
        answerRetransmission.Flags = DiameterMessageFlags.PotentiallyRetransmitted;
        DiameterMessage outOfByteRange = CreateMessage();
        outOfByteRange.Flags = (DiameterMessageFlags)256;

        Assert.Multiple(() =>
        {
            AssertInvalidFlags(MessageEncoder.MeasureHeader(reserved, 20));
            AssertInvalidFlags(MessageEncoder.MeasureHeader(requestError, 20));
            AssertInvalidFlags(MessageEncoder.MeasureHeader(answerRetransmission, 20));
            AssertInvalidFlags(MessageEncoder.MeasureHeader(outOfByteRange, 20));
        });
    }

    [Test]
    public void HeaderValidationRejectsVersionLengthAndCommandFailuresAtStableOffsets()
    {
        DiameterMessage unsupportedVersion = CreateMessage();
        unsupportedVersion.Version = 2;
        DiameterMessage oversizedCommand = CreateMessage();
        oversizedCommand.CommandCode = DiameterProtocolConstants.MaximumCommandCode + 1U;

        DiameterEncodeResult missing = MessageEncoder.MeasureHeader(null, 20);
        DiameterEncodeResult version = MessageEncoder.MeasureHeader(unsupportedVersion, 20);
        DiameterEncodeResult tooShort = MessageEncoder.MeasureHeader(CreateMessage(), 19);
        DiameterEncodeResult misaligned = MessageEncoder.MeasureHeader(CreateMessage(), 21);
        DiameterEncodeResult oversized = MessageEncoder.MeasureHeader(
            CreateMessage(),
            DiameterProtocolConstants.MaximumMessageLength + 4);
        DiameterEncodeResult command = MessageEncoder.MeasureHeader(oversizedCommand, 20);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, DiameterCodecStatus.Invalid, 0, "missing-message");
            AssertFailure(version, DiameterCodecStatus.Invalid, 0, "unsupported-version");
            AssertFailure(tooShort, DiameterCodecStatus.Invalid, 1, "invalid-message-length");
            AssertFailure(misaligned, DiameterCodecStatus.Invalid, 1, "invalid-message-length");
            AssertFailure(oversized, DiameterCodecStatus.LimitExceeded, 1, "message-length-limit");
            AssertFailure(command, DiameterCodecStatus.LimitExceeded, 5, "command-code-limit");
        });
    }

    [Test]
    public void ShortDestinationFailsWithoutPartialHeaderWrites()
    {
        byte[] destination = new byte[DiameterProtocolConstants.MessageHeaderSize - 1];
        Array.Fill(destination, (byte)170);

        DiameterEncodeResult result = MessageEncoder.EncodeHeader(destination, CreateMessage(), 20);

        Assert.Multiple(() =>
        {
            AssertFailure(result, DiameterCodecStatus.DestinationTooSmall, 0, "destination-too-small");
            Assert.That(result.RequiredCount, Is.EqualTo(20));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    private static DiameterMessage CreateMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable |
            DiameterMessageFlags.PotentiallyRetransmitted;
        message.CommandCode = 1193046U;
        message.ApplicationId = 2023406814U;
        message.HopByHopIdentifier = 19088743U;
        message.EndToEndIdentifier = 2309737967U;
        return message;
    }

    private static void AssertInvalidFlags(DiameterEncodeResult result)
    {
        AssertFailure(result, DiameterCodecStatus.Invalid, 4, "invalid-message-flags");
    }

    private static void AssertFailure(
        DiameterEncodeResult result,
        DiameterCodecStatus status,
        int offset,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(DiameterIssueSeverity.Error));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }
}
