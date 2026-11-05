using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffExpandedImageInfoTests
{
    [Test]
    public void GetInfo_ExposesExpandedBaselineAndExtensionMetadata()
    {
        byte[] fileBytes = CreateClassicTiffFile(CreateExpandedValues());
        TiffImage image = TiffFile.OpenRead(fileBytes).GetImage(0);

        TiffImageInfo info = image.GetInfo();

        Assert.That(info.ImageDepth, Is.EqualTo(5));
        Assert.That(info.TileDepth, Is.EqualTo(2));
        Assert.That(info.NewSubfileType, Is.EqualTo(31));
        Assert.That(info.SubfileType, Is.EqualTo(3));
        Assert.That(info.IsReducedResolution, Is.True);
        Assert.That(info.IsPage, Is.True);
        Assert.That(info.IsTransparencyMask, Is.True);
        Assert.That(info.IsDepthMap, Is.True);
        Assert.That(info.IsEnhancedImage, Is.True);
        Assert.That(info.XResolution, Is.EqualTo(300));
        Assert.That(info.YResolution, Is.EqualTo(150));
        Assert.That(info.ResolutionUnit, Is.EqualTo(TiffResolutionUnit.Centimeter));
        Assert.That(info.XPosition, Is.EqualTo(0.5));
        Assert.That(info.YPosition, Is.EqualTo(1.5));
        Assert.That(info.MinSampleValues, Is.EqualTo(new double[] { 1, 2, 3 }));
        Assert.That(info.MaxSampleValues, Is.EqualTo(new double[] { 250, 1000, 4095 }));
        Assert.That(info.SMinSampleValues, Is.EqualTo(new double[] { -1, -2, -3 }));
        Assert.That(info.SMaxSampleValues, Is.EqualTo(new double[] { 1.5, 2.5, 3.5 }));
        Assert.That(info.WhitePoint, Is.EqualTo(new double[] { 0.3127, 0.3290 }).Within(0.000001));
        Assert.That(info.PrimaryChromaticities, Is.EqualTo(new double[] { 0.64, 0.33, 0.30, 0.60, 0.15, 0.06 }).Within(0.000001));
        Assert.That(info.InkSet, Is.EqualTo(2));
        Assert.That(info.InkNames, Is.EqualTo(new string[] { "Cyan", "Magenta", "Yellow" }));
        Assert.That(info.NumberOfInks, Is.EqualTo(3));
        Assert.That(info.DotRange, Is.EqualTo(new ushort[] { 5, 250 }));
        Assert.That(info.TargetPrinter, Is.EqualTo("Proof printer"));
        Assert.That(info.TransferFunction, Is.EqualTo(new ushort[] { 0, 128, 255 }));
        Assert.That(info.TransferRange, Is.EqualTo(new ushort[] { 0, 255, 0, 255, 0, 255 }));
        Assert.That(info.JpegTables, Is.EqualTo(new byte[] { 255, 216, 255, 217 }));
        Assert.That(info.CFARepeatPatternDimensions, Is.EqualTo(new int[] { 2, 2 }));
        Assert.That(info.CFAPattern, Is.EqualTo(new byte[] { 0, 1, 1, 2 }));
        Assert.That(info.Thresholding, Is.EqualTo(2));
        Assert.That(info.CellWidth, Is.EqualTo(8));
        Assert.That(info.CellLength, Is.EqualTo(6));
        Assert.That(info.GrayResponseUnit, Is.EqualTo(3));
        Assert.That(info.GrayResponseCurve, Is.EqualTo(new ushort[] { 0, 100, 200 }));
        Assert.That(info.ColorResponseUnit, Is.EqualTo(4));
        Assert.That(info.HalftoneHints, Is.EqualTo(new int[] { 10, 240 }));
        Assert.That(info.PageNumber, Is.EqualTo(new int[] { 2, 5 }));
        Assert.That(info.YCbCrPositioning, Is.EqualTo(2));
        Assert.That(info.DocumentName, Is.EqualTo("Document"));
        Assert.That(info.ImageDescription, Is.EqualTo("Description"));
        Assert.That(info.Make, Is.EqualTo("Camera make"));
        Assert.That(info.Model, Is.EqualTo("Camera model"));
        Assert.That(info.PageName, Is.EqualTo("Page name"));
        Assert.That(info.Software, Is.EqualTo("TIFF tests"));
        Assert.That(info.DateTime, Is.EqualTo("2026:08:26 12:34:56"));
        Assert.That(info.Artist, Is.EqualTo("Artist"));
        Assert.That(info.HostComputer, Is.EqualTo("Host"));
        Assert.That(info.Copyright, Is.EqualTo("Copyright owner"));
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void GetInfo_DerivesExpandedDefaultsFromSampleLayout()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 20),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.Rgb),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, 3),
            TiffTagValue.CreateUnsignedArray(TiffTagId.BitsPerSample, TiffDataType.Short, new ulong[] { 8, 10, 12 })
        };

        TiffImageInfo info = TiffFile.OpenRead(CreateClassicTiffFile(values)).GetImage(0).GetInfo();

        Assert.That(info.ImageDepth, Is.EqualTo(1));
        Assert.That(info.TileDepth, Is.EqualTo(1));
        Assert.That(info.NewSubfileType, Is.EqualTo(0));
        Assert.That(info.IsReducedResolution, Is.False);
        Assert.That(info.IsPage, Is.False);
        Assert.That(info.IsTransparencyMask, Is.False);
        Assert.That(info.ResolutionUnit, Is.EqualTo(TiffResolutionUnit.Inch));
        Assert.That(info.MinSampleValues, Is.EqualTo(new double[] { 0, 0, 0 }));
        Assert.That(info.MaxSampleValues, Is.EqualTo(new double[] { 255, 1023, 4095 }));
        Assert.That(info.SMinSampleValues, Is.Empty);
        Assert.That(info.SMaxSampleValues, Is.Empty);
        Assert.That(info.InkSet, Is.EqualTo(1));
        Assert.That(info.NumberOfInks, Is.EqualTo(4));
        Assert.That(info.Thresholding, Is.EqualTo(1));
        Assert.That(info.GrayResponseUnit, Is.EqualTo(2));
        Assert.That(info.YCbCrPositioning, Is.EqualTo(1));
        Assert.That(info.WhitePoint, Is.Empty);
        Assert.That(info.JpegTables, Is.Empty);
        Assert.That(info.DocumentName, Is.Empty);
    }

    [Test]
    public void MemoryExtractor_UsesTheSameExpandedMetadataRules()
    {
        byte[] fileBytes = CreateClassicTiffFile(CreateExpandedValues());
        TiffDirectoryReadResult directoryResult = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8);

        TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directoryResult.Directories[0], fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Info.ImageDepth, Is.EqualTo(5));
        Assert.That(result.Info.InkNames, Is.EqualTo(new string[] { "Cyan", "Magenta", "Yellow" }));
        Assert.That(result.Info.CFAPattern, Is.EqualTo(new byte[] { 0, 1, 1, 2 }));
        Assert.That(result.Info.Copyright, Is.EqualTo("Copyright owner"));
    }

    [Test]
    public void ApplyExpandedValues_ReportsInvalidFixedAndPerSampleCounts()
    {
        TiffImageInfo info = new TiffImageInfo();
        info.SamplesPerPixel = 3;
        info.BitsPerSample = new int[] { 8, 8, 8 };
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedArray(TiffTagId.MinSampleValue, TiffDataType.Short, new ulong[] { 1, 2 }),
            TiffTagValue.CreateRationalScalar(TiffTagId.WhitePoint, TiffDataType.Rational, 1, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.CFARepeatPatternDim, TiffDataType.Short, 2)
        };
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffImageInfoExtractor.ApplyExtendedValues(info, values, 7, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(CountDiagnostics(diagnostics, TiffErrorCode.InvalidTagCount), Is.EqualTo(3));
        Assert.That(info.MinSampleValues, Is.EqualTo(new double[] { 1, 2 }));
        Assert.That(info.WhitePoint, Is.EqualTo(new double[] { 0.5 }));
        Assert.That(info.CFARepeatPatternDimensions, Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void Copy_DetachesExpandedMutableMetadata()
    {
        TiffImageInfo original = TiffFile.OpenRead(CreateClassicTiffFile(CreateExpandedValues())).GetImage(0).GetInfo();

        TiffImageInfo copy = original.Copy();
        copy.MinSampleValues[0] = 99;
        copy.InkNames[0] = "Changed";
        copy.TransferFunction[0] = 99;
        copy.JpegTables[0] = 0;
        copy.CFARepeatPatternDimensions[0] = 9;
        copy.CFAPattern[0] = 9;

        Assert.That(original.MinSampleValues[0], Is.EqualTo(1));
        Assert.That(original.InkNames[0], Is.EqualTo("Cyan"));
        Assert.That(original.TransferFunction[0], Is.EqualTo(0));
        Assert.That(original.JpegTables[0], Is.EqualTo(255));
        Assert.That(original.CFARepeatPatternDimensions[0], Is.EqualTo(2));
        Assert.That(original.CFAPattern[0], Is.EqualTo(0));
    }

    static TiffTagValue[] CreateExpandedValues()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 20),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.Rgb),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, 3),
            TiffTagValue.CreateUnsignedArray(TiffTagId.BitsPerSample, TiffDataType.Short, new ulong[] { 8, 10, 12 }),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageDepth, TiffDataType.Long, 5),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileDepth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 31),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.SubfileType, TiffDataType.Short, 3),
            TiffTagValue.CreateRationalScalar(TiffTagId.XResolution, TiffDataType.Rational, 300, 1),
            TiffTagValue.CreateRationalScalar(TiffTagId.YResolution, TiffDataType.Rational, 150, 1),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ResolutionUnit, TiffDataType.Short, 3),
            TiffTagValue.CreateRationalScalar(TiffTagId.XPosition, TiffDataType.Rational, 1, 2),
            TiffTagValue.CreateRationalScalar(TiffTagId.YPosition, TiffDataType.Rational, 3, 2),
            TiffTagValue.CreateUnsignedArray(TiffTagId.MinSampleValue, TiffDataType.Short, new ulong[] { 1, 2, 3 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.MaxSampleValue, TiffDataType.Short, new ulong[] { 250, 1000, 4095 }),
            TiffTagValue.CreateSignedArray(TiffTagId.SMinSampleValue, TiffDataType.SLong, new long[] { -1, -2, -3 }),
            TiffTagValue.CreateFloatingArray(TiffTagId.SMaxSampleValue, TiffDataType.Float, new double[] { 1.5, 2.5, 3.5 }),
            TiffTagValue.CreateRationalArray(TiffTagId.WhitePoint, TiffDataType.Rational, CreateRationals(new long[] { 3127, 3290 }, new long[] { 10000, 10000 })),
            TiffTagValue.CreateRationalArray(TiffTagId.PrimaryChromaticities, TiffDataType.Rational, CreateRationals(new long[] { 64, 33, 30, 60, 15, 6 }, new long[] { 100, 100, 100, 100, 100, 100 })),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.InkSet, TiffDataType.Short, 2),
            TiffTagValue.CreateAsciiArray(TiffTagId.InkNames, new string[] { "Cyan", "Magenta", "Yellow" }),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.NumberOfInks, TiffDataType.Short, 3),
            TiffTagValue.CreateUnsignedArray(TiffTagId.DotRange, TiffDataType.Byte, new ulong[] { 5, 250 }),
            TiffTagValue.CreateAscii(TiffTagId.TargetPrinter, "Proof printer"),
            TiffTagValue.CreateUnsignedArray(TiffTagId.TransferFunction, TiffDataType.Short, new ulong[] { 0, 128, 255 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.TransferRange, TiffDataType.Short, new ulong[] { 0, 255, 0, 255, 0, 255 }),
            TiffTagValue.CreateRaw(TiffTagId.JPEGTables, TiffDataType.Undefined, 4, new byte[] { 255, 216, 255, 217 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.CFARepeatPatternDim, TiffDataType.Short, new ulong[] { 2, 2 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.CFAPattern, TiffDataType.Byte, new ulong[] { 0, 1, 1, 2 }),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.Thresholding, TiffDataType.Short, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.CellWidth, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.CellLength, TiffDataType.Short, 6),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.GrayResponseUnit, TiffDataType.Short, 3),
            TiffTagValue.CreateUnsignedArray(TiffTagId.GrayResponseCurve, TiffDataType.Short, new ulong[] { 0, 100, 200 }),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ColorResponseUnit, TiffDataType.Short, 4),
            TiffTagValue.CreateUnsignedArray(TiffTagId.HalftoneHints, TiffDataType.Short, new ulong[] { 10, 240 }),
            TiffTagValue.CreateUnsignedArray(TiffTagId.PageNumber, TiffDataType.Short, new ulong[] { 2, 5 }),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.YCbCrPositioning, TiffDataType.Short, 2),
            TiffTagValue.CreateAscii(TiffTagId.DocumentName, "Document"),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "Description"),
            TiffTagValue.CreateAscii(TiffTagId.Make, "Camera make"),
            TiffTagValue.CreateAscii(TiffTagId.Model, "Camera model"),
            TiffTagValue.CreateAscii(TiffTagId.PageName, "Page name"),
            TiffTagValue.CreateAscii(TiffTagId.Software, "TIFF tests"),
            TiffTagValue.CreateAscii(TiffTagId.DateTime, "2026:08:26 12:34:56"),
            TiffTagValue.CreateAscii(TiffTagId.Artist, "Artist"),
            TiffTagValue.CreateAscii(TiffTagId.HostComputer, "Host"),
            TiffTagValue.CreateAscii(TiffTagId.Copyright, "Copyright owner")
        };
    }

    static TiffRational[] CreateRationals(long[] numerators, long[] denominators)
    {
        TiffRational[] values = new TiffRational[numerators.Length];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = TiffRational.Create(numerators[index], denominators[index]);
            index = index + 1;
        }

        return values;
    }

    static byte[] CreateClassicTiffFile(TiffTagValue[] values)
    {
        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(values);
        TiffDiagnostics encodeDiagnostics = new TiffDiagnostics();
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(values, TiffByteOrder.LittleEndian, encodeDiagnostics);
        int layoutByteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        byte[] fileBytes = new byte[8 + layoutByteCount];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(fileBytes.AsSpan(8, layoutByteCount), entries, valueBytes, TiffByteOrder.LittleEndian, 8, 0);
        Assert.That(encodeDiagnostics.HasErrors(), Is.False);
        Assert.That(writeDiagnostics.HasErrors(), Is.False);
        return fileBytes;
    }

    static int CountDiagnostics(TiffDiagnostics diagnostics, TiffErrorCode code)
    {
        int count = 0;
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (diagnostics.Get(index).Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
