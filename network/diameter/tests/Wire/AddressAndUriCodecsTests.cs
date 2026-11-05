using System;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class AddressAndUriCodecsTests
{
    [Test]
    public void AddressCodecUsesIanaFamiliesAndNetworkOctetOrder()
    {
        IPAddress ipv4 = IPAddress.Parse("192.0.2.1");
        IPAddress ipv6 = IPAddress.Parse("2001:db8::1");
        byte[] encodedIpv4 = new byte[6];
        byte[] encodedIpv6 = new byte[18];

        DiameterValueResult ipv4Encoding = ValueCodecs.EncodeAddress(encodedIpv4, ipv4);
        DiameterValueResult ipv6Encoding = ValueCodecs.EncodeAddress(encodedIpv6, ipv6);
        DiameterValueResult ipv4Decoding = ValueCodecs.DecodeAddress(encodedIpv4);
        DiameterValueResult ipv6Decoding = ValueCodecs.DecodeAddress(encodedIpv6);

        Assert.Multiple(() =>
        {
            AssertSuccess(ipv4Encoding, 6, 6, 0);
            AssertSuccess(ipv6Encoding, 18, 18, 0);
            Assert.That(encodedIpv4, Is.EqualTo(new byte[] { 0, 1, 192, 0, 2, 1 }));
            Assert.That(
                encodedIpv6,
                Is.EqualTo(new byte[]
                {
                    0, 2,
                    32, 1, 13, 184,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 1
                }));
            AssertSuccess(ipv4Decoding, 6, 0, 6);
            AssertSuccess(ipv6Decoding, 18, 0, 18);
            Assert.That(ipv4Decoding.Address, Is.EqualTo(ipv4));
            Assert.That(ipv6Decoding.Address, Is.EqualTo(ipv6));
        });
    }

    [Test]
    public void AddressDecoderRejectsShortUnknownAndTrailingForms()
    {
        DiameterValueResult shortFamily = ValueCodecs.DecodeAddress(new byte[] { 0 });
        DiameterValueResult shortIpv4 = ValueCodecs.DecodeAddress(
            new byte[] { 0, 1, 192, 0, 2 });
        DiameterValueResult shortIpv6 = ValueCodecs.DecodeAddress(new byte[] { 0, 2, 32, 1 });
        DiameterValueResult unknown = ValueCodecs.DecodeAddress(new byte[] { 0, 3 });
        DiameterValueResult trailing = ValueCodecs.DecodeAddress(
            new byte[] { 0, 1, 192, 0, 2, 1, 0 });

        Assert.Multiple(() =>
        {
            AssertFailure(shortFamily, DiameterCodecStatus.Incomplete, 2, 1, "incomplete-address-family");
            AssertFailure(shortIpv4, DiameterCodecStatus.Incomplete, 6, 5, "incomplete-address");
            AssertFailure(shortIpv6, DiameterCodecStatus.Incomplete, 18, 4, "incomplete-address");
            AssertFailure(unknown, DiameterCodecStatus.Invalid, 2, 0, "unsupported-address-family");
            AssertFailure(trailing, DiameterCodecStatus.Invalid, 6, 6, "invalid-address-length");
        });
    }

    [Test]
    public void AddressEncoderRejectsMissingOrShortDestinationWithoutWriting()
    {
        byte[] destination = new byte[] { 170, 170, 170, 170, 170 };

        DiameterValueResult missing = ValueCodecs.EncodeAddress(new byte[0], null);
        DiameterValueResult shortDestination = ValueCodecs.EncodeAddress(
            destination,
            IPAddress.Parse("192.0.2.1"));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(missing.Issues[0].Code, Is.EqualTo("missing-address"));
            Assert.That(shortDestination.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
            Assert.That(shortDestination.RequiredCount, Is.EqualTo(6));
            Assert.That(shortDestination.WrittenCount, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [Test]
    public void DiameterUriParserAppliesDefaultsAndCanonicalNormalization()
    {
        DiameterValueResult defaultUri = ValueCodecs.ParseDiameterUri(
            "AAA://HOST.Example.COM.");
        DiameterValueResult secureUri = ValueCodecs.ParseDiameterUri(
            "AAAS://Secure.Example.COM.");
        DiameterValueResult explicitUri = ValueCodecs.ParseDiameterUri(
            "aaa://HOST.Example.COM.:06666;transport=SCTP;protocol=DIAMETER");
        DiameterValueResult radiusUri = ValueCodecs.ParseDiameterUri(
            "aaa://radius.example:1813;transport=udp;protocol=radius");

        Assert.Multiple(() =>
        {
            AssertUri(defaultUri, "aaa", "host.example.com", 3868, "tcp", "diameter");
            Assert.That(defaultUri.Text, Is.EqualTo("aaa://host.example.com"));
            AssertUri(secureUri, "aaas", "secure.example.com", 5868, "tcp", "diameter");
            Assert.That(secureUri.Text, Is.EqualTo("aaas://secure.example.com"));
            AssertUri(explicitUri, "aaa", "host.example.com", 6666, "sctp", "diameter");
            Assert.That(
                explicitUri.Text,
                Is.EqualTo("aaa://host.example.com:6666;transport=sctp"));
            AssertUri(radiusUri, "aaa", "radius.example", 1813, "udp", "radius");
            Assert.That(
                radiusUri.Text,
                Is.EqualTo("aaa://radius.example:1813;transport=udp;protocol=radius"));
        });
    }

    [Test]
    public void DiameterUriCodecRoundTripsCanonicalAsciiForm()
    {
        DiameterUri uri = new DiameterUri();
        uri.Scheme = "AAAS";
        uri.Host.Value = "Peer.Example.COM.";
        uri.Port = DiameterProtocolConstants.SecurePort;
        uri.Transport = "SCTP";
        uri.Protocol = "DIAMETER";
        byte[] destination = new byte[38];

        DiameterValueResult encoding = ValueCodecs.EncodeDiameterUri(destination, uri);
        DiameterValueResult decoding = ValueCodecs.DecodeDiameterUri(destination);
        destination[7] = (byte)'x';

        Assert.Multiple(() =>
        {
            AssertSuccess(encoding, 38, 38, 0);
            Assert.That(encoding.Text, Is.EqualTo("aaas://peer.example.com;transport=sctp"));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.RequiredCount, Is.EqualTo(38));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(38));
            AssertUri(decoding, "aaas", "peer.example.com", 5868, "sctp", "diameter");
            Assert.That(decoding.Text, Is.EqualTo("aaas://peer.example.com;transport=sctp"));
        });
    }

    [Test]
    public void DiameterUriParserRejectsMalformedComponentsAndCombinations()
    {
        string[] invalidValues = new string[]
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
            "aaa://host.example;protocol=radius;transport=udp",
            "aaa://host.example/path",
            "aaa://host.example;unknown=value"
        };

        int index = 0;
        while (index < invalidValues.Length)
        {
            DiameterValueResult result = ValueCodecs.ParseDiameterUri(invalidValues[index]);
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.Uri, Is.Null);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Message, Is.Not.Empty);
            index = index + 1;
        }
    }

    [Test]
    public void DiameterUriNormalizationRejectsInvalidObjectFields()
    {
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

        Assert.Multiple(() =>
        {
            AssertCode(ValueCodecs.NormalizeDiameterUri(null), "invalid-diameter-uri");
            AssertCode(ValueCodecs.NormalizeDiameterUri(invalidScheme), "invalid-uri-scheme");
            AssertCode(ValueCodecs.NormalizeDiameterUri(invalidPort), "invalid-uri-port");
            AssertCode(ValueCodecs.NormalizeDiameterUri(invalidTransport), "invalid-uri-transport");
            AssertCode(ValueCodecs.NormalizeDiameterUri(invalidProtocol), "invalid-uri-protocol");
            AssertCode(
                ValueCodecs.NormalizeDiameterUri(invalidCombination),
                "invalid-uri-transport-protocol");
        });
    }

    [Test]
    public void DiameterUriCodecRejectsInvalidUtf8AndShortDestination()
    {
        DiameterValueResult invalidUtf8 = ValueCodecs.DecodeDiameterUri(
            new byte[] { 97, 97, 97, 58, 47, 47, 192, 128 });
        DiameterUri uri = CreateUri();
        byte[] destination = new byte[5];
        Array.Fill(destination, (byte)170);
        DiameterValueResult shortDestination = ValueCodecs.EncodeDiameterUri(destination, uri);

        Assert.Multiple(() =>
        {
            Assert.That(invalidUtf8.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(invalidUtf8.Issues[0].Code, Is.EqualTo("invalid-utf8"));
            Assert.That(shortDestination.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
            Assert.That(shortDestination.RequiredCount, Is.EqualTo(18));
            Assert.That(shortDestination.WrittenCount, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    private static DiameterUri CreateUri()
    {
        DiameterUri uri = new DiameterUri();
        uri.Host.Value = "peer.example";
        return uri;
    }

    private static void AssertSuccess(
        DiameterValueResult result,
        int requiredCount,
        int writtenCount,
        int consumedCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.WrittenCount, Is.EqualTo(writtenCount));
        Assert.That(result.ConsumedCount, Is.EqualTo(consumedCount));
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertFailure(
        DiameterValueResult result,
        DiameterCodecStatus status,
        int requiredCount,
        int offset,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }

    private static void AssertUri(
        DiameterValueResult result,
        string scheme,
        string host,
        int port,
        string transport,
        string protocol)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.Uri, Is.Not.Null);
        Assert.That(result.Uri!.Scheme, Is.EqualTo(scheme));
        Assert.That(result.Uri.Host.Value, Is.EqualTo(host));
        Assert.That(result.Uri.Port, Is.EqualTo(port));
        Assert.That(result.Uri.Transport, Is.EqualTo(transport));
        Assert.That(result.Uri.Protocol, Is.EqualTo(protocol));
    }

    private static void AssertCode(DiameterValueResult result, string code)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }
}
