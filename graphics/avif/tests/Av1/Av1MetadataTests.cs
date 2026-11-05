namespace avif.Tests;

public class Av1MetadataTests
{
    [Test]
    public void HdrMetadataParsesContentLightAndMasteringDisplayValues()
    {
        byte[] contentPayload = new byte[] { 1, 3, 232, 1, 144, 128 };
        Av1Obu contentObu = new Av1Obu(
            new Av1ObuHeader(Av1ObuType.Metadata, false, true, 0, 0, 1),
            0,
            1,
            2,
            contentPayload.Length,
            contentPayload.Length + 2,
            true,
            contentPayload);
        Av1MetadataParseResult content = Av1MetadataParser.ParseObu(contentObu);

        byte[] volumePayload = new byte[64];
        BitWriter writer = BitWriter.Create(volumePayload.Length);
        writer.WriteBits(volumePayload, 2, 8);
        int[] coordinates = new int[]
        {
            44564, 21627, 8500, 39846, 6550, 2300
        };
        int index = 0;
        while (index < coordinates.Length)
        {
            writer.WriteBits(volumePayload, (uint)coordinates[index], 16);
            index++;
        }

        writer.WriteBits(volumePayload, 15635, 16);
        writer.WriteBits(volumePayload, 16450, 16);
        writer.WriteBits(volumePayload, 1000000, 32);
        writer.WriteBits(volumePayload, 50, 32);
        volumePayload = FinishPayload(volumePayload, writer);
        Av1MetadataParseResult volume = Av1MetadataParser.ParsePayload(volumePayload);

        Assert.Multiple(() =>
        {
            Assert.That(content.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((content.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).MetadataType,
                Is.EqualTo((ulong)Av1MetadataType.HdrContentLightLevel));
            Assert.That((content.Metadata.ContentLightLevel ?? throw new InvalidOperationException("Invalid internal state.")).MaximumContentLightLevel,
                Is.EqualTo(1000));
            Assert.That(content.Metadata.ContentLightLevel.MaximumFrameAverageLightLevel,
                Is.EqualTo(400));
            Assert.That(volume.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((volume.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).MasteringDisplayColorVolume ?? throw new InvalidOperationException("Invalid internal state.")).PrimaryChromaticityX,
                Is.EqualTo(new int[] { 44564, 8500, 6550 }));
            Assert.That((volume.Metadata.MasteringDisplayColorVolume ?? throw new InvalidOperationException("Invalid internal state.")).PrimaryChromaticityY,
                Is.EqualTo(new int[] { 21627, 39846, 2300 }));
            Assert.That(volume.Metadata.MasteringDisplayColorVolume.WhitePointChromaticityX,
                Is.EqualTo(15635));
            Assert.That(volume.Metadata.MasteringDisplayColorVolume.WhitePointChromaticityY,
                Is.EqualTo(16450));
            Assert.That(volume.Metadata.MasteringDisplayColorVolume.MaximumLuminance,
                Is.EqualTo(1000000));
            Assert.That(volume.Metadata.MasteringDisplayColorVolume.MinimumLuminance,
                Is.EqualTo(50));
        });
    }

    [Test]
    public void ScalabilityStructureRetainsLayersAndTemporalGroups()
    {
        byte[] payload = new byte[128];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 3, 8);
        writer.WriteBits(payload, 14, 8);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 640, 16);
        writer.WriteBits(payload, 360, 16);
        writer.WriteBits(payload, 1280, 16);
        writer.WriteBits(payload, 720, 16);
        writer.WriteBits(payload, 4, 8);
        writer.WriteBits(payload, 9, 8);
        writer.WriteBits(payload, 2, 8);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 1, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 2, 3);
        writer.WriteBits(payload, 1, 8);
        writer.WriteBits(payload, 3, 8);
        payload = FinishPayload(payload, writer);

        Av1MetadataParseResult result = Av1MetadataParser.ParsePayload(payload);
        Av1ScalabilityMetadata? scalability = (result.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).Scalability;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((scalability ?? throw new InvalidOperationException("Invalid internal state.")).ModeIdc, Is.EqualTo(14));
            Assert.That(scalability.HasScalabilityStructure, Is.True);
            Assert.That(scalability.SpatialLayerCount, Is.EqualTo(2));
            Assert.That(scalability.SpatialLayerDimensionsPresent, Is.True);
            Assert.That(scalability.SpatialLayerDescriptionsPresent, Is.True);
            Assert.That(scalability.TemporalGroupDescriptionsPresent, Is.True);
            Assert.That(scalability.SpatialLayerMaximumWidths,
                Is.EqualTo(new int[] { 640, 1280 }));
            Assert.That(scalability.SpatialLayerMaximumHeights,
                Is.EqualTo(new int[] { 360, 720 }));
            Assert.That(scalability.SpatialLayerReferenceIds,
                Is.EqualTo(new int[] { 4, 9 }));
            Assert.That(scalability.TemporalGroups, Has.Length.EqualTo(2));
            Assert.That(scalability.TemporalGroups[0].TemporalId, Is.Zero);
            Assert.That(scalability.TemporalGroups[0].TemporalSwitchingUpPoint, Is.True);
            Assert.That(scalability.TemporalGroups[0].SpatialSwitchingUpPoint, Is.False);
            Assert.That(scalability.TemporalGroups[1].TemporalId, Is.EqualTo(1));
            Assert.That(scalability.TemporalGroups[1].TemporalSwitchingUpPoint, Is.False);
            Assert.That(scalability.TemporalGroups[1].SpatialSwitchingUpPoint, Is.True);
            Assert.That(scalability.TemporalGroups[1].ReferencePictureDifferences,
                Is.EqualTo(new int[] { 1, 3 }));
        });
    }

    [Test]
    public void ItuT35RecognizesHdr10PlusAndUnknownMetadataStaysOpaque()
    {
        byte[] ituPayload = new byte[]
        {
            4, 181, 0, 60, 0, 1, 4, 1, 170, 0, 187, 128, 0
        };
        Av1MetadataParseResult ituResult = Av1MetadataParser.ParsePayload(ituPayload);

        byte[] unknownPayload = new byte[]
        {
            172, 2, 16, 0, 32, 128, 0, 0
        };
        Av1MetadataParseResult unknown = Av1MetadataParser.ParsePayload(unknownPayload);

        Assert.Multiple(() =>
        {
            Assert.That(ituResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((ituResult.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).ItuT35 ?? throw new InvalidOperationException("Invalid internal state.")).CountryCode, Is.EqualTo(181));
            Assert.That((ituResult.Metadata.ItuT35 ?? throw new InvalidOperationException("Invalid internal state.")).Payload,
                Is.EqualTo(new byte[] { 0, 60, 0, 1, 4, 1, 170, 0, 187 }));
            Assert.That(ituResult.Metadata.ItuT35.HasHdr10PlusEnvelope, Is.True);
            Assert.That(ituResult.Metadata.ItuT35.TerminalProviderCode, Is.EqualTo(60));
            Assert.That(ituResult.Metadata.ItuT35.TerminalProviderOrientedCode, Is.EqualTo(1));
            Assert.That(ituResult.Metadata.ItuT35.ApplicationIdentifier, Is.EqualTo(4));
            Assert.That(ituResult.Metadata.ItuT35.ApplicationMode, Is.EqualTo(1));
            Assert.That(ituResult.Metadata.ItuT35.Hdr10PlusPayload,
                Is.EqualTo(new byte[] { 170, 0, 187 }));
            Assert.That(unknown.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((unknown.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).MetadataType, Is.EqualTo(300));
            Assert.That(unknown.Metadata.IsKnownType(), Is.False);
            Assert.That(unknown.Metadata.OpaquePayload,
                Is.EqualTo(new byte[] { 16, 0, 32 }));
        });
    }

    [Test]
    public void FullAndPartialTimecodesExposePresenceAndOffsetFields()
    {
        byte[] fullPayload = new byte[32];
        BitWriter writer = BitWriter.Create(fullPayload.Length);
        writer.WriteBits(fullPayload, 5, 8);
        writer.WriteBits(fullPayload, 3, 5);
        writer.WriteBit(fullPayload, 1);
        writer.WriteBit(fullPayload, 1);
        writer.WriteBit(fullPayload, 0);
        writer.WriteBits(fullPayload, 100, 9);
        writer.WriteBits(fullPayload, 59, 6);
        writer.WriteBits(fullPayload, 58, 6);
        writer.WriteBits(fullPayload, 23, 5);
        writer.WriteBits(fullPayload, 5, 5);
        writer.WriteBits(fullPayload, 21, 5);
        fullPayload = FinishPayload(fullPayload, writer);

        byte[] partialPayload = new byte[32];
        writer = BitWriter.Create(partialPayload.Length);
        writer.WriteBits(partialPayload, 5, 8);
        writer.WriteBits(partialPayload, 2, 5);
        writer.WriteBit(partialPayload, 0);
        writer.WriteBit(partialPayload, 0);
        writer.WriteBit(partialPayload, 1);
        writer.WriteBits(partialPayload, 7, 9);
        writer.WriteBit(partialPayload, 1);
        writer.WriteBits(partialPayload, 12, 6);
        writer.WriteBit(partialPayload, 1);
        writer.WriteBits(partialPayload, 34, 6);
        writer.WriteBit(partialPayload, 0);
        writer.WriteBits(partialPayload, 0, 5);
        partialPayload = FinishPayload(partialPayload, writer);

        Av1TimecodeMetadata? full =
            (Av1MetadataParser.ParsePayload(fullPayload).Metadata ?? throw new InvalidOperationException("Invalid internal state.")).Timecode;
        Av1TimecodeMetadata? partial =
            (Av1MetadataParser.ParsePayload(partialPayload).Metadata ?? throw new InvalidOperationException("Invalid internal state.")).Timecode;

        Assert.Multiple(() =>
        {
            Assert.That((full ?? throw new InvalidOperationException("Invalid internal state.")).CountingType, Is.EqualTo(3));
            Assert.That(full.FullTimestamp, Is.True);
            Assert.That(full.Discontinuity, Is.True);
            Assert.That(full.CountDropped, Is.False);
            Assert.That(full.FrameCount, Is.EqualTo(100));
            Assert.That(full.Seconds, Is.EqualTo(59));
            Assert.That(full.Minutes, Is.EqualTo(58));
            Assert.That(full.Hours, Is.EqualTo(23));
            Assert.That(full.TimeOffsetLength, Is.EqualTo(5));
            Assert.That(full.TimeOffsetValue, Is.EqualTo(21));
            Assert.That((partial ?? throw new InvalidOperationException("Invalid internal state.")).FullTimestamp, Is.False);
            Assert.That(partial.CountDropped, Is.True);
            Assert.That(partial.FrameCount, Is.EqualTo(7));
            Assert.That(partial.SecondsPresent, Is.True);
            Assert.That(partial.Seconds, Is.EqualTo(12));
            Assert.That(partial.MinutesPresent, Is.True);
            Assert.That(partial.Minutes, Is.EqualTo(34));
            Assert.That(partial.HoursPresent, Is.False);
            Assert.That(partial.TimeOffsetLength, Is.Zero);
        });
    }

    [Test]
    public void InvalidMetadataLengthsReservedBitsValuesAndTrailingDataAreRejected()
    {
        Av1MetadataParseResult truncatedType =
            Av1MetadataParser.ParsePayload(new byte[] { 128 });
        Av1MetadataParseResult truncatedContent =
            Av1MetadataParser.ParsePayload(new byte[] { 1, 0, 1, 0 });
        Av1MetadataParseResult badTrailing =
            Av1MetadataParser.ParsePayload(new byte[] { 1, 0, 1, 0, 2, 0 });
        Av1MetadataParseResult missingItuTrailing =
            Av1MetadataParser.ParsePayload(new byte[] { 4, 181, 1 });

        byte[] reserved = new byte[8];
        BitWriter writer = BitWriter.Create(reserved.Length);
        writer.WriteBits(reserved, 3, 8);
        writer.WriteBits(reserved, 14, 8);
        writer.WriteBits(reserved, 0, 2);
        writer.WriteBit(reserved, 0);
        writer.WriteBit(reserved, 0);
        writer.WriteBit(reserved, 0);
        writer.WriteBits(reserved, 1, 3);
        reserved = FinishPayload(reserved, writer);
        Av1MetadataParseResult reservedBits = Av1MetadataParser.ParsePayload(reserved);

        byte[] invalidTime = new byte[16];
        writer = BitWriter.Create(invalidTime.Length);
        writer.WriteBits(invalidTime, 5, 8);
        writer.WriteBits(invalidTime, 0, 5);
        writer.WriteBit(invalidTime, 1);
        writer.WriteBit(invalidTime, 0);
        writer.WriteBit(invalidTime, 0);
        writer.WriteBits(invalidTime, 0, 9);
        writer.WriteBits(invalidTime, 60, 6);
        writer.WriteBits(invalidTime, 0, 6);
        writer.WriteBits(invalidTime, 0, 5);
        writer.WriteBits(invalidTime, 0, 5);
        invalidTime = FinishPayload(invalidTime, writer);
        Av1MetadataParseResult time = Av1MetadataParser.ParsePayload(invalidTime);

        Assert.Multiple(() =>
        {
            Assert.That(truncatedType.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncatedContent.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(badTrailing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(missingItuTrailing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(reservedBits.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(time.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(Av1MetadataParser.ParseObu(null).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static byte[] FinishPayload(byte[] payload, BitWriter writer)
    {
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        return payload[..(int)(writer.BitPosition / 8)];
    }
}
