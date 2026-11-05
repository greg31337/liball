using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffOmeDatasetTests
{
    [Test]
    public void ReadOmeMetadata_ExpandsDimensionOrderAndMultiFilePlaneMappings()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\" UUID=\"urn:uuid:current\"><Image ID=\"Image:0\" Name=\"volume\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYZCT\" Type=\"uint8\" SizeX=\"16\" SizeY=\"8\" SizeC=\"2\" SizeZ=\"2\" SizeT=\"1\"><TiffData IFD=\"0\" FirstC=\"0\" FirstZ=\"0\" FirstT=\"0\" PlaneCount=\"2\"><UUID FileName=\"current.ome.tif\">urn:uuid:current</UUID></TiffData><TiffData IFD=\"0\" FirstC=\"1\" FirstZ=\"0\" FirstT=\"0\" PlaneCount=\"2\"><UUID FileName=\"other.ome.tif\">urn:uuid:other</UUID></TiffData></Pixels></Image></OME>";
        TiffDirectory directory = CreateImageDescriptionDirectory(xml, 64, out byte[] fileBytes);

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.IsPartialMetadata, Is.False);
        Assert.That(result.Metadata.IsMultiFile, Is.True);
        Assert.That(result.Metadata.Uuid, Is.EqualTo("urn:uuid:current"));
        Assert.That(result.Metadata.Images, Has.Length.EqualTo(1));
        Assert.That(result.Metadata.Images[0].DimensionOrder, Is.EqualTo(TiffOmeDimensionOrder.XYZCT));
        Assert.That(result.Metadata.Images[0].SizeC, Is.EqualTo(2));
        Assert.That(result.Metadata.Images[0].SizeZ, Is.EqualTo(2));
        Assert.That(result.Metadata.Images[0].Planes, Has.Length.EqualTo(4));
        Assert.That(result.Metadata.Images[0].Planes[1].ChannelIndex, Is.EqualTo(0));
        Assert.That(result.Metadata.Images[0].Planes[1].ZIndex, Is.EqualTo(1));
        Assert.That(result.Metadata.Images[0].Planes[2].ChannelIndex, Is.EqualTo(1));
        Assert.That(result.Metadata.Images[0].Planes[2].ZIndex, Is.EqualTo(0));
        Assert.That(result.Metadata.Images[0].Planes[0].IsExternal, Is.False);
        Assert.That(result.Metadata.Images[0].Planes[2].IsExternal, Is.True);
        Assert.That(result.Metadata.Images[0].Planes[2].FileName, Is.EqualTo("other.ome.tif"));
        Assert.That(result.Metadata.FileReferences, Has.Length.EqualTo(2));
    }

    [Test]
    public void ReadOmeMetadata_ExposesBinaryOnlyPartialMetadataReference()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\" UUID=\"urn:uuid:partial\"><BinaryOnly MetadataFile=\"dataset.companion.ome\" UUID=\"urn:uuid:master\" /></OME>";
        TiffDirectory directory = CreateImageDescriptionDirectory(xml, 64, out byte[] fileBytes);

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.IsPartialMetadata, Is.True);
        Assert.That(result.Metadata.IsMultiFile, Is.True);
        Assert.That(result.Metadata.PartialMetadataFileName, Is.EqualTo("dataset.companion.ome"));
        Assert.That(result.Metadata.PartialMetadataUuid, Is.EqualTo("urn:uuid:master"));
        Assert.That(result.Metadata.Images, Is.Empty);
        Assert.That(result.Metadata.FileReferences, Has.Length.EqualTo(1));
        Assert.That(result.Metadata.FileReferences[0].IsCurrentFile, Is.False);
    }

    [Test]
    public void ReadOmeMetadata_DecodesUtf8ImageDescription()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><Image ID=\"Image:0\" Name=\"café\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYZCT\" Type=\"uint8\" SizeX=\"1\" SizeY=\"1\" SizeC=\"1\" SizeZ=\"1\" SizeT=\"1\"><TiffData /></Pixels></Image></OME>";
        byte[] encoded = Encoding.UTF8.GetBytes(xml + "\0");
        TiffDirectory directory = CreateRawImageDescriptionDirectory(encoded, 64, out byte[] fileBytes);

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.Images[0].Name, Is.EqualTo("café"));
        Assert.That(result.Metadata.ImageDescriptionRawBytes, Is.EqualTo(encoded));
    }

    [Test]
    public void ReadOmeMetadata_AllowsWarningCommentBeforeOmeRoot()
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><!-- Warning: this is an OME-XML metadata block. --><OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><BinaryOnly MetadataFile=\"dataset.companion.ome\" UUID=\"urn:uuid:master\" /></OME>";
        TiffDirectory directory = CreateImageDescriptionDirectory(xml, 64, out byte[] fileBytes);

        TiffOmeMetadataReadResult result = TiffCodec.ReadOmeMetadata(directory, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata.HasOmeXml, Is.True);
        Assert.That(result.Metadata.Xml, Is.EqualTo(xml));
    }

    [Test]
    public void File_NavigatesSingleFileOmePlanesByCoordinates()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYCZT\" Type=\"uint8\" SizeX=\"1\" SizeY=\"1\" SizeC=\"2\" SizeZ=\"2\" SizeT=\"1\"><TiffData /></Pixels></Image></OME>";
        TiffWriter writer = TiffWriter.Create();
        int index = 0;
        while (index < 4)
        {
            string description = "";
            if (index == 0)
            {
                description = xml;
            }

            int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 1, false, description)));
            writer.WriteRawStrip(imageIndex, 0, new byte[] { (byte)(index + 1) });
            index = index + 1;
        }

        TiffDiagnostics flushDiagnostics = writer.Flush();
        ITiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        TiffOmeMetadataReadResult dataset = file.GetOmeDataset();
        TiffImage plane = file.GetOmePlaneByCoordinates(0, 1, 1, 0);

        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(dataset.Success, Is.True);
        Assert.That(dataset.Metadata.IsMultiFile, Is.False);
        Assert.That(dataset.Metadata.Images[0].Planes[3].IfdIndex, Is.EqualTo(3));
        Assert.That(plane.GetIndex(), Is.EqualTo(3));
        Assert.That(plane.ReadEncodedStrip(0).Bytes, Is.EqualTo(new byte[] { 4 }));
    }

    [Test]
    public void File_RejectsExternalOmePlaneAsALocalImage()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\" UUID=\"urn:uuid:current\"><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYZCT\" Type=\"uint8\" SizeX=\"1\" SizeY=\"1\" SizeC=\"1\" SizeZ=\"1\" SizeT=\"1\"><TiffData IFD=\"0\"><UUID FileName=\"other.ome.tif\">urn:uuid:other</UUID></TiffData></Pixels></Image></OME>";
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(1, 1, false, xml)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 });
        writer.Flush();
        ITiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        TiffImage plane = file.GetOmePlaneByCoordinates(0, 0, 0, 0);

        Assert.That(plane.GetSuccess(), Is.False);
        Assert.That(file.GetSuccess(), Is.False);
        Assert.That(file.GetDiagnostics().ContainsCode(TiffErrorCode.UnsupportedStream), Is.True);
    }

    [Test]
    public void File_ExposesOmeSubIfdPyramidRelationships()
    {
        string xml = "<OME xmlns=\"http://www.openmicroscopy.org/Schemas/OME/2016-06\"><Image ID=\"Image:0\"><Pixels ID=\"Pixels:0\" DimensionOrder=\"XYZCT\" Type=\"uint8\" SizeX=\"4\" SizeY=\"4\" SizeC=\"1\" SizeZ=\"1\" SizeT=\"1\"><TiffData IFD=\"0\" PlaneCount=\"1\" /></Pixels></Image></OME>";
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(4, 4, false, xml)));
        writer.AddSubImage(imageIndex, TiffImageDefinition.Create(CreateStripTags(2, 2, true, "")));
        writer.WriteRawStrip(imageIndex, 0, CreateBytes(16, 7));
        TiffDiagnostics flushDiagnostics = writer.Flush();
        ITiffFile file = TiffFile.OpenRead(writer.GetFileBytes());

        TiffOmeMetadataReadResult dataset = file.GetOmeDataset();

        Assert.That(flushDiagnostics.Count(), Is.EqualTo(0));
        Assert.That(dataset.Success, Is.True);
        Assert.That(dataset.Metadata.PyramidLevels, Has.Length.EqualTo(2));
        Assert.That(dataset.Metadata.PyramidLevels[0].BaseIfdIndex, Is.EqualTo(0));
        Assert.That(dataset.Metadata.PyramidLevels[0].LevelIndex, Is.EqualTo(0));
        Assert.That(dataset.Metadata.PyramidLevels[0].Width, Is.EqualTo(4));
        Assert.That(dataset.Metadata.PyramidLevels[1].LevelIndex, Is.EqualTo(1));
        Assert.That(dataset.Metadata.PyramidLevels[1].Width, Is.EqualTo(2));
    }

    static TiffDirectory CreateImageDescriptionDirectory(string text, int offset, out byte[] fileBytes)
    {
        byte[] encoded = TiffAscii.EncodeString(text);
        return CreateRawImageDescriptionDirectory(encoded, offset, out fileBytes);
    }

    static TiffDirectory CreateRawImageDescriptionDirectory(byte[] encoded, int offset, out byte[] fileBytes)
    {
        fileBytes = new byte[offset + encoded.Length + 16];
        WriteBytes(fileBytes, offset, encoded);
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.ImageDescription;
        entry.DataType = TiffDataType.Ascii;
        entry.Count = encoded.Length;
        entry.RawCount = (ulong)encoded.Length;
        entry.ValueOffset = (ulong)offset;
        entry.ValueOffsetBytes = new byte[4];
        TiffBinaryWriter.WriteUInt32(entry.ValueOffsetBytes, (uint)offset, TiffByteOrder.LittleEndian);
        entry.ValueByteCount = encoded.Length;
        entry.ValueSlotByteCount = 4;
        return TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { entry });
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length, bool reducedResolution, string description)
    {
        List<TiffTagValue> tags = new List<TiffTagValue>();
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)TiffPlanarConfiguration.Contiguous));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length));
        if (reducedResolution)
        {
            tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.NewSubfileType, TiffDataType.Long, 1));
        }

        if (description.Length > 0)
        {
            tags.Add(TiffTagValue.CreateAscii(TiffTagId.ImageDescription, description));
        }

        return tags.ToArray();
    }

    static void WriteBytes(byte[] destination, int offset, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
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
}
