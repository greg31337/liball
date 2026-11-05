namespace avif.Tests;

public class HdrAuthoringCompletionTests
{
    [Test]
    public void AuthorsCompleteHdrViewingCameraAndAv1Metadata()
    {
        int[] masteringX = new int[] { 34000, 13250, 7500 };
        int[] masteringY = new int[] { 16000, 34500, 3000 };
        int[] colorX = new int[] { -10, 20, 30 };
        int[] colorY = new int[] { 40, 50, 60 };
        byte[] hdr10Plus = new byte[] { 170, 0, 187 };
        AvifHdrMetadata authored = new AvifHdrMetadata();
        AvifCameraIntrinsicMatrix intrinsic = new AvifCameraIntrinsicMatrix(
            true, 3, 5, 400, 200, 100, 800, -8);
        AvifCameraExtrinsicMatrix extrinsic = new AvifCameraExtrinsicMatrix(
            true, false, true, true, false, true,
            11, 0, -22, 100, -200, 300, 42);

        AvifResultCode lightCode = authored.SetContentLightLevel(1000, 400);
        AvifResultCode masteringCode = authored.SetMasteringDisplay(
            masteringX, masteringY, 15635, 16450, 10000000, 50);
        AvifResultCode colorCode = authored.SetContentColorVolume(
            colorX,
            colorY,
            1,
            1000,
            250,
            AvifContentColorVolumeFields.MinimumLuminance
                | AvifContentColorVolumeFields.MaximumLuminance
                | AvifContentColorVolumeFields.AverageLuminance);
        AvifResultCode ambientCode = authored.SetAmbientViewingEnvironment(
            100000, 15635, 16450);
        AvifResultCode referenceCode = authored.SetReferenceViewingEnvironment(
            50000, 3000, 3100, 25000, 3100, 3200);
        AvifResultCode diffuseCode = authored.SetNominalDiffuseWhite(2030000);
        AvifResultCode intrinsicCode = authored.SetCameraIntrinsicMatrix(intrinsic);
        AvifResultCode extrinsicCode = authored.SetCameraExtrinsicMatrix(extrinsic);
        AvifResultCode hdr10Code = authored.SetHdr10PlusPayload(hdr10Plus);

        masteringX[0] = 0;
        colorX[0] = 0;
        hdr10Plus[0] = 0;
        AvifImage image = CreateImage();
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        AvifResultCode setCode = writer.SetHdrMetadata(itemId, authored);
        authored.SetContentLightLevel(1, 1);
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = AvifReader.Create(encoded.GetData());
        AvifResultCode parseCode = reader.Parse();
        AvifImageItem item = (reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifHdrMetadata parsed = item.GetHdrMetadata();
        AvifPayloadExtraction clli = reader.ExtractItemMetadataObu(
            itemId, 0, 65536);
        AvifPayloadExtraction mdcv = reader.ExtractItemMetadataObu(
            itemId, 1, 65536);
        AvifPayloadExtraction itu = reader.ExtractItemMetadataObu(
            itemId, 2, 65536);
        AvifResultCode clliMerge = MergeExtractedMetadata(parsed, clli);
        AvifResultCode mdcvMerge = MergeExtractedMetadata(parsed, mdcv);
        AvifResultCode ituMerge = MergeExtractedMetadata(parsed, itu);
        AvifCameraIntrinsicMatrix parsedIntrinsic = parsed.GetCameraIntrinsicMatrix()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifCameraExtrinsicMatrix parsedExtrinsic = parsed.GetCameraExtrinsicMatrix()
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(lightCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(masteringCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(colorCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ambientCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(referenceCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(diffuseCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(intrinsic.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(extrinsic.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(intrinsicCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(extrinsicCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(hdr10Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(setCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parseCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.HasCameraIntrinsicMatrix(), Is.True);
            Assert.That(parsed.HasCameraExtrinsicMatrix(), Is.True);
            Assert.That(GetPropertyTypes(item), Does.Contain("clli"));
            Assert.That(GetPropertyTypes(item), Does.Contain("mdcv"));
            Assert.That(GetPropertyTypes(item), Does.Contain("cclv"));
            Assert.That(GetPropertyTypes(item), Does.Contain("amve"));
            Assert.That(GetPropertyTypes(item), Does.Contain("reve"));
            Assert.That(GetPropertyTypes(item), Does.Contain("ndwt"));
            Assert.That(GetPropertyTypes(item), Does.Contain("cmin"));
            Assert.That(GetPropertyTypes(item), Does.Contain("cmex"));
            Assert.That((parsed.GetContentLightLevel()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetMaximumContentLightLevel(), Is.EqualTo(1000));
            Assert.That((parsed.GetMasteringDisplay()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPrimaryChromaticityX(0), Is.EqualTo(34000));
            Assert.That((parsed.GetContentColorVolume()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPrimaryChromaticityX(0), Is.EqualTo(-10));
            Assert.That((parsed.GetAmbientViewingEnvironment()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetAmbientIlluminance(), Is.EqualTo(100000));
            Assert.That((parsed.GetReferenceViewingEnvironment()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetPeripheryLightY(), Is.EqualTo(3200));
            Assert.That(parsed.GetNominalDiffuseWhiteLuminance(),
                Is.EqualTo(2030000));
            Assert.That(parsedIntrinsic.GetDenominatorShift(), Is.EqualTo(3));
            Assert.That(parsedIntrinsic.GetFocalLengthX(), Is.EqualTo(400));
            Assert.That(parsedIntrinsic.GetFocalLengthY(), Is.EqualTo(800));
            Assert.That(parsedExtrinsic.HasPositionY(), Is.False);
            Assert.That(parsedExtrinsic.HasPositionZ(), Is.True);
            Assert.That(parsedExtrinsic.GetPositionZ(), Is.EqualTo(-22));
            Assert.That(parsedExtrinsic.GetQuaternionYRaw(), Is.EqualTo(-200));
            Assert.That(parsedExtrinsic.GetQuaternionZRaw(), Is.EqualTo(300));
            Assert.That(parsedExtrinsic.GetWorldCoordinateSystemId(), Is.EqualTo(42));
            Assert.That(clli.GetMetadataType(), Is.EqualTo(1));
            Assert.That(mdcv.GetMetadataType(), Is.EqualTo(2));
            Assert.That(itu.GetMetadataType(), Is.EqualTo(4));
            Assert.That(clliMerge, Is.EqualTo(AvifResultCode.Success));
            Assert.That(mdcvMerge, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ituMerge, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.HasHdr10PlusPayload(), Is.True);
            Assert.That(parsed.GetHdr10PlusPayload(),
                Is.EqualTo(new byte[] { 170, 0, 187 }));
        });

        reader.Close();
        writer.Close();
        image.Close();
    }

    [Test]
    public void InvalidAuthoringValuesAreRejectedWithoutReplacingValidValues()
    {
        AvifHdrMetadata metadata = new AvifHdrMetadata();
        metadata.SetAmbientViewingEnvironment(1, 100, 200);
        AvifCameraIntrinsicMatrix badIntrinsic = new AvifCameraIntrinsicMatrix(
            false, 32, 0, 1, 2, 3, 0, 0);
        AvifCameraExtrinsicMatrix badExtrinsic = new AvifCameraExtrinsicMatrix(
            false, false, false, true, false, false,
            0, 0, 0, 16384, 1, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.SetContentColorVolume(
                    null, null, 0, 0, 0, AvifContentColorVolumeFields.None),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetContentColorVolume(
                    new int[] { 5000001, 0, 0 },
                    new int[] { 0, 0, 0 },
                    0, 0, 0, AvifContentColorVolumeFields.None),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetAmbientViewingEnvironment(0, 0, 0),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That((metadata.GetAmbientViewingEnvironment()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetAmbientIlluminance(), Is.EqualTo(1));
            Assert.That(metadata.SetReferenceViewingEnvironment(
                    0, 10001, 0, 0, 0, 0),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(badIntrinsic.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetCameraIntrinsicMatrix(badIntrinsic),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(badExtrinsic.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetCameraExtrinsicMatrix(badExtrinsic),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetItuT35Payload(
                    254, 1, new byte[] { 9 }),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetItuT35Payload(255, 256, new byte[] { 9 }),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(metadata.SetHdr10PlusPayload(null),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [Test]
    public void ReplacingHdrMetadataRemovesEarlierContainerAndAv1Values()
    {
        AvifHdrMetadata first = new AvifHdrMetadata();
        first.SetContentLightLevel(1000, 400);
        first.SetHdr10PlusPayload(new byte[] { 1, 2, 3 });
        AvifHdrMetadata second = new AvifHdrMetadata();
        second.SetContentLightLevel(2000, 500);
        second.SetItuT35Payload(255, 42, new byte[] { 9, 8 });
        AvifImage image = CreateImage();
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddImage(image);
        AvifResultCode firstCode = writer.SetHdrMetadata(itemId, first);
        AvifResultCode secondCode = writer.SetHdrMetadata(itemId, second);
        AvifEncodeResult encoded = writer.Finish();
        AvifReader reader = AvifReader.Create(encoded.GetData());
        reader.Parse();
        AvifImageItem item = (reader.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .FindItem(itemId)
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifPayloadExtraction light = reader.ExtractItemMetadataObu(
            itemId, 0, 65536);
        AvifPayloadExtraction itu = reader.ExtractItemMetadataObu(
            itemId, 1, 65536);
        AvifPayloadExtraction absent = reader.ExtractItemMetadataObu(
            itemId, 2, 65536);
        AvifHdrMetadata parsed = item.GetHdrMetadata();
        AvifResultCode lightMerge = MergeExtractedMetadata(parsed, light);
        AvifResultCode ituMerge = MergeExtractedMetadata(parsed, itu);

        Assert.Multiple(() =>
        {
            Assert.That(firstCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.GetContentLightLevel()
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetMaximumContentLightLevel(), Is.EqualTo(2000));
            Assert.That(light.GetMetadataType(), Is.EqualTo(1));
            Assert.That(itu.GetMetadataType(), Is.EqualTo(4));
            Assert.That(absent.GetCode(), Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(lightMerge, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ituMerge, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.GetItuT35CountryCode(), Is.EqualTo(255));
            Assert.That(parsed.GetItuT35CountryCodeExtension(), Is.EqualTo(42));
            Assert.That(parsed.GetItuT35Payload(),
                Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(parsed.HasHdr10PlusPayload(), Is.False);
        });

        reader.Close();
        writer.Close();
        image.Close();
    }

    static AvifResultCode MergeExtractedMetadata(
        AvifHdrMetadata metadata,
        AvifPayloadExtraction extraction)
    {
        Av1ObuReader reader = new Av1ObuReader(
            extraction.GetData(),
            new AvifSecurityLimits());
        AvifResultCode readCode = reader.ReadNext();
        AvifResultCode result = readCode == AvifResultCode.Success
            ? metadata.AddAv1MetadataObuPayload(reader.GetMetadata()) : readCode;
        reader.Close();
        return result;
    }

    static AvifImage CreateImage()
    {
        AvifImage image = AvifImage.Create(
            4,
            4,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane plane = image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Invalid internal state.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            plane.GetRowSpan(row).Fill(47);
            row++;
        }
        return image;
    }

    static List<string> GetPropertyTypes(AvifImageItem item)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            result.Add((item.GetProperty(index)
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetTypeName());
            index++;
        }
        return result;
    }
}
