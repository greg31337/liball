using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ReaderOptionsTests
{
    [Test]
    public void DefaultOptions_EnableMetadataParsingWithBoundedLimits()
    {
        ImageMetadataReaderOptions options = new ImageMetadataReaderOptions();

        Assert.That(options.MaxFileBytes, Is.EqualTo(536870912));
        Assert.That(options.MaxSegmentBytes, Is.EqualTo(67108864));
        Assert.That(options.MaxRawBlockBytes, Is.EqualTo(16777216));
        Assert.That(options.MaxProfileBytes, Is.EqualTo(33554432));
        Assert.That(options.MaxXmlBytes, Is.EqualTo(16777216));
        Assert.That(options.MaxDecompressedBytes, Is.EqualTo(33554432));
        Assert.That(options.MaxIfdCount, Is.EqualTo(4096));
        Assert.That(options.MaxNestingDepth, Is.EqualTo(64));
        Assert.That(options.MaxTagCount, Is.EqualTo(65536));
        Assert.That(options.MaxFrameCount, Is.EqualTo(10000));
        Assert.That(options.CaptureRawBlocks, Is.True);
        Assert.That(options.CaptureRawBlockData, Is.False);
        Assert.That(options.ParseXmp, Is.True);
        Assert.That(options.ParseIcc, Is.True);
        Assert.That(options.PreserveMakerNotes, Is.True);
        Assert.That(options.DecodeMakerNotes, Is.False);
        Assert.That(options.DiagnosticsVerbosity, Is.EqualTo(MetadataDiagnosticsVerbosity.Normal));
    }

    [Test]
    public void ConservativeOptions_LowerRiskyResourceLimits()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataReaderOptions defaults = factory.CreateDefaultReaderOptions();
        ImageMetadataReaderOptions conservative = factory.CreateConservativeReaderOptions();

        Assert.That(conservative.MaxFileBytes, Is.LessThan(defaults.MaxFileBytes));
        Assert.That(conservative.MaxSegmentBytes, Is.LessThan(defaults.MaxSegmentBytes));
        Assert.That(conservative.MaxRawBlockBytes, Is.LessThan(defaults.MaxRawBlockBytes));
        Assert.That(conservative.MaxProfileBytes, Is.LessThan(defaults.MaxProfileBytes));
        Assert.That(conservative.MaxXmlBytes, Is.LessThan(defaults.MaxXmlBytes));
        Assert.That(conservative.MaxDecompressedBytes, Is.LessThan(defaults.MaxDecompressedBytes));
        Assert.That(conservative.MaxIfdCount, Is.LessThan(defaults.MaxIfdCount));
        Assert.That(conservative.MaxNestingDepth, Is.LessThan(defaults.MaxNestingDepth));
        Assert.That(conservative.MaxTagCount, Is.LessThan(defaults.MaxTagCount));
        Assert.That(conservative.MaxFrameCount, Is.LessThan(defaults.MaxFrameCount));
        Assert.That(conservative.ParseXmp, Is.True);
        Assert.That(conservative.ParseIcc, Is.True);
        Assert.That(conservative.PreserveMakerNotes, Is.True);
        Assert.That(conservative.DecodeMakerNotes, Is.False);
    }

    [Test]
    public void Options_ArePlainMutableFieldsForParserCallSites()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        ImageMetadataReaderOptions options = factory.CreateMetadataOnlyReaderOptions();

        options.MaxFileBytes = 10;
        options.CaptureRawBlocks = false;
        options.ParseXmp = false;
        options.ParseIcc = false;
        options.PreserveMakerNotes = false;
        options.DecodeMakerNotes = true;
        options.DiagnosticsVerbosity = MetadataDiagnosticsVerbosity.Verbose;

        Assert.That(options.MaxFileBytes, Is.EqualTo(10));
        Assert.That(options.CaptureRawBlocks, Is.False);
        Assert.That(options.ParseXmp, Is.False);
        Assert.That(options.ParseIcc, Is.False);
        Assert.That(options.PreserveMakerNotes, Is.False);
        Assert.That(options.DecodeMakerNotes, Is.True);
        Assert.That(options.DiagnosticsVerbosity, Is.EqualTo(MetadataDiagnosticsVerbosity.Verbose));
    }
}
