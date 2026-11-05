using System;

namespace diameter.Tests;

public class AvpEncoderTests
{
    [Test]
    public void MeasurementIncludesRequiredZeroPadding()
    {
        DiameterAvp baseAvp = new DiameterAvp();
        baseAvp.Data = new byte[] { 1, 2, 3 };
        DiameterAvp vendorAvp = new DiameterAvp();
        vendorAvp.Flags = DiameterAvpFlags.VendorSpecific;
        vendorAvp.VendorId = 10415U;
        vendorAvp.Data = new byte[] { 1, 2, 3, 4, 5 };

        DiameterEncodeResult baseResult = AvpEncoder.MeasureAvp(baseAvp);
        DiameterEncodeResult vendorResult = AvpEncoder.MeasureAvp(vendorAvp);

        Assert.Multiple(() =>
        {
            Assert.That(baseResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(baseResult.RequiredCount, Is.EqualTo(12));
            Assert.That(baseResult.WrittenCount, Is.Zero);
            Assert.That(vendorResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(vendorResult.RequiredCount, Is.EqualTo(20));
            Assert.That(vendorResult.WrittenCount, Is.Zero);
        });
    }

    [Test]
    public void BaseAvpEncodingUsesExactLengthAndZeroPadding()
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 16909060U;
        avp.Flags = DiameterAvpFlags.Mandatory | DiameterAvpFlags.Protected;
        avp.Data = new byte[] { 170, 187, 204 };
        byte[] destination = new byte[13];
        Array.Fill(destination, (byte)238);

        DiameterEncodeResult result = AvpEncoder.EncodeAvp(destination, avp);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(12));
            Assert.That(result.WrittenCount, Is.EqualTo(12));
            Assert.That(
                destination,
                Is.EqualTo(new byte[]
                {
                    1, 2, 3, 4,
                    96, 0, 0, 11,
                    170, 187, 204, 0,
                    238
                }));
        });
    }

    [Test]
    public void VendorAvpEncodingIncludesVendorIdBeforePayload()
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.UserEquipmentInfo;
        avp.Flags = DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory;
        avp.VendorId = DiameterVendorIds.ThreeGpp;
        avp.Data = new byte[] { 1, 2, 3, 4 };
        byte[] destination = new byte[16];

        DiameterEncodeResult result = AvpEncoder.EncodeAvp(destination, avp);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(16));
            Assert.That(result.WrittenCount, Is.EqualTo(16));
            Assert.That(
                destination,
                Is.EqualTo(new byte[]
                {
                    0, 0, 1, 202,
                    192, 0, 0, 16,
                    0, 0, 40, 175,
                    1, 2, 3, 4
                }));
        });
    }

    [Test]
    public void EveryLegalVmpFlagCombinationCanBeEncoded()
    {
        DiameterAvpFlags[] flags = new DiameterAvpFlags[]
        {
            DiameterAvpFlags.None,
            DiameterAvpFlags.Mandatory,
            DiameterAvpFlags.Protected,
            DiameterAvpFlags.Mandatory | DiameterAvpFlags.Protected,
            DiameterAvpFlags.VendorSpecific,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Protected,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory | DiameterAvpFlags.Protected
        };

        int index = 0;
        while (index < flags.Length)
        {
            DiameterAvp avp = new DiameterAvp();
            avp.Flags = flags[index];
            if ((flags[index] & DiameterAvpFlags.VendorSpecific) != DiameterAvpFlags.None)
            {
                avp.VendorId = 1U;
            }

            DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
            byte[] destination = new byte[measurement.RequiredCount];
            DiameterEncodeResult encoding = AvpEncoder.EncodeAvp(destination, avp);

            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(destination[4], Is.EqualTo((byte)flags[index]));
            index = index + 1;
        }
    }

    [Test]
    public void InvalidFlagsVendorCombinationsAndDataAreRejectedAtStableOffsets()
    {
        DiameterAvp reserved = new DiameterAvp();
        reserved.Flags = (DiameterAvpFlags)1;
        DiameterAvp outOfByteRange = new DiameterAvp();
        outOfByteRange.Flags = (DiameterAvpFlags)256;
        DiameterAvp missingVendor = new DiameterAvp();
        missingVendor.Flags = DiameterAvpFlags.VendorSpecific;
        DiameterAvp missingVendorFlag = new DiameterAvp();
        missingVendorFlag.VendorId = 1U;
        DiameterAvp missingData = new DiameterAvp();
        missingData.Data = null!;

        Assert.Multiple(() =>
        {
            AssertFailure(AvpEncoder.MeasureAvp(null), DiameterCodecStatus.Invalid, 0, "missing-avp");
            AssertFailure(AvpEncoder.MeasureAvp(reserved), DiameterCodecStatus.Invalid, 4, "invalid-avp-flags");
            AssertFailure(AvpEncoder.MeasureAvp(outOfByteRange), DiameterCodecStatus.Invalid, 4, "invalid-avp-flags");
            AssertFailure(AvpEncoder.MeasureAvp(missingVendor), DiameterCodecStatus.Invalid, 8, "invalid-vendor-id");
            AssertFailure(AvpEncoder.MeasureAvp(missingVendorFlag), DiameterCodecStatus.Invalid, 4, "missing-vendor-flag");
            AssertFailure(AvpEncoder.MeasureAvp(missingData), DiameterCodecStatus.Invalid, 8, "missing-avp-data");
        });
    }

    [Test]
    public void AvpMeasurementRejectsLengthsBeyondThe24BitField()
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Data = new byte[DiameterProtocolConstants.MaximumAvpLength -
            DiameterProtocolConstants.AvpHeaderSize + 1];

        DiameterEncodeResult result = AvpEncoder.MeasureAvp(avp);

        AssertFailure(result, DiameterCodecStatus.LimitExceeded, 5, "avp-length-limit");
    }

    [Test]
    public void ShortDestinationFailsWithoutPartialAvpWrites()
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.UserName;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[] { 1 };
        byte[] destination = new byte[11];
        Array.Fill(destination, (byte)170);

        DiameterEncodeResult result = AvpEncoder.EncodeAvp(destination, avp);

        Assert.Multiple(() =>
        {
            AssertFailure(result, DiameterCodecStatus.DestinationTooSmall, 0, "destination-too-small");
            Assert.That(result.RequiredCount, Is.EqualTo(12));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(170));
        });
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
