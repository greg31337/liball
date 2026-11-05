using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace diameter.Tests;

public class PublicAvpCodecKnownVectorTests
{
    [Test]
    public void Integer32AndEnumeratedCodecsRoundTripExtremaAndKnownVectors()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        int[] values = new int[]
        {
            int.MinValue,
            -1,
            0,
            1,
            305419896,
            int.MaxValue
        };
        byte[][] vectors = new byte[][]
        {
            new byte[] { 128, 0, 0, 0 },
            new byte[] { 255, 255, 255, 255 },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 1 },
            new byte[] { 18, 52, 86, 120 },
            new byte[] { 127, 255, 255, 255 }
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] integerBytes = new byte[4];
            byte[] enumeratedBytes = new byte[4];
            DiameterValueResult integerEncoding = codec.EncodeInteger32(
                integerBytes,
                values[index]);
            DiameterValueResult enumeratedEncoding = codec.EncodeEnumerated(
                enumeratedBytes,
                values[index]);
            DiameterValueResult integerDecoding = codec.DecodeInteger32(integerBytes);
            DiameterValueResult enumeratedDecoding = codec.DecodeEnumerated(enumeratedBytes);

            AssertValueEncoding(integerEncoding, 4);
            AssertValueEncoding(enumeratedEncoding, 4);
            Assert.That(integerBytes, Is.EqualTo(vectors[index]), $"Integer32 vector {index}");
            Assert.That(enumeratedBytes, Is.EqualTo(vectors[index]), $"Enumerated vector {index}");
            AssertValueDecoding(integerDecoding, 4);
            AssertValueDecoding(enumeratedDecoding, 4);
            Assert.That(integerDecoding.Integer32, Is.EqualTo(values[index]));
            Assert.That(enumeratedDecoding.Integer32, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void Integer64CodecRoundTripsExtremaAndKnownVectors()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        long[] values = new long[]
        {
            long.MinValue,
            -1L,
            0L,
            1L,
            81985529216486895L,
            long.MaxValue
        };
        byte[][] vectors = new byte[][]
        {
            new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
            new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 },
            new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 }
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] destination = new byte[8];
            DiameterValueResult encoding = codec.EncodeInteger64(destination, values[index]);
            DiameterValueResult decoding = codec.DecodeInteger64(destination);

            AssertValueEncoding(encoding, 8);
            Assert.That(destination, Is.EqualTo(vectors[index]), $"Integer64 vector {index}");
            AssertValueDecoding(decoding, 8);
            Assert.That(decoding.Integer64, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void Unsigned32CodecRoundTripsMinimumMaximumAndKnownVectors()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        uint[] values = new uint[] { uint.MinValue, 1U, 2309737967U, uint.MaxValue };
        byte[][] vectors = new byte[][]
        {
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 1 },
            new byte[] { 137, 171, 205, 239 },
            new byte[] { 255, 255, 255, 255 }
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] destination = new byte[4];
            DiameterValueResult encoding = codec.EncodeUnsigned32(destination, values[index]);
            DiameterValueResult decoding = codec.DecodeUnsigned32(destination);

            AssertValueEncoding(encoding, 4);
            Assert.That(destination, Is.EqualTo(vectors[index]), $"Unsigned32 vector {index}");
            AssertValueDecoding(decoding, 4);
            Assert.That(decoding.Unsigned32, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void Unsigned64CodecRoundTripsMinimumMaximumAndKnownVectors()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        ulong[] values = new ulong[]
        {
            ulong.MinValue,
            1UL,
            81985529216486895UL,
            ulong.MaxValue
        };
        byte[][] vectors = new byte[][]
        {
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
            new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 },
            new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] destination = new byte[8];
            DiameterValueResult encoding = codec.EncodeUnsigned64(destination, values[index]);
            DiameterValueResult decoding = codec.DecodeUnsigned64(destination);

            AssertValueEncoding(encoding, 8);
            Assert.That(destination, Is.EqualTo(vectors[index]), $"Unsigned64 vector {index}");
            AssertValueDecoding(decoding, 8);
            Assert.That(decoding.Unsigned64, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void Float32CodecPreservesEverySelectedIeee754BitVector()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        int[] bits = new int[]
        {
            0,
            int.MinValue,
            1065353216,
            -1071644672,
            2139095039,
            -8388609,
            2139095040,
            -8388608,
            2143289345
        };

        int index = 0;
        while (index < bits.Length)
        {
            float value = BitConverter.Int32BitsToSingle(bits[index]);
            byte[] expected = GetInt32Bytes(bits[index]);
            byte[] destination = new byte[4];
            DiameterValueResult encoding = codec.EncodeFloat32(destination, value);
            DiameterValueResult decoding = codec.DecodeFloat32(destination);

            AssertValueEncoding(encoding, 4);
            Assert.That(destination, Is.EqualTo(expected), $"Float32 vector {index}");
            AssertValueDecoding(decoding, 4);
            Assert.That(
                BitConverter.SingleToInt32Bits(decoding.Float32),
                Is.EqualTo(bits[index]),
                $"Float32 bits {index}");
            index = index + 1;
        }
    }

    [Test]
    public void Float64CodecPreservesEverySelectedIeee754BitVector()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        long[] bits = new long[]
        {
            0L,
            long.MinValue,
            4607182418800017408L,
            -4611123068473966592L,
            9218868437227405311L,
            -4503599627370497L,
            9218868437227405312L,
            -4503599627370496L,
            9221120237041090561L
        };

        int index = 0;
        while (index < bits.Length)
        {
            double value = BitConverter.Int64BitsToDouble(bits[index]);
            byte[] expected = GetInt64Bytes(bits[index]);
            byte[] destination = new byte[8];
            DiameterValueResult encoding = codec.EncodeFloat64(destination, value);
            DiameterValueResult decoding = codec.DecodeFloat64(destination);

            AssertValueEncoding(encoding, 8);
            Assert.That(destination, Is.EqualTo(expected), $"Float64 vector {index}");
            AssertValueDecoding(decoding, 8);
            Assert.That(
                BitConverter.DoubleToInt64Bits(decoding.Float64),
                Is.EqualTo(bits[index]),
                $"Float64 bits {index}");
            index = index + 1;
        }
    }

    [Test]
    public void OctetStringCodecRoundTripsEmptyAndFullOctetRangeVectorsWithOwnership()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        byte[][] vectors = new byte[][]
        {
            new byte[0],
            new byte[] { 0 },
            new byte[] { 0, 1, 127, 128, 254, 255 }
        };

        int index = 0;
        while (index < vectors.Length)
        {
            byte[] destination = new byte[vectors[index].Length];
            DiameterValueResult encoding = codec.EncodeOctetString(
                destination,
                vectors[index]);
            DiameterValueResult decoding = codec.DecodeOctetString(destination);
            if (destination.Length > 0)
            {
                destination[0] = 99;
            }

            AssertValueEncoding(encoding, vectors[index].Length);
            AssertValueDecoding(decoding, vectors[index].Length);
            Assert.That(decoding.Data, Is.EqualTo(vectors[index]));
            Assert.That(decoding.Data, Is.Not.SameAs(destination));
            index = index + 1;
        }
    }

    [Test]
    public void Utf8StringCodecRoundTripsEncodingWidthBoundariesAndKnownBytes()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        string[] values = new string[]
        {
            string.Empty,
            "A",
            "\u007f\u0080\u07ff\u0800\ud7ff\ue000\udbff\udfff",
            "Gr\u00fc\u00dfe \u4e16\u754c"
        };
        byte[][] vectors = new byte[][]
        {
            new byte[0],
            new byte[] { 65 },
            new byte[]
            {
                127,
                194, 128,
                223, 191,
                224, 160, 128,
                237, 159, 191,
                238, 128, 128,
                244, 143, 191, 191
            },
            new byte[]
            {
                71, 114, 195, 188, 195, 159, 101, 32,
                228, 184, 150, 231, 149, 140
            }
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] destination = new byte[vectors[index].Length];
            DiameterValueResult encoding = codec.EncodeUtf8String(destination, values[index]);
            DiameterValueResult decoding = codec.DecodeUtf8String(destination);

            AssertValueEncoding(encoding, vectors[index].Length);
            Assert.That(destination, Is.EqualTo(vectors[index]), $"UTF8String vector {index}");
            AssertValueDecoding(decoding, vectors[index].Length);
            Assert.That(decoding.Text, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void AddressCodecRoundTripsIpv4AndIpv6KnownVectors()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        IPAddress[] addresses = new IPAddress[]
        {
            IPAddress.Parse("0.0.0.0"),
            IPAddress.Parse("255.255.255.255"),
            IPAddress.Parse("192.0.2.1"),
            IPAddress.Parse("::"),
            IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"),
            IPAddress.Parse("2001:db8::1")
        };
        byte[][] vectors = new byte[][]
        {
            new byte[] { 0, 1, 0, 0, 0, 0 },
            new byte[] { 0, 1, 255, 255, 255, 255 },
            new byte[] { 0, 1, 192, 0, 2, 1 },
            new byte[] { 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[]
            {
                0, 2, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255
            },
            new byte[]
            {
                0, 2, 32, 1, 13, 184, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1
            }
        };

        int index = 0;
        while (index < addresses.Length)
        {
            byte[] destination = new byte[vectors[index].Length];
            DiameterValueResult encoding = codec.EncodeAddress(destination, addresses[index]);
            DiameterValueResult decoding = codec.DecodeAddress(destination);

            AssertValueEncoding(encoding, vectors[index].Length);
            Assert.That(destination, Is.EqualTo(vectors[index]), $"Address vector {index}");
            AssertValueDecoding(decoding, vectors[index].Length);
            Assert.That(decoding.Address, Is.EqualTo(addresses[index]));
            index = index + 1;
        }
    }

    [Test]
    public void TimeCodecRoundTripsNtpUnixAndEraBoundaryKnownVectors()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DateTimeOffset[] values = new DateTimeOffset[]
        {
            new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero),
            DateTimeOffset.UnixEpoch,
            new DateTimeOffset(2036, 2, 7, 6, 28, 15, TimeSpan.Zero),
            new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero),
            new DateTimeOffset(2036, 2, 7, 6, 28, 17, TimeSpan.Zero)
        };
        byte[][] vectors = new byte[][]
        {
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 131, 170, 126, 128 },
            new byte[] { 255, 255, 255, 255 },
            new byte[] { 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 1 }
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] destination = new byte[4];
            DiameterValueResult encoding = codec.EncodeTime(destination, values[index]);
            DiameterValueResult decoding = codec.DecodeTime(destination, values[index]);

            AssertValueEncoding(encoding, 4);
            Assert.That(destination, Is.EqualTo(vectors[index]), $"Time vector {index}");
            AssertValueDecoding(decoding, 4);
            Assert.That(decoding.Time, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void DiameterIdentityCodecRoundTripsCanonicalDnsBoundaryForms()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        string[] inputs = new string[]
        {
            "A.",
            "NODE.Example.COM.",
            new string('a', 63) + "." + new string('B', 63) + ".example."
        };
        string[] canonical = new string[]
        {
            "a",
            "node.example.com",
            new string('a', 63) + "." + new string('b', 63) + ".example"
        };

        int index = 0;
        while (index < inputs.Length)
        {
            DiameterIdentity identity = new DiameterIdentity();
            identity.Value = inputs[index];
            byte[] expected = Encoding.ASCII.GetBytes(canonical[index]);
            byte[] destination = new byte[expected.Length];
            DiameterValueResult encoding = codec.EncodeDiameterIdentity(destination, identity);
            DiameterValueResult decoding = codec.DecodeDiameterIdentity(destination);

            AssertValueEncoding(encoding, expected.Length);
            Assert.That(destination, Is.EqualTo(expected));
            AssertValueDecoding(decoding, expected.Length);
            Assert.That(decoding.Identity!.Value, Is.EqualTo(canonical[index]));
            Assert.That(decoding.Text, Is.EqualTo(canonical[index]));
            index = index + 1;
        }
    }

    [Test]
    public void DiameterUriCodecRoundTripsDefaultSecureAndExplicitKnownForms()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterUri[] values = new DiameterUri[]
        {
            CreateUri("aaa", "HOST.Example.", 3868, "tcp", "diameter"),
            CreateUri("aaas", "SECURE.Example.", 5868, "tcp", "diameter"),
            CreateUri("aaa", "SCTP.Example.", 6666, "sctp", "diameter"),
            CreateUri("aaa", "RADIUS.Example.", 1813, "udp", "radius")
        };
        string[] canonical = new string[]
        {
            "aaa://host.example",
            "aaas://secure.example",
            "aaa://sctp.example:6666;transport=sctp",
            "aaa://radius.example:1813;transport=udp;protocol=radius"
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] expected = Encoding.ASCII.GetBytes(canonical[index]);
            byte[] destination = new byte[expected.Length];
            DiameterValueResult encoding = codec.EncodeDiameterUri(destination, values[index]);
            DiameterValueResult decoding = codec.DecodeDiameterUri(destination);

            AssertValueEncoding(encoding, expected.Length);
            Assert.That(destination, Is.EqualTo(expected), $"DiameterURI vector {index}");
            AssertValueDecoding(decoding, expected.Length);
            Assert.That(decoding.Text, Is.EqualTo(canonical[index]));
            Assert.That(decoding.Uri, Is.Not.Null);
            index = index + 1;
        }
    }

    [Test]
    public void FilterRuleCodecsRoundTripRepresentativeKnownForms()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        string[] ipRules = new string[]
        {
            "permit out ip from any to assigned",
            "deny in 6 from 192.0.2.0/24 80,443 to !198.51.100.0/24 1024-65535 established tcpflags syn,!ack",
            "permit in 17 from 2001:db8::/32 53 to any"
        };
        string[] qosRules = new string[]
        {
            "tag out 17 from 192.0.2.0/24 53 to any DSCP AF21",
            "tag in ip from any to assigned DSCP 46",
            "meter out 6 from any to 2001:db8::1 443 metering 250000 CS1 EF"
        };

        int index = 0;
        while (index < ipRules.Length)
        {
            byte[] ipBytes = Encoding.ASCII.GetBytes(ipRules[index]);
            byte[] ipDestination = new byte[ipBytes.Length];
            DiameterValueResult ipEncoding = codec.EncodeIpFilterRule(
                ipDestination,
                ipRules[index]);
            DiameterValueResult ipDecoding = codec.DecodeIpFilterRule(ipDestination);
            AssertValueEncoding(ipEncoding, ipBytes.Length);
            Assert.That(ipDestination, Is.EqualTo(ipBytes));
            AssertValueDecoding(ipDecoding, ipBytes.Length);
            Assert.That(ipDecoding.Text, Is.EqualTo(ipRules[index]));

            byte[] qosBytes = Encoding.ASCII.GetBytes(qosRules[index]);
            byte[] qosDestination = new byte[qosBytes.Length];
            DiameterValueResult qosEncoding = codec.EncodeQosFilterRule(
                qosDestination,
                qosRules[index]);
            DiameterValueResult qosDecoding = codec.DecodeQosFilterRule(qosDestination);
            AssertValueEncoding(qosEncoding, qosBytes.Length);
            Assert.That(qosDestination, Is.EqualTo(qosBytes));
            AssertValueDecoding(qosDecoding, qosBytes.Length);
            Assert.That(qosDecoding.Text, Is.EqualTo(qosRules[index]));
            index = index + 1;
        }
    }

    [Test]
    public void GroupedCodecRoundTripsEmptyOrderedRepeatedAndVendorAvpsAgainstKnownBytes()
    {
        IDiameterAvpCodec codec = new DiameterAvpCodec();
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        List<DiameterAvp> groupedTypes = new List<DiameterAvp>();
        List<DiameterAvp> empty = new List<DiameterAvp>();
        DiameterValueResult emptyMeasurement = codec.MeasureGrouped(
            empty,
            groupedTypes,
            limits);
        DiameterValueResult emptyEncoding = codec.EncodeGrouped(
            new byte[0],
            empty,
            groupedTypes,
            limits);
        DiameterValueResult emptyDecoding = codec.DecodeGrouped(
            new byte[0],
            groupedTypes,
            limits);

        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(CreateAvp(1U, DiameterAvpFlags.Mandatory, 0U, new byte[] { 170 }));
        avps.Add(CreateAvp(
            1U,
            DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Protected,
            10415U,
            new byte[] { 187, 204 }));
        byte[] expected = new byte[]
        {
            0, 0, 0, 1,
            64, 0, 0, 9,
            170, 0, 0, 0,
            0, 0, 0, 1,
            160, 0, 0, 14,
            0, 0, 40, 175,
            187, 204, 0, 0
        };
        DiameterValueResult measurement = codec.MeasureGrouped(avps, groupedTypes, limits);
        byte[] destination = new byte[measurement.RequiredCount];
        DiameterValueResult encoding = codec.EncodeGrouped(
            destination,
            avps,
            groupedTypes,
            limits);
        DiameterValueResult decoding = codec.DecodeGrouped(
            destination,
            groupedTypes,
            limits);

        Assert.Multiple(() =>
        {
            AssertValueMeasurement(emptyMeasurement, 0);
            AssertValueEncoding(emptyEncoding, 0);
            AssertValueDecoding(emptyDecoding, 0);
            AssertValueMeasurement(measurement, expected.Length);
            AssertValueEncoding(encoding, expected.Length);
            Assert.That(destination, Is.EqualTo(expected));
            AssertValueDecoding(decoding, expected.Length);
            Assert.That(decoding.Avps, Has.Count.EqualTo(2));
            Assert.That(decoding.Avps[0].Code, Is.EqualTo(1U));
            Assert.That(decoding.Avps[0].Data, Is.EqualTo(new byte[] { 170 }));
            Assert.That(decoding.Avps[1].Code, Is.EqualTo(1U));
            Assert.That(decoding.Avps[1].VendorId, Is.EqualTo(10415U));
            Assert.That(decoding.Avps[1].Data, Is.EqualTo(new byte[] { 187, 204 }));
        });
    }

    private static byte[] GetInt32Bytes(int value)
    {
        return new byte[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
    }

    private static byte[] GetInt64Bytes(long value)
    {
        return new byte[]
        {
            (byte)(value >> 56),
            (byte)(value >> 48),
            (byte)(value >> 40),
            (byte)(value >> 32),
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
    }

    private static DiameterUri CreateUri(
        string scheme,
        string host,
        int port,
        string transport,
        string protocol)
    {
        DiameterUri uri = new DiameterUri();
        uri.Scheme = scheme;
        uri.Host.Value = host;
        uri.Port = port;
        uri.Transport = transport;
        uri.Protocol = protocol;
        return uri;
    }

    private static DiameterAvp CreateAvp(
        uint code,
        DiameterAvpFlags flags,
        uint vendorId,
        byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = flags;
        avp.VendorId = vendorId;
        avp.Data = data;
        return avp;
    }

    private static void AssertValueMeasurement(DiameterValueResult result, int requiredCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertValueEncoding(DiameterValueResult result, int requiredCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.WrittenCount, Is.EqualTo(requiredCount));
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertValueDecoding(DiameterValueResult result, int requiredCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.ConsumedCount, Is.EqualTo(requiredCount));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Is.Empty);
    }
}
