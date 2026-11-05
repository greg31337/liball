using System;

namespace diameter.Tests;

public class MessageEncoderTests
{
    [Test]
    public void CompleteMessageMeasurementAndEncodingUseExactWireSizes()
    {
        DiameterMessage message = CreateCompleteMessage();
        byte[] destination = new byte[50];
        Array.Fill(destination, (byte)238);

        DiameterEncodeResult measurement = MessageEncoder.MeasureMessage(message);
        DiameterEncodeResult encoding = MessageEncoder.EncodeMessage(destination, message);

        Assert.Multiple(() =>
        {
            AssertSuccess(measurement, 48, 0);
            AssertSuccess(encoding, 48, 48);
            Assert.That(
                destination,
                Is.EqualTo(new byte[]
                {
                    1, 0, 0, 48,
                    192, 0, 1, 1,
                    0, 0, 0, 0,
                    1, 2, 3, 4,
                    170, 187, 204, 221,
                    0, 0, 0, 1,
                    64, 0, 0, 11,
                    97, 98, 99, 0,
                    0, 0, 0, 2,
                    192, 0, 0, 16,
                    0, 0, 40, 175,
                    1, 2, 3, 4,
                    238, 238
                }));
        });
    }

    [Test]
    public void OwnedMessageEncodingReturnsAnIndependentExactArray()
    {
        DiameterMessage message = CreateCompleteMessage();

        DiameterEncodeResult first = MessageEncoder.EncodeMessageOwned(message);
        DiameterEncodeResult second = MessageEncoder.EncodeMessageOwned(message);
        message.Avps[0].Data[0] = 120;
        second.Data[0] = 99;

        Assert.Multiple(() =>
        {
            AssertSuccess(first, 48, 48);
            AssertSuccess(second, 48, 48);
            Assert.That(first.Data, Has.Length.EqualTo(48));
            Assert.That(first.Data[0], Is.EqualTo(1));
            Assert.That(first.Data[28], Is.EqualTo(97));
            Assert.That(second.Data, Is.Not.SameAs(first.Data));
            Assert.That(first.Data[0], Is.Not.EqualTo(second.Data[0]));
        });
    }

    [Test]
    public void EmptyMessageEncodesAsAnOwnedHeaderOnlyFrame()
    {
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.HopByHopIdentifier = 1U;
        message.EndToEndIdentifier = 2U;

        DiameterEncodeResult result = MessageEncoder.EncodeMessageOwned(message);

        Assert.Multiple(() =>
        {
            AssertSuccess(result, 20, 20);
            Assert.That(result.Data, Has.Length.EqualTo(20));
            Assert.That(result.Data[0], Is.EqualTo(DiameterProtocolConstants.Version));
            Assert.That(result.Data[1], Is.Zero);
            Assert.That(result.Data[2], Is.Zero);
            Assert.That(result.Data[3], Is.EqualTo(20));
            Assert.That(result.Data[5], Is.Zero);
            Assert.That(result.Data[6], Is.EqualTo(1));
            Assert.That(result.Data[7], Is.EqualTo(24));
        });
    }

    [Test]
    public void ShortDestinationDoesNotReceiveAPartialMessage()
    {
        DiameterMessage message = CreateCompleteMessage();
        byte[] destination = new byte[47];
        Array.Fill(destination, (byte)170);

        DiameterEncodeResult result = MessageEncoder.EncodeMessage(destination, message);

        Assert.Multiple(() =>
        {
            AssertFailure(
                result,
                DiameterCodecStatus.DestinationTooSmall,
                0,
                "destination-too-small");
            Assert.That(result.RequiredCount, Is.EqualTo(48));
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [Test]
    public void InvalidLaterAvpIsReportedAtTheMessageRelativeOffsetWithoutWriting()
    {
        DiameterMessage message = new DiameterMessage();
        DiameterAvp valid = CreateAvp(1U, DiameterAvpFlags.Mandatory, new byte[] { 1 });
        DiameterAvp invalid = CreateAvp(2U, (DiameterAvpFlags)1, new byte[0]);
        message.Avps.Add(valid);
        message.Avps.Add(invalid);
        byte[] destination = new byte[64];
        Array.Fill(destination, (byte)170);

        DiameterEncodeResult measurement = MessageEncoder.MeasureMessage(message);
        DiameterEncodeResult encoding = MessageEncoder.EncodeMessage(destination, message);
        DiameterEncodeResult owned = MessageEncoder.EncodeMessageOwned(message);

        Assert.Multiple(() =>
        {
            AssertFailure(
                measurement,
                DiameterCodecStatus.Invalid,
                36,
                "invalid-avp-flags");
            Assert.That(measurement.RequiredCount, Is.EqualTo(40));
            AssertFailure(
                encoding,
                DiameterCodecStatus.Invalid,
                36,
                "invalid-avp-flags");
            Assert.That(destination, Is.All.EqualTo(170));
            AssertFailure(
                owned,
                DiameterCodecStatus.Invalid,
                36,
                "invalid-avp-flags");
            Assert.That(owned.Data, Is.Empty);
        });
    }

    [Test]
    public void CompleteMeasurementRejectsMissingMessageAndAvpCollections()
    {
        DiameterMessage missingAvps = new DiameterMessage();
        missingAvps.Avps = null!;

        DiameterEncodeResult missingMessage = MessageEncoder.MeasureMessage(null);
        DiameterEncodeResult missingCollection = MessageEncoder.MeasureMessage(missingAvps);

        Assert.Multiple(() =>
        {
            AssertFailure(
                missingMessage,
                DiameterCodecStatus.Invalid,
                0,
                "missing-message");
            AssertFailure(
                missingCollection,
                DiameterCodecStatus.Invalid,
                20,
                "missing-message-avps");
            Assert.That(missingCollection.RequiredCount, Is.EqualTo(20));
        });
    }

    [Test]
    public void CompleteMeasurementValidatesHeaderBeforeAvpContent()
    {
        DiameterMessage message = CreateCompleteMessage();
        message.Version = 2;
        message.Avps[0].Flags = (DiameterAvpFlags)1;

        DiameterEncodeResult result = MessageEncoder.MeasureMessage(message);

        AssertFailure(result, DiameterCodecStatus.Invalid, 0, "unsupported-version");
    }

    [Test]
    public void MessageLengthBoundaryUsesTheExactPaddedAvpSize()
    {
        DiameterMessage message = new DiameterMessage();
        DiameterAvp avp = new DiameterAvp();
        avp.Data = new byte[DiameterProtocolConstants.MaximumMessageLength -
            DiameterProtocolConstants.MessageHeaderSize -
            DiameterProtocolConstants.AvpHeaderSize];
        message.Avps.Add(avp);

        DiameterEncodeResult exact = MessageEncoder.MeasureMessage(message);
        avp.Data = new byte[avp.Data.Length + 1];
        DiameterEncodeResult oversized = MessageEncoder.MeasureMessage(message);

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, DiameterProtocolConstants.MaximumMessageLength, 0);
            AssertFailure(
                oversized,
                DiameterCodecStatus.LimitExceeded,
                1,
                "message-length-limit");
            Assert.That(
                oversized.RequiredCount,
                Is.EqualTo(DiameterProtocolConstants.MaximumMessageLength + 4));
        });
    }

    private static DiameterMessage CreateCompleteMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        message.CommandCode = 257U;
        message.HopByHopIdentifier = 16909060U;
        message.EndToEndIdentifier = 2864434397U;
        message.Avps.Add(CreateAvp(
            1U,
            DiameterAvpFlags.Mandatory,
            new byte[] { 97, 98, 99 }));

        DiameterAvp vendorAvp = CreateAvp(
            2U,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory,
            new byte[] { 1, 2, 3, 4 });
        vendorAvp.VendorId = DiameterVendorIds.ThreeGpp;
        message.Avps.Add(vendorAvp);
        return message;
    }

    private static DiameterAvp CreateAvp(
        uint code,
        DiameterAvpFlags flags,
        byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = flags;
        avp.Data = data;
        return avp;
    }

    private static void AssertSuccess(
        DiameterEncodeResult result,
        int requiredCount,
        int writtenCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.WrittenCount, Is.EqualTo(writtenCount));
        Assert.That(result.Issues, Is.Empty);
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
