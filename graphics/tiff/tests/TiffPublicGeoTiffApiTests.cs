using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicGeoTiffApiTests
{
    const int SemiMajorAxisKey = 2057;
    const int PrivateKey = 32768;

    [Test]
    public void GeoTiffRoundTrip_ExposesModelEpsgParametersPrivateKeysAndAreaTransforms()
    {
        string citation = "WGS 84 / UTM zone 33N|";
        string privateValue = "private-grid|";
        TiffGeoParameterMetadata parameters = CreateParameters(citation + privateValue, new double[] { 6378137 });
        TiffGeoKeyDirectory keys = CreateKeyDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 1),
            CreateInlineKey(TiffGeoKeyId.GeographicCrs, 4326),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32633),
            CreateInlineKey(TiffGeoKeyId.VerticalCrs, 5703),
            CreatePoolKey(TiffGeoKeyId.ProjectedCitation, TiffTagId.GeoAsciiParams, citation.Length, 0),
            CreatePoolKey(SemiMajorAxisKey, TiffTagId.GeoDoubleParams, 1, 0),
            CreatePoolKey(PrivateKey, TiffTagId.GeoAsciiParams, privateValue.Length, citation.Length)
        });
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateTiledImage(16));
        AssertSuccess(writer.WriteSourceRaster(imageIndex, CreateBytes(16 * 16, 3)));
        AssertSuccess(writer.WriteGeoTiffMetadata(imageIndex, CreateScaleModel(), keys, parameters));
        AssertSuccess(writer.Flush());
        TiffImage image = OpenMetadata(writer.GetFileBytes()).GetImage(0);

        TiffGeoModelMetadata model = image.ReadGeoModel().Metadata;
        TiffGeoKeyDirectoryReadResult keyResult = image.ReadGeoKeyDirectory();
        TiffGeoParameterReadResult parameterResult = image.ReadGeoParameters();
        TiffGeoKeyEntry semiMajor = FindKey(keyResult.Directory.Keys, SemiMajorAxisKey);
        TiffGeoTransformResult forward = TiffApi.Codec.ImagePixelToGeoModel(model, keyResult.Directory.RasterType, 0, 0, 0);
        TiffGeoTransformResult inverse = TiffApi.Codec.GeoModelToImagePixel(model, keyResult.Directory.RasterType, 105, 195, 0);

        Assert.That(model.HasPixelScale, Is.True);
        Assert.That(model.PixelScale, Is.EqualTo(new double[] { 10, 10, 1 }));
        Assert.That(model.HasTiepoints, Is.True);
        Assert.That(model.Tiepoints[0].ModelX, Is.EqualTo(100));
        Assert.That(model.Tiepoints[0].ModelY, Is.EqualTo(200));
        Assert.That(keyResult.Success, Is.True, DiagnosticText(keyResult.Diagnostics));
        Assert.That(keyResult.Directory.ModelType, Is.EqualTo(TiffGeoModelType.Projected));
        Assert.That(keyResult.Directory.RasterType, Is.EqualTo(TiffGeoRasterType.PixelIsArea));
        Assert.That(keyResult.Directory.HasGeographicCrsEpsgCode, Is.True);
        Assert.That(keyResult.Directory.GeographicCrsEpsgCode, Is.EqualTo(4326));
        Assert.That(keyResult.Directory.HasProjectedCrsEpsgCode, Is.True);
        Assert.That(keyResult.Directory.ProjectedCrsEpsgCode, Is.EqualTo(32633));
        Assert.That(keyResult.Directory.HasVerticalCrsEpsgCode, Is.True);
        Assert.That(keyResult.Directory.VerticalCrsEpsgCode, Is.EqualTo(5703));
        Assert.That(semiMajor.DoubleValues, Is.EqualTo(new double[] { 6378137 }));
        Assert.That(keyResult.Directory.PrivateKeys, Has.Length.EqualTo(1));
        Assert.That(keyResult.Directory.PrivateKeys[0].IsPrivate, Is.True);
        Assert.That(keyResult.Directory.PrivateKeys[0].AsciiValue, Is.EqualTo("private-grid"));
        Assert.That(parameterResult.Success, Is.True, DiagnosticText(parameterResult.Diagnostics));
        Assert.That(parameterResult.Metadata.DoubleParams, Is.EqualTo(new double[] { 6378137 }));
        Assert.That(parameterResult.Metadata.AsciiText, Is.EqualTo(citation + privateValue));
        Assert.That(forward.Success, Is.True, DiagnosticText(forward.Diagnostics));
        Assert.That(forward.Coordinate.X, Is.EqualTo(105).Within(0.0000001));
        Assert.That(forward.Coordinate.Y, Is.EqualTo(195).Within(0.0000001));
        Assert.That(inverse.Success, Is.True, DiagnosticText(inverse.Diagnostics));
        Assert.That(inverse.Coordinate.X, Is.EqualTo(0).Within(0.0000001));
        Assert.That(inverse.Coordinate.Y, Is.EqualTo(0).Within(0.0000001));
    }

    [Test]
    public void UserDefinedPointCrs_RoundTripsMatrixAndForwardInverseTransforms()
    {
        string citation = "Local engineering grid|";
        TiffGeoKeyDirectory keys = CreateKeyDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 2),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32767),
            CreatePoolKey(TiffGeoKeyId.ProjectedCitation, TiffTagId.GeoAsciiParams, citation.Length, 0)
        });
        TiffGeoModelMetadata model = new TiffGeoModelMetadata();
        model.HasTransformation = true;
        model.Transformation = new double[] { 2, 0, 0, 100, 0, -3, 0, 200, 0, 0, 1, 5, 0, 0, 0, 1 };
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateTiledImage(8));
        AssertSuccess(writer.WriteSourceRaster(imageIndex, CreateBytes(8 * 8, 7)));
        AssertSuccess(writer.WriteGeoTiffMetadata(imageIndex, model, keys, CreateParameters(citation, new double[0])));
        AssertSuccess(writer.Flush());
        TiffImage image = OpenMetadata(writer.GetFileBytes()).GetImage(0);

        TiffGeoModelMetadata storedModel = image.ReadGeoModel().Metadata;
        TiffGeoKeyDirectory storedKeys = image.ReadGeoKeyDirectory().Directory;
        TiffGeoTransformResult forward = TiffApi.Codec.ImagePixelToGeoModel(storedModel, storedKeys.RasterType, 4, 5, 6);
        TiffGeoTransformResult inverse = TiffApi.Codec.GeoModelToImagePixel(storedModel, storedKeys.RasterType, 108, 185, 11);

        Assert.That(storedModel.HasTransformation, Is.True);
        Assert.That(storedModel.Transformation, Is.EqualTo(model.Transformation));
        Assert.That(storedKeys.RasterType, Is.EqualTo(TiffGeoRasterType.PixelIsPoint));
        Assert.That(storedKeys.HasUserDefinedCrs, Is.True);
        Assert.That(storedKeys.HasProjectedCrsEpsgCode, Is.False);
        Assert.That(storedKeys.UserDefinedCrsName, Is.EqualTo("Local engineering grid"));
        Assert.That(forward.Success, Is.True, DiagnosticText(forward.Diagnostics));
        Assert.That(forward.Coordinate.X, Is.EqualTo(108).Within(0.0000001));
        Assert.That(forward.Coordinate.Y, Is.EqualTo(185).Within(0.0000001));
        Assert.That(forward.Coordinate.Z, Is.EqualTo(11).Within(0.0000001));
        Assert.That(inverse.Success, Is.True, DiagnosticText(inverse.Diagnostics));
        Assert.That(inverse.Coordinate.X, Is.EqualTo(4).Within(0.0000001));
        Assert.That(inverse.Coordinate.Y, Is.EqualTo(5).Within(0.0000001));
        Assert.That(inverse.Coordinate.Z, Is.EqualTo(6).Within(0.0000001));
    }

    [Test]
    public void CloudOptimizedGeoTiff_WritesGeospatialInternalOverviewsInRangeFriendlyOrder()
    {
        TiffCloudOptimizedGeoTiffWriteConfiguration configuration = CreateCloudConfiguration(32, new int[] { 16, 8 });
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult configured = writer.AddCloudOptimizedGeoTiff(configuration);

        Assert.That(configured.Success, Is.True, DiagnosticText(configured.Diagnostics));
        AssertSuccess(writer.WriteSourceRaster(configured.BaseImageIndex, CreateBytes(32 * 32, 11)));
        AssertSuccess(writer.WriteSubImageSourceRaster(configured.BaseImageIndex, configured.OverviewImageIndices[0], CreateBytes(16 * 16, 12)));
        AssertSuccess(writer.WriteSubImageSourceRaster(configured.BaseImageIndex, configured.OverviewImageIndices[1], CreateBytes(8 * 8, 13)));
        AssertSuccess(writer.Flush());
        byte[] bytes = writer.GetFileBytes();
        TiffFile file = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffImage image = file.GetImage(0);
        TiffImage middle = image.GetPyramidLevel(1);
        TiffImage smallest = image.GetPyramidLevel(2);
        ulong baseFirstTile = image.GetUnsignedTagArray(TiffTagId.TileOffsets)[0];
        ulong middleFirstTile = middle.GetUnsignedTagArray(TiffTagId.TileOffsets)[0];
        ulong smallestFirstTile = smallest.GetUnsignedTagArray(TiffTagId.TileOffsets)[0];
        TiffCloudOptimizedGeoTiffValidationResult validation = TiffApi.Validation.ValidateCloudOptimizedFile(file);

        Assert.That(validation.Success, Is.True, DiagnosticText(validation.Diagnostics));
        Assert.That(validation.HasRangeFriendlyLayout, Is.True);
        Assert.That(validation.FullResolutionImageCount, Is.EqualTo(1));
        Assert.That(validation.InternalOverviewImageCount, Is.EqualTo(2));
        Assert.That(validation.OverviewImageCount, Is.EqualTo(2));
        Assert.That(image.ReadGeoKeyDirectory().Directory.ProjectedCrsEpsgCode, Is.EqualTo(32633));
        Assert.That(image.GetPyramidLevelCount(), Is.EqualTo(3));
        Assert.That(middle.GetInfo().Width, Is.EqualTo(16));
        Assert.That(smallest.GetInfo().Width, Is.EqualTo(8));
        Assert.That(smallestFirstTile, Is.LessThan(middleFirstTile));
        Assert.That(middleFirstTile, Is.LessThan(baseFirstTile));
        Assert.That(validation.FirstTileOffset, Is.EqualTo(smallestFirstTile));
        Assert.That(validation.LastTileEndOffset, Is.EqualTo((ulong)bytes.Length));
    }

    [Test]
    public void CloudOptimizedGeoTiff_RejectsMissingAndNonRangeFriendlyConfiguration()
    {
        TiffCloudOptimizedGeoTiffWriteConfiguration strippedBase = CreateCloudConfiguration(32, new int[] { 16 });
        strippedBase.BaseImage.Layout.Kind = TiffWriterLayoutKind.Strips;
        TiffWriter strippedBaseWriter = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult strippedBaseResult = strippedBaseWriter.AddCloudOptimizedGeoTiff(strippedBase);

        TiffCloudOptimizedGeoTiffWriteConfiguration strippedOverview = CreateCloudConfiguration(32, new int[] { 16 });
        strippedOverview.Overviews[0].Layout.Kind = TiffWriterLayoutKind.Strips;
        TiffWriter strippedOverviewWriter = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult strippedOverviewResult = strippedOverviewWriter.AddCloudOptimizedGeoTiff(strippedOverview);

        TiffCloudOptimizedGeoTiffWriteConfiguration unordered = CreateCloudConfiguration(32, new int[] { 16, 20 });
        TiffWriter unorderedWriter = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult unorderedResult = unorderedWriter.AddCloudOptimizedGeoTiff(unordered);

        TiffCloudOptimizedGeoTiffWriteConfiguration missingGeo = new TiffCloudOptimizedGeoTiffWriteConfiguration();
        missingGeo.BaseImage = CreateTiledImage(32);
        TiffWriter missingGeoWriter = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffCloudOptimizedGeoTiffWriteResult missingGeoResult = missingGeoWriter.AddCloudOptimizedGeoTiff(missingGeo);

        AssertRejectedCloudConfiguration(strippedBaseResult, TiffErrorCode.InvalidTagValue);
        AssertRejectedCloudConfiguration(strippedOverviewResult, TiffErrorCode.InvalidTagValue);
        AssertRejectedCloudConfiguration(unorderedResult, TiffErrorCode.InvalidTagValue);
        AssertRejectedCloudConfiguration(missingGeoResult, TiffErrorCode.MissingRequiredTag);
    }

    [Test]
    public void ExternalOverviewStreams_ResolveAndOrderCompatiblePublicImages()
    {
        byte[] baseBytes = CreateGeoTiffBytes(16);
        byte[] overviewBytes = CreateExternalOverviewBytes();
        TiffFile baseFile = TiffApi.Factory.OpenReadBytes(baseBytes, new TiffReadOptions());
        MemoryStream stream = new MemoryStream(overviewBytes);
        TiffReadOptions options = new TiffReadOptions();
        options.LeaveOpen = true;

        TiffExternalOverviewSet overviews = TiffApi.Factory.ResolveExternalOverviewStreams(baseFile.GetImage(0), new Stream[] { stream }, options);
        TiffExternalOverviewRelationship first = overviews.GetRelationship(0);
        TiffExternalOverviewRelationship second = overviews.GetRelationship(1);

        Assert.That(overviews.GetSuccess(), Is.True, DiagnosticText(overviews.GetDiagnostics()));
        Assert.That(overviews.GetRelationshipCount(), Is.EqualTo(2));
        Assert.That(first.OverviewWidth, Is.EqualTo(8));
        Assert.That(first.HorizontalReduction, Is.EqualTo(2));
        Assert.That(first.IsReducedResolution, Is.True);
        Assert.That(second.OverviewWidth, Is.EqualTo(4));
        Assert.That(second.HorizontalReduction, Is.EqualTo(4));
        Assert.That(second.IsReducedResolution, Is.True);
        Assert.That(overviews.GetOverviewImage(0).ReadEncodedStrip(0).Bytes, Is.EqualTo(CreateBytes(8 * 8, 21)));
        Assert.That(overviews.GetOverviewImage(1).ReadEncodedStrip(0).Bytes, Is.EqualTo(CreateBytes(4 * 4, 22)));

        overviews.Close();
        Assert.That(stream.CanRead, Is.True);
        baseFile.Close();
        stream.Dispose();
    }

    [Test]
    public void MalformedGeospatialTags_ReportStructuredPublicDiagnostics()
    {
        TiffImageDefinition definition = TiffApi.Data.CreateImageDefinition(new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 1),
            TiffApi.Data.CreateFloatingArrayTag(TiffTagId.ModelPixelScale, TiffDataType.Double, new double[] { 1, 1 }),
            TiffApi.Data.CreateUnsignedArrayTag(TiffTagId.GeoKeyDirectory, TiffDataType.Short, new ulong[] { 1, 1, 1, 1 })
        });
        TiffApi.Data.SetRawStrip(definition, 0, new byte[] { 1 });
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        writer.AddImage(definition);
        AssertSuccess(writer.Flush());
        TiffReadOptions options = new TiffReadOptions();
        options.Strict = false;
        TiffImage image = TiffApi.Factory.OpenMetadataBytes(writer.GetFileBytes(), options).GetImage(0);

        TiffGeoModelReadResult model = image.ReadGeoModel();
        TiffGeoKeyDirectoryReadResult keys = image.ReadGeoKeyDirectory();

        Assert.That(model.Success, Is.False);
        Assert.That(model.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(FindDiagnostic(model.Diagnostics, TiffErrorCode.InvalidTagCount).TagId, Is.EqualTo(TiffTagId.ModelPixelScale));
        Assert.That(keys.Success, Is.False);
        Assert.That(keys.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(FindDiagnostic(keys.Diagnostics, TiffErrorCode.InvalidTagCount).TagId, Is.EqualTo(TiffTagId.GeoKeyDirectory));
    }

    static TiffGeoModelMetadata CreateScaleModel()
    {
        TiffGeoModelMetadata model = new TiffGeoModelMetadata();
        model.HasPixelScale = true;
        model.PixelScale = new double[] { 10, 10, 1 };
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
        parameters.HasDoubleParams = doubles.Length > 0;
        parameters.DoubleParams = doubles;
        return parameters;
    }

    static TiffGeoKeyDirectory CreateKeyDirectory(TiffGeoKeyEntry[] keys)
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

    static TiffWriterImageConfiguration CreateTiledImage(int size)
    {
        TiffWriterImageConfiguration image = new TiffWriterImageConfiguration();
        image.Width = size;
        image.Length = size;
        image.Layout.Kind = TiffWriterLayoutKind.Tiles;
        image.Layout.TileWidth = 8;
        image.Layout.TileLength = 8;
        image.Layout.TileDepth = 1;
        return image;
    }

    static TiffCloudOptimizedGeoTiffWriteConfiguration CreateCloudConfiguration(int baseSize, int[] overviewSizes)
    {
        TiffCloudOptimizedGeoTiffWriteConfiguration configuration = new TiffCloudOptimizedGeoTiffWriteConfiguration();
        configuration.BaseImage = CreateTiledImage(baseSize);
        configuration.Overviews = new TiffWriterImageConfiguration[overviewSizes.Length];
        int index = 0;
        while (index < overviewSizes.Length)
        {
            configuration.Overviews[index] = CreateTiledImage(overviewSizes[index]);
            index = index + 1;
        }

        configuration.Model = CreateScaleModel();
        configuration.KeyDirectory = CreateKeyDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 1),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32633)
        });
        return configuration;
    }

    static byte[] CreateGeoTiffBytes(int size)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateTiledImage(size));
        AssertSuccess(writer.WriteSourceRaster(imageIndex, CreateBytes(size * size, 20)));
        TiffGeoKeyDirectory keys = CreateKeyDirectory(new TiffGeoKeyEntry[]
        {
            CreateInlineKey(TiffGeoKeyId.ModelType, 1),
            CreateInlineKey(TiffGeoKeyId.RasterType, 1),
            CreateInlineKey(TiffGeoKeyId.ProjectedCrs, 32633)
        });
        AssertSuccess(writer.WriteGeoTiffMetadata(imageIndex, CreateScaleModel(), keys, new TiffGeoParameterMetadata()));
        AssertSuccess(writer.Flush());
        return writer.GetFileBytes();
    }

    static byte[] CreateExternalOverviewBytes()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int first = writer.AddImage(CreateStrippedDefinition(4, true, 22));
        int second = writer.AddImage(CreateStrippedDefinition(8, true, 21));
        Assert.That(first, Is.EqualTo(0));
        Assert.That(second, Is.EqualTo(1));
        AssertSuccess(writer.Flush());
        return writer.GetFileBytes();
    }

    static TiffImageDefinition CreateStrippedDefinition(int size, bool reduced, byte value)
    {
        int tagCount = reduced ? 6 : 5;
        TiffTagValue[] tags = new TiffTagValue[tagCount];
        tags[0] = TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, (ulong)size);
        tags[1] = TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, (ulong)size);
        tags[2] = TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8);
        tags[3] = TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero);
        tags[4] = TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, (ulong)size);
        if (reduced)
        {
            tags[5] = TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.NewSubfileType, TiffDataType.Long, 1);
        }

        TiffImageDefinition definition = TiffApi.Data.CreateImageDefinition(tags);
        TiffApi.Data.SetRawStrip(definition, 0, CreateBytes(size * size, value));
        return definition;
    }

    static TiffFile OpenMetadata(byte[] bytes)
    {
        return TiffApi.Factory.OpenMetadataBytes(bytes, new TiffReadOptions());
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

    static TiffDiagnostic FindDiagnostic(TiffDiagnostics diagnostics, TiffErrorCode code)
    {
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (diagnostics.Get(index).Code == code)
            {
                return diagnostics.Get(index);
            }

            index = index + 1;
        }

        Assert.Fail("Diagnostic was not found: " + code);
        return new TiffDiagnostic(TiffDiagnosticSeverity.Error, TiffErrorCode.UnknownError, "missing diagnostic");
    }

    static void AssertRejectedCloudConfiguration(TiffCloudOptimizedGeoTiffWriteResult result, TiffErrorCode code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.BaseImageIndex, Is.EqualTo(-1));
        Assert.That(result.Diagnostics.ContainsCode(code), Is.True, DiagnosticText(result.Diagnostics));
    }

    static byte[] CreateBytes(int count, byte value)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = value;
            index = index + 1;
        }

        return bytes;
    }

    static void AssertSuccess(TiffDiagnostics diagnostics)
    {
        Assert.That(diagnostics.HasErrors(), Is.False, DiagnosticText(diagnostics));
    }

    static string DiagnosticText(TiffDiagnostics diagnostics)
    {
        string text = "";
        int index = 0;
        while (index < diagnostics.Count())
        {
            if (text.Length > 0)
            {
                text = text + " | ";
            }

            text = text + diagnostics.Format(diagnostics.Get(index));
            index = index + 1;
        }

        return text;
    }
}
