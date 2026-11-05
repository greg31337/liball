using System;

namespace diameter.Tests;

public class AvpDecoderTests
{
    [Test]
    public void BaseAvpDecodingReadsPayloadAndConsumesPadding()
    {
        byte[] source = new byte[]
        {
            1, 2, 3, 4,
            96, 0, 0, 11,
            170, 187, 204, 0,
            238
        };

        DiameterAvpDecodeResult result = AvpDecoder.DecodeAvp(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(12));
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Avp, Is.Not.Null);
            Assert.That(result.Avp!.Code, Is.EqualTo(16909060U));
            Assert.That(
                result.Avp.Flags,
                Is.EqualTo(DiameterAvpFlags.Mandatory | DiameterAvpFlags.Protected));
            Assert.That(result.Avp.VendorId, Is.Zero);
            Assert.That(result.Avp.Data, Is.EqualTo(new byte[] { 170, 187, 204 }));
        });
    }

    [Test]
    public void VendorAvpDecodingReadsVendorIdBeforePayload()
    {
        byte[] source = new byte[]
        {
            0, 0, 1, 202,
            192, 0, 0, 16,
            0, 0, 40, 175,
            1, 2, 3, 4
        };

        DiameterAvpDecodeResult result = AvpDecoder.DecodeAvp(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(16));
            Assert.That(result.ConsumedCount, Is.EqualTo(16));
            Assert.That(result.Avp, Is.Not.Null);
            Assert.That(result.Avp!.Code, Is.EqualTo(DiameterAvpCodes.UserEquipmentInfo));
            Assert.That(
                result.Avp.Flags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory));
            Assert.That(result.Avp.VendorId, Is.EqualTo(DiameterVendorIds.ThreeGpp));
            Assert.That(result.Avp.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void ShortHeadersPayloadsAndPaddingReturnIncomplete()
    {
        byte[] incompletePayload = CreateBaseAvp(5, 12);
        byte[] incompletePadding = CreateBaseAvp(1, 9);

        DiameterAvpDecodeResult empty = AvpDecoder.DecodeAvp(new byte[0]);
        DiameterAvpDecodeResult shortHeader = AvpDecoder.DecodeAvp(new byte[7]);
        DiameterAvpDecodeResult payload = AvpDecoder.DecodeAvp(incompletePayload);
        DiameterAvpDecodeResult padding = AvpDecoder.DecodeAvp(incompletePadding);

        Assert.Multiple(() =>
        {
            AssertFailure(empty, DiameterCodecStatus.Incomplete, 0, "incomplete-avp-header");
            Assert.That(empty.RequiredCount, Is.EqualTo(8));
            AssertFailure(shortHeader, DiameterCodecStatus.Incomplete, 7, "incomplete-avp-header");
            Assert.That(shortHeader.RequiredCount, Is.EqualTo(8));
            AssertFailure(payload, DiameterCodecStatus.Incomplete, 12, "incomplete-avp");
            Assert.That(payload.RequiredCount, Is.EqualTo(16));
            AssertFailure(padding, DiameterCodecStatus.Incomplete, 9, "incomplete-avp");
            Assert.That(padding.RequiredCount, Is.EqualTo(12));
        });
    }

    [Test]
    public void InvalidFlagsLengthsAndVendorHeadersAreRejectedAtStableOffsets()
    {
        byte[] reserved = CreateBaseAvp(0, 8);
        reserved[4] = 1;
        byte[] shortBaseLength = CreateBaseAvp(0, 8);
        SetLength(shortBaseLength, 7);
        byte[] shortVendorLength = CreateBaseAvp(0, 8);
        shortVendorLength[4] = 128;
        byte[] zeroVendor = new byte[]
        {
            0, 0, 0, 1,
            128, 0, 0, 12,
            0, 0, 0, 0
        };

        Assert.Multiple(() =>
        {
            AssertFailure(
                AvpDecoder.DecodeAvp(reserved),
                DiameterCodecStatus.Invalid,
                4,
                "invalid-avp-flags");
            AssertFailure(
                AvpDecoder.DecodeAvp(shortBaseLength),
                DiameterCodecStatus.Invalid,
                5,
                "invalid-avp-length");
            AssertFailure(
                AvpDecoder.DecodeAvp(shortVendorLength),
                DiameterCodecStatus.Invalid,
                5,
                "missing-vendor-header");
            AssertFailure(
                AvpDecoder.DecodeAvp(zeroVendor),
                DiameterCodecStatus.Invalid,
                8,
                "invalid-vendor-id");
        });
    }

    [Test]
    public void NonzeroPaddingIsRejectedAtItsExactOffset()
    {
        byte[] source = CreateBaseAvp(1, 12);
        source[8] = 42;
        source[9] = 0;
        source[10] = 7;
        source[11] = 0;

        DiameterAvpDecodeResult result = AvpDecoder.DecodeAvp(source);

        AssertFailure(result, DiameterCodecStatus.Invalid, 10, "nonzero-avp-padding");
    }

    [Test]
    public void DecodedPayloadIsOwnedAndExcludesPaddingAndFollowingInput()
    {
        byte[] source = CreateBaseAvp(1, 16);
        source[8] = 42;
        source[12] = 99;

        DiameterAvpDecodeResult result = AvpDecoder.DecodeAvp(source);
        source[8] = 77;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.Avp, Is.Not.Null);
            Assert.That(result.Avp!.Data, Is.EqualTo(new byte[] { 42 }));
            Assert.That(result.Avp.Data, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SequenceDecodingPreservesOrderAndRepeatedAvpCodes()
    {
        byte[] first = EncodeAvp(DiameterAvpCodes.RouteRecord, 1);
        byte[] second = EncodeAvp(DiameterAvpCodes.RouteRecord, 2);
        byte[] sequence = new byte[first.Length + second.Length];
        Array.Copy(first, 0, sequence, 0, first.Length);
        Array.Copy(second, 0, sequence, first.Length, second.Length);

        DiameterValueResult result = AvpDecoder.DecodeAvpSequence(sequence);
        sequence[8] = 99;
        sequence[first.Length + 8] = 100;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.RequiredCount, Is.EqualTo(sequence.Length));
            Assert.That(result.ConsumedCount, Is.EqualTo(sequence.Length));
            Assert.That(result.Avps, Has.Count.EqualTo(2));
            Assert.That(result.Avps[0].Code, Is.EqualTo(DiameterAvpCodes.RouteRecord));
            Assert.That(result.Avps[1].Code, Is.EqualTo(DiameterAvpCodes.RouteRecord));
            Assert.That(result.Avps[0].Data, Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.Avps[1].Data, Is.EqualTo(new byte[] { 2 }));
            Assert.That(result.Avps[0], Is.Not.SameAs(result.Avps[1]));
        });
    }

    [Test]
    public void SequenceFailureRetainsCompletedItemsAndAdjustsIssueOffset()
    {
        byte[] first = EncodeAvp(DiameterAvpCodes.RouteRecord, 1);
        byte[] second = EncodeAvp(DiameterAvpCodes.UserName, 2);
        second[10] = 9;
        byte[] sequence = new byte[first.Length + second.Length];
        Array.Copy(first, 0, sequence, 0, first.Length);
        Array.Copy(second, 0, sequence, first.Length, second.Length);

        DiameterValueResult result = AvpDecoder.DecodeAvpSequence(sequence);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.RequiredCount, Is.EqualTo(sequence.Length));
            Assert.That(result.ConsumedCount, Is.EqualTo(first.Length));
            Assert.That(result.Avps, Has.Count.EqualTo(1));
            Assert.That(result.Avps[0].Data, Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Offset, Is.EqualTo(first.Length + 10));
            Assert.That(result.Issues[0].Code, Is.EqualTo("nonzero-avp-padding"));
        });
    }

    private static byte[] CreateBaseAvp(int payloadLength, int sourceLength)
    {
        byte[] source = new byte[sourceLength];
        source[3] = 1;
        SetLength(source, DiameterProtocolConstants.AvpHeaderSize + payloadLength);
        return source;
    }

    private static byte[] EncodeAvp(uint code, byte payload)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[] { payload };
        DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
        byte[] encoded = new byte[measurement.RequiredCount];
        DiameterEncodeResult result = AvpEncoder.EncodeAvp(encoded, avp);
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return encoded;
    }

    private static void SetLength(byte[] source, int length)
    {
        source[5] = (byte)(length >> 16);
        source[6] = (byte)((length >> 8) & 255);
        source[7] = (byte)(length & 255);
    }

    private static void AssertFailure(
        DiameterAvpDecodeResult result,
        DiameterCodecStatus status,
        int offset,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Avp, Is.Null);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }
}
