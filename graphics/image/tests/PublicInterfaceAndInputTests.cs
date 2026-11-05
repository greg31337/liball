using System;
using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicInterfaceAndInputTests
{
    [Test]
    public void EveryPublicImplementation_IsUsableThroughItsContract()
    {
        IImageFormatDetector detector = new ImageFormatDetector();
        IImageMetadataDiff diff = new ImageMetadataDiff();
        IImageMetadataFactory factory = new ImageMetadataFactory();
        IImageMetadataFormatter formatter = new ImageMetadataFormatter();
        IImageMetadataHash hash = new ImageMetadataHash();
        IImageMetadataNormalizer normalizer = new ImageMetadataNormalizer();
        IImageMetadataQuery query = new ImageMetadataQuery();
        IImageMetadataReader reader = new ImageMetadataReader();
        IImageMetadataReader compatibilityReader = new DefaultImageMetadataReader();
        IImageMetadataScanner scanner = new ImageMetadataScanner();
        IMetadataDateParser dateParser = new MetadataDateParser();

        Assert.That(detector, Is.InstanceOf<ImageFormatDetector>());
        Assert.That(diff, Is.InstanceOf<ImageMetadataDiff>());
        Assert.That(factory, Is.InstanceOf<ImageMetadataFactory>());
        Assert.That(formatter, Is.InstanceOf<ImageMetadataFormatter>());
        Assert.That(hash, Is.InstanceOf<ImageMetadataHash>());
        Assert.That(normalizer, Is.InstanceOf<ImageMetadataNormalizer>());
        Assert.That(query, Is.InstanceOf<ImageMetadataQuery>());
        Assert.That(reader, Is.InstanceOf<ImageMetadataReader>());
        Assert.That(compatibilityReader, Is.InstanceOf<DefaultImageMetadataReader>());
        Assert.That(scanner, Is.InstanceOf<ImageMetadataScanner>());
        Assert.That(dateParser, Is.InstanceOf<MetadataDateParser>());
    }

    [Test]
    public void NullableInputs_ReturnDocumentedEmptyOrDiagnosticResultsThroughContracts()
    {
        IImageFormatDetector detector = new ImageFormatDetector();
        IImageMetadataDiff diff = new ImageMetadataDiff();
        IImageMetadataFactory factory = new ImageMetadataFactory();
        IImageMetadataFormatter formatter = new ImageMetadataFormatter();
        IImageMetadataHash hash = new ImageMetadataHash();
        IImageMetadataNormalizer normalizer = new ImageMetadataNormalizer();
        IImageMetadataScanner scanner = new ImageMetadataScanner();
        IMetadataDateParser dateParser = new MetadataDateParser();

        ImageFormatInfo nullBytes = detector.DetectBytes(null);
        ImageFormatInfo nullBytesWithHint = detector.DetectBytesWithHint(null, null);
        ImageFormatInfo nullStream = detector.DetectStream(null);
        ImageFormatInfo nullStreamWithHint = detector.DetectStreamWithHint(null, null);
        ImageMetadataDiffResult nullDiff = diff.Compare(null, null);
        GpsCoordinate inferredGps = factory.CreateGpsCoordinate(1, 0, 0, null, 2, 0, 0, null, 0, null);
        ImageMetadataHashResult nullBytesHash = hash.ComputeBytes(null, null, null);
        ImageMetadataHashResult nullStreamHash = hash.ComputeStream(null, null, null);
        ImageMetadataHashResult emptySpanHash = hash.ComputeSpan(ReadOnlySpan<byte>.Empty, null, null);
        ImageMetadataSummary nullSummary = normalizer.BuildSummary(null);
        ImageMetadataScanResult nullScan = scanner.ScanDirectory(null, null);
        MetadataDate nullDate = dateParser.Parse(null);

        Assert.That(nullBytes.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(nullBytes.Diagnostics[0].Code, Is.EqualTo("format.unknown"));
        Assert.That(nullBytesWithHint.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(nullStream.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(nullStreamWithHint.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(nullDiff.AreEqual, Is.True);
        Assert.That(nullDiff.Differences, Is.Empty);
        Assert.That(inferredGps.LatitudeReference, Is.EqualTo("N"));
        Assert.That(inferredGps.LongitudeReference, Is.EqualTo("E"));
        Assert.That(inferredGps.AltitudeReference, Is.Empty);
        Assert.That(formatter.ToText(null), Is.Empty);
        Assert.That(formatter.ToJson(null), Is.EqualTo("{}"));
        Assert.That(formatter.ToGpsText(null), Is.Empty);
        Assert.That(hash.ComputeSha256(null, null), Is.Empty);
        Assert.That(hash.ComputeHash(null, null, null), Is.Empty);
        Assert.That(nullBytesHash.Status, Is.EqualTo(ImageMetadataHashStatus.InvalidInput));
        Assert.That(nullStreamHash.Status, Is.EqualTo(ImageMetadataHashStatus.InvalidInput));
        Assert.That(emptySpanHash.Status, Is.EqualTo(ImageMetadataHashStatus.InvalidInput));
        Assert.That(nullSummary.Width, Is.Zero);
        Assert.That(nullSummary.Keywords, Is.Empty);
        Assert.That(nullScan.Files, Is.Empty);
        Assert.That(nullScan.Diagnostics[0].Code, Is.EqualTo("scanner.directory.empty"));
        Assert.That(nullDate.IsValid, Is.False);
        Assert.That(nullDate.OriginalText, Is.Empty);
    }

    [Test]
    public void QueryContract_ReturnsEmptyResultsForEveryNullDocumentOperation()
    {
        IImageMetadataQuery query = new ImageMetadataQuery();

        Assert.That(query.GetTags(null), Is.Empty);
        Assert.That(query.Find(null, "tag"), Is.Empty);
        Assert.That(query.FindFirst(null, "tag"), Is.Null);
        Assert.That(query.GetTagsForFrame(null, 0), Is.Empty);
        Assert.That(query.FindTagsForFrame(null, 0, "tag"), Is.Empty);
        Assert.That(query.GetDirectories(null), Is.Empty);
        Assert.That(query.FindDirectoriesByFamily(null, MetadataFamily.Exif), Is.Empty);
        Assert.That(query.FindDirectoriesByPath(null, "path"), Is.Empty);
        Assert.That(query.GetDirectoriesForFrame(null, 0), Is.Empty);
        Assert.That(query.GetProfiles(null), Is.Empty);
        Assert.That(query.FindProfilesByFamily(null, MetadataFamily.Exif), Is.Empty);
        Assert.That(query.FindProfilesByKind(null, MetadataProfileKind.Exif), Is.Empty);
        Assert.That(query.FindProfilesByKey(null, "key"), Is.Empty);
        Assert.That(query.FindProfilesForFrame(null, 0), Is.Empty);
        Assert.That(query.FindProfilesByRole(null, ImageMetadataRole.Preview), Is.Empty);
        Assert.That(query.FindRoleProfilesForFrame(null, 0, ImageMetadataRole.Preview), Is.Empty);
        Assert.That(query.GetRawBlocks(null), Is.Empty);
        Assert.That(query.FindRawBlocksByFamily(null, MetadataFamily.Exif), Is.Empty);
        Assert.That(query.FindRawBlocksByType(null, "APP1"), Is.Empty);
        Assert.That(query.FindFramesByRole(null, ImageMetadataRole.Thumbnail), Is.Empty);
        Assert.That(query.FindRelatedFrames(null, 0, ImageMetadataRole.Preview), Is.Empty);
    }

    [Test]
    public void InvalidInputs_ReturnStableFailureResultsThroughContracts()
    {
        IImageFormatDetector detector = new ImageFormatDetector();
        IImageMetadataFormatter formatter = new ImageMetadataFormatter();
        IImageMetadataHash hash = new ImageMetadataHash();
        IImageMetadataQuery query = new ImageMetadataQuery();
        IImageMetadataScanner scanner = new ImageMetadataScanner();
        IMetadataDateParser dateParser = new MetadataDateParser();
        ImageMetadataDocument document = new ImageMetadataDocument();
        string missingDirectory = Path.Combine(Path.GetTempPath(), "image-api-missing-" + Guid.NewGuid().ToString("N"));

        ImageFormatInfo unknown = detector.DetectBytes(new byte[] { 1, 2, 3, 4 });
        ImageMetadataHashResult unsupportedAlgorithm = hash.ComputeBytes(new byte[] { 1 }, document, new ImageMetadataHashOptions { AlgorithmName = "CRC32" });
        ImageMetadataScanResult missingScan = scanner.ScanDirectory(missingDirectory, new ImageMetadataScanOptions());
        MetadataDate invalidDate = dateParser.Parse("not a date");

        Assert.That(unknown.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(unknown.Diagnostics[0].Code, Is.EqualTo("format.unknown"));
        Assert.That(formatter.ToGpsText(new GpsCoordinate()), Is.Empty);
        Assert.That(unsupportedAlgorithm.Status, Is.EqualTo(ImageMetadataHashStatus.UnsupportedAlgorithm));
        Assert.That(query.Find(document, null), Is.Empty);
        Assert.That(query.FindFirst(document, ""), Is.Null);
        Assert.That(query.GetTagsForFrame(document, -1), Is.Empty);
        Assert.That(query.FindTagsForFrame(document, -1, null), Is.Empty);
        Assert.That(query.FindDirectoriesByPath(document, null), Is.Empty);
        Assert.That(query.GetDirectoriesForFrame(document, -1), Is.Empty);
        Assert.That(query.FindProfilesByKey(document, null), Is.Empty);
        Assert.That(query.FindProfilesForFrame(document, -1), Is.Empty);
        Assert.That(query.FindRoleProfilesForFrame(document, -1, ImageMetadataRole.Preview), Is.Empty);
        Assert.That(query.FindRawBlocksByType(document, null), Is.Empty);
        Assert.That(query.FindRelatedFrames(document, -1, ImageMetadataRole.Preview), Is.Empty);
        Assert.That(missingScan.Files, Is.Empty);
        Assert.That(missingScan.Diagnostics[0].Code, Is.EqualTo("scanner.directory.not-found"));
        Assert.That(invalidDate.IsValid, Is.False);
        Assert.That(invalidDate.OriginalText, Is.EqualTo("not a date"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BothReaderImplementations_AgreeAcrossEveryEntryPoint(bool compatibilityName)
    {
        IImageMetadataReader reader = CreateReader(compatibilityName);
        byte[] qoi = QoiMetadataParserTests.CreateQoi(3, 2, 3, 0, new byte[] { 254, 1, 2, 3 });
        string path = Path.Combine(Path.GetTempPath(), "image-reader-api-" + Guid.NewGuid().ToString("N") + ".qoi");
        File.WriteAllBytes(path, qoi);

        try
        {
            ImageMetadataDocument fileDefault = reader.ReadFile(path);
            ImageMetadataDocument fileOptions = reader.ReadFileWithOptions(path, new ImageMetadataReaderOptions());
            MemoryStream defaultStream = new MemoryStream(qoi);
            MemoryStream optionStream = new MemoryStream(qoi);
            ImageMetadataDocument streamDefault = reader.ReadStream(defaultStream);
            ImageMetadataDocument streamOptions = reader.ReadStreamWithOptions(optionStream, null);
            ImageMetadataDocument nullFile = reader.ReadFile(null);
            ImageMetadataDocument emptyFile = reader.ReadFileWithOptions("", null);
            ImageMetadataDocument nullStream = reader.ReadStream(null);
            ImageMetadataDocument nullStreamWithOptions = reader.ReadStreamWithOptions(null, new ImageMetadataReaderOptions());

            AssertQoi(fileDefault);
            AssertQoi(fileOptions);
            AssertQoi(streamDefault);
            AssertQoi(streamOptions);
            Assert.That(defaultStream.Position, Is.Zero);
            Assert.That(optionStream.Position, Is.Zero);
            Assert.That(nullFile.Diagnostics[0].Code, Is.EqualTo("reader.path.empty"));
            Assert.That(emptyFile.Diagnostics[0].Code, Is.EqualTo("reader.path.empty"));
            Assert.That(nullStream.Diagnostics[0].Code, Is.EqualTo("reader.stream.null"));
            Assert.That(nullStreamWithOptions.Diagnostics[0].Code, Is.EqualTo("reader.stream.null"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static IImageMetadataReader CreateReader(bool compatibilityName)
    {
        if (compatibilityName)
        {
            return new DefaultImageMetadataReader();
        }

        return new ImageMetadataReader();
    }

    public static void AssertQoi(ImageMetadataDocument document)
    {
        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
        Assert.That(document.Summary.Width, Is.EqualTo(3));
        Assert.That(document.Summary.Height, Is.EqualTo(2));
        Assert.That(document.Frames, Has.Count.EqualTo(1));
        Assert.That(document.Frames[0].Width, Is.EqualTo(3));
        Assert.That(document.Frames[0].Height, Is.EqualTo(2));
    }
}
