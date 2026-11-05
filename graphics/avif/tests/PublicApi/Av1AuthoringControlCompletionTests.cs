namespace avif.Tests;

public class Av1AuthoringControlCompletionTests
{
    [Test]
    public void LowLevelEncoderAuthorsSequenceControlsAndMetadataObu()
    {
        AvifImage image = CreateImage(4, 4, 37);
        Av1SpatialLayerOptions spatial = new Av1SpatialLayerOptions();
        spatial.Width = 4;
        spatial.Height = 4;
        spatial.Quality = 82;
        Av1OperatingPointOptions point = new Av1OperatingPointOptions();
        point.Idc = 259;
        point.Level = 13;
        point.Tier = 0;
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.SequencePolicy = AvifSequenceEncodingPolicy.InterFrame;
        options.FrameLag = 2;
        options.EnableShowExistingFrames = true;
        options.EnableGlobalMotion = false;
        options.EnableWarpedMotion = false;
        options.FilmGrainParametersPresent = true;
        options.TemporalLayerCount = 2;
        options.SpatialLayers = new Av1SpatialLayerOptions[] { spatial };
        options.OperatingPoints = new Av1OperatingPointOptions[] { point };
        options.ProgressiveOutput = true;
        Av1Encoder encoder = new Av1Encoder();
        AvifResultCode configured = encoder.Configure(options);
        spatial.Width = 1;
        point.Idc = 0;
        Av1EncoderOptions snapshot = encoder.GetOptions()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifResultCode frameCode = encoder.AddFrame(image, 100);
        AvifMetadataObu metadata = new AvifMetadataObu(
            73, new byte[] { 1, 2, 3 });
        AvifResultCode metadataCode = encoder.AddMetadataObu(metadata);
        AvifEncodeResult encoded = encoder.Finish();
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            encoded.GetData(), new AvifSecurityLimits(), 0);
        Av1SequenceHeaderParseResult header =
            Av1SequenceHeaderParser.ParsePayload(parsed.Obus[0].GetPayload());
        Av1MetadataParseResult parsedMetadata = Av1MetadataParser.ParseObu(
            parsed.Obus[parsed.Obus.Count - 1]);

        Assert.Multiple(() =>
        {
            Assert.That(configured, Is.EqualTo(AvifResultCode.Success));
            Assert.That(snapshot.FrameLag, Is.EqualTo(2));
            Assert.That(snapshot.EnableGlobalMotion, Is.False);
            Assert.That(snapshot.SpatialLayers[0].Width, Is.EqualTo(4));
            Assert.That(snapshot.OperatingPoints[0].Idc, Is.EqualTo(259));
            Assert.That(frameCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadataCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(header.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((header.Header
                ?? throw new InvalidOperationException("Invalid internal state."))
                .IsStillPicture(), Is.False);
            Assert.That(header.Header.HasReducedStillPictureHeader(), Is.False);
            Assert.That(header.Header.HasFilmGrainParameters(), Is.True);
            Assert.That(header.Header.GetOperatingPointCount(), Is.EqualTo(1));
            Assert.That((header.Header.GetOperatingPoint(0)
                ?? throw new InvalidOperationException("Invalid internal state."))
                .Idc, Is.EqualTo(259));
            Assert.That(parsedMetadata.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsedMetadata.Metadata
                ?? throw new InvalidOperationException("Invalid internal state."))
                .MetadataType, Is.EqualTo(73));
            Assert.That(parsedMetadata.Metadata.OpaquePayload,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        encoder.Close();
        image.Close();
    }

    [Test]
    public void FrameControlsValidateAndSnapshotFilmGrainAndQuality()
    {
        AvifImage image = CreateImage(4, 4, 41);
        AvifFrame frame = new AvifFrame(image, null, 100, false);
        AvifFilmGrain grain = new AvifFilmGrain();
        grain.GrainSeed = 1234;
        grain.YPoints = new int[][]
        {
            new int[] { 10, 20 },
            new int[] { 200, 80 }
        };
        grain.ArCoefficientsY = new int[] { -1, 2 };

        AvifResultCode show = frame.SetShowExistingFrame(3);
        AvifResultCode refresh = frame.SetRefreshFrameFlags(0);
        AvifResultCode layers = frame.SetLayerIds(2, 1);
        AvifResultCode signature = frame.SetContentSignature(9988);
        AvifResultCode film = frame.SetFilmGrain(grain);
        AvifResultCode quality = frame.SetEncodingQuality(71, 96);
        AvifResultCode motion = frame.SetMotionTools(false, true);
        grain.YPoints[0][0] = 99;
        grain.ArCoefficientsY[0] = 99;
        AvifFilmGrain snapshot = frame.GetFilmGrain()
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(show, Is.EqualTo(AvifResultCode.Success));
            Assert.That(refresh, Is.EqualTo(AvifResultCode.Success));
            Assert.That(layers, Is.EqualTo(AvifResultCode.Success));
            Assert.That(signature, Is.EqualTo(AvifResultCode.Success));
            Assert.That(film, Is.EqualTo(AvifResultCode.Success));
            Assert.That(quality, Is.EqualTo(AvifResultCode.Success));
            Assert.That(motion, Is.EqualTo(AvifResultCode.Success));
            Assert.That(frame.GetShowExistingFrameSlot(), Is.EqualTo(3));
            Assert.That(frame.GetRefreshFrameFlags(), Is.Zero);
            Assert.That(frame.GetTemporalId(), Is.EqualTo(2));
            Assert.That(frame.GetSpatialId(), Is.EqualTo(1));
            Assert.That(frame.GetContentSignature(), Is.EqualTo(9988));
            Assert.That(frame.GetColorQuality(), Is.EqualTo(71));
            Assert.That(frame.GetAlphaQuality(), Is.EqualTo(96));
            Assert.That(frame.IsGlobalMotionEnabled(), Is.False);
            Assert.That(frame.IsWarpedMotionEnabled(), Is.True);
            Assert.That(snapshot.YPoints[0][0], Is.EqualTo(10));
            Assert.That(snapshot.ArCoefficientsY[0], Is.EqualTo(-1));
            Assert.That(frame.SetShowExistingFrame(8),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(frame.SetRefreshFrameFlags(256),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(frame.SetLayerIds(8, 0),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(frame.SetEncodingQuality(101, 0),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        image.Close();
    }

    [Test]
    public void SequencePlanExposesLagShowExistingAndRefreshDecisions()
    {
        AvifImage firstImage = CreateImage(4, 4, 11);
        AvifImage secondImage = CreateImage(4, 4, 22);
        AvifImage thirdImage = CreateImage(4, 4, 33);
        AvifFrame first = new AvifFrame(firstImage, null, 100, true);
        AvifFrame second = new AvifFrame(secondImage, null, 100, false);
        AvifFrame third = new AvifFrame(thirdImage, null, 100, false);
        second.SetShowExistingFrame(3);
        third.SetRefreshFrameFlags(4);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(first);
        sequence.AddFrame(second);
        sequence.AddFrame(third);
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.SequencePolicy = AvifSequenceEncodingPolicy.InterFrame;
        options.FrameLag = 1;
        AvifSequenceEncodingPlan plan = sequence.PlanEncoding(options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetPresentationIndexes(plan.Frames),
                Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(GetCompositionOffsets(plan.Frames),
                Is.EqualTo(new long[] { 0, 100, -100 }));
            Assert.That(plan.Frames[0].EncodeIndex, Is.Zero);
            Assert.That(plan.Frames[1].EncodeIndex, Is.EqualTo(1));
            Assert.That(plan.Frames[2].EncodeIndex, Is.EqualTo(2));
            Assert.That(plan.Frames[0].RefreshFrameFlags, Is.EqualTo(255));
            Assert.That(plan.Frames[1].RefreshFrameFlags, Is.EqualTo(4));
            Assert.That(plan.Frames[2].ShowExistingFrame, Is.True);
            Assert.That(plan.Frames[2].ShowExistingSlot, Is.EqualTo(3));
            Assert.That(plan.Frames[2].RefreshFrameFlags, Is.Zero);
        });

        firstImage.Close();
        secondImage.Close();
        thirdImage.Close();
    }

    [Test]
    public void WriterAcceptsControlledInterSequenceAndCopiesIndependentQualities()
    {
        Av1SpatialLayerOptions baseLayer = new Av1SpatialLayerOptions();
        baseLayer.Width = 2;
        baseLayer.Height = 2;
        baseLayer.Quality = 60;
        Av1SpatialLayerOptions fullLayer = new Av1SpatialLayerOptions();
        fullLayer.Width = 4;
        fullLayer.Height = 4;
        fullLayer.Quality = 90;
        AvifEncoderOptions options = new AvifEncoderOptions();
        options.SequencePolicy = AvifSequenceEncodingPolicy.InterFrame;
        options.FrameLag = 1;
        options.FilmGrainParametersPresent = true;
        options.TemporalLayerCount = 2;
        options.SpatialLayers = new Av1SpatialLayerOptions[]
        {
            baseLayer, fullLayer
        };
        options.Quality = 70;
        options.AlphaQuality = 100;
        options.AuxiliaryQuality = 65;
        options.GainMapQuality = 75;
        options.GridCellQuality = 80;
        options.LayerQuality = 85;
        AvifImage firstImage = CreateImage(4, 4, 11);
        AvifImage secondImage = CreateImage(4, 4, 22);
        AvifFrame first = new AvifFrame(firstImage, null, 100, true);
        first.SetRefreshFrameFlags(255);
        AvifFrame second = new AvifFrame(secondImage, null, 100, false);
        second.SetLayerIds(1, 1);
        second.SetShowExistingFrame(0);
        second.SetEncodingQuality(55, -1);
        AvifFilmGrain grain = new AvifFilmGrain();
        grain.GrainSeed = 9;
        second.SetFilmGrain(grain);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(first);
        sequence.AddFrame(second);
        AvifWriter writer = new AvifWriter(options);
        baseLayer.Width = 1;
        uint trackId = writer.AddSequence(sequence);
        AvifEncoderOptions snapshot = writer.GetOptions()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifEncodeResult encoded = writer.Finish();
        AvifReadResult read = Avif.Read(encoded.GetData());
        AvifMovie movie = (read.GetDocument()
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetMovie()
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(trackId, Is.EqualTo(1));
            Assert.That(snapshot.FrameLag, Is.EqualTo(1));
            Assert.That(snapshot.Quality, Is.EqualTo(70));
            Assert.That(snapshot.AlphaQuality, Is.EqualTo(100));
            Assert.That(snapshot.AuxiliaryQuality, Is.EqualTo(65));
            Assert.That(snapshot.GainMapQuality, Is.EqualTo(75));
            Assert.That(snapshot.GridCellQuality, Is.EqualTo(80));
            Assert.That(snapshot.LayerQuality, Is.EqualTo(85));
            Assert.That(snapshot.SpatialLayers[0].Width, Is.EqualTo(2));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(movie.FindTrack(trackId), Is.Not.Null);
            Assert.That((movie.FindTrack(trackId)
                ?? throw new InvalidOperationException("Invalid internal state."))
                .GetSampleCount(), Is.EqualTo(2));
        });

        writer.Close();
        firstImage.Close();
        secondImage.Close();
    }

    [Test]
    public void LayerPlannerAndQualityOverridesHonorPublicControls()
    {
        Av1EncoderOptions options = new Av1EncoderOptions();
        options.EnableGlobalMotion = false;
        options.EnableWarpedMotion = false;
        options.TemporalLayerCount = 3;
        options.ProgressiveOutput = true;
        Av1SpatialLayerOptions small = new Av1SpatialLayerOptions();
        small.Width = 32;
        small.Height = 32;
        Av1SpatialLayerOptions large = new Av1SpatialLayerOptions();
        large.Width = 64;
        large.Height = 64;
        options.SpatialLayers = new Av1SpatialLayerOptions[] { small, large };
        Av1EncoderPresetDecision preset = Av1EncoderPresetResolver.Resolve(options);
        Av1EncoderTileLayerPlanRequest request =
            new Av1EncoderTileLayerPlanRequest();
        request.Options = options;
        request.Width = 64;
        request.Height = 64;
        request.FrameCount = 4;
        Av1EncoderTileLayerPlan plan = Av1EncoderTileLayerPlanner.Plan(request);
        AvifImage image = CreateImage(2, 2, 1);
        AvifLayer layer = new AvifLayer(image);
        AvifAuxiliaryImage auxiliary = new AvifAuxiliaryImage(
            image, AvifAuxiliaryKind.Depth, false);

        Assert.Multiple(() =>
        {
            Assert.That(preset.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(preset.EnableGlobalMotion, Is.False);
            Assert.That(preset.EnableWarpedMotion, Is.False);
            Assert.That(plan.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(plan.OperatingPoints.Length, Is.EqualTo(2));
            Assert.That(plan.LayerFrames.Length, Is.EqualTo(8));
            Assert.That(plan.LayerFrames[0].Output, Is.True);
            Assert.That(plan.LayerFrames[2].TemporalId, Is.EqualTo(2));
            Assert.That(layer.SetQuality(44), Is.EqualTo(AvifResultCode.Success));
            Assert.That(layer.GetQuality(), Is.EqualTo(44));
            Assert.That(layer.SetQuality(101),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(auxiliary.SetQuality(33),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(auxiliary.GetQuality(), Is.EqualTo(33));
        });

        image.Close();
    }

    static AvifImage CreateImage(
        int width,
        int height,
        byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane plane = image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Invalid internal state.");
        int row = 0;
        while (row < plane.GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static int[] GetPresentationIndexes(
        AvifFrameEncodingDecision[] frames)
    {
        int[] result = new int[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            result[index] = frames[index].PresentationIndex;
            index++;
        }
        return result;
    }

    static long[] GetCompositionOffsets(
        AvifFrameEncodingDecision[] frames)
    {
        long[] result = new long[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            result[index] = frames[index].CompositionOffset;
            index++;
        }
        return result;
    }
}
