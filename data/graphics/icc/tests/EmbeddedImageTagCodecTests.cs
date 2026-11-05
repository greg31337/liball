using icc;

namespace icc.Tests;

public class EmbeddedImageTagCodecTests
{
    [Test]
    public void HeightPngRoundTripsWithMetadataAndSurfaceMapProjection()
    {
        IccEmbeddedImage source = CreateImage(CreatePng(2, 1, 1), 2, 1, 1, 0, true);
        source.IsSeamless = true;
        source.MinimumHeightMeters = -0.25;
        source.MaximumHeightMeters = 0.5;

        byte[] payload = EmbeddedImageTagCodec.SerializeEmbeddedImage(source, IccTypeSignatures.EmbeddedHeightImage);
        TagDecodeState state = Decode(payload, IccVersionKind.Version5, IccTagSignatures.SurfaceMap, true, 0, 1024);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.EmbeddedImage));
            Assert.That(state.Data.EmbeddedImage, Is.Not.Null);
            Assert.That(state.Data.EmbeddedImage!.Width, Is.EqualTo(2));
            Assert.That(state.Data.EmbeddedImage.Height, Is.EqualTo(1));
            Assert.That(state.Data.EmbeddedImage.Components, Is.EqualTo(1));
            Assert.That(state.Data.EmbeddedImage.EncodingSignature, Is.Zero);
            Assert.That(state.Data.EmbeddedImage.IsCompressed, Is.True);
            Assert.That(state.Data.EmbeddedImage.IsSeamless, Is.True);
            Assert.That(state.Data.EmbeddedImage.MinimumHeightMeters, Is.EqualTo(-0.25));
            Assert.That(state.Data.EmbeddedImage.MaximumHeightMeters, Is.EqualTo(0.5));
            Assert.That(state.Data.SurfaceMap, Is.Not.Null);
            Assert.That(state.Data.SurfaceMap!.Width, Is.EqualTo(2));
            Assert.That(state.Data.SurfaceMap.HeightImage, Is.SameAs(state.Data.EmbeddedImage));
            Assert.That(state.Data.SurfaceMap.NormalImage, Is.Null);
            Assert.That(state.Data.SurfaceMap.Scale, Is.EqualTo(new double[] { -0.25, 0.5 }));
            Assert.That(EmbeddedImageTagCodec.SerializeEmbeddedImage(state.Data.EmbeddedImage, IccTypeSignatures.EmbeddedHeightImage), Is.EqualTo(payload));
            Assert.That(clone.EmbeddedImage, Is.Not.SameAs(state.Data.EmbeddedImage));
            Assert.That(clone.EmbeddedImage!.Bytes, Is.Not.SameAs(state.Data.EmbeddedImage.Bytes));
            Assert.That(clone.EmbeddedImage.IsSeamless, Is.True);
            Assert.That(clone.EmbeddedImage.MinimumHeightMeters, Is.EqualTo(-0.25));
            Assert.That(clone.SurfaceMap!.HeightImage, Is.SameAs(clone.EmbeddedImage));
        });
    }

    [Test]
    public void NormalPngRoundTripsInsideGenericStructure()
    {
        IccEmbeddedImage image = CreateImage(CreatePng(1, 2, 3), 1, 2, 3, 0, true);
        IccTagData nested = new IccTagData
        {
            Kind = IccTagDataKind.EmbeddedImage,
            EmbeddedImage = image,
            RawData = new IccRawData
            {
                HasTypeSignature = true,
                TypeSignature = IccTypeSignatures.EmbeddedNormalImage
            }
        };
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = 0x74657374,
            Members = new IccStructureMember[]
            {
                new IccStructureMember { Signature = 0x6e6d6170, Data = nested }
            }
        };

        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);
        TagDecodeState state = Decode(payload, IccVersionKind.Version5, 0, false, 0, 1024);
        IccEmbeddedImage decoded = state.Data.TagStructure!.Members[0].Data.EmbeddedImage!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.Width, Is.EqualTo(1));
            Assert.That(decoded.Height, Is.EqualTo(2));
            Assert.That(decoded.Components, Is.EqualTo(3));
            Assert.That(decoded.IsCompressed, Is.True);
            Assert.That(decoded.IsSeamless, Is.False);
        });
    }

    [Test]
    public void NormalTiffMetadataAndUncompressedStateRoundTrip()
    {
        byte[] tiff = CreateTiff(2, 1, 3);
        IccEmbeddedImage source = CreateImage(tiff, 2, 1, 3, 1, false);
        source.IsSeamless = true;

        byte[] payload = EmbeddedImageTagCodec.SerializeEmbeddedImage(source, IccTypeSignatures.EmbeddedNormalImage);
        TagDecodeState state = Decode(payload, IccVersionKind.Version5, IccTagSignatures.SurfaceMap, true, 0, 1024);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.EmbeddedImage!.EncodingSignature, Is.EqualTo(1));
            Assert.That(state.Data.EmbeddedImage.Width, Is.EqualTo(2));
            Assert.That(state.Data.EmbeddedImage.Components, Is.EqualTo(3));
            Assert.That(state.Data.EmbeddedImage.IsCompressed, Is.False);
            Assert.That(state.Data.SurfaceMap!.NormalImage, Is.SameAs(state.Data.EmbeddedImage));
            Assert.That(state.Data.SurfaceMap.HeightImage, Is.Null);
            Assert.That(EmbeddedImageTagCodec.SerializeEmbeddedImage(state.Data.EmbeddedImage, IccTypeSignatures.EmbeddedNormalImage), Is.EqualTo(payload));
        });
    }

    [Test]
    public void DecoderRejectsInvalidTagMetadata()
    {
        IccEmbeddedImage image = CreateImage(CreatePng(1, 1, 1), 1, 1, 1, 0, true);
        image.MinimumHeightMeters = 0.0;
        image.MaximumHeightMeters = 1.0;
        byte[] valid = EmbeddedImageTagCodec.SerializeEmbeddedImage(image, IccTypeSignatures.EmbeddedHeightImage);
        byte[] reserved = (byte[])valid.Clone();
        reserved[7] = 1;
        byte[] seamless = (byte[])valid.Clone();
        WriteBigUInt32(seamless, 8, 2);
        byte[] format = (byte[])valid.Clone();
        WriteBigUInt32(format, 12, 2);
        byte[] nonFinite = (byte[])valid.Clone();
        WriteBigUInt32(nonFinite, 16, 0x7f800000);
        byte[] subnormal = (byte[])valid.Clone();
        WriteBigUInt32(subnormal, 20, 1);
        byte[] reversed = (byte[])valid.Clone();
        WriteBigUInt32(reversed, 16, 0x40000000);
        byte[] empty = valid.Take(24).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(Decode(reserved).Result.Message, Does.Contain("reserved bytes"));
            Assert.That(Decode(seamless).Result.Message, Does.Contain("zero or one"));
            Assert.That(Decode(format).Result.Message, Does.Contain("PNG or TIFF"));
            Assert.That(Decode(nonFinite).Result.Message, Does.Contain("finite and ordered"));
            Assert.That(Decode(subnormal).Result.Message, Does.Contain("finite and ordered"));
            Assert.That(Decode(reversed).Result.Message, Does.Contain("finite and ordered"));
            Assert.That(Decode(empty).Result.Message, Does.Contain("followed by image data"));
            Assert.That(Decode(empty).Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    [Test]
    public void PngValidationRejectsCorruptionWrongComponentsAndScanlineShape()
    {
        byte[] corrupt = CreatePng(1, 1, 1);
        corrupt[corrupt.Length - 5] ^= 1;
        byte[] wrongComponents = CreatePng(1, 1, 3);
        byte[] invalidFilter = CreatePngWithRawScanlines(1, 1, 1, new byte[] { 5, 20 });
        IccEmbeddedImage corruptImage = CreateImage(corrupt, 1, 1, 1, 0, true);
        IccEmbeddedImage wrongImage = CreateImage(wrongComponents, 1, 1, 3, 0, true);
        IccEmbeddedImage filterImage = CreateImage(invalidFilter, 1, 1, 1, 0, true);

        byte[] corruptPayload = CreateUncheckedPayload(corruptImage, IccTypeSignatures.EmbeddedHeightImage);
        byte[] wrongPayload = CreateUncheckedPayload(wrongImage, IccTypeSignatures.EmbeddedHeightImage);
        byte[] filterPayload = CreateUncheckedPayload(filterImage, IccTypeSignatures.EmbeddedHeightImage);

        Assert.Multiple(() =>
        {
            Assert.That(Decode(corruptPayload).Result.Message, Does.Contain("CRC-32"));
            Assert.That(Decode(wrongPayload).Result.Message, Does.Contain("grayscale"));
            Assert.That(Decode(filterPayload).Result.Message, Does.Contain("filter method"));
        });
    }

    [Test]
    public void ExpandedImageLimitIsEnforcedForReadingAndWriting()
    {
        IccEmbeddedImage image = CreateImage(CreatePng(20, 20, 3), 20, 20, 3, 0, true);
        byte[] payload = EmbeddedImageTagCodec.SerializeEmbeddedImage(image, IccTypeSignatures.EmbeddedNormalImage);

        TagDecodeState state = Decode(payload, IccVersionKind.Version5, 0, false, 0, 100);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.ResourceLimitExceeded));
            Assert.That(state.Result.Message, Does.Contain("exceeding the configured maximum"));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedImageTagCodec.SerializeEmbeddedImageWithLimit(
                image, IccTypeSignatures.EmbeddedNormalImage, 100));
        });
    }

    [Test]
    public void WriterRejectsMetadataThatDoesNotMatchEncodedImage()
    {
        IccEmbeddedImage dimensions = CreateImage(CreatePng(1, 1, 3), 2, 1, 3, 0, true);
        IccEmbeddedImage compression = CreateImage(CreateTiff(1, 1, 1), 1, 1, 1, 1, true);
        IccEmbeddedImage components = CreateImage(CreatePng(1, 1, 1), 1, 1, 1, 0, true);
        IccEmbeddedImage heightOrder = CreateImage(CreatePng(1, 1, 1), 1, 1, 1, 0, true);
        heightOrder.MinimumHeightMeters = 2.0;
        heightOrder.MaximumHeightMeters = 1.0;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => EmbeddedImageTagCodec.SerializeEmbeddedImage(dimensions, IccTypeSignatures.EmbeddedNormalImage));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedImageTagCodec.SerializeEmbeddedImage(compression, IccTypeSignatures.EmbeddedHeightImage));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedImageTagCodec.SerializeEmbeddedImage(components, IccTypeSignatures.EmbeddedNormalImage));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedImageTagCodec.SerializeEmbeddedImage(heightOrder, IccTypeSignatures.EmbeddedHeightImage));
            Assert.Throws<IccProfileWriteException>(() => EmbeddedImageTagCodec.SerializeEmbeddedImage(components, IccTypeSignatures.Text));
        });
    }

    [Test]
    public void TopLevelImageRequiresSurfaceMapTagWhileNestedImageDoesNot()
    {
        IccEmbeddedImage image = CreateImage(CreatePng(1, 1, 3), 1, 1, 3, 0, true);
        byte[] payload = EmbeddedImageTagCodec.SerializeEmbeddedImage(image, IccTypeSignatures.EmbeddedNormalImage);

        TagDecodeState wrongTag = Decode(payload, IccVersionKind.Version5, IccTagSignatures.Copyright, true, 0, 1024);
        TagDecodeState nested = Decode(payload, IccVersionKind.Version5, IccTagSignatures.Copyright, true, 1, 1024);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTag.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongTag.Result.Message, Does.Contain("surfaceMapTag"));
            Assert.That(nested.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(nested.Data.SurfaceMap, Is.Null);
        });
    }

    [Test]
    public void EmbeddedImageTypesRemainRawForEarlierProfileVersions()
    {
        IccEmbeddedImage image = CreateImage(CreatePng(1, 1, 1), 1, 1, 1, 0, true);
        byte[] payload = EmbeddedImageTagCodec.SerializeEmbeddedImage(image, IccTypeSignatures.EmbeddedHeightImage);

        TagDecodeState state = Decode(payload, IccVersionKind.Version4, 0, false, 0, 1024);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    private static IccEmbeddedImage CreateImage(byte[] bytes, int width, int height, int components, uint encoding, bool compressed)
    {
        return new IccEmbeddedImage
        {
            Width = width,
            Height = height,
            Components = components,
            EncodingSignature = encoding,
            Bytes = bytes,
            IsCompressed = compressed
        };
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        return Decode(payload, IccVersionKind.Version5, 0, false, 0, 1024);
    }

    private static TagDecodeState Decode(byte[] payload, IccVersionKind version, uint tagSignature,
        bool hasProfileContext, int nestingDepth, int maximumExpandedBytes)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 500, version, true);
        state.TagSignature = tagSignature;
        state.HasProfileContext = hasProfileContext;
        state.CurrentNestingDepth = nestingDepth;
        state.MaximumDecompressedBytes = maximumExpandedBytes;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] CreateUncheckedPayload(IccEmbeddedImage image, uint typeSignature)
    {
        int headerSize = typeSignature == IccTypeSignatures.EmbeddedHeightImage ? 24 : 16;
        byte[] payload = new byte[headerSize + image.Bytes.Length];
        WriteBigUInt32(payload, 0, typeSignature);
        WriteBigUInt32(payload, 8, image.IsSeamless ? 1u : 0u);
        WriteBigUInt32(payload, 12, image.EncodingSignature);
        Array.Copy(image.Bytes, 0, payload, headerSize, image.Bytes.Length);
        return payload;
    }

    private static byte[] CreatePng(int width, int height, int components)
    {
        int rowBytes = width * components;
        byte[] scanlines = new byte[(rowBytes + 1) * height];
        int row = 0;
        while (row < height)
        {
            int offset = row * (rowBytes + 1);
            scanlines[offset] = 0;
            int index = 0;
            while (index < rowBytes)
            {
                scanlines[offset + index + 1] = (byte)((row + index + 1) & 255);
                index++;
            }

            row++;
        }

        return CreatePngWithRawScanlines(width, height, components, scanlines);
    }

    private static byte[] CreatePngWithRawScanlines(int width, int height, int components, byte[] scanlines)
    {
        byte[] header = new byte[13];
        WriteBigUInt32(header, 0, (uint)width);
        WriteBigUInt32(header, 4, (uint)height);
        header[8] = 8;
        header[9] = components == 1 ? (byte)0 : (byte)2;
        byte[] compressed = ZlibCodec.EncodeStored(scanlines);
        List<byte> bytes = new List<byte>(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        AppendPngChunk(bytes, PngImageInspector.Ihdr, header);
        AppendPngChunk(bytes, PngImageInspector.Idat, compressed);
        AppendPngChunk(bytes, PngImageInspector.Iend, Array.Empty<byte>());
        return bytes.ToArray();
    }

    private static void AppendPngChunk(List<byte> destination, uint type, byte[] data)
    {
        byte[] chunk = new byte[data.Length + 12];
        WriteBigUInt32(chunk, 0, (uint)data.Length);
        WriteBigUInt32(chunk, 4, type);
        Array.Copy(data, 0, chunk, 8, data.Length);
        uint crc = PngImageInspector.ComputePngCrc(new ReadOnlyMemory<byte>(chunk, 4, data.Length + 4));
        WriteBigUInt32(chunk, data.Length + 8, crc);
        destination.AddRange(chunk);
    }

    private static byte[] CreateTiff(int width, int height, int components)
    {
        const int entryCount = 10;
        int directoryEnd = 8 + 2 + (entryCount * 12) + 4;
        int bitsOffset = directoryEnd;
        int pixelOffset = components == 1 ? directoryEnd : directoryEnd + (components * 2);
        int pixelBytes = width * height * components;
        byte[] bytes = new byte[pixelOffset + pixelBytes];
        bytes[0] = 73;
        bytes[1] = 73;
        WriteLittleUInt16(bytes, 2, 42);
        WriteLittleUInt32(bytes, 4, 8);
        WriteLittleUInt16(bytes, 8, entryCount);
        int entry = 10;
        WriteTiffEntry(bytes, entry, 256, 4, 1, (uint)width);
        entry += 12;
        WriteTiffEntry(bytes, entry, 257, 4, 1, (uint)height);
        entry += 12;
        WriteTiffEntry(bytes, entry, 258, 3, (uint)components, components == 1 ? 8u : (uint)bitsOffset);
        entry += 12;
        WriteTiffEntry(bytes, entry, 259, 3, 1, 1);
        entry += 12;
        WriteTiffEntry(bytes, entry, 262, 3, 1, components == 1 ? 1u : 2u);
        entry += 12;
        WriteTiffEntry(bytes, entry, 273, 4, 1, (uint)pixelOffset);
        entry += 12;
        WriteTiffEntry(bytes, entry, 277, 3, 1, (uint)components);
        entry += 12;
        WriteTiffEntry(bytes, entry, 278, 4, 1, (uint)height);
        entry += 12;
        WriteTiffEntry(bytes, entry, 279, 4, 1, (uint)pixelBytes);
        entry += 12;
        WriteTiffEntry(bytes, entry, 284, 3, 1, 1);
        if (components != 1)
        {
            int index = 0;
            while (index < components)
            {
                WriteLittleUInt16(bytes, bitsOffset + (index * 2), 8);
                index++;
            }
        }

        int pixel = 0;
        while (pixel < pixelBytes)
        {
            bytes[pixelOffset + pixel] = (byte)(pixel + 1);
            pixel++;
        }

        return bytes;
    }

    private static void WriteTiffEntry(byte[] bytes, int offset, ushort tag, ushort type, uint count, uint value)
    {
        WriteLittleUInt16(bytes, offset, tag);
        WriteLittleUInt16(bytes, offset + 2, type);
        WriteLittleUInt32(bytes, offset + 4, count);
        if (type == 3 && count == 1)
        {
            WriteLittleUInt16(bytes, offset + 8, (ushort)value);
        }
        else
        {
            WriteLittleUInt32(bytes, offset + 8, value);
        }
    }

    private static void WriteBigUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static void WriteLittleUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteLittleUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }
}
