using rtp;

namespace rtp.Tests.Robustness;

public class RtpHostileInputCorpusTests
{
    [Test]
    public void DeclaredLengthAndCountBombsFailWithoutProducingModels()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);
        byte[][] rtpBombs = new byte[][]
        {
            CreateRtpExtensionLengthBomb(),
            CreateRtpContributingSourceCountBomb()
        };
        byte[][] rtcpBombs = new byte[][]
        {
            new byte[] { 0x80, 208, 0xFF, 0xFF },
            new byte[] { 0x9F, 200, 0, 1, 0, 0, 0, 1 },
            new byte[] { 0x9F, 201, 0, 1, 0, 0, 0, 1 },
            new byte[] { 0x9F, 202, 0, 0 },
            new byte[] { 0x9F, 203, 0, 0 }
        };

        int rtpIndex = 0;
        while (rtpIndex < rtpBombs.Length)
        {
            RtpCodecResult result = rtpCodec.TryDecodeRtpPacket(
                rtpBombs[rtpIndex]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.Packet, Is.Null);
                Assert.That(result.ErrorMessage, Is.Not.Empty);
            });
            rtpIndex = rtpIndex + 1;
        }

        int rtcpIndex = 0;
        while (rtcpIndex < rtcpBombs.Length)
        {
            RtcpCodecResult result = rtcpCodec.TryDecodeRtcpPacket(
                rtcpBombs[rtcpIndex]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.Packet, Is.Null);
                Assert.That(result.ErrorMessage, Is.Not.Empty);
            });
            rtcpIndex = rtcpIndex + 1;
        }
    }

    [Test]
    public void InvalidNestingAndMalformedUtf8FailInBothValidationModes()
    {
        RtpSessionConfiguration strictConfiguration = CreateConfiguration();
        strictConfiguration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        RtpSessionConfiguration tolerantConfiguration =
            CreateConfiguration();
        tolerantConfiguration.StrictValidation = false;
        tolerantConfiguration.RtcpCompoundMode =
            RtcpCompoundMode.ReducedSize;
        IRtpPacketCodec strictRtp =
            new RtpPacketCodec(strictConfiguration);
        IRtpPacketCodec tolerantRtp =
            new RtpPacketCodec(tolerantConfiguration);
        IRtcpPacketCodec strictRtcp =
            new RtcpPacketCodec(strictConfiguration);
        IRtcpPacketCodec tolerantRtcp =
            new RtcpPacketCodec(tolerantConfiguration);
        byte[][] nestedRtp = new byte[][]
        {
            CreateMalformedOneByteExtension(),
            CreateMalformedTwoByteExtension()
        };
        byte[][] nestedRtcp = new byte[][]
        {
            CreateMalformedPrivateSourceDescription(),
            CreateExtendedReportBlockLengthBomb(),
            CreateTruncatedFullIntraRequest(),
            CreateMalformedCanonicalName(),
            CreateMalformedGoodbyeReason()
        };

        int rtpIndex = 0;
        while (rtpIndex < nestedRtp.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(strictRtp.TryDecodeRtpPacket(
                    nestedRtp[rtpIndex]).Success, Is.False);
                Assert.That(tolerantRtp.TryDecodeRtpPacket(
                    nestedRtp[rtpIndex]).Success, Is.False);
            });
            rtpIndex = rtpIndex + 1;
        }

        int rtcpIndex = 0;
        while (rtcpIndex < nestedRtcp.Length)
        {
            RtcpCodecResult strict = strictRtcp.TryDecodeRtcpPacket(
                nestedRtcp[rtcpIndex]);
            RtcpCodecResult tolerant = tolerantRtcp.TryDecodeRtcpPacket(
                nestedRtcp[rtcpIndex]);
            Assert.Multiple(() =>
            {
                Assert.That(strict.Success, Is.False);
                Assert.That(strict.Packet, Is.Null);
                Assert.That(tolerant.Success, Is.False);
                Assert.That(tolerant.Packet, Is.Null);
            });
            rtcpIndex = rtcpIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(strictRtcp.TryDecodeRtcpPacket(
                CreateMalformedCanonicalName()).ErrorMessage,
                Does.Contain("UTF-8"));
            Assert.That(tolerantRtcp.TryDecodeRtcpPacket(
                CreateMalformedGoodbyeReason()).ErrorMessage,
                Does.Contain("UTF-8"));
        });
    }

    [Test]
    public void DeterministicRandomBytesNeverEscapeTryDecodeContracts()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.StrictValidation = false;
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        configuration.MaximumRtpPacketSize = 256;
        configuration.MaximumRtcpCompoundPacketSize = 256;
        configuration.MaximumRtcpCompoundPacketCount = 32;
        IRtpPacketCodec rtpCodec = new RtpPacketCodec(configuration);
        IRtcpPacketCodec rtcpCodec = new RtcpPacketCodec(configuration);
        Random random = new Random(126126);
        int invalidSuccessCount = 0;
        int corpusIndex = 0;
        while (corpusIndex < 2000)
        {
            byte[] source = new byte[random.Next(0, 257)];
            random.NextBytes(source);
            RtpCodecResult rtp = rtpCodec.TryDecodeRtpPacket(source);
            RtcpCodecResult rtcp = rtcpCodec.TryDecodeRtcpPacket(source);
            RtcpCodecResult compound =
                rtcpCodec.TryDecodeRtcpCompoundPacket(source);

            if (rtp.Success &&
                (rtp.Packet == null ||
                rtp.ConsumedCount != source.Length ||
                rtpCodec.EncodeRtpPacketToArray(rtp.Packet).Length > 256))
            {
                invalidSuccessCount = invalidSuccessCount + 1;
            }

            if (rtcp.Success &&
                (rtcp.Packet == null ||
                rtcp.ConsumedCount <= 0 ||
                rtcp.ConsumedCount > source.Length ||
                rtcpCodec.EncodeRtcpPacketToArray(rtcp.Packet).Length > 256))
            {
                invalidSuccessCount = invalidSuccessCount + 1;
            }

            if (compound.Success &&
                (compound.CompoundPacket == null ||
                compound.ConsumedCount != source.Length ||
                compound.CompoundPacket.Packets.Length > 32))
            {
                invalidSuccessCount = invalidSuccessCount + 1;
            }

            corpusIndex = corpusIndex + 1;
        }

        Assert.That(invalidSuccessCount, Is.Zero);
    }

    [Test]
    public void RepeatedUnknownPacketsRespectCountAndSizeLimits()
    {
        RtpSessionConfiguration validConfiguration = CreateConfiguration();
        validConfiguration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        validConfiguration.MaximumRtcpCompoundPacketSize = 256;
        validConfiguration.MaximumRtcpCompoundPacketCount = 16;
        IRtcpPacketCodec validCodec =
            new RtcpPacketCodec(validConfiguration);
        byte[] sixteenUnknownPackets = CreateRepeatedUnknownPackets(16);
        RtcpCodecResult valid = validCodec.TryDecodeRtcpCompoundPacket(
            sixteenUnknownPackets);
        RtcpCodecResult countBomb = validCodec.TryDecodeRtcpCompoundPacket(
            CreateRepeatedUnknownPackets(17));

        RtpSessionConfiguration countConfiguration = CreateConfiguration();
        countConfiguration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        countConfiguration.MaximumRtcpCompoundPacketSize = 256;
        countConfiguration.MaximumRtcpCompoundPacketCount = 4;
        RtcpCodecResult configuredCount = new RtcpPacketCodec(
            countConfiguration).TryDecodeRtcpCompoundPacket(
                CreateRepeatedUnknownPackets(5));

        RtpSessionConfiguration sizeConfiguration = CreateConfiguration();
        sizeConfiguration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        sizeConfiguration.MaximumRtcpCompoundPacketSize = 64;
        sizeConfiguration.MaximumRtcpCompoundPacketCount = 32;
        RtcpCodecResult configuredSize = new RtcpPacketCodec(
            sizeConfiguration).TryDecodeRtcpCompoundPacket(
                CreateRepeatedUnknownPackets(9));

        RtpSessionConfiguration rtpSizeConfiguration = CreateConfiguration();
        rtpSizeConfiguration.MaximumRtpPacketSize = 20;
        byte[] oversizedRtp = new byte[21];
        oversizedRtp[0] = 0x80;
        RtpCodecResult configuredRtpSize = new RtpPacketCodec(
            rtpSizeConfiguration).TryDecodeRtpPacket(oversizedRtp);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.ConsumedCount,
                Is.EqualTo(sixteenUnknownPackets.Length));
            Assert.That(valid.CompoundPacket?.Packets,
                Has.Length.EqualTo(16));
            Assert.That(valid.CompoundPacket?.Packets.All(
                value => value.Type == RtcpPacketType.Unknown), Is.True);
            Assert.That(countBomb.Success, Is.False);
            Assert.That(countBomb.ErrorMessage, Does.Contain("count"));
            Assert.That(configuredCount.Success, Is.False);
            Assert.That(configuredCount.ErrorMessage, Does.Contain("count"));
            Assert.That(configuredSize.Success, Is.False);
            Assert.That(configuredSize.ErrorMessage, Does.Contain("size"));
            Assert.That(configuredRtpSize.Success, Is.False);
            Assert.That(configuredRtpSize.ErrorMessage,
                Does.Contain("maximum packet size"));
        });
    }

    [Test]
    public void SessionIngressRejectsCorpusWithoutMutatingSourceState()
    {
        RtpSessionConfiguration configuration = CreateConfiguration();
        configuration.MaximumRtpPacketSize = 64;
        configuration.MaximumRtcpCompoundPacketSize = 64;
        configuration.MaximumRtcpCompoundPacketCount = 4;
        RtpSession session = CreateSession(configuration);
        RtpPacketContext context = CreateContext();
        byte[][] rtpCorpus = new byte[][]
        {
            CreateRtpExtensionLengthBomb(),
            CreateRtpContributingSourceCountBomb(),
            CreateMalformedOneByteExtension(),
            CreateMalformedTwoByteExtension(),
            CreateInvalidVersionRandomBytes(17, 101),
            CreateInvalidVersionRandomBytes(31, 102)
        };
        byte[][] rtcpCorpus = new byte[][]
        {
            new byte[] { 0x80, 208, 0xFF, 0xFF },
            new byte[] { 0x9F, 201, 0, 1, 0, 0, 0, 1 },
            CreateMalformedPrivateSourceDescription(),
            CreateExtendedReportBlockLengthBomb(),
            CreateTruncatedFullIntraRequest(),
            CreateMalformedCanonicalName(),
            CreateMalformedGoodbyeReason(),
            CreateInvalidVersionRandomBytes(20, 103)
        };
        List<RtpSessionUpdate> updates = new List<RtpSessionUpdate>();

        int rtpIndex = 0;
        while (rtpIndex < rtpCorpus.Length)
        {
            updates.Add(session.ProcessInboundRtpDatagram(
                rtpCorpus[rtpIndex],
                context));
            rtpIndex = rtpIndex + 1;
        }

        int rtcpIndex = 0;
        while (rtcpIndex < rtcpCorpus.Length)
        {
            updates.Add(session.ProcessInboundRtcpDatagram(
                rtcpCorpus[rtcpIndex],
                context));
            rtcpIndex = rtcpIndex + 1;
        }

        RtpSessionSnapshot snapshot = session.GetSessionSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(updates.All(value =>
                value.AcceptedPackets.Length == 0 &&
                value.Events.Length == 1 &&
                value.Events[0].Type ==
                    RtpSessionEventType.RecoverableError), Is.True);
            Assert.That(snapshot.Sources, Has.Length.EqualTo(1));
            Assert.That(snapshot.Sources[0].IsLocal, Is.True);
            Assert.That(snapshot.MemberCount, Is.EqualTo(1));
            Assert.That(snapshot.SenderCount, Is.Zero);
        });
    }

    static byte[] CreateRtpExtensionLengthBomb()
    {
        return new byte[]
        {
            0x90, 96, 0, 1,
            0, 0, 0, 1,
            0, 0, 0, 1,
            0xBE, 0xDE, 0xFF, 0xFF
        };
    }

    static byte[] CreateRtpContributingSourceCountBomb()
    {
        byte[] source = new byte[RtpProtocolConstants.FixedHeaderSize];
        source[0] = 0x8F;
        source[1] = 96;
        return source;
    }

    static byte[] CreateMalformedOneByteExtension()
    {
        return new byte[]
        {
            0x90, 96, 0, 1,
            0, 0, 0, 1,
            0, 0, 0, 1,
            0xBE, 0xDE, 0, 1,
            0x1F, 0, 0, 0
        };
    }

    static byte[] CreateMalformedTwoByteExtension()
    {
        return new byte[]
        {
            0x90, 96, 0, 1,
            0, 0, 0, 1,
            0, 0, 0, 1,
            0x10, 0, 0, 1,
            1, 0xFF, 0, 0
        };
    }

    static byte[] CreateMalformedPrivateSourceDescription()
    {
        return new byte[]
        {
            0x81, 202, 0, 2,
            0, 0, 0, 1,
            8, 2, 3, 0
        };
    }

    static byte[] CreateExtendedReportBlockLengthBomb()
    {
        return new byte[]
        {
            0x80, 207, 0, 2,
            0, 0, 0, 1,
            4, 0, 0xFF, 0xFF
        };
    }

    static byte[] CreateTruncatedFullIntraRequest()
    {
        return new byte[]
        {
            0x84, 206, 0, 3,
            0, 0, 0, 1,
            0, 0, 0, 2,
            0, 0, 0, 3
        };
    }

    static byte[] CreateMalformedCanonicalName()
    {
        return new byte[]
        {
            0x81, 202, 0, 3,
            0, 0, 0, 1,
            1, 2, 0xC0, 0x80,
            0, 0, 0, 0
        };
    }

    static byte[] CreateMalformedGoodbyeReason()
    {
        return new byte[]
        {
            0x81, 203, 0, 2,
            0, 0, 0, 1,
            2, 0xC0, 0x80, 0
        };
    }

    static byte[] CreateRepeatedUnknownPackets(int packetCount)
    {
        byte[] source = new byte[packetCount * 8];
        int packetIndex = 0;
        while (packetIndex < packetCount)
        {
            int offset = packetIndex * 8;
            source[offset] = (byte)(0x80 | packetIndex % 32);
            source[offset + 1] = 223;
            source[offset + 2] = 0;
            source[offset + 3] = 1;
            source[offset + 4] = (byte)packetIndex;
            source[offset + 5] = 0xA5;
            source[offset + 6] = 0x5A;
            source[offset + 7] = (byte)(255 - packetIndex);
            packetIndex = packetIndex + 1;
        }

        return source;
    }

    static byte[] CreateInvalidVersionRandomBytes(int length, int seed)
    {
        byte[] source = new byte[length];
        new Random(seed).NextBytes(source);
        source[0] = (byte)(source[0] & 0x3F);
        return source;
    }

    static RtpSession CreateSession(RtpSessionConfiguration configuration)
    {
        return new RtpSession(
            configuration,
            new FixedClock(),
            new FixedRandomSource(),
            CreatePayloadRegistry(),
            new RtpHeaderExtensionRegistry(),
            null,
            null,
            null,
            null);
    }

    static RtpSessionConfiguration CreateConfiguration()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Separate;
        configuration.RtcpCompoundMode = RtcpCompoundMode.Full;
        configuration.MinimumSequential = 1;
        configuration.MaximumSourceCount = 8;
        configuration.CanonicalName = "hostile@example";
        configuration.RtcpScheduler.SessionBandwidthBitsPerSecond = 64000.0;
        return configuration;
    }

    static RtpPayloadTypeRegistry CreatePayloadRegistry()
    {
        RtpPayloadTypeRegistry registry = new RtpPayloadTypeRegistry(
            RtpSessionTransportMode.Separate);
        RtpPayloadTypeDefinition definition =
            new RtpPayloadTypeDefinition();
        definition.PayloadType = 96;
        definition.ClockRate = 90000;
        definition.MediaIdentity = "video";
        definition.ChannelCount = 1;
        registry.SetPayloadTypeDefinition(definition);
        return registry;
    }

    static RtpPacketContext CreateContext()
    {
        RtpPacketContext context = new RtpPacketContext();
        context.TransportIdentity = "hostile-peer";
        context.ClockRate = 90000;
        return context;
    }

    private class FixedClock : IRtpClock
    {
        public long GetMonotonicTimeTicks()
        {
            return 0L;
        }

        public long GetUtcTimeTicks()
        {
            return 0L;
        }

        public ulong GetNtpTimestamp()
        {
            return 0UL;
        }
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return 100U;
        }

        public ushort CreateSequenceNumber()
        {
            return 10;
        }

        public uint CreateTimestamp()
        {
            return 1000U;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }
}
