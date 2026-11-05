namespace avif.Tests;

public class Av1LayerSelectionTests
{
    [Test]
    public void AutomaticAndExplicitOperatingPointSelectionResolveLayerCaps()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence();
        Av1DecoderOptions automaticOptions = new Av1DecoderOptions();
        Av1LayerSelection automatic = Av1LayerSelector.Select(
            sequence, automaticOptions);
        Av1DecoderOptions explicitOptions = new Av1DecoderOptions();
        explicitOptions.OperatingPoint = 1;
        Av1LayerSelection explicitSelection = Av1LayerSelector.Select(
            sequence, explicitOptions);
        Av1DecoderOptions cappedOptions = new Av1DecoderOptions();
        cappedOptions.SpatialLayer = 0;
        cappedOptions.TemporalLayer = 0;
        cappedOptions.AllowProgressive = true;
        cappedOptions.DecodeLargeScaleTiles = true;
        Av1LayerSelection capped = Av1LayerSelector.Select(
            sequence, cappedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(automatic.OperatingPointIndex, Is.Zero);
            Assert.That(automatic.OperatingPointIdc, Is.EqualTo(771));
            Assert.That(automatic.MaximumSpatialLayer, Is.EqualTo(1));
            Assert.That(automatic.MaximumTemporalLayer, Is.EqualTo(1));
            Assert.That(explicitSelection.Code,
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(explicitSelection.OperatingPointIndex, Is.EqualTo(1));
            Assert.That(explicitSelection.MaximumSpatialLayer, Is.Zero);
            Assert.That(explicitSelection.MaximumTemporalLayer, Is.Zero);
            Assert.That(capped.MaximumSpatialLayer, Is.Zero);
            Assert.That(capped.MaximumTemporalLayer, Is.Zero);
            Assert.That(capped.Progressive, Is.True);
            Assert.That(capped.LargeScaleTiles, Is.True);
        });
    }

    [Test]
    public void SpatialTemporalFilteringKeepsGlobalAndSelectedObus()
    {
        Av1DecoderOptions options = new Av1DecoderOptions();
        options.SpatialLayer = 1;
        options.TemporalLayer = 0;
        Av1LayerSelection selection = Av1LayerSelector.Select(
            CreateSequence(), options);
        byte[] source = new byte[]
        {
            0x0A,1,0x80,
            0x1E,0x08,1,0x80,
            0x1E,0x48,1,0x80,
            0x1E,0x28,1,0x80
        };
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            source, new AvifSecurityLimits(), selection.OperatingPointIdc);

        Assert.Multiple(() =>
        {
            Assert.That(Av1LayerSelector.ShouldDecodeObu(
                selection, Header(Av1ObuType.SequenceHeader, false, 0, 0)),
                Is.True);
            Assert.That(Av1LayerSelector.ShouldDecodeObu(
                selection, Header(Av1ObuType.Metadata, false, 0, 0)),
                Is.True);
            Assert.That(Av1LayerSelector.ShouldDecodeObu(
                selection, Header(Av1ObuType.Frame, true, 0, 0)),
                Is.True);
            Assert.That(Av1LayerSelector.ShouldDecodeObu(
                selection, Header(Av1ObuType.TileGroup, true, 0, 1)),
                Is.True);
            Assert.That(Av1LayerSelector.ShouldDecodeObu(
                selection, Header(Av1ObuType.FrameHeader, true, 1, 0)),
                Is.False);
            Assert.That(Av1LayerSelector.ShouldDecodeObu(
                selection, Header(Av1ObuType.Padding, true, 1, 1)),
                Is.False);
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.GetIncludedObusForLayer(selection),
                Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void OutputPolicyChoosesHighestSpatialLayerOrEveryProgressiveLayer()
    {
        Av1LayerFrame[] frames = new Av1LayerFrame[]
        {
            new Av1LayerFrame(0, 0, 0, true),
            new Av1LayerFrame(0, 0, 1, true),
            new Av1LayerFrame(1, 1, 0, true),
            new Av1LayerFrame(1, 1, 1, true),
            new Av1LayerFrame(2, 2, 0, true),
            new Av1LayerFrame(2, 2, 1, true)
        };
        Av1DecoderOptions displayOptions = new Av1DecoderOptions();
        Av1LayerSelection displaySelection = Av1LayerSelector.Select(
            CreateSequence(), displayOptions);
        Av1LayerOutputSelection display = Av1LayerSelector.SelectOutputs(
            displaySelection, frames);
        Av1DecoderOptions progressiveOptions = new Av1DecoderOptions();
        progressiveOptions.AllowProgressive = true;
        Av1LayerSelection progressiveSelection = Av1LayerSelector.Select(
            CreateSequence(), progressiveOptions);
        Av1LayerOutputSelection progressive = Av1LayerSelector.SelectOutputs(
            progressiveSelection, frames);

        Assert.Multiple(() =>
        {
            Assert.That(display.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(display.Decode,
                Is.EqualTo(new bool[] { true,true,true,true,false,false }));
            Assert.That(display.Output,
                Is.EqualTo(new bool[] { false,true,false,true,false,false }));
            Assert.That(progressive.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(progressive.Output,
                Is.EqualTo(new bool[] { true,true,true,true,false,false }));
        });
    }

    [Test]
    public void MissingOperatingPointsAndLayersAreReported()
    {
        Av1DecoderOptions operatingPoint = new Av1DecoderOptions();
        operatingPoint.OperatingPoint = 3;
        Av1DecoderOptions spatial = new Av1DecoderOptions();
        spatial.SpatialLayer = 2;
        Av1DecoderOptions temporal = new Av1DecoderOptions();
        temporal.TemporalLayer = 3;

        Assert.Multiple(() =>
        {
            Assert.That(Av1LayerSelector.Select(
                CreateSequence(), operatingPoint).Code,
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(Av1LayerSelector.Select(
                CreateSequence(), spatial).Code,
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(Av1LayerSelector.Select(
                CreateSequence(), temporal).Code,
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(Av1LayerSelector.SelectOutputs(
                null, System.Array.Empty<Av1LayerFrame>()).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static Av1ObuHeader Header(
        Av1ObuType type,
        bool extension,
        int temporalId,
        int spatialId)
    {
        return new Av1ObuHeader(
            type, extension, true, temporalId, spatialId, extension ? 2 : 1);
    }

    static ParsedAv1SequenceHeader CreateSequence()
    {
        return new ParsedAv1SequenceHeader(
            0,
            false,
            false,
            null,
            null,
            false,
            new Av1OperatingPoint[]
            {
                new Av1OperatingPoint(771, 4, 0),
                new Av1OperatingPoint(257, 4, 0)
            },
            1920,
            1080,
            new Av1SequenceFeatures(),
            false,
            false,
            false,
            true,
            true,
            0,
            1,
            13,
            1,
            false,
            true,
            false,
            false,
            System.Array.Empty<byte>());
    }
}
