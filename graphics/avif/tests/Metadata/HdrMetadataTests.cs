using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif.Tests;

public class HdrMetadataTests
{
    [Test]
    public void ItemPropertiesExposeAllHdrAndViewingMetadata()
    {
        List<AvifItemProperty> properties = new List<AvifItemProperty>
        {
            CreateProperty("clli", HdrCameraPropertyBox.ParseContentLightLevel(
                CreateContentLightPayload(1000, 400))),
            CreateProperty("mdcv", HdrCameraPropertyBox.ParseMasteringDisplayColourVolume(
                CreateMasteringDisplayPayload())),
            CreateProperty("cclv", HdrCameraPropertyBox.ParseContentColourVolume(
                CreateContentColorVolumePayload())),
            CreateProperty("amve", HdrCameraPropertyBox.ParseAmbientViewingEnvironment(
                CreateAmbientPayload())),
            CreateProperty("reve", HdrCameraPropertyBox.ParseReferenceViewingEnvironment(
                CreateReferencePayload())),
            CreateProperty("ndwt", HdrCameraPropertyBox.ParseNominalDiffuseWhite(
                CreateUInt32Payload(2030000)))
        };
        AvifImageItem item = CreateItem(properties);
        AvifHdrMetadata metadata = item.GetHdrMetadata();

        AvifContentLightLevel? content = metadata.GetContentLightLevel();
        AvifMasteringDisplay? mastering = metadata.GetMasteringDisplay();
        AvifContentColorVolume? color = metadata.GetContentColorVolume();
        AvifAmbientViewingEnvironment? ambient =
            metadata.GetAmbientViewingEnvironment();
        AvifReferenceViewingEnvironment? reference =
            metadata.GetReferenceViewingEnvironment();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.HasAnyMetadata(), Is.True);
            Assert.That(metadata.HasContentLightLevel(), Is.True);
            Assert.That(metadata.HasMasteringDisplay(), Is.True);
            Assert.That(metadata.HasContentColorVolume(), Is.True);
            Assert.That(metadata.HasAmbientViewingEnvironment(), Is.True);
            Assert.That(metadata.HasReferenceViewingEnvironment(), Is.True);
            Assert.That((content ?? throw new InvalidOperationException("Invalid internal state.")).GetMaximumContentLightLevel(), Is.EqualTo(1000));
            Assert.That(content.GetMaximumFrameAverageLightLevel(), Is.EqualTo(400));
            Assert.That((mastering ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryChromaticityX(1), Is.EqualTo(13250));
            Assert.That(mastering.GetPrimaryChromaticityY(2), Is.EqualTo(3000));
            Assert.That(mastering.GetWhitePointChromaticityX(), Is.EqualTo(15635));
            Assert.That(mastering.GetWhitePointChromaticityY(), Is.EqualTo(16450));
            Assert.That(mastering.GetMaximumLuminance(), Is.EqualTo(10000000));
            Assert.That(mastering.GetMinimumLuminance(), Is.EqualTo(50));
            Assert.That((color ?? throw new InvalidOperationException("Invalid internal state.")).HasPrimaries(), Is.True);
            Assert.That(color.GetPrimaryChromaticityX(0), Is.EqualTo(-10));
            Assert.That(color.GetPrimaryChromaticityY(2), Is.EqualTo(60));
            Assert.That(color.GetMinimumLuminance(), Is.EqualTo(1));
            Assert.That(color.GetMaximumLuminance(), Is.EqualTo(1000));
            Assert.That(color.GetAverageLuminance(), Is.EqualTo(250));
            Assert.That((ambient ?? throw new InvalidOperationException("Invalid internal state.")).GetAmbientIlluminance(), Is.EqualTo(100000));
            Assert.That(ambient.GetAmbientLightX(), Is.EqualTo(15635));
            Assert.That(ambient.GetAmbientLightY(), Is.EqualTo(16450));
            Assert.That((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetSurroundLuminance(), Is.EqualTo(50000));
            Assert.That(reference.GetSurroundLightX(), Is.EqualTo(3127));
            Assert.That(reference.GetSurroundLightY(), Is.EqualTo(3290));
            Assert.That(reference.GetPeripheryLuminance(), Is.EqualTo(10000));
            Assert.That(reference.GetPeripheryLightX(), Is.EqualTo(3000));
            Assert.That(reference.GetPeripheryLightY(), Is.EqualTo(3200));
            Assert.That(metadata.HasNominalDiffuseWhite(), Is.True);
            Assert.That(metadata.GetNominalDiffuseWhiteLuminance(), Is.EqualTo(2030000));
        });
    }

    [Test]
    public void MatchingObuValuesReconcileAndConflictsLeaveContainerValuesUnchanged()
    {
        List<AvifItemProperty> properties = new List<AvifItemProperty>
        {
            CreateProperty("clli", HdrCameraPropertyBox.ParseContentLightLevel(
                CreateContentLightPayload(1000, 400))),
            CreateProperty("mdcv", HdrCameraPropertyBox.ParseMasteringDisplayColourVolume(
                CreateMasteringDisplayPayload()))
        };
        AvifHdrMetadata metadata = CreateItem(properties).GetHdrMetadata();

        AvifResultCode matchingContent = metadata.AddAv1MetadataObuPayload(
            CreateAv1ContentLightPayload(1000, 400));
        AvifResultCode matchingMastering = metadata.AddAv1MetadataObuPayload(
            CreateAv1MasteringDisplayPayload());
        AvifResultCode conflictingContent = metadata.AddAv1MetadataObuPayload(
            CreateAv1ContentLightPayload(1001, 400));

        Assert.Multiple(() =>
        {
            Assert.That(matchingContent, Is.EqualTo(AvifResultCode.Success));
            Assert.That(matchingMastering, Is.EqualTo(AvifResultCode.Success));
            Assert.That(conflictingContent, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(
                (metadata.GetContentLightLevel() ?? throw new InvalidOperationException("Invalid internal state.")).GetMaximumContentLightLevel(),
                Is.EqualTo(1000));
            Assert.That((metadata.GetMasteringDisplay() ?? throw new InvalidOperationException("Invalid internal state.")).GetMaximumLuminance(),
                Is.EqualTo(10000000));
        });
    }

    [Test]
    public void ConflictingContainerHdrPropertiesAreRejectedDuringReconciliation()
    {
        ParsedItemProperty first = CreateParsedProperty(
            "clli",
            HdrCameraPropertyBox.ParseContentLightLevel(
                CreateContentLightPayload(1000, 400)));
        ParsedItemProperty matching = CreateParsedProperty(
            "clli",
            HdrCameraPropertyBox.ParseContentLightLevel(
                CreateContentLightPayload(1000, 400)));
        ParsedItemProperty conflicting = CreateParsedProperty(
            "clli",
            HdrCameraPropertyBox.ParseContentLightLevel(
                CreateContentLightPayload(1200, 400)));
        ParsedItem item = new ParsedItem(
            1,
            2,
            false,
            0,
            "av01",
            "image",
            string.Empty,
            string.Empty,
            string.Empty);
        ParsedItem matchingItem = item.WithProperties(
            new List<ParsedPropertyAssociation>
            {
                new ParsedPropertyAssociation(1, false, first),
                new ParsedPropertyAssociation(2, false, matching)
            });
        ParsedItem conflictingItem = item.WithProperties(
            new List<ParsedPropertyAssociation>
            {
                new ParsedPropertyAssociation(1, false, first),
                new ParsedPropertyAssociation(2, false, conflicting)
            });

        Assert.Multiple(() =>
        {
            Assert.That(MetaBoxParser.ValidateHdrPropertyConsistency(matchingItem),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(MetaBoxParser.ValidateHdrPropertyConsistency(conflictingItem),
                Is.EqualTo(AvifResultCode.NonConformingData));
        });
    }

    [Test]
    public void ItuT35AndHdr10PlusPayloadsAreExposedAsDefensiveCopies()
    {
        AvifHdrMetadata metadata = new AvifHdrMetadata();
        byte[] payload = new byte[]
        {
            4, 181, 0, 60, 0, 1, 4, 1, 170, 0, 187, 128, 0
        };

        AvifResultCode code = metadata.AddAv1MetadataObuPayload(payload);
        byte[] itu = metadata.GetItuT35Payload();
        byte[] hdr10Plus = metadata.GetHdr10PlusPayload();
        itu[0] = 255;
        hdr10Plus[0] = 255;

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.HasItuT35Payload(), Is.True);
            Assert.That(metadata.GetItuT35CountryCode(), Is.EqualTo(181));
            Assert.That(metadata.GetItuT35Payload(),
                Is.EqualTo(new byte[] { 0, 60, 0, 1, 4, 1, 170, 0, 187 }));
            Assert.That(metadata.HasHdr10PlusPayload(), Is.True);
            Assert.That(metadata.GetHdr10PlusTerminalProviderCode(), Is.EqualTo(60));
            Assert.That(metadata.GetHdr10PlusTerminalProviderOrientedCode(), Is.EqualTo(1));
            Assert.That(metadata.GetHdr10PlusApplicationIdentifier(), Is.EqualTo(4));
            Assert.That(metadata.GetHdr10PlusApplicationMode(), Is.EqualTo(1));
            Assert.That(metadata.GetHdr10PlusPayload(),
                Is.EqualTo(new byte[] { 170, 0, 187 }));
        });
    }

    [Test]
    public void ExtendedItuT35CountryCodeRemainsOpaqueAndDoesNotImplyHdr10Plus()
    {
        AvifHdrMetadata metadata = new AvifHdrMetadata();
        byte[] payload = new byte[] { 4, 255, 42, 9, 8, 7, 128, 0 };

        AvifResultCode code = metadata.AddAv1MetadataObuPayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.HasAnyMetadata(), Is.True);
            Assert.That(metadata.HasItuT35Payload(), Is.True);
            Assert.That(metadata.GetItuT35CountryCode(), Is.EqualTo(255));
            Assert.That(metadata.HasItuT35CountryCodeExtension(), Is.True);
            Assert.That(metadata.GetItuT35CountryCodeExtension(), Is.EqualTo(42));
            Assert.That(metadata.GetItuT35Payload(),
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(metadata.HasHdr10PlusPayload(), Is.False);
            Assert.That(metadata.GetHdr10PlusPayload(), Is.Empty);
        });
    }

    [Test]
    public void InvalidArgumentsAndInvalidMasteringDisplayMetadataAreRejected()
    {
        AvifHdrMetadata metadata = new AvifHdrMetadata();
        byte[] invalidMastering = CreateAv1MasteringDisplayPayload();
        invalidMastering[1] = 255;
        invalidMastering[2] = 255;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.AddAv1MetadataObuPayload(null),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.AddAv1MetadataObuPayload(new byte[] { 128 }),
                Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(metadata.AddAv1MetadataObuPayload(invalidMastering),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(metadata.HasAnyMetadata(), Is.False);
        });
    }

    static AvifItemProperty CreateProperty(
        string type,
        HdrCameraPropertyParseResult parsed)
    {
        ParsedItemProperty property = CreateParsedProperty(type, parsed);
        return new AvifItemProperty(new ParsedPropertyAssociation(1, false, property));
    }

    static ParsedItemProperty CreateParsedProperty(
        string type,
        HdrCameraPropertyParseResult parsed)
    {
        Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
        ParsedItemProperty property = new ParsedItemProperty(
            1,
            type,
            0,
            false,
            System.Array.Empty<byte>(),
            System.Array.Empty<byte>());
        property.SetHdrCameraProperties(parsed);
        return property;
    }

    static AvifImageItem CreateItem(List<AvifItemProperty> properties)
    {
        ParsedItem item = new ParsedItem(
            1,
            2,
            false,
            0,
            "av01",
            "image",
            string.Empty,
            string.Empty,
            string.Empty);
        return new AvifImageItem(
            item,
            null,
            true,
            new List<AvifItemExtent>(),
            properties,
            new List<AvifItemReference>(),
            new List<AvifItemReference>(),
            new List<uint>());
    }

    static byte[] CreateContentLightPayload(int maximum, int average)
    {
        byte[] payload = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(0, 2), (ushort)maximum);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(2, 2), (ushort)average);
        return payload;
    }

    static byte[] CreateMasteringDisplayPayload()
    {
        byte[] payload = new byte[24];
        int[] values = new int[]
        {
            34000, 16000, 13250, 34500, 7500, 3000, 15635, 16450
        };
        int index = 0;
        while (index < values.Length)
        {
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(index * 2, 2),
                (ushort)values[index]);
            index++;
        }

        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), 10000000);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(20, 4), 50);
        return payload;
    }

    static byte[] CreateContentColorVolumePayload()
    {
        byte[] payload = new byte[37];
        payload[0] = 60;
        int[] values = new int[] { -10, 20, 30, 40, 50, 60 };
        int index = 0;
        while (index < values.Length)
        {
            BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(1 + index * 4, 4),
                values[index]);
            index++;
        }

        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(25, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(29, 4), 1000);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(33, 4), 250);
        return payload;
    }

    static byte[] CreateAmbientPayload()
    {
        byte[] payload = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), 100000);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(4, 2), 15635);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(6, 2), 16450);
        return payload;
    }

    static byte[] CreateReferencePayload()
    {
        byte[] payload = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), 50000);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(4, 2), 3127);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(6, 2), 3290);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), 10000);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(12, 2), 3000);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(14, 2), 3200);
        return payload;
    }

    static byte[] CreateUInt32Payload(uint value)
    {
        byte[] payload = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(payload, value);
        return payload;
    }

    static byte[] CreateAv1ContentLightPayload(int maximum, int average)
    {
        return new byte[]
        {
            1,
            (byte)(maximum >> 8),
            (byte)maximum,
            (byte)(average >> 8),
            (byte)average,
            128
        };
    }

    static byte[] CreateAv1MasteringDisplayPayload()
    {
        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 2, 8);
        int[] values = new int[]
        {
            34000, 16000, 13250, 34500, 7500, 3000, 15635, 16450
        };
        int index = 0;
        while (index < values.Length)
        {
            writer.WriteBits(payload, (uint)values[index], 16);
            index++;
        }

        writer.WriteBits(payload, 10000000, 32);
        writer.WriteBits(payload, 50, 32);
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        return payload[..(int)(writer.BitPosition / 8)];
    }
}
