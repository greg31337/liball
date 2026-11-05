using System.Collections.Generic;

namespace jpeg.Tests;

internal class PublicApiCoverageCompletionTests
{
    [Test]
    public void IndependentCorpus_AllRemainingReadAndInspectionFormsAgree()
    {
        byte[] data = IndependentJpegCorpus.CreateNeutralGrayscale(0xC0, 8);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
        global::jpeg.JpegIdentifierService identifier = new global::jpeg.JpegIdentifierService();
        global::jpeg.JpegInspectionService inspector = new global::jpeg.JpegInspectionService();
        global::jpeg.JpegReader reader = new global::jpeg.JpegReader();
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, data);

        try
        {
            Assert.That(
                identifier.IdentifyStreamWithOptions(new System.IO.MemoryStream(data), options).Width,
                Is.EqualTo(8));

            Assert.That(inspector.ReadMarkers(data), Has.Length.GreaterThan(1));
            Assert.That(inspector.ReadMarkersStream(new System.IO.MemoryStream(data)), Has.Length.GreaterThan(1));
            Assert.That(inspector.ReadMarkersFile(path), Has.Length.GreaterThan(1));
            AssertInspection(inspector.InspectMarkersWithCancellation(data, cancellationToken));
            AssertInspection(inspector.InspectMarkersWithOptionsAndCancellation(data, options, cancellationToken));
            AssertInspection(inspector.InspectMarkersSpan(data));
            AssertInspection(inspector.InspectMarkersSpanWithCancellation(data, cancellationToken));
            AssertInspection(inspector.InspectMarkersSpanWithOptionsAndCancellation(data, options, cancellationToken));
            AssertInspection(inspector.InspectMarkersStreamWithCancellation(new System.IO.MemoryStream(data), cancellationToken));
            AssertInspection(inspector.InspectMarkersStreamWithOptionsAndCancellation(new System.IO.MemoryStream(data), options, cancellationToken));
            AssertInspection(inspector.InspectMarkersFile(path));
            AssertInspection(inspector.InspectMarkersFileWithCancellation(path, cancellationToken));
            AssertInspection(inspector.InspectMarkersFileWithOptionsAndCancellation(path, options, cancellationToken));

            Assert.That(inspector.ReadCoefficientsWithOptions(data, options).Width, Is.EqualTo(8));
            Assert.That(inspector.ReadCoefficientsStreamWithOptions(new System.IO.MemoryStream(data), options).Width, Is.EqualTo(8));
            Assert.That(inspector.ReadCoefficientsFileWithOptions(path, options).Width, Is.EqualTo(8));

            Assert.That(reader.ReadFileWithCancellation(path, cancellationToken).Pixels, Is.All.EqualTo(128));
            Assert.That(reader.ReadStream(new System.IO.MemoryStream(data)).Pixels, Is.All.EqualTo(128));
            Assert.That(reader.ReadStreamWithOptions(new System.IO.MemoryStream(data), options).Pixels, Is.All.EqualTo(128));
            Assert.That(reader.ReadStreamWithCancellation(new System.IO.MemoryStream(data), cancellationToken).Pixels, Is.All.EqualTo(128));
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Test]
    public void IndependentCorpus_MetadataServiceSettersCreateParseableMarkers()
    {
        byte[] data = IndependentJpegCorpus.CreateNeutralGrayscale(0xC0, 8);
        global::jpeg.JpegMetadataEditor editor = new global::jpeg.JpegMetadataEditor();
        byte[] exif = ExifMetadataParserTests.CreateExifPayload(
            new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        byte[] xmp = XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<corpus />"));

        data = editor.SetIccProfile(data, new byte[] { 9, 8, 7 });
        data = editor.SetExif(data, exif);
        data = editor.SetXmp(data, xmp);
        global::jpeg.JpegInfo info = new global::jpeg.JpegIdentifierService().Identify(data);

        Assert.Multiple(
            delegate
            {
                Assert.That(info.Metadata.HasIccProfile, Is.True);
                Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 9, 8, 7 }));
                Assert.That(info.Metadata.HasExif, Is.True);
                Assert.That(info.Metadata.Exif, Is.EqualTo(exif));
                Assert.That(info.Metadata.HasXmp, Is.True);
                Assert.That(info.Metadata.Xmp, Is.EqualTo(xmp));
            });
    }

    [Test]
    public void PublicWriter_RemainingFileAndStreamFormsProduceIndependentValidStreams()
    {
        global::jpeg.JpegImage source = StandardsInteroperabilityCorpusTests.CreateSource(8);
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        global::jpeg.JpegWriter writer = new global::jpeg.JpegWriter();
        System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
        string path = JpegFilePathApiTests.CreateTempJpegPath();

        try
        {
            writer.WriteFileWithCancellation(source, path, cancellationToken);
            Assert.That(IndependentJpegCorpus.ValidateStructure(System.IO.File.ReadAllBytes(path)).SawEndOfImage, Is.True);

            System.IO.MemoryStream defaultStream = new System.IO.MemoryStream();
            writer.WriteToStream(source, defaultStream);
            Assert.That(IndependentJpegCorpus.ValidateStructure(defaultStream.ToArray()).SawEndOfImage, Is.True);

            System.IO.MemoryStream optionsStream = new System.IO.MemoryStream();
            writer.WriteToStreamWithOptions(source, optionsStream, options);
            Assert.That(IndependentJpegCorpus.ValidateStructure(optionsStream.ToArray()).SawEndOfImage, Is.True);

            System.IO.MemoryStream cancellationStream = new System.IO.MemoryStream();
            writer.WriteToStreamWithCancellation(source, cancellationStream, cancellationToken);
            Assert.That(IndependentJpegCorpus.ValidateStructure(cancellationStream.ToArray()).SawEndOfImage, Is.True);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Test]
    public void IndependentCorpus_RemainingSimpleTransformsPreserveDecodability()
    {
        byte[] data = IndependentJpegCorpus.CreateNeutralGrayscale(0xC0, 8);
        global::jpeg.JpegTransformer transformer = new global::jpeg.JpegTransformer();
        List<byte[]> results = new List<byte[]>();
        results.Add(transformer.Rotate90(data));
        results.Add(transformer.Rotate270(data));
        results.Add(transformer.FlipHorizontal(data));
        results.Add(transformer.FlipVertical(data));
        results.Add(transformer.Transpose(data));
        results.Add(transformer.Transverse(data));
        results.Add(transformer.Crop(data, 0, 0, 8, 8));

        int index = 0;
        while (index < results.Count)
        {
            global::jpeg.JpegImage decoded = new global::jpeg.JpegReader().Read(results[index]);
            Assert.That(decoded.Pixels, Is.All.EqualTo(128));
            index = index + 1;
        }
    }

    [Test]
    public void TransformerStreamForms_RejectNullInputAndOutput()
    {
        byte[] data = IndependentJpegCorpus.CreateNeutralGrayscale(0xC0, 8);
        global::jpeg.JpegTransformer transformer = new global::jpeg.JpegTransformer();
        global::jpeg.JpegTransformOptions options = new global::jpeg.JpegTransformOptions();
        System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                transformer.Rotate90StreamWithOptionsAndCancellation(
                    null!,
                    new System.IO.MemoryStream(),
                    options,
                    cancellationToken);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                transformer.Rotate90StreamWithOptionsAndCancellation(
                    new System.IO.MemoryStream(data),
                    null!,
                    options,
                    cancellationToken);
            });
    }

    public static void AssertInspection(global::jpeg.JpegInspectionResult result)
    {
        Assert.That(result.Info.Width, Is.EqualTo(8));
        Assert.That(result.Info.Height, Is.EqualTo(8));
        Assert.That(result.Markers, Has.Count.GreaterThan(1));
    }
}
