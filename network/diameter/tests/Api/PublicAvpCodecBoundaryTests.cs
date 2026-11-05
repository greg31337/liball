using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class PublicAvpCodecBoundaryTests
{
    [Test]
    public void GroupedCodecEnforcesDepthCountAndByteLimitsThroughPublicContract()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterAvp leaf = CreateAvp(2001U, new byte[] { 1 });
        DiameterAvp nested = CreateAvp(1000U, codec.EncodeAvpToArray(leaf).Data);
        DiameterAvp outer = CreateAvp(1000U, codec.EncodeAvpToArray(nested).Data);
        List<DiameterAvp> nestedValues = new List<DiameterAvp>();
        nestedValues.Add(outer);
        List<DiameterAvp> groupedTypes = new List<DiameterAvp>();
        groupedTypes.Add(CreateAvp(1000U, new byte[0]));
        byte[] nestedBytes = codec.EncodeAvpToArray(outer).Data;
        DiameterLimitConfiguration shallow = CreateLimits(2, 8, 128);
        DiameterLimitConfiguration sufficient = CreateLimits(3, 8, 128);

        DiameterValueResult measuredDepthFailure = codec.MeasureGrouped(
            nestedValues,
            groupedTypes,
            shallow);
        DiameterValueResult decodedDepthFailure = codec.DecodeGrouped(
            nestedBytes,
            groupedTypes,
            shallow);
        DiameterValueResult measuredDepthSuccess = codec.MeasureGrouped(
            nestedValues,
            groupedTypes,
            sufficient);
        DiameterValueResult decodedDepthSuccess = codec.DecodeGrouped(
            nestedBytes,
            groupedTypes,
            sufficient);

        DiameterAvp first = CreateAvp(1U, new byte[] { 1 });
        DiameterAvp second = CreateAvp(2U, new byte[] { 2 });
        List<DiameterAvp> twoValues = new List<DiameterAvp>();
        twoValues.Add(first);
        twoValues.Add(second);
        byte[] twoValueBytes = Combine(
            codec.EncodeAvpToArray(first).Data,
            codec.EncodeAvpToArray(second).Data);
        DiameterLimitConfiguration countLimit = CreateLimits(2, 1, 64);
        DiameterValueResult measuredCountFailure = codec.MeasureGrouped(
            twoValues,
            groupedTypes,
            countLimit);
        DiameterValueResult decodedCountFailure = codec.DecodeGrouped(
            twoValueBytes,
            groupedTypes,
            countLimit);

        List<DiameterAvp> oneValue = new List<DiameterAvp>();
        oneValue.Add(first);
        byte[] oneValueBytes = codec.EncodeAvpToArray(first).Data;
        DiameterValueResult exactBytes = codec.DecodeGrouped(
            oneValueBytes,
            groupedTypes,
            CreateLimits(2, 2, oneValueBytes.Length));
        DiameterValueResult measuredByteFailure = codec.MeasureGrouped(
            oneValue,
            groupedTypes,
            CreateLimits(2, 2, oneValueBytes.Length - 1));
        DiameterValueResult decodedByteFailure = codec.DecodeGrouped(
            oneValueBytes,
            groupedTypes,
            CreateLimits(2, 2, oneValueBytes.Length - 1));

        Assert.Multiple(() =>
        {
            AssertFailure(
                measuredDepthFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-depth-limit");
            AssertFailure(
                decodedDepthFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-depth-limit");
            Assert.That(measuredDepthFailure.Issues[0].Offset, Is.EqualTo(16));
            Assert.That(decodedDepthFailure.Issues[0].Offset, Is.EqualTo(16));
            Assert.That(measuredDepthSuccess.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decodedDepthSuccess.Status, Is.EqualTo(DiameterCodecStatus.Success));

            AssertFailure(
                measuredCountFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-avp-count-limit");
            AssertFailure(
                decodedCountFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-avp-count-limit");
            Assert.That(decodedCountFailure.ConsumedCount, Is.EqualTo(12));
            Assert.That(decodedCountFailure.Avps, Has.Count.EqualTo(1));

            Assert.That(exactBytes.Status, Is.EqualTo(DiameterCodecStatus.Success));
            AssertFailure(
                measuredByteFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-byte-limit");
            AssertFailure(
                decodedByteFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-byte-limit");
        });
    }

    [Test]
    public void UnknownAvpsRemainOrderedOwnedAndLosslessThroughPublicMessageCodec()
    {
        IDiameterMessageCodec messageCodec = new DiameterMessageCodec();
        DiameterMessage message = new DiameterMessage();
        message.Flags = DiameterMessageFlags.Request | DiameterMessageFlags.Proxyable;
        message.CommandCode = 400U;
        message.ApplicationId = 17U;
        message.HopByHopIdentifier = 16909060U;
        message.EndToEndIdentifier = 2864434397U;
        message.Avps.Add(CreateAvp(4000000U, new byte[] { 1, 2, 3 }));
        DiameterAvp vendor = CreateAvp(4000001U, new byte[] { 4, 5, 6, 7 });
        vendor.Flags = DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory;
        vendor.VendorId = 4242U;
        message.Avps.Add(vendor);
        message.Avps.Add(CreateAvp(4000000U, new byte[] { 8, 9 }));

        DiameterEncodeResult original = messageCodec.EncodeMessageToArray(message);
        byte[] expected = new byte[original.Data.Length];
        Array.Copy(original.Data, expected, expected.Length);
        DiameterDecodeResult decoding = messageCodec.DecodeMessage(
            original.Data,
            false,
            new DiameterLimitConfiguration());
        original.Data[28] = 99;
        DiameterEncodeResult reencoding = messageCodec.EncodeMessageToArray(decoding.Message);

        Assert.Multiple(() =>
        {
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.Message!.Avps, Has.Count.EqualTo(3));
            Assert.That(decoding.Message.Avps[0].Code, Is.EqualTo(4000000U));
            Assert.That(decoding.Message.Avps[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(decoding.Message.Avps[1].Code, Is.EqualTo(4000001U));
            Assert.That(decoding.Message.Avps[1].VendorId, Is.EqualTo(4242U));
            Assert.That(decoding.Message.Avps[2].Code, Is.EqualTo(4000000U));
            Assert.That(decoding.Message.Avps[2].Data, Is.EqualTo(new byte[] { 8, 9 }));
            Assert.That(reencoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(reencoding.Data, Is.EqualTo(expected));
            Assert.That(reencoding.Data, Is.Not.SameAs(expected));
        });
    }

    [Test]
    public void Utf8AndIdentityCodecsRejectEveryMalformedBoundaryFamily()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        byte[][] invalidUtf8 = new byte[][]
        {
            new byte[] { 0 },
            new byte[] { 128 },
            new byte[] { 192, 128 },
            new byte[] { 224, 128, 128 },
            new byte[] { 237, 160, 128 },
            new byte[] { 240, 128, 128, 128 },
            new byte[] { 244, 144, 128, 128 },
            new byte[] { 194 },
            new byte[] { 224, 160 },
            new byte[] { 240, 144, 128 }
        };

        int utf8Index = 0;
        while (utf8Index < invalidUtf8.Length)
        {
            DiameterValueResult result = codec.DecodeUtf8String(invalidUtf8[utf8Index]);
            AssertFailure(result, DiameterCodecStatus.Invalid, "invalid-utf8");
            utf8Index = utf8Index + 1;
        }

        string[] invalidIdentities = new string[]
        {
            string.Empty,
            ".",
            ".example.com",
            "example..com",
            "-node.example",
            "node-.example",
            "bad_name.example",
            "n\u00f6d.example",
            new string('a', 64) + ".example",
            new string('a', 254)
        };
        int identityIndex = 0;
        while (identityIndex < invalidIdentities.Length)
        {
            DiameterIdentity identity = new DiameterIdentity();
            identity.Value = invalidIdentities[identityIndex];
            DiameterValueResult result = codec.EncodeDiameterIdentity(new byte[512], identity);
            AssertFailure(result, DiameterCodecStatus.Invalid, "invalid-diameter-identity");
            identityIndex = identityIndex + 1;
        }

        DiameterValueResult unicodeIdentity = codec.DecodeDiameterIdentity(
            Encoding.UTF8.GetBytes("n\u00f6d.example"));
        AssertFailure(
            unicodeIdentity,
            DiameterCodecStatus.Invalid,
            "invalid-diameter-identity");
    }

    [Test]
    public void DiameterUriCodecRejectsMalformedObjectsTextAndUtf8()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterUri invalidScheme = CreateUri();
        invalidScheme.Scheme = "http";
        DiameterUri invalidPort = CreateUri();
        invalidPort.Port = 0;
        DiameterUri invalidTransport = CreateUri();
        invalidTransport.Transport = "quic";
        DiameterUri invalidProtocol = CreateUri();
        invalidProtocol.Protocol = "unknown";
        DiameterUri invalidCombination = CreateUri();
        invalidCombination.Transport = "udp";

        AssertFailure(
            codec.EncodeDiameterUri(new byte[64], null),
            DiameterCodecStatus.Invalid,
            "invalid-diameter-uri");
        AssertFailure(
            codec.EncodeDiameterUri(new byte[64], invalidScheme),
            DiameterCodecStatus.Invalid,
            "invalid-uri-scheme");
        AssertFailure(
            codec.EncodeDiameterUri(new byte[64], invalidPort),
            DiameterCodecStatus.Invalid,
            "invalid-uri-port");
        AssertFailure(
            codec.EncodeDiameterUri(new byte[64], invalidTransport),
            DiameterCodecStatus.Invalid,
            "invalid-uri-transport");
        AssertFailure(
            codec.EncodeDiameterUri(new byte[64], invalidProtocol),
            DiameterCodecStatus.Invalid,
            "invalid-uri-protocol");
        AssertFailure(
            codec.EncodeDiameterUri(new byte[64], invalidCombination),
            DiameterCodecStatus.Invalid,
            "invalid-uri-transport-protocol");

        string[] invalidText = new string[]
        {
            "http://host.example",
            "aaa://",
            "aaa://bad_name.example",
            "aaa://host.example:",
            "aaa://host.example:0",
            "aaa://host.example:65536",
            "aaa://host.example:abc",
            "aaa://host.example;transport=quic",
            "aaa://host.example;protocol=unknown",
            "aaa://host.example;transport=udp",
            "aaa://host.example;transport=tcp;transport=sctp",
            "aaa://host.example/path",
            "aaa://host.example;unknown=value"
        };
        int index = 0;
        while (index < invalidText.Length)
        {
            DiameterValueResult result = codec.DecodeDiameterUri(
                Encoding.ASCII.GetBytes(invalidText[index]));
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.Uri, Is.Null);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            index = index + 1;
        }

        DiameterValueResult invalidUtf8 = codec.DecodeDiameterUri(
            new byte[] { 97, 97, 97, 58, 47, 47, 192, 128 });
        AssertFailure(invalidUtf8, DiameterCodecStatus.Invalid, "invalid-utf8");
    }

    [Test]
    public void AddressCodecRejectsUnknownTruncatedAndTrailingForms()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        byte[][] inputs = new byte[][]
        {
            new byte[0],
            new byte[] { 0 },
            new byte[] { 0, 1 },
            new byte[] { 0, 1, 192, 0, 2 },
            new byte[] { 0, 1, 192, 0, 2, 1, 0 },
            new byte[] { 0, 2 },
            new byte[] { 0, 2, 32, 1 },
            new byte[] { 0, 2, 32, 1, 13, 184, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 3 }
        };
        string[] codes = new string[]
        {
            "incomplete-address-family",
            "incomplete-address-family",
            "incomplete-address",
            "incomplete-address",
            "invalid-address-length",
            "incomplete-address",
            "incomplete-address",
            "incomplete-address",
            "unsupported-address-family"
        };

        int index = 0;
        while (index < inputs.Length)
        {
            DiameterValueResult result = codec.DecodeAddress(inputs[index]);
            DiameterCodecStatus expectedStatus = DiameterCodecStatus.Incomplete;
            if (codes[index] == "invalid-address-length" ||
                codes[index] == "unsupported-address-family")
            {
                expectedStatus = DiameterCodecStatus.Invalid;
            }

            AssertFailure(result, expectedStatus, codes[index]);
            index = index + 1;
        }

        AssertFailure(
            codec.EncodeAddress(new byte[18], null),
            DiameterCodecStatus.Invalid,
            "missing-address");
    }

    [Test]
    public void FilterRuleCodecsRejectMalformedGrammarAndNonAsciiInput()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        string[] invalidIpRules = new string[]
        {
            "allow out ip from any to any",
            "permit sideways ip from any to any",
            "permit out tcp from any to any",
            "permit out ip from 192.0.2.1/24 to any",
            "permit out ip from any 80 to any",
            "permit out 6 from any 100-50 to any",
            "permit out 17 from any to any tcpflags syn",
            "permit out ip from any to any mystery"
        };
        string[] invalidQosRules = new string[]
        {
            "tag out ip from any to any",
            "tag out ip from any to any DSCP AF49",
            "tag out ip from any to any DSCP drop",
            "meter out ip from any to any DSCP EF",
            "meter out ip from any to any metering 0 AF11 AF12",
            "meter out ip from any to any metering 100 drop AF12"
        };

        int ipIndex = 0;
        while (ipIndex < invalidIpRules.Length)
        {
            DiameterValueResult result = codec.DecodeIpFilterRule(
                Encoding.ASCII.GetBytes(invalidIpRules[ipIndex]));
            AssertFailure(result, DiameterCodecStatus.Invalid, "invalid-ip-filter-rule");
            ipIndex = ipIndex + 1;
        }

        int qosIndex = 0;
        while (qosIndex < invalidQosRules.Length)
        {
            DiameterValueResult result = codec.DecodeQosFilterRule(
                Encoding.ASCII.GetBytes(invalidQosRules[qosIndex]));
            AssertFailure(result, DiameterCodecStatus.Invalid, "invalid-qos-filter-rule");
            qosIndex = qosIndex + 1;
        }

        AssertFailure(
            codec.DecodeIpFilterRule(Encoding.UTF8.GetBytes("permit out ip from any to caf\u00e9")),
            DiameterCodecStatus.Invalid,
            "invalid-ip-filter-rule");
        AssertFailure(
            codec.EncodeQosFilterRule(new byte[0], null),
            DiameterCodecStatus.Invalid,
            "invalid-qos-filter-rule");
    }

    [Test]
    public void TimeCodecHandlesEpochEraTieAndInvalidBoundariesThroughPublicContract()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DateTimeOffset ntpEpoch = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset eraOne = new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero);
        DateTimeOffset beforeEpoch = ntpEpoch.AddSeconds(-1);
        byte[] zero = new byte[] { 0, 0, 0, 0 };
        byte[] halfEra = new byte[] { 128, 0, 0, 0 };
        long expectedTieSeconds = DiameterProtocolConstants.NtpEraSeconds / 2L -
            DiameterProtocolConstants.NtpUnixEpochOffsetSeconds;

        DiameterValueResult eraZeroResult = codec.DecodeTime(zero, ntpEpoch);
        DiameterValueResult eraOneResult = codec.DecodeTime(zero, eraOne);
        DiameterValueResult tieResult = codec.DecodeTime(halfEra, eraOne);
        DiameterValueResult earlyEncoding = codec.EncodeTime(new byte[4], beforeEpoch);
        DiameterValueResult earlyReference = codec.DecodeTime(zero, beforeEpoch);
        DiameterValueResult truncated = codec.DecodeTime(new byte[3], ntpEpoch);
        DiameterValueResult trailing = codec.DecodeTime(new byte[5], ntpEpoch);
        DiameterValueResult unfoldedBeforeEpoch = codec.DecodeTime(
            new byte[] { 255, 255, 255, 255 },
            ntpEpoch);

        Assert.Multiple(() =>
        {
            Assert.That(eraZeroResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(eraZeroResult.Time, Is.EqualTo(ntpEpoch));
            Assert.That(eraOneResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(eraOneResult.Time, Is.EqualTo(eraOne));
            Assert.That(tieResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(
                tieResult.Time,
                Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(expectedTieSeconds)));
            AssertFailure(
                earlyEncoding,
                DiameterCodecStatus.Invalid,
                "time-before-ntp-epoch");
            AssertFailure(
                earlyReference,
                DiameterCodecStatus.Invalid,
                "reference-before-ntp-epoch");
            AssertFailure(truncated, DiameterCodecStatus.Incomplete, "incomplete-time");
            AssertFailure(trailing, DiameterCodecStatus.Invalid, "invalid-time-length");
            AssertFailure(
                unfoldedBeforeEpoch,
                DiameterCodecStatus.Invalid,
                "time-before-ntp-epoch");
        });
    }

    private static DiameterAvp CreateAvp(uint code, byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = data;
        return avp;
    }

    private static DiameterLimitConfiguration CreateLimits(
        int depth,
        int count,
        int bytes)
    {
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumGroupedDepth = depth;
        limits.MaximumGroupedAvpCount = count;
        limits.MaximumGroupedBytes = bytes;
        return limits;
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private static DiameterUri CreateUri()
    {
        DiameterUri uri = new DiameterUri();
        uri.Host.Value = "peer.example";
        return uri;
    }

    private static void AssertFailure(
        DiameterValueResult result,
        DiameterCodecStatus status,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }
}
