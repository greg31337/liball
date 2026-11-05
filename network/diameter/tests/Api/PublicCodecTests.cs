using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class PublicCodecTests
{
    [Test]
    public void MessageCodecContractMeasuresEncodesAndDecodesOwnedFrames()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        DiameterMessage message = CreateMessage();
        byte[] destination = new byte[32];

        DiameterEncodeResult measurement = codec.MeasureMessage(message);
        DiameterEncodeResult spanEncoding = codec.EncodeMessage(destination, message);
        DiameterEncodeResult ownedEncoding = codec.EncodeMessageToArray(message);
        DiameterDecodeResult decoding = codec.DecodeMessage(
            ownedEncoding.Data,
            false,
            new DiameterLimitConfiguration());
        ownedEncoding.Data[28] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(measurement.RequiredCount, Is.EqualTo(32));
            Assert.That(spanEncoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(spanEncoding.WrittenCount, Is.EqualTo(32));
            Assert.That(ownedEncoding.Data, Has.Length.EqualTo(32));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(32));
            Assert.That(decoding.Message!.Avps, Has.Count.EqualTo(1));
            Assert.That(decoding.Message.Avps[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void MessageCodecContractExposesProfilesLimitsAndIncrementalFraming()
    {
        IDiameterMessageCodec codec = new DiameterMessageCodec();
        byte[] frame = codec.EncodeMessageToArray(CreateMessage()).Data;
        frame[4] = (byte)(frame[4] | 1);
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();

        DiameterDecodeResult strict = codec.DecodeMessage(frame, false, limits);
        DiameterDecodeResult permissive = codec.DecodeMessage(frame, true, limits);
        DiameterFramingResult first = codec.FrameMessageStream(
            new byte[0],
            new ReadOnlySpan<byte>(frame, 0, 11),
            false,
            64);
        DiameterFramingResult second = codec.FrameMessageStream(
            first.Remainder,
            new ReadOnlySpan<byte>(frame, 11, frame.Length - 11),
            true,
            64);
        DiameterFramingResult limited = codec.FrameMessageStream(
            new byte[0],
            frame,
            false,
            28);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(strict.Issues[0].Code, Is.EqualTo("invalid-message-flags"));
            Assert.That(permissive.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(permissive.Issues, Has.Count.EqualTo(1));
            Assert.That(first.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(first.Remainder, Has.Length.EqualTo(11));
            Assert.That(second.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(second.Frames, Has.Count.EqualTo(1));
            Assert.That(second.CleanEndOfStream, Is.True);
            Assert.That(limited.Status, Is.EqualTo(DiameterCodecStatus.LimitExceeded));
            Assert.That(limited.Issues[0].Code, Is.EqualTo("configured-frame-size-limit"));
        });
    }

    [Test]
    public void AvpCodecContractMeasuresAndRoundTripsOwnedAvpsWithProfiles()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 4000U;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[] { 1, 2, 3 };
        byte[] destination = new byte[12];

        DiameterEncodeResult measurement = codec.MeasureAvp(avp);
        DiameterEncodeResult spanEncoding = codec.EncodeAvp(destination, avp);
        DiameterEncodeResult ownedEncoding = codec.EncodeAvpToArray(avp);
        ownedEncoding.Data[4] = (byte)(ownedEncoding.Data[4] | 1);
        DiameterAvpDecodeResult strict = codec.DecodeAvp(ownedEncoding.Data, false);
        DiameterAvpDecodeResult permissive = codec.DecodeAvp(ownedEncoding.Data, true);
        ownedEncoding.Data[8] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(measurement.RequiredCount, Is.EqualTo(12));
            Assert.That(spanEncoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(destination, Is.EqualTo(new byte[]
            {
                0, 0, 15, 160,
                64, 0, 0, 11,
                1, 2, 3, 0
            }));
            Assert.That(strict.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(permissive.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(permissive.Issues, Has.Count.EqualTo(1));
            Assert.That(permissive.Avp!.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void AvpCodecContractDelegatesEveryNumericValueCodec()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        byte[] integer32 = new byte[4];
        byte[] integer64 = new byte[8];
        byte[] unsigned32 = new byte[4];
        byte[] unsigned64 = new byte[8];
        byte[] float32 = new byte[4];
        byte[] float64 = new byte[8];
        byte[] enumerated = new byte[4];

        DiameterValueResult encodedInteger32 = codec.EncodeInteger32(integer32, -123);
        DiameterValueResult encodedInteger64 = codec.EncodeInteger64(integer64, -456L);
        DiameterValueResult encodedUnsigned32 = codec.EncodeUnsigned32(unsigned32, 4000000000U);
        DiameterValueResult encodedUnsigned64 = codec.EncodeUnsigned64(unsigned64, 9000000000UL);
        DiameterValueResult encodedFloat32 = codec.EncodeFloat32(float32, 1.5F);
        DiameterValueResult encodedFloat64 = codec.EncodeFloat64(float64, -2.25D);
        DiameterValueResult encodedEnumerated = codec.EncodeEnumerated(enumerated, -9);

        Assert.Multiple(() =>
        {
            Assert.That(encodedInteger32.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodedInteger64.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodedUnsigned32.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodedUnsigned64.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodedFloat32.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodedFloat64.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodedEnumerated.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeInteger32(integer32).Integer32, Is.EqualTo(-123));
            Assert.That(codec.DecodeInteger64(integer64).Integer64, Is.EqualTo(-456L));
            Assert.That(codec.DecodeUnsigned32(unsigned32).Unsigned32, Is.EqualTo(4000000000U));
            Assert.That(codec.DecodeUnsigned64(unsigned64).Unsigned64, Is.EqualTo(9000000000UL));
            Assert.That(codec.DecodeFloat32(float32).Float32, Is.EqualTo(1.5F));
            Assert.That(codec.DecodeFloat64(float64).Float64, Is.EqualTo(-2.25D));
            Assert.That(codec.DecodeEnumerated(enumerated).Integer32, Is.EqualTo(-9));
        });
    }

    [Test]
    public void AvpCodecContractDelegatesOctetTextAddressAndTimeCodecs()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        byte[] octets = new byte[3];
        byte[] utf8 = new byte[5];
        byte[] address = new byte[6];
        byte[] time = new byte[4];
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(1704067200L);

        DiameterValueResult encodedOctets = codec.EncodeOctetString(
            octets,
            new byte[] { 7, 8, 9 });
        DiameterValueResult encodedText = codec.EncodeUtf8String(utf8, "caf\u00e9");
        DiameterValueResult encodedAddress = codec.EncodeAddress(
            address,
            IPAddress.Parse("192.0.2.1"));
        DiameterValueResult encodedTime = codec.EncodeTime(time, timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(encodedOctets.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeOctetString(octets).Data, Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(encodedText.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeUtf8String(utf8).Text, Is.EqualTo("caf\u00e9"));
            Assert.That(encodedAddress.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(
                codec.DecodeAddress(address).Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.1")));
            Assert.That(encodedTime.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeTime(time, timestamp).Time, Is.EqualTo(timestamp));
        });
    }

    [Test]
    public void AvpCodecContractDelegatesIdentityUriAndFilterRuleCodecs()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "PEER.Example.COM.";
        byte[] identityBytes = new byte[16];
        DiameterUri uri = new DiameterUri();
        uri.Host.Value = "peer.example";
        byte[] uriBytes = new byte[18];
        string ipRule = "permit out ip from any to assigned";
        string qosRule = "tag in ip from any to assigned DSCP 46";
        byte[] ipRuleBytes = new byte[ipRule.Length];
        byte[] qosRuleBytes = new byte[qosRule.Length];

        DiameterValueResult encodedIdentity = codec.EncodeDiameterIdentity(
            identityBytes,
            identity);
        DiameterValueResult encodedUri = codec.EncodeDiameterUri(uriBytes, uri);
        DiameterValueResult encodedIpRule = codec.EncodeIpFilterRule(ipRuleBytes, ipRule);
        DiameterValueResult encodedQosRule = codec.EncodeQosFilterRule(qosRuleBytes, qosRule);

        Assert.Multiple(() =>
        {
            Assert.That(encodedIdentity.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(
                codec.DecodeDiameterIdentity(identityBytes).Identity!.Value,
                Is.EqualTo("peer.example.com"));
            Assert.That(encodedUri.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeDiameterUri(uriBytes).Uri!.Host.Value, Is.EqualTo("peer.example"));
            Assert.That(encodedIpRule.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeIpFilterRule(ipRuleBytes).Text, Is.EqualTo(ipRule));
            Assert.That(encodedQosRule.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(codec.DecodeQosFilterRule(qosRuleBytes).Text, Is.EqualTo(qosRule));
        });
    }

    [Test]
    public void AvpCodecContractMeasuresEncodesAndDecodesGroupedValues()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterAvp leaf = new DiameterAvp();
        leaf.Code = 2000U;
        leaf.Data = new byte[] { 1, 2, 3 };
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(leaf);
        List<DiameterAvp> groupedTypes = new List<DiameterAvp>();
        DiameterAvp groupedType = new DiameterAvp();
        groupedType.Code = 1000U;
        groupedTypes.Add(groupedType);
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();

        DiameterValueResult measurement = codec.MeasureGrouped(avps, groupedTypes, limits);
        byte[] destination = new byte[measurement.RequiredCount];
        DiameterValueResult encoding = codec.EncodeGrouped(
            destination,
            avps,
            groupedTypes,
            limits);
        DiameterValueResult decoding = codec.DecodeGrouped(destination, groupedTypes, limits);
        destination[8] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(measurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(measurement.RequiredCount, Is.EqualTo(12));
            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoding.WrittenCount, Is.EqualTo(12));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.Avps, Has.Count.EqualTo(1));
            Assert.That(decoding.Avps[0].Code, Is.EqualTo(2000U));
            Assert.That(decoding.Avps[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    private static DiameterMessage CreateMessage()
    {
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request;
        message.CommandCode = DiameterCommandCodes.DeviceWatchdog;
        message.HopByHopIdentifier = 1U;
        message.EndToEndIdentifier = 2U;
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.OriginHost;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[] { 1, 2, 3 };
        message.Avps.Add(avp);
        return message;
    }
}
