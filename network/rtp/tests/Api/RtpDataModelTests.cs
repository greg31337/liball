using rtp;

namespace rtp.Tests.Api;

public class RtpDataModelTests
{
    [Test]
    public void PacketAndCollectionDefaultsAreReadyForUse()
    {
        RtpPacket packet = new RtpPacket();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        RtpCodecResult codecResult = new RtpCodecResult();
        RtpMixerAudioLevelExtension mixerLevels = new RtpMixerAudioLevelExtension();

        Assert.Multiple(() =>
        {
            Assert.That(packet.Version, Is.EqualTo(RtpProtocolConstants.Version));
            Assert.That(packet.HasPadding, Is.False);
            Assert.That(packet.HasExtension, Is.False);
            Assert.That(packet.ContributingSources, Is.Empty);
            Assert.That(packet.HeaderExtension, Is.Null);
            Assert.That(packet.Payload, Is.Empty);
            Assert.That(packet.Padding, Is.Empty);
            Assert.That(extension.Elements, Is.Empty);
            Assert.That(extension.Data, Is.Empty);
            Assert.That(element.Data, Is.Empty);
            Assert.That(codecResult.Data, Is.Empty);
            Assert.That(mixerLevels.Levels, Is.Empty);
        });
    }

    [Test]
    public void PacketCopyDeepCopiesWireArraysAndHeaderExtension()
    {
        RtpHeaderExtensionElement element = new RtpHeaderExtensionElement();
        element.Identifier = 3;
        element.Data = new byte[] { 10, 11 };
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = RtpHeaderExtensionProfiles.OneByte;
        extension.Format = RtpHeaderExtensionFormat.OneByte;
        extension.ApplicationBits = 0;
        extension.Elements = new RtpHeaderExtensionElement[] { element };
        extension.Data = new byte[] { 49, 10, 11, 0 };
        RtpPacket packet = new RtpPacket();
        packet.HasPadding = true;
        packet.HasExtension = true;
        packet.Marker = true;
        packet.PayloadType = 96;
        packet.SequenceNumber = 65535;
        packet.Timestamp = 0x89ABCDEFU;
        packet.SynchronizationSource = 0x01234567U;
        packet.ContributingSources = new uint[] { 1U, 2U };
        packet.HeaderExtension = extension;
        packet.Payload = new byte[] { 20, 21, 22 };
        packet.Padding = new byte[] { 0, 2 };

        RtpPacket copy = packet.Copy();
        packet.ContributingSources[0] = 99U;
        packet.HeaderExtension.Data[0] = 255;
        packet.HeaderExtension.Elements[0].Data[0] = 254;
        packet.Payload[0] = 253;
        packet.Padding[0] = 252;

        Assert.Multiple(() =>
        {
            Assert.That(copy.Version, Is.EqualTo(2));
            Assert.That(copy.HasPadding, Is.True);
            Assert.That(copy.HasExtension, Is.True);
            Assert.That(copy.Marker, Is.True);
            Assert.That(copy.PayloadType, Is.EqualTo(96));
            Assert.That(copy.SequenceNumber, Is.EqualTo(65535));
            Assert.That(copy.Timestamp, Is.EqualTo(0x89ABCDEFU));
            Assert.That(copy.SynchronizationSource, Is.EqualTo(0x01234567U));
            Assert.That(copy.ContributingSources, Is.EqualTo(new uint[] { 1U, 2U }));
            Assert.That(copy.HeaderExtension, Is.Not.Null);
            Assert.That(copy.HeaderExtension, Is.Not.SameAs(packet.HeaderExtension));
            Assert.That(copy.HeaderExtension?.Profile,
                Is.EqualTo(RtpHeaderExtensionProfiles.OneByte));
            Assert.That(copy.HeaderExtension?.Data, Is.EqualTo(new byte[] { 49, 10, 11, 0 }));
            Assert.That(copy.HeaderExtension?.Elements[0].Identifier, Is.EqualTo(3));
            Assert.That(copy.HeaderExtension?.Elements[0].Data, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(copy.Payload, Is.EqualTo(new byte[] { 20, 21, 22 }));
            Assert.That(copy.Padding, Is.EqualTo(new byte[] { 0, 2 }));
            Assert.That(copy.ContributingSources, Is.Not.SameAs(packet.ContributingSources));
            Assert.That(copy.Payload, Is.Not.SameAs(packet.Payload));
            Assert.That(copy.Padding, Is.Not.SameAs(packet.Padding));
        });
    }

    [Test]
    public void CodecResultCopyOwnsEncodedDataAndDecodedPacket()
    {
        RtpPacket packet = new RtpPacket();
        packet.Payload = new byte[] { 7, 8 };
        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.ConsumedCount = 14;
        result.WrittenCount = 14;
        result.Packet = packet;
        result.HeaderExtension = new RtpHeaderExtension();
        result.HeaderExtension.Data = new byte[] { 5, 6 };
        result.AudioLevelExtension = new RtpAudioLevelExtension();
        result.AudioLevelExtension.Level = 42;
        result.MixerAudioLevelExtension = new RtpMixerAudioLevelExtension();
        result.MixerAudioLevelExtension.Levels = new byte[] { 10, 20 };
        result.Ntp64Extension = new RtpNtp64Extension();
        result.Ntp64Extension.Timestamp = 0x0123456789ABCDEFUL;
        result.StreamIdentifierExtension = new RtpStreamIdentifierExtension();
        result.StreamIdentifierExtension.Value = "stream-a";
        result.Data = new byte[] { 128, 96, 0, 1 };
        result.ErrorMessage = "retained diagnostic";

        RtpCodecResult copy = result.Copy();
        result.Packet.Payload[0] = 90;
        result.HeaderExtension.Data[0] = 92;
        result.MixerAudioLevelExtension.Levels[0] = 93;
        result.StreamIdentifierExtension.Value = "changed";
        result.Data[0] = 91;
        result.ErrorMessage = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(copy.Success, Is.True);
            Assert.That(copy.ConsumedCount, Is.EqualTo(14));
            Assert.That(copy.WrittenCount, Is.EqualTo(14));
            Assert.That(copy.Packet, Is.Not.SameAs(result.Packet));
            Assert.That(copy.Packet?.Payload, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(copy.HeaderExtension, Is.Not.SameAs(result.HeaderExtension));
            Assert.That(copy.HeaderExtension?.Data, Is.EqualTo(new byte[] { 5, 6 }));
            Assert.That(copy.AudioLevelExtension?.Level, Is.EqualTo(42));
            Assert.That(copy.MixerAudioLevelExtension?.Levels,
                Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(copy.Ntp64Extension?.Timestamp,
                Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(copy.StreamIdentifierExtension?.Value, Is.EqualTo("stream-a"));
            Assert.That(copy.Data, Is.EqualTo(new byte[] { 128, 96, 0, 1 }));
            Assert.That(copy.Data, Is.Not.SameAs(result.Data));
            Assert.That(copy.ErrorMessage, Is.EqualTo("retained diagnostic"));
        });
    }

    [Test]
    public void ClassificationAndRegistryModelsCopyAllMetadata()
    {
        RtpDatagramClassification classification = new RtpDatagramClassification();
        classification.Kind = RtpPacketKind.Rtcp;
        classification.PayloadType = 81;
        classification.ControlPacketType = RtcpPacketType.ReceiverSummaryInformation;
        classification.ErrorMessage = "diagnostic";
        RtpHeaderExtensionMapping mapping = new RtpHeaderExtensionMapping();
        mapping.Identifier = 7;
        mapping.Uri = RtpHeaderExtensionUris.SsrcAudioLevel;
        mapping.SupportsOneByte = true;
        mapping.SupportsTwoByte = true;
        mapping.PreferredFormat = RtpHeaderExtensionFormat.OneByte;
        mapping.Attributes = "vad=on";
        RtpPayloadTypeDefinition payloadType = new RtpPayloadTypeDefinition();
        payloadType.PayloadType = 97;
        payloadType.ClockRate = 48000;
        payloadType.MediaIdentity = "opus";
        payloadType.ChannelCount = 2;
        payloadType.IsRetransmission = true;
        payloadType.HasAssociatedPayloadType = true;
        payloadType.AssociatedPayloadType = 96;

        RtpDatagramClassification classificationCopy = classification.Copy();
        RtpHeaderExtensionMapping mappingCopy = mapping.Copy();
        RtpPayloadTypeDefinition payloadTypeCopy = payloadType.Copy();
        classification.ErrorMessage = "changed";
        mapping.Uri = "changed";
        payloadType.MediaIdentity = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(classificationCopy.Kind, Is.EqualTo(RtpPacketKind.Rtcp));
            Assert.That(classificationCopy.PayloadType, Is.EqualTo(81));
            Assert.That(classificationCopy.ControlPacketType,
                Is.EqualTo(RtcpPacketType.ReceiverSummaryInformation));
            Assert.That(classificationCopy.ErrorMessage, Is.EqualTo("diagnostic"));
            Assert.That(mappingCopy.Identifier, Is.EqualTo(7));
            Assert.That(mappingCopy.Uri, Is.EqualTo(RtpHeaderExtensionUris.SsrcAudioLevel));
            Assert.That(mappingCopy.SupportsOneByte, Is.True);
            Assert.That(mappingCopy.SupportsTwoByte, Is.True);
            Assert.That(mappingCopy.PreferredFormat, Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
            Assert.That(mappingCopy.Attributes, Is.EqualTo("vad=on"));
            Assert.That(payloadTypeCopy.PayloadType, Is.EqualTo(97));
            Assert.That(payloadTypeCopy.ClockRate, Is.EqualTo(48000));
            Assert.That(payloadTypeCopy.MediaIdentity, Is.EqualTo("opus"));
            Assert.That(payloadTypeCopy.ChannelCount, Is.EqualTo(2));
            Assert.That(payloadTypeCopy.IsRetransmission, Is.True);
            Assert.That(payloadTypeCopy.HasAssociatedPayloadType, Is.True);
            Assert.That(payloadTypeCopy.AssociatedPayloadType, Is.EqualTo(96));
        });
    }

    [Test]
    public void KnownExtensionModelsCopyTheirValues()
    {
        RtpAudioLevelExtension audioLevel = new RtpAudioLevelExtension();
        audioLevel.VoiceActivity = true;
        audioLevel.Level = 42;
        RtpMixerAudioLevelExtension mixerLevels = new RtpMixerAudioLevelExtension();
        mixerLevels.Levels = new byte[] { 10, 20, 30 };
        RtpNtp64Extension ntp = new RtpNtp64Extension();
        ntp.Timestamp = 0x0123456789ABCDEFUL;
        RtpStreamIdentifierExtension stream = new RtpStreamIdentifierExtension();
        stream.Uri = RtpHeaderExtensionUris.RepairedRtpStreamIdentifier;
        stream.Value = "repair-stream";

        RtpAudioLevelExtension audioLevelCopy = audioLevel.Copy();
        RtpMixerAudioLevelExtension mixerLevelsCopy = mixerLevels.Copy();
        RtpNtp64Extension ntpCopy = ntp.Copy();
        RtpStreamIdentifierExtension streamCopy = stream.Copy();
        mixerLevels.Levels[0] = 99;
        stream.Value = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(audioLevelCopy.VoiceActivity, Is.True);
            Assert.That(audioLevelCopy.Level, Is.EqualTo(42));
            Assert.That(mixerLevelsCopy.Levels, Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(mixerLevelsCopy.Levels, Is.Not.SameAs(mixerLevels.Levels));
            Assert.That(ntpCopy.Timestamp, Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(streamCopy.Uri,
                Is.EqualTo(RtpHeaderExtensionUris.RepairedRtpStreamIdentifier));
            Assert.That(streamCopy.Value, Is.EqualTo("repair-stream"));
        });
    }

    [Test]
    public void CopyHelpersPreserveAbsentOptionalPacketData()
    {
        RtpPacket packet = new RtpPacket();
        RtpCodecResult result = new RtpCodecResult();

        RtpPacket packetCopy = packet.Copy();
        RtpCodecResult resultCopy = result.Copy();

        Assert.Multiple(() =>
        {
            Assert.That(packetCopy.HeaderExtension, Is.Null);
            Assert.That(resultCopy.Packet, Is.Null);
            Assert.That(resultCopy.ErrorMessage, Is.Null);
        });
    }
}
