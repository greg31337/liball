using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffGeoTiffMetadataWriterTests
{
    [Test]
    public void WriteClassicGeoTiff_WritesAndReadsModelKeysAndParams()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };
        TiffGeoModelMetadata model = CreateModel();
        TiffGeoKeyDirectory keys = CreateKeys();
        TiffGeoParameterMetadata parameters = CreateParameters();
        byte[] fileBytes = new byte[8 + TiffGeoTiffMetadataWriter.CalculateClassicWriteByteCount(values, model, keys, parameters)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffGeoTiffWriteResult writeResult = TiffCodec.WriteClassicGeoTiff(fileBytes, values, model, keys, parameters, TiffByteOrder.LittleEndian, 8, 0);
        TiffHeaderReadResult header = TiffHeader.ReadClassic(fileBytes);
        TiffDirectory directory = TiffDirectoryReader.Read(header.Header, fileBytes).Directories[0];
        TiffGeoModelReadResult modelRead = TiffCodec.ReadGeoModel(directory, fileBytes, header.Header);
        TiffGeoKeyDirectoryReadResult keyRead = TiffCodec.ReadGeoKeyDirectory(directory, fileBytes, header.Header);
        TiffGeoParameterReadResult parameterRead = TiffCodec.ReadGeoParameters(directory, fileBytes, header.Header);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(7));
        Assert.That(directory.FindEntry(TiffTagId.ModelPixelScale)!.DataType, Is.EqualTo(TiffDataType.Double));
        Assert.That(directory.FindEntry(TiffTagId.GeoKeyDirectory)!.DataType, Is.EqualTo(TiffDataType.Short));
        Assert.That(directory.FindEntry(TiffTagId.GeoAsciiParams)!.DataType, Is.EqualTo(TiffDataType.Ascii));
        Assert.That(modelRead.Success, Is.True);
        Assert.That(modelRead.Metadata.PixelScale, Is.EqualTo(new double[] { 30, 30, 0 }));
        Assert.That(modelRead.Metadata.TiepointCount(), Is.EqualTo(1));
        Assert.That(modelRead.Metadata.Tiepoints[0].ModelX, Is.EqualTo(100));
        Assert.That(modelRead.Metadata.Transformation[15], Is.EqualTo(1));
        Assert.That(keyRead.Success, Is.True);
        Assert.That(keyRead.Directory.KeyCount(), Is.EqualTo(3));
        Assert.That(keyRead.Directory.Keys[2].TiffTagLocation, Is.EqualTo(TiffTagId.GeoAsciiParams));
        Assert.That(parameterRead.Success, Is.True);
        Assert.That(parameterRead.Metadata.DoubleParams, Is.EqualTo(new double[] { 298.257223563 }));
        Assert.That(parameterRead.Metadata.AsciiText, Is.EqualTo("WGS 84|"));
        Assert.That(parameterRead.Metadata.AsciiValues[0], Is.EqualTo("WGS 84"));
    }

    [Test]
    public void WriteClassicGeoTiff_ReplacesExistingGeoTiffTagsAndPreservesOtherValues()
    {
        TiffTagValue[] values = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10),
            TiffTagValue.CreateFloatingArray(TiffTagId.ModelPixelScale, TiffDataType.Double, new double[] { 1, 1, 0 }),
            TiffGeoTiffMetadataWriter.CreateGeoAsciiParamsValue(TiffGeoParameterMetadata.Create(false, new double[0], true, "old|", new string[] { "old" }, new byte[0]), new TiffDiagnostics())
        };
        TiffGeoModelMetadata model = TiffGeoModelMetadata.Create(true, new double[] { 2, 3, 0 }, false, new TiffGeoTiepoint[0], false, new double[0]);
        TiffGeoKeyDirectory keys = new TiffGeoKeyDirectory();
        TiffGeoParameterMetadata parameters = TiffGeoParameterMetadata.Create(false, new double[0], true, "new|", new string[] { "new" }, new byte[0]);
        byte[] fileBytes = new byte[8 + TiffGeoTiffMetadataWriter.CalculateClassicWriteByteCount(values, model, keys, parameters)];
        TiffHeader.WriteClassic(fileBytes.AsSpan(0, 8), TiffByteOrder.LittleEndian, 8);

        TiffGeoTiffWriteResult writeResult = TiffCodec.WriteClassicGeoTiff(fileBytes, values, model, keys, parameters, TiffByteOrder.LittleEndian, 8, 0);
        TiffDirectory directory = TiffDirectoryReader.ReadClassic(fileBytes, TiffByteOrder.LittleEndian, 8).Directories[0];
        TiffGeoModelReadResult modelRead = TiffCodec.ReadGeoModel(directory, fileBytes, TiffByteOrder.LittleEndian);
        TiffGeoParameterReadResult parameterRead = TiffCodec.ReadGeoParameters(directory, fileBytes, TiffByteOrder.LittleEndian);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(directory.EntryCount(), Is.EqualTo(3));
        Assert.That(modelRead.Metadata.PixelScale, Is.EqualTo(new double[] { 2, 3, 0 }));
        Assert.That(parameterRead.Metadata.AsciiText, Is.EqualTo("new|"));
    }

    [Test]
    public void WriteClassicGeoTiff_ReportsShortDestination()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };

        TiffGeoTiffWriteResult result = TiffCodec.WriteClassicGeoTiff(new byte[16], values, CreateModel(), CreateKeys(), CreateParameters(), TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void WriteClassicGeoTiff_ReportsInvalidModelTagCount()
    {
        TiffTagValue[] values = new TiffTagValue[] { TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 10) };
        TiffGeoModelMetadata model = TiffGeoModelMetadata.Create(true, new double[] { 30, 30 }, false, new TiffGeoTiepoint[0], false, new double[0]);
        byte[] fileBytes = new byte[128];

        TiffGeoTiffWriteResult result = TiffCodec.WriteClassicGeoTiff(fileBytes, values, model, new TiffGeoKeyDirectory(), new TiffGeoParameterMetadata(), TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.ModelPixelScale));
    }

    [Test]
    public void WriteClassicGeoTiff_ReportsGeoKeyValueOutsideShortRange()
    {
        TiffGeoKeyDirectory keys = TiffGeoKeyDirectory.Create(true, 1, 1, 1, 1, new TiffGeoKeyEntry[] { TiffGeoKeyEntry.Create(1024, 0, 1, 70000) });
        byte[] fileBytes = new byte[128];

        TiffGeoTiffWriteResult result = TiffCodec.WriteClassicGeoTiff(fileBytes, new TiffTagValue[0], new TiffGeoModelMetadata(), keys, new TiffGeoParameterMetadata(), TiffByteOrder.LittleEndian, 8, 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoKeyDirectory));
    }

    static TiffGeoModelMetadata CreateModel()
    {
        return TiffGeoModelMetadata.Create(
            true,
            new double[] { 30, 30, 0 },
            true,
            new TiffGeoTiepoint[] { TiffGeoTiepoint.Create(0, 0, 0, 100, 200, 0) },
            true,
            new double[] { 1, 0, 0, 100, 0, -1, 0, 200, 0, 0, 1, 0, 0, 0, 0, 1 });
    }

    static TiffGeoKeyDirectory CreateKeys()
    {
        return TiffGeoKeyDirectory.Create(
            true,
            1,
            1,
            1,
            3,
            new TiffGeoKeyEntry[]
            {
                TiffGeoKeyEntry.Create(1024, 0, 1, 1),
                TiffGeoKeyEntry.Create(2059, TiffTagId.GeoDoubleParams, 1, 0),
                TiffGeoKeyEntry.Create(2049, TiffTagId.GeoAsciiParams, 7, 0)
            });
    }

    static TiffGeoParameterMetadata CreateParameters()
    {
        return TiffGeoParameterMetadata.Create(true, new double[] { 298.257223563 }, true, "WGS 84|", new string[] { "WGS 84" }, new byte[0]);
    }
}
