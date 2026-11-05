using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicScientificSlideApiTests
{
    [Test]
    public void OmeSingleFile_MapsEveryCztCoordinateToTheExpectedPlane()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><Image ID=\"Image:0\" Name=\"eight-plane-volume\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYCZT\" Type=\"uint8\" SizeX=\"1\" SizeY=\"1\" SizeC=\"2\" SizeZ=\"2\" SizeT=\"2\"><TiffData /></Pixels></Image></OME>";
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int index = 0;
        while (index < 8)
        {
            int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(1, 1));
            AssertSuccess(writer.WriteSourceRaster(imageIndex, new byte[] { (byte)(index + 1) }));
            index = index + 1;
        }

        TiffOmeMetadata metadata = new TiffOmeMetadata();
        metadata.HasOmeXml = true;
        metadata.Xml = xml;
        AssertSuccess(writer.WriteOmeMetadata(0, metadata));
        AssertSuccess(writer.Flush());
        TiffFile file = OpenRead(writer.GetFileBytes());

        TiffOmeMetadataReadResult dataset = file.GetOmeDataset();
        TiffImage plane = file.GetOmePlaneByCoordinates(0, 1, 0, 1);
        byte[] pixel = new byte[1];
        TiffDiagnostics read = plane.ReadSourceRasterToBuffer(pixel);

        Assert.That(dataset.Success, Is.True, DiagnosticText(dataset.Diagnostics));
        Assert.That(dataset.Metadata.IsMultiFile, Is.False);
        Assert.That(dataset.Metadata.Images, Has.Length.EqualTo(1));
        Assert.That(dataset.Metadata.Images[0].DimensionOrder, Is.EqualTo(TiffOmeDimensionOrder.XYCZT));
        Assert.That(dataset.Metadata.Images[0].Planes, Has.Length.EqualTo(8));
        Assert.That(dataset.Metadata.Images[0].Planes[5].ChannelIndex, Is.EqualTo(1));
        Assert.That(dataset.Metadata.Images[0].Planes[5].ZIndex, Is.EqualTo(0));
        Assert.That(dataset.Metadata.Images[0].Planes[5].TimeIndex, Is.EqualTo(1));
        Assert.That(dataset.Metadata.Images[0].Planes[5].IfdIndex, Is.EqualTo(5));
        Assert.That(plane.GetIndex(), Is.EqualTo(5));
        Assert.That(read.HasErrors(), Is.False, DiagnosticText(read));
        Assert.That(pixel, Is.EqualTo(new byte[] { 6 }));
    }

    [Test]
    public void OmeMultiFile_SeparatesLocalAndExternalPlaneMappings()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\" UUID=\"urn:uuid:current\"><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYCZT\" Type=\"uint8\" SizeX=\"1\" SizeY=\"1\" SizeC=\"2\" SizeZ=\"1\" SizeT=\"1\"><TiffData IFD=\"0\" FirstC=\"0\" FirstZ=\"0\" FirstT=\"0\" PlaneCount=\"1\"><UUID FileName=\"current.ome.tif\">urn:uuid:current</UUID></TiffData><TiffData IFD=\"0\" FirstC=\"1\" FirstZ=\"0\" FirstT=\"0\" PlaneCount=\"1\"><UUID FileName=\"companion.ome.tif\">urn:uuid:companion</UUID></TiffData></Pixels></Image></OME>";
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(1, 1));
        AssertSuccess(writer.WriteSourceRaster(imageIndex, new byte[] { 91 }));
        TiffOmeMetadata metadata = new TiffOmeMetadata();
        metadata.HasOmeXml = true;
        metadata.Xml = xml;
        AssertSuccess(writer.WriteOmeMetadata(imageIndex, metadata));
        AssertSuccess(writer.Flush());
        TiffFile file = OpenRead(writer.GetFileBytes());

        TiffOmeMetadataReadResult result = file.GetOmeDataset();
        TiffOmePlane local = result.Metadata.Images[0].Planes[0];
        TiffOmePlane external = result.Metadata.Images[0].Planes[1];
        TiffImage localImage = file.GetOmePlaneByCoordinates(0, 0, 0, 0);

        Assert.That(result.Success, Is.True, DiagnosticText(result.Diagnostics));
        Assert.That(result.Metadata.IsMultiFile, Is.True);
        Assert.That(result.Metadata.FileReferences, Has.Length.EqualTo(2));
        Assert.That(local.IsExternal, Is.False);
        Assert.That(local.FileName, Is.EqualTo("current.ome.tif"));
        Assert.That(local.IfdIndex, Is.EqualTo(0));
        Assert.That(external.IsExternal, Is.True);
        Assert.That(external.FileName, Is.EqualTo("companion.ome.tif"));
        Assert.That(external.Uuid, Is.EqualTo("urn:uuid:companion"));
        Assert.That(localImage.ReadEncodedStrip(0).Bytes, Is.EqualTo(new byte[] { 91 }));
    }

    [Test]
    public void ImageJHyperstack_ExposesMetadataAxesAndNavigableSeriesCoordinates()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int index = 0;
        while (index < 8)
        {
            int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(1, 1));
            AssertSuccess(writer.WriteSourceRaster(imageIndex, new byte[] { (byte)(101 + index) }));
            index = index + 1;
        }

        TiffImageJMetadata metadata = new TiffImageJMetadata();
        metadata.HasImageJ = true;
        metadata.IsHyperstack = true;
        metadata.Version = "1.54f";
        metadata.ImageCount = 8;
        metadata.Channels = 2;
        metadata.Slices = 2;
        metadata.Frames = 2;
        metadata.Mode = "composite";
        metadata.Unit = "micron";
        AssertSuccess(writer.WriteImageJMetadata(0, metadata));
        AssertSuccess(writer.Flush());
        TiffFile file = OpenRead(writer.GetFileBytes());

        TiffImageJMetadataReadResult imageJ = file.GetImage(0).ReadImageJMetadata();
        TiffSeriesGroupResult group = file.GetSeriesGroup();
        TiffSeries series = file.GetSeriesByIndex(0);
        TiffImage plane = file.GetSeriesPlaneByCoordinates(0, 1, 0, 1);

        Assert.That(imageJ.Success, Is.True, DiagnosticText(imageJ.Diagnostics));
        Assert.That(imageJ.Metadata.IsHyperstack, Is.True);
        Assert.That(imageJ.Metadata.Axes, Has.Length.EqualTo(3));
        Assert.That(imageJ.Metadata.Calibration.Unit, Is.EqualTo("micron"));
        Assert.That(group.Success, Is.True, DiagnosticText(group.Diagnostics));
        Assert.That(file.GetSeriesCount(), Is.EqualTo(1));
        Assert.That(series.HasMultidimensionalMetadata, Is.True);
        Assert.That(series.SourceMetadata, Is.EqualTo("ImageJ"));
        Assert.That(series.SizeC, Is.EqualTo(2));
        Assert.That(series.SizeZ, Is.EqualTo(2));
        Assert.That(series.SizeT, Is.EqualTo(2));
        Assert.That(series.Planes[5].ChannelIndex, Is.EqualTo(1));
        Assert.That(series.Planes[5].ZIndex, Is.EqualTo(0));
        Assert.That(series.Planes[5].TimeIndex, Is.EqualTo(1));
        Assert.That(plane.GetIndex(), Is.EqualTo(5));
        Assert.That(plane.ReadEncodedStrip(0).Bytes, Is.EqualTo(new byte[] { 106 }));
    }

    [Test]
    public void SparseLargeSlide_PreservesTilesAndNavigatesInternalPyramid()
    {
        TiffWriterImageConfiguration baseConfiguration = CreateTiledConfiguration(4096, 2048, 512, 512);
        baseConfiguration.Layout.AllowSparseSegments = true;
        TiffWriterImageConfiguration overviewConfiguration = CreateTiledConfiguration(2048, 1024, 512, 512);
        overviewConfiguration.Layout.AllowSparseSegments = true;
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(baseConfiguration);
        int overviewIndex = writer.AddPyramidLevel(imageIndex, overviewConfiguration);
        byte[] finalTile = CreateBytes(512 * 512, 37);

        TiffRawTileWriteResult write = writer.WriteEncodedTile(imageIndex, 31, finalTile);
        AssertSuccess(writer.WriteImageDescription(imageIndex, "Aperio Image Library v12|AppMag = 40|MPP = 0.25"));
        AssertSuccess(writer.Flush());
        TiffImage image = OpenRead(writer.GetFileBytes()).GetImage(0);

        TiffVendorMetadataReadResult vendor = image.ReadVendorMetadata();
        TiffPyramidDetectionResult pyramid = image.GetPyramid();
        TiffTileLookupResult storedLookup = image.LookupPyramidTile(0, 4095, 2047, 0, 0);
        TiffRawTileReadResult stored = image.ReadPyramidTile(0, 4095, 2047, 0, 0);
        TiffRawTileReadResult sparse = image.ReadPyramidTile(0, 0, 0, 0, 0);
        TiffRawTileReadResult overview = image.ReadPyramidTile(1, 2047, 1023, 0, 0);

        Assert.That(overviewIndex, Is.EqualTo(0));
        Assert.That(write.Success, Is.True, DiagnosticText(write.Diagnostics));
        Assert.That(vendor.Success, Is.True, DiagnosticText(vendor.Diagnostics));
        Assert.That(vendor.Metadata.Aperio.HasMetadata, Is.True);
        Assert.That(vendor.Metadata.Aperio.IsSlide, Is.True);
        Assert.That(pyramid.Success, Is.True, DiagnosticText(pyramid.Diagnostics));
        Assert.That(image.GetPyramidLevelCount(), Is.EqualTo(2));
        Assert.That(pyramid.Pyramid.Levels[0].Width, Is.EqualTo(2048));
        Assert.That(storedLookup.Lookup.TileIndex, Is.EqualTo(31));
        Assert.That(stored.Bytes, Is.EqualTo(finalTile));
        Assert.That(sparse.IsSparse, Is.True);
        Assert.That(sparse.Bytes, Is.Empty);
        Assert.That(overview.IsSparse, Is.True);
        Assert.That(overview.Bytes, Is.Empty);
    }

    [Test]
    public void VolumetricTiles_ExposeDepthTagsPartialEdgesAndCoordinateLookup()
    {
        TiffWriterImageConfiguration configuration = CreateTiledConfiguration(3, 3, 2, 2);
        configuration.Depth = 3;
        configuration.Layout.TileDepth = 2;
        configuration.Layout.AllowSparseSegments = true;
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        byte[] finalTile = new byte[] { 71, 0, 0, 0, 0, 0, 0, 0 };

        TiffRawTileWriteResult write = writer.WriteEncodedTile(imageIndex, 7, finalTile);
        AssertSuccess(writer.Flush());
        TiffImage image = OpenRead(writer.GetFileBytes()).GetImage(0);
        TiffImageInfo info = image.GetInfo();
        TiffTileLayoutResult layout = image.GetTileLayout();
        TiffTileLookupResult lookup = image.LookupPyramidTile(0, 2, 2, 2, 0);
        TiffRawTileReadResult decoded = image.ReadEncodedTile(7);

        Assert.That(write.Success, Is.True, DiagnosticText(write.Diagnostics));
        Assert.That(info.ImageDepth, Is.EqualTo(3));
        Assert.That(info.TileDepth, Is.EqualTo(2));
        Assert.That(image.GetUnsignedTagScalar(TiffTagId.ImageDepth), Is.EqualTo(3));
        Assert.That(image.GetUnsignedTagScalar(TiffTagId.TileDepth), Is.EqualTo(2));
        Assert.That(layout.Success, Is.True, DiagnosticText(layout.Diagnostics));
        Assert.That(layout.Layout.TilesDeep, Is.EqualTo(2));
        Assert.That(layout.Layout.Segments[7].FirstDepth, Is.EqualTo(2));
        Assert.That(layout.Layout.Segments[7].Depth, Is.EqualTo(1));
        Assert.That(layout.Layout.Segments[7].IsPartial, Is.True);
        Assert.That(lookup.Success, Is.True, DiagnosticText(lookup.Diagnostics));
        Assert.That(lookup.Lookup.TileIndex, Is.EqualTo(7));
        Assert.That(lookup.Lookup.TileDepthIndex, Is.EqualTo(1));
        Assert.That(decoded.Bytes, Is.EqualTo(finalTile));
    }

    [Test]
    public void DngRecognizer_ExposesTagEvidenceAndTypedIdentity()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(1, 1));
        AssertSuccess(writer.WriteSourceRaster(imageIndex, new byte[] { 22 }));
        AssertSuccess(writer.UpdateMetadata(imageIndex, new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedArrayTag(TiffTagId.DngVersion, TiffDataType.Byte, new ulong[] { 1, 6, 0, 0 }),
            TiffApi.Data.CreateAsciiTag(TiffTagId.UniqueCameraModel, "Public API Camera")
        }));
        AssertSuccess(writer.Flush());

        TiffVendorMetadataReadResult result = OpenRead(writer.GetFileBytes()).GetImage(0).ReadVendorMetadata();

        Assert.That(result.Success, Is.True, DiagnosticText(result.Diagnostics));
        Assert.That(result.Metadata.PrimaryVendor, Is.EqualTo("Adobe"));
        Assert.That(result.Metadata.PrimaryFormat, Is.EqualTo("DNG"));
        Assert.That(result.Metadata.Matches[0].Source, Is.EqualTo("TIFF tag"));
        Assert.That(result.Metadata.Dng.HasDng, Is.True);
        Assert.That(result.Metadata.Dng.Version, Is.EqualTo(new int[] { 1, 6, 0, 0 }));
        Assert.That(result.Metadata.Dng.UniqueCameraModel, Is.EqualTo("Public API Camera"));
    }

    [TestCase("Aperio Image Library|MPP = 0.50", "Aperio", "SVS", true, false)]
    [TestCase("Hamamatsu NDP.scan NDPI", "Hamamatsu", "NDPI", true, false)]
    [TestCase("Leica SCN slide scanner", "Leica", "SCN", true, false)]
    [TestCase("Philips PIM_DP_SCANNED_IMAGES", "Philips", "Philips TIFF", true, false)]
    [TestCase("Roche Ventana BIF slide", "Roche/Ventana", "BIF", true, false)]
    [TestCase("Carl Zeiss LSM 980", "Zeiss", "LSM", false, true)]
    [TestCase("Micro-Manager Metadata", "Micro-Manager", "Micro-Manager", false, true)]
    [TestCase("SIMPLE  = T / FITS Astro-TIFF", "Astro-TIFF", "Astro-TIFF", false, true)]
    public void PlannedVendorRecognizer_IdentifiesEveryNamedVariant(string software, string vendor, string format, bool isSlide, bool isScientific)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(CreateGrayConfiguration(1, 1));
        AssertSuccess(writer.WriteSourceRaster(imageIndex, new byte[] { 1 }));
        AssertSuccess(writer.UpdateMetadata(imageIndex, new TiffTagValue[] { TiffApi.Data.CreateAsciiTag(TiffTagId.Software, software) }));
        AssertSuccess(writer.Flush());

        TiffVendorMetadataReadResult result = OpenRead(writer.GetFileBytes()).GetImage(0).ReadVendorMetadata();

        Assert.That(result.Success, Is.True, DiagnosticText(result.Diagnostics));
        Assert.That(result.Metadata.HasVendorMetadata, Is.True);
        Assert.That(result.Metadata.PrimaryVendor, Is.EqualTo(vendor));
        Assert.That(result.Metadata.PrimaryFormat, Is.EqualTo(format));
        Assert.That(result.Metadata.Matches, Has.Length.EqualTo(1));
        Assert.That(result.Metadata.Matches[0].Source, Is.EqualTo("Software"));
        Assert.That(result.Metadata.Matches[0].IsSlide, Is.EqualTo(isSlide));
        Assert.That(result.Metadata.Matches[0].IsScientific, Is.EqualTo(isScientific));
    }

    static TiffWriterImageConfiguration CreateGrayConfiguration(int width, int length)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        return configuration;
    }

    static TiffWriterImageConfiguration CreateTiledConfiguration(int width, int length, int tileWidth, int tileLength)
    {
        TiffWriterImageConfiguration configuration = CreateGrayConfiguration(width, length);
        configuration.Layout.Kind = TiffWriterLayoutKind.Tiles;
        configuration.Layout.TileWidth = tileWidth;
        configuration.Layout.TileLength = tileLength;
        configuration.Layout.TileDepth = 1;
        return configuration;
    }

    static TiffFile OpenRead(byte[] bytes)
    {
        return TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
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
