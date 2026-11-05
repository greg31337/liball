namespace raw;

internal enum TiffPlanarConfiguration
{
    Unknown = 0,
    Chunky = 1,
    Planar = 2
}

internal enum TiffPredictor
{
    Unknown = 0,
    None = 1,
    Horizontal = 2,
    FloatingPoint = 3
}

internal enum TiffSampleFormat
{
    Unknown = 0,
    UnsignedInteger = 1,
    SignedInteger = 2,
    FloatingPoint = 3,
    Undefined = 4
}

internal enum TiffStorageKind
{
    None = 0,
    Strips = 1,
    Tiles = 2,
    Mixed = 3
}

internal class TiffDataBlock
{
    internal ulong Offset;
    internal ulong ByteCount;
    internal bool Tiled;
    internal int Index;
    internal int Plane;
    internal int IndexInPlane;

    internal TiffDataBlock(ulong offset, ulong byteCount, bool tiled, int index, int plane, int indexInPlane)
    {
        Offset = offset;
        ByteCount = byteCount;
        Tiled = tiled;
        Index = index;
        Plane = plane;
        IndexInPlane = indexInPlane;
    }
}

internal class TiffImageDescriptor
{
    internal string Path;
    internal TiffIfd Ifd;
    internal int ImageIndex;
    internal ulong Width;
    internal ulong Height;
    internal ulong SamplesPerPixel;
    internal ulong[] BitsPerSample;
    internal ulong CompressionCode;
    internal RawCompression Compression;
    internal ulong PhotometricCode;
    internal RawPhotometricInterpretation Photometric;
    internal ulong FillOrder;
    internal ulong OrientationCode;
    internal RawOrientation Orientation;
    internal ulong PlanarConfigurationCode;
    internal TiffPlanarConfiguration PlanarConfiguration;
    internal ulong PredictorCode;
    internal TiffPredictor Predictor;
    internal ulong[] SampleFormatCodes;
    internal TiffSampleFormat[] SampleFormats;
    internal ulong[] ExtraSamples;
    internal ulong RowsPerStrip;
    internal ulong TileWidth;
    internal ulong TileHeight;
    internal ulong[] StripOffsets;
    internal ulong[] StripByteCounts;
    internal ulong[] TileOffsets;
    internal ulong[] TileByteCounts;
    internal TiffDataBlock[] Blocks;
    internal TiffStorageKind StorageKind;
    internal ulong NewSubfileType;
    internal ulong OldSubfileType;
    internal bool ReducedResolution;
    internal bool Page;
    internal bool TransparencyMask;

    internal TiffImageDescriptor(string path, TiffIfd ifd, int imageIndex)
    {
        Path = path;
        Ifd = ifd;
        ImageIndex = imageIndex;
        BitsPerSample = new ulong[0];
        SampleFormatCodes = new ulong[0];
        SampleFormats = new TiffSampleFormat[0];
        ExtraSamples = new ulong[0];
        StripOffsets = new ulong[0];
        StripByteCounts = new ulong[0];
        TileOffsets = new ulong[0];
        TileByteCounts = new ulong[0];
        Blocks = new TiffDataBlock[0];
    }
}

internal static class TiffImageData
{
    internal const ushort NewSubfileTypeTag = 254;
    internal const ushort OldSubfileTypeTag = 255;
    internal const ushort ImageWidthTag = 256;
    internal const ushort ImageLengthTag = 257;
    internal const ushort BitsPerSampleTag = 258;
    internal const ushort CompressionTag = 259;
    internal const ushort PhotometricTag = 262;
    internal const ushort FillOrderTag = 266;
    internal const ushort StripOffsetsTag = 273;
    internal const ushort OrientationTag = 274;
    internal const ushort SamplesPerPixelTag = 277;
    internal const ushort RowsPerStripTag = 278;
    internal const ushort StripByteCountsTag = 279;
    internal const ushort PlanarConfigurationTag = 284;
    internal const ushort PredictorTag = 317;
    internal const ushort TileWidthTag = 322;
    internal const ushort TileLengthTag = 323;
    internal const ushort TileOffsetsTag = 324;
    internal const ushort TileByteCountsTag = 325;
    internal const ushort ExtraSamplesTag = 338;
    internal const ushort SampleFormatTag = 339;

    internal static TiffImageDescriptor[] Describe(TiffIfdGraph graph, RawLimits? limits)
    {
        if (graph == null || limits == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "TIFF image discovery requires an IFD graph and resource limits.", -1);
        }
        CheckedMath.RequirePositiveLimit(limits.MaxStripOrTileCount, "The configured strip-or-tile limit must be greater than zero.");

        System.Collections.Generic.List<TiffImageDescriptor> images = new System.Collections.Generic.List<TiffImageDescriptor>();
        for (int ifdIndex = 0; ifdIndex < graph.Ifds.Length; ifdIndex++)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            if (ifd.FindEntry(ImageWidthTag) != null && ifd.FindEntry(ImageLengthTag) != null)
            {
                images.Add(DescribeIfd(ifd, images.Count, limits));
            }
        }
        return images.ToArray();
    }

    internal static TiffImageDescriptor DescribeIfd(TiffIfd ifd, int imageIndex, RawLimits? limits)
    {
        TiffImageDescriptor descriptor = new TiffImageDescriptor(ifd.Path, ifd, imageIndex);
        descriptor.Width = GetUnsignedScalar(ifd, ImageWidthTag, 0);
        descriptor.Height = GetUnsignedScalar(ifd, ImageLengthTag, 0);
        descriptor.SamplesPerPixel = GetUnsignedScalar(ifd, SamplesPerPixelTag, 1);
        descriptor.BitsPerSample = GetUnsignedValues(ifd.FindEntry(BitsPerSampleTag));
        if (descriptor.BitsPerSample.Length == 0)
        {
            descriptor.BitsPerSample = new ulong[] { 1 };
        }

        descriptor.CompressionCode = GetUnsignedScalar(ifd, CompressionTag, 1);
        descriptor.Compression = GetCompression(descriptor.CompressionCode);
        descriptor.PhotometricCode = GetUnsignedScalar(ifd, PhotometricTag, ulong.MaxValue);
        descriptor.Photometric = GetPhotometric(descriptor.PhotometricCode);
        descriptor.FillOrder = GetUnsignedScalar(ifd, FillOrderTag, 1);
        descriptor.OrientationCode = GetUnsignedScalar(ifd, OrientationTag, 1);
        descriptor.Orientation = GetOrientation(descriptor.OrientationCode);
        descriptor.PlanarConfigurationCode = GetUnsignedScalar(ifd, PlanarConfigurationTag, 1);
        descriptor.PlanarConfiguration = GetPlanarConfiguration(descriptor.PlanarConfigurationCode);
        descriptor.PredictorCode = GetUnsignedScalar(ifd, PredictorTag, 1);
        descriptor.Predictor = GetPredictor(descriptor.PredictorCode);

        descriptor.SampleFormatCodes = GetUnsignedValues(ifd.FindEntry(SampleFormatTag));
        if (descriptor.SampleFormatCodes.Length == 0)
        {
            descriptor.SampleFormatCodes = new ulong[] { 1 };
        }
        descriptor.SampleFormats = new TiffSampleFormat[descriptor.SampleFormatCodes.Length];
        for (int index = 0; index < descriptor.SampleFormatCodes.Length; index++)
        {
            descriptor.SampleFormats[index] = GetSampleFormat(descriptor.SampleFormatCodes[index]);
        }
        descriptor.ExtraSamples = GetUnsignedValues(ifd.FindEntry(ExtraSamplesTag));

        descriptor.RowsPerStrip = GetUnsignedScalar(ifd, RowsPerStripTag, uint.MaxValue);
        descriptor.TileWidth = GetUnsignedScalar(ifd, TileWidthTag, 0);
        descriptor.TileHeight = GetUnsignedScalar(ifd, TileLengthTag, 0);
        descriptor.StripOffsets = GetUnsignedValues(ifd.FindEntry(StripOffsetsTag));
        descriptor.StripByteCounts = GetUnsignedValues(ifd.FindEntry(StripByteCountsTag));
        descriptor.TileOffsets = GetUnsignedValues(ifd.FindEntry(TileOffsetsTag));
        descriptor.TileByteCounts = GetUnsignedValues(ifd.FindEntry(TileByteCountsTag));
        RequireBlockLimits(descriptor, limits);
        descriptor.StorageKind = GetStorageKind(descriptor);
        descriptor.Blocks = CreateBlocks(descriptor);

        descriptor.NewSubfileType = GetUnsignedScalar(ifd, NewSubfileTypeTag, 0);
        descriptor.OldSubfileType = GetUnsignedScalar(ifd, OldSubfileTypeTag, 0);
        descriptor.ReducedResolution = (descriptor.NewSubfileType & 1) != 0 || descriptor.OldSubfileType == 2;
        descriptor.Page = (descriptor.NewSubfileType & 2) != 0 || descriptor.OldSubfileType == 3;
        descriptor.TransparencyMask = (descriptor.NewSubfileType & 4) != 0;
        return descriptor;
    }

    internal static ulong[] GetUnsignedValues(TiffEntry? entry)
    {
        if (entry == null)
        {
            return new ulong[0];
        }
        if (entry.FieldType == TiffFieldType.Byte)
        {
            byte[] values = entry.GetByteValues();
            ulong[] converted = new ulong[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                converted[index] = values[index];
            }
            return converted;
        }
        if (entry.FieldType == TiffFieldType.Short)
        {
            ushort[] values = entry.GetUInt16Values();
            ulong[] converted = new ulong[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                converted[index] = values[index];
            }
            return converted;
        }
        if (entry.FieldType == TiffFieldType.Long || entry.FieldType == TiffFieldType.Ifd)
        {
            uint[] values = entry.GetUInt32Values();
            ulong[] converted = new ulong[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                converted[index] = values[index];
            }
            return converted;
        }
        if (entry.FieldType == TiffFieldType.Long8 || entry.FieldType == TiffFieldType.Ifd8)
        {
            return entry.GetUInt64Values();
        }
        return new ulong[0];
    }

    internal static ulong GetUnsignedScalar(TiffIfd ifd, ushort tag, ulong defaultValue)
    {
        ulong[] values = GetUnsignedValues(ifd.FindEntry(tag));
        if (values.Length == 0)
        {
            return defaultValue;
        }
        return values[0];
    }

    internal static RawCompression GetCompression(ulong code)
    {
        if (code == 1)
        {
            return RawCompression.None;
        }
        if (code == 5)
        {
            return RawCompression.Lzw;
        }
        if (code == 6)
        {
            return RawCompression.BaselineJpeg;
        }
        if (code == 8)
        {
            return RawCompression.AdobeDeflate;
        }
        if (code == 32773)
        {
            return RawCompression.PackBits;
        }
        if (code == 32946)
        {
            return RawCompression.Deflate;
        }
        return RawCompression.Unknown;
    }

    internal static RawPhotometricInterpretation GetPhotometric(ulong code)
    {
        if (code == 0)
        {
            return RawPhotometricInterpretation.WhiteIsZero;
        }
        if (code == 1)
        {
            return RawPhotometricInterpretation.BlackIsZero;
        }
        if (code == 2)
        {
            return RawPhotometricInterpretation.Rgb;
        }
        if (code == 3)
        {
            return RawPhotometricInterpretation.Palette;
        }
        if (code == 6)
        {
            return RawPhotometricInterpretation.YCbCr;
        }
        if (code == 32803)
        {
            return RawPhotometricInterpretation.ColorFilterArray;
        }
        if (code == 34892)
        {
            return RawPhotometricInterpretation.LinearRaw;
        }
        return RawPhotometricInterpretation.Unknown;
    }

    internal static RawOrientation GetOrientation(ulong code)
    {
        if (code >= 1 && code <= 8)
        {
            return (RawOrientation)code;
        }
        return RawOrientation.Unknown;
    }

    internal static TiffPlanarConfiguration GetPlanarConfiguration(ulong code)
    {
        if (code == 1)
        {
            return TiffPlanarConfiguration.Chunky;
        }
        if (code == 2)
        {
            return TiffPlanarConfiguration.Planar;
        }
        return TiffPlanarConfiguration.Unknown;
    }

    internal static TiffPredictor GetPredictor(ulong code)
    {
        if (code == 1)
        {
            return TiffPredictor.None;
        }
        if (code == 2)
        {
            return TiffPredictor.Horizontal;
        }
        if (code == 3)
        {
            return TiffPredictor.FloatingPoint;
        }
        return TiffPredictor.Unknown;
    }

    internal static TiffSampleFormat GetSampleFormat(ulong code)
    {
        if (code >= 1 && code <= 4)
        {
            return (TiffSampleFormat)code;
        }
        return TiffSampleFormat.Unknown;
    }

    internal static TiffStorageKind GetStorageKind(TiffImageDescriptor descriptor)
    {
        bool strips = descriptor.StripOffsets.Length != 0 || descriptor.StripByteCounts.Length != 0;
        bool tiles = descriptor.TileOffsets.Length != 0 || descriptor.TileByteCounts.Length != 0;
        if (strips && tiles)
        {
            return TiffStorageKind.Mixed;
        }
        if (strips)
        {
            return TiffStorageKind.Strips;
        }
        if (tiles)
        {
            return TiffStorageKind.Tiles;
        }
        return TiffStorageKind.None;
    }

    internal static void RequireBlockLimits(TiffImageDescriptor descriptor, RawLimits? limits)
    {
        long count = descriptor.StripOffsets.Length;
        if (descriptor.StripByteCounts.Length > count)
        {
            count = descriptor.StripByteCounts.Length;
        }
        long tileCount = descriptor.TileOffsets.Length;
        if (descriptor.TileByteCounts.Length > tileCount)
        {
            tileCount = descriptor.TileByteCounts.Length;
        }
        count = CheckedMath.Add(count, tileCount);
        if (count > (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxStripOrTileCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The TIFF strip or tile count exceeds the configured limit.", descriptor.Ifd.AbsoluteOffset);
        }
        CheckedMath.AllocationSize(count, 8, limits);
    }

    internal static TiffDataBlock[] CreateBlocks(TiffImageDescriptor descriptor)
    {
        int stripCount = descriptor.StripOffsets.Length;
        if (descriptor.StripByteCounts.Length < stripCount)
        {
            stripCount = descriptor.StripByteCounts.Length;
        }
        int tileCount = descriptor.TileOffsets.Length;
        if (descriptor.TileByteCounts.Length < tileCount)
        {
            tileCount = descriptor.TileByteCounts.Length;
        }
        TiffDataBlock[] blocks = new TiffDataBlock[stripCount + tileCount];
        ulong stripsPerPlane = GetStripsPerPlane(descriptor);
        ulong tilesPerPlane = GetTilesPerPlane(descriptor);
        for (int index = 0; index < stripCount; index++)
        {
            blocks[index] = CreateBlock(descriptor.StripOffsets[index], descriptor.StripByteCounts[index], false, index, stripsPerPlane, descriptor.PlanarConfiguration);
        }
        for (int index = 0; index < tileCount; index++)
        {
            blocks[stripCount + index] = CreateBlock(descriptor.TileOffsets[index], descriptor.TileByteCounts[index], true, index, tilesPerPlane, descriptor.PlanarConfiguration);
        }
        return blocks;
    }

    internal static TiffDataBlock CreateBlock(ulong offset, ulong byteCount, bool tiled, int index, ulong blocksPerPlane, TiffPlanarConfiguration planarConfiguration)
    {
        int plane = 0;
        int indexInPlane = index;
        if (planarConfiguration == TiffPlanarConfiguration.Planar && blocksPerPlane != 0)
        {
            ulong planeValue = (ulong)index / blocksPerPlane;
            ulong indexValue = (ulong)index % blocksPerPlane;
            if (planeValue <= int.MaxValue)
            {
                plane = (int)planeValue;
            }
            if (indexValue <= int.MaxValue)
            {
                indexInPlane = (int)indexValue;
            }
        }
        return new TiffDataBlock(offset, byteCount, tiled, index, plane, indexInPlane);
    }

    internal static ulong GetStripsPerPlane(TiffImageDescriptor descriptor)
    {
        if (descriptor.Height == 0 || descriptor.RowsPerStrip == 0)
        {
            return 0;
        }
        return CeilingDivide(descriptor.Height, descriptor.RowsPerStrip);
    }

    internal static ulong GetTilesPerPlane(TiffImageDescriptor descriptor)
    {
        if (descriptor.Width == 0 || descriptor.Height == 0 || descriptor.TileWidth == 0 || descriptor.TileHeight == 0)
        {
            return 0;
        }
        ulong across = CeilingDivide(descriptor.Width, descriptor.TileWidth);
        ulong down = CeilingDivide(descriptor.Height, descriptor.TileHeight);
        if (across != 0 && down > ulong.MaxValue / across)
        {
            return 0;
        }
        return across * down;
    }

    internal static ulong CeilingDivide(ulong value, ulong divisor)
    {
        if (divisor == 0)
        {
            return 0;
        }
        ulong quotient = value / divisor;
        if (value % divisor != 0)
        {
            quotient++;
        }
        return quotient;
    }
}
