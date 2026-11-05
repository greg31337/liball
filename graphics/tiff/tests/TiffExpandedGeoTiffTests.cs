using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffExpandedGeoTiffTests
{
    const int SemiMajorAxisKey = 2057;
    const int PrivateKey = 32768;

    [Test]
    public void ReadGeoKeyDirectory_ResolvesEpsgPoolsRasterSemanticsAndPrivateKeys()
    {
        string ascii = "WGS 84 / UTM zone 33N|private-name|";
        TiffGeoParameterMetadata parameters = CreateParameters(ascii, new double[] { 6378137 });
        TiffGeoKeyDirectory keys = CreateDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 1),
            CreateInlineKey(TiffGeoKeyId.GeographicCrs, 4326),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32633),
            CreateInlineKey(TiffGeoKeyId.VerticalCrs, 5703),
            CreatePoolKey(TiffGeoKeyId.ProjectedCitation, TiffTagId.GeoAsciiParams, 22, 0),
            CreatePoolKey(SemiMajorAxisKey, TiffTagId.GeoDoubleParams, 1, 0),
            CreatePoolKey(PrivateKey, TiffTagId.GeoAsciiParams, 13, 22)
        });

        TiffGeoKeyDirectoryReadResult result = WriteAndRead(CreateScaleModel(), keys, parameters);
        TiffGeoKeyEntry citation = FindKey(result.Directory.Keys, TiffGeoKeyId.ProjectedCitation);
        TiffGeoKeyEntry semiMajor = FindKey(result.Directory.Keys, SemiMajorAxisKey);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Directory.ModelType, Is.EqualTo(TiffGeoModelType.Projected));
        Assert.That(result.Directory.RasterType, Is.EqualTo(TiffGeoRasterType.PixelIsArea));
        Assert.That(result.Directory.HasGeographicCrsEpsgCode, Is.True);
        Assert.That(result.Directory.GeographicCrsEpsgCode, Is.EqualTo(4326));
        Assert.That(result.Directory.HasProjectedCrsEpsgCode, Is.True);
        Assert.That(result.Directory.ProjectedCrsEpsgCode, Is.EqualTo(32633));
        Assert.That(result.Directory.HasVerticalCrsEpsgCode, Is.True);
        Assert.That(result.Directory.VerticalCrsEpsgCode, Is.EqualTo(5703));
        Assert.That(citation.HasResolvedValue, Is.True);
        Assert.That(citation.ValueKind, Is.EqualTo(TiffGeoKeyValueKind.Ascii));
        Assert.That(citation.AsciiValue, Is.EqualTo("WGS 84 / UTM zone 33N"));
        Assert.That(semiMajor.ValueKind, Is.EqualTo(TiffGeoKeyValueKind.Double));
        Assert.That(semiMajor.DoubleValues, Is.EqualTo(new double[] { 6378137 }));
        Assert.That(result.Directory.PrivateKeys.Length, Is.EqualTo(1));
        Assert.That(result.Directory.PrivateKeys[0].KeyId, Is.EqualTo(PrivateKey));
        Assert.That(result.Directory.PrivateKeys[0].IsPrivate, Is.True);
        Assert.That(result.Directory.PrivateKeys[0].AsciiValue, Is.EqualTo("private-name"));
    }

    [Test]
    public void ReadGeoKeyDirectory_ExposesUserDefinedCrsCitation()
    {
        string citation = "Local engineering grid|";
        TiffGeoKeyDirectory keys = CreateDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 2),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32767),
            CreatePoolKey(TiffGeoKeyId.ProjectedCitation, TiffTagId.GeoAsciiParams, citation.Length, 0)
        });

        TiffGeoKeyDirectoryReadResult result = WriteAndRead(CreateScaleModel(), keys, CreateParameters(citation, new double[0]));

        Assert.That(result.Success, Is.True);
        Assert.That(result.Directory.HasUserDefinedCrs, Is.True);
        Assert.That(result.Directory.HasProjectedCrsEpsgCode, Is.False);
        Assert.That(result.Directory.UserDefinedCrsName, Is.EqualTo("Local engineering grid"));
        Assert.That(result.Directory.RasterType, Is.EqualTo(TiffGeoRasterType.PixelIsPoint));
    }

    [Test]
    public void GeoTransforms_ExposeInverseAndHonorRasterPixelSemantics()
    {
        TiffGeoModelMetadata model = CreateScaleModel();

        TiffGeoTransformResult areaForward = TiffApi.Codec.ImagePixelToGeoModel(model, TiffGeoRasterType.PixelIsArea, 0, 0, 0);
        TiffGeoTransformResult areaInverse = TiffApi.Codec.GeoModelToImagePixel(model, TiffGeoRasterType.PixelIsArea, 105, 195, 0);
        TiffGeoTransformResult pointForward = TiffApi.Codec.ImagePixelToGeoModel(model, TiffGeoRasterType.PixelIsPoint, 0, 0, 0);
        TiffGeoTransformResult rawInverse = TiffApi.Codec.GeoModelToImage(model, 120, 170, 0);
        TiffGeoTransformResult unknown = TiffApi.Codec.ImagePixelToGeoModel(model, TiffGeoRasterType.Unknown, 0, 0, 0);

        Assert.That(areaForward.Success, Is.True);
        Assert.That(areaForward.Coordinate.X, Is.EqualTo(105).Within(0.0000001));
        Assert.That(areaForward.Coordinate.Y, Is.EqualTo(195).Within(0.0000001));
        Assert.That(areaInverse.Success, Is.True);
        Assert.That(areaInverse.Coordinate.X, Is.EqualTo(0).Within(0.0000001));
        Assert.That(areaInverse.Coordinate.Y, Is.EqualTo(0).Within(0.0000001));
        Assert.That(pointForward.Coordinate.X, Is.EqualTo(100).Within(0.0000001));
        Assert.That(pointForward.Coordinate.Y, Is.EqualTo(200).Within(0.0000001));
        Assert.That(rawInverse.Success, Is.True);
        Assert.That(rawInverse.Coordinate.X, Is.EqualTo(2).Within(0.0000001));
        Assert.That(rawInverse.Coordinate.Y, Is.EqualTo(3).Within(0.0000001));
        Assert.That(unknown.Success, Is.False);
        Assert.That(unknown.Diagnostics.Get(0).TagId, Is.EqualTo(TiffTagId.GeoKeyDirectory));
    }

    [Test]
    public void WriteGeoTiffMetadata_ReportsKeyAndModelConflicts()
    {
        TiffWriter keyWriter = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int keyImageIndex = keyWriter.AddImage(new TiffImageDefinition());
        TiffGeoKeyDirectory conflictingKeys = CreateDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 2),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32633),
            CreateInlineKey(TiffGeoKeyId.RasterType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 2)
        });
        TiffDiagnostics keyDiagnostics = keyWriter.WriteGeoTiffMetadata(keyImageIndex, CreateScaleModel(), conflictingKeys, new TiffGeoParameterMetadata());

        TiffWriter modelWriter = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int modelImageIndex = modelWriter.AddImage(new TiffImageDefinition());
        TiffGeoModelMetadata conflictingModel = CreateScaleModel();
        conflictingModel.HasTransformation = true;
        conflictingModel.Transformation = new double[] { 1, 0, 0, 100, 0, -1, 0, 200, 0, 0, 1, 0, 0, 0, 0, 1 };
        TiffDiagnostics modelDiagnostics = modelWriter.WriteGeoTiffMetadata(modelImageIndex, conflictingModel, CreateDirectory(new TiffGeoKeyEntry[0]), new TiffGeoParameterMetadata());

        Assert.That(keyDiagnostics.ContainsCode(TiffErrorCode.DuplicateTag), Is.True);
        Assert.That(keyDiagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(modelDiagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(FindDiagnostic(modelDiagnostics, TiffTagId.ModelTransformation).Severity, Is.EqualTo(TiffDiagnosticSeverity.Warning));
    }

    static TiffGeoKeyDirectoryReadResult WriteAndRead(TiffGeoModelMetadata model, TiffGeoKeyDirectory keys, TiffGeoParameterMetadata parameters)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(new TiffImageDefinition());
        TiffDiagnostics write = writer.WriteGeoTiffMetadata(imageIndex, model, keys, parameters);
        Assert.That(write.ContainsCode(TiffErrorCode.InvalidTagValue), Is.False);
        writer.Flush();
        TiffImage image = TiffFile.OpenMetadata(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);
        return image.ReadGeoKeyDirectory();
    }

    static TiffGeoModelMetadata CreateScaleModel()
    {
        TiffGeoModelMetadata model = new TiffGeoModelMetadata();
        model.HasPixelScale = true;
        model.PixelScale = new double[] { 10, 10, 0 };
        model.HasTiepoints = true;
        TiffGeoTiepoint tiepoint = new TiffGeoTiepoint();
        tiepoint.ModelX = 100;
        tiepoint.ModelY = 200;
        model.Tiepoints = new TiffGeoTiepoint[] { tiepoint };
        return model;
    }

    static TiffGeoParameterMetadata CreateParameters(string ascii, double[] doubles)
    {
        TiffGeoParameterMetadata parameters = new TiffGeoParameterMetadata();
        parameters.HasAsciiParams = ascii.Length > 0;
        parameters.AsciiText = ascii;
        parameters.AsciiValues = ascii.Length > 0 ? new string[] { ascii } : new string[0];
        parameters.HasDoubleParams = doubles.Length > 0;
        parameters.DoubleParams = doubles;
        return parameters;
    }

    static TiffGeoKeyDirectory CreateDirectory(TiffGeoKeyEntry[] keys)
    {
        TiffGeoKeyDirectory directory = new TiffGeoKeyDirectory();
        directory.HasDirectory = true;
        directory.KeyDirectoryVersion = 1;
        directory.KeyRevision = 1;
        directory.MinorRevision = 1;
        directory.NumberOfKeys = keys.Length;
        directory.Keys = keys;
        return directory;
    }

    static TiffGeoKeyEntry CreateInlineKey(int keyId, int value)
    {
        TiffGeoKeyEntry key = new TiffGeoKeyEntry();
        key.KeyId = keyId;
        key.Count = 1;
        key.ValueOffset = value;
        return key;
    }

    static TiffGeoKeyEntry CreatePoolKey(int keyId, int tagId, int count, int offset)
    {
        TiffGeoKeyEntry key = new TiffGeoKeyEntry();
        key.KeyId = keyId;
        key.TiffTagLocation = tagId;
        key.Count = count;
        key.ValueOffset = offset;
        return key;
    }

    static TiffGeoKeyEntry FindKey(TiffGeoKeyEntry[] keys, int keyId)
    {
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index].KeyId == keyId)
            {
                return keys[index];
            }

            index = index + 1;
        }

        Assert.Fail("GeoKey was not found: " + keyId);
        return new TiffGeoKeyEntry();
    }

    static TiffDiagnostic FindDiagnostic(TiffDiagnostics diagnostics, int tagId)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (diagnostics.Get(index).TagId == tagId)
            {
                return diagnostics.Get(index);
            }

            index = index + 1;
        }

        Assert.Fail("Diagnostic was not found for tag: " + tagId);
        return new TiffDiagnostic(TiffDiagnosticSeverity.Error, TiffErrorCode.UnknownError, "missing");
    }
}
