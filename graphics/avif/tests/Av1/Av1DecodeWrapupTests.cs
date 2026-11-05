namespace avif.Tests;

public class Av1DecodeWrapupTests
{
    [Test]
    public void IncompleteTileSetDoesNotUpdateReferencesOrTakeOwnership()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 0);
        ParsedAv1FrameHeader header = CreateHeader(
            Av1FrameType.Key, 0, 255, true, false, true);
        (header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns = 2;
        header.Tiles.MiColumnStarts = new int[] { 0,1,2 };
        PlaneBuffer plane = CreatePlane(8, 8);
        Av1DecodeWrapupRequest request = CreateRequest(
            sequence, header, plane, new bool[] { true,false });

        AvifResultCode code = Av1DecodeWrapup.Complete(manager, request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(manager.GetPendingOutputCount(), Is.Zero);
            Assert.That(manager.GetReferenceFrame(0), Is.Null);
            Assert.That(plane.Closed, Is.False);
        });
        plane.Close();
        manager.Close();
    }

    [Test]
    public void HiddenKeyShowExistingAndShownInterStayInDecodeOrder()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 4);
        ParsedAv1FrameHeader hiddenKey = CreateHeader(
            Av1FrameType.Key, 2, 255, false, true, false);
        hiddenKey.CurrentFrameId = 9;
        AvifResultCode hiddenCode = Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, hiddenKey, CreatePlane(8, 8),
                new bool[] { true }));

        ParsedAv1FrameHeader showExisting = CreateShowExisting(
            Av1FrameType.Key, 0, 255, 9);
        AvifResultCode existingCode = Av1DecodeWrapup.Complete(
            manager, CreateExistingRequest(sequence, showExisting));

        ParsedAv1FrameHeader shownInter = CreateHeader(
            Av1FrameType.Inter, 3, 1, true, true, false);
        FillReferences(shownInter, 0);
        AvifResultCode interCode = Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, shownInter, CreatePlane(8, 8),
                new bool[] { true }));
        Av1OutputFrame? first = manager.TakeOutput();
        Av1OutputFrame? second = manager.TakeOutput();

        Assert.Multiple(() =>
        {
            Assert.That(hiddenCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(existingCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(interCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That((first ?? throw new InvalidOperationException("Invalid internal state.")).OutputIndex, Is.Zero);
            Assert.That(first.FromShowExisting, Is.True);
            Assert.That((first.Frame ?? throw new InvalidOperationException("Invalid internal state.")).FrameType, Is.EqualTo(Av1FrameType.Key));
            Assert.That((second ?? throw new InvalidOperationException("Invalid internal state.")).OutputIndex, Is.EqualTo(1));
            Assert.That(second.FromShowExisting, Is.False);
            Assert.That((second.Frame ?? throw new InvalidOperationException("Invalid internal state.")).FrameType, Is.EqualTo(Av1FrameType.Inter));
            Assert.That(manager.GetPendingOutputCount(), Is.Zero);
        });
        manager.ReleaseOutput(first);
        manager.ReleaseOutput(second);
        manager.Close();
    }

    [Test]
    public void ErrorResilientFrameRejectsAnInvalidatedReference()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 4);
        ParsedAv1FrameHeader seed = CreateHeader(
            Av1FrameType.Key, 1, 255, false, true, false);
        Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, seed, CreatePlane(8, 8),
                new bool[] { true }));
        (manager.HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceValid[3] = false;

        ParsedAv1FrameHeader header = CreateHeader(
            Av1FrameType.Inter, 2, 1, true, true, true);
        header.PrimaryReferenceFrame = 7;
        FillReferences(header, 3);
        PlaneBuffer plane = CreatePlane(8, 8);
        AvifResultCode code = Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, header, plane, new bool[] { true }));

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(manager.GetPendingOutputCount(), Is.Zero);
            Assert.That(manager.GetReferenceFrame(3), Is.Null);
            Assert.That(plane.Closed, Is.False);
        });
        plane.Close();
        manager.Close();
    }

    [Test]
    public void ResetFramesRequireErrorResilienceAndFullRefresh()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 0);
        ParsedAv1FrameHeader key = CreateHeader(
            Av1FrameType.Key, 0, 255, true, false, false);
        PlaneBuffer keyPlane = CreatePlane(8, 8);
        AvifResultCode keyCode = Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, key, keyPlane, new bool[] { true }));

        ParsedAv1FrameHeader switchFrame = CreateHeader(
            Av1FrameType.Switch, 0, 127, true, true, true);
        PlaneBuffer switchPlane = CreatePlane(8, 8);
        AvifResultCode switchCode = Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, switchFrame, switchPlane,
                new bool[] { true }));

        Assert.Multiple(() =>
        {
            Assert.That(keyCode, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(switchCode, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(keyPlane.Closed, Is.False);
            Assert.That(switchPlane.Closed, Is.False);
        });
        keyPlane.Close();
        switchPlane.Close();
        manager.Close();
    }

    [Test]
    public void MotionFieldIsStoredBeforeReferenceRefresh()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 4);
        ParsedAv1FrameHeader seed = CreateHeader(
            Av1FrameType.Key, 2, 255, false, true, false);
        Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, seed, CreatePlane(8, 8),
                new bool[] { true }));

        ParsedAv1FrameHeader current = CreateHeader(
            Av1FrameType.Inter, 5, 1 << 4, false, true, false);
        FillReferences(current, 0);
        Av1BlockMotionField motion = new Av1BlockMotionField(2, 2);
        motion.Set(1, 1, 0, Av1ReferenceFrame.Last, -9, 13);
        Av1DecodeWrapupRequest request = CreateRequest(
            sequence, current, CreatePlane(8, 8), new bool[] { true });
        request.MotionField = motion;

        AvifResultCode code = Av1DecodeWrapup.Complete(manager, request);
        Av1DecodedFrame? stored = manager.GetReferenceFrame(4);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((stored ?? throw new InvalidOperationException("Invalid internal state.")).MotionField ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceFrame(1, 1),
                Is.EqualTo(Av1ReferenceFrame.Last));
            Assert.That((stored.MotionField ?? throw new InvalidOperationException("Invalid internal state.")).GetMotionVectorRow(1, 1),
                Is.EqualTo(-9));
            Assert.That(stored.MotionField.GetMotionVectorColumn(1, 1),
                Is.EqualTo(13));
            Assert.That(stored.ReferenceOrderHints, Is.All.EqualTo(2));
        });
        manager.Close();
    }

    [Test]
    public void SegmentationMapIsInheritedFromPrimaryReference()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 0);
        ParsedAv1FrameHeader seed = CreateHeader(
            Av1FrameType.Key, 0, 255, false, true, false);
        (seed.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled = true;
        seed.Segmentation.UpdateMap = true;
        int[] seedIds = new int[] { 1,2,3,4 };
        Av1DecodeWrapupRequest seedRequest = CreateRequest(
            sequence, seed, CreatePlane(8, 8), new bool[] { true });
        seedRequest.SegmentIds = seedIds;
        Av1DecodeWrapup.Complete(manager, seedRequest);

        ParsedAv1FrameHeader current = CreateHeader(
            Av1FrameType.Inter, 0, 1 << 1, false, true, false);
        FillReferences(current, 0);
        current.PrimaryReferenceFrame = 0;
        (current.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled = true;
        current.Segmentation.UpdateMap = false;
        Av1DecodeWrapupRequest currentRequest = CreateRequest(
            sequence, current, CreatePlane(8, 8), new bool[] { true });
        currentRequest.SegmentIds = new int[] { 7,7,7,7 };

        AvifResultCode code = Av1DecodeWrapup.Complete(manager, currentRequest);
        Av1DecodedFrame? stored = manager.GetReferenceFrame(1);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((stored ?? throw new InvalidOperationException("Invalid internal state.")).SegmentIds, Is.EqualTo(seedIds));
            Assert.That((manager.HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceSegmentIds[1],
                Is.EqualTo(seedIds));
        });
        manager.Close();
    }

    [Test]
    public void ShowingStoredKeyReloadsSavedFrameState()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(false, 0);
        ParsedAv1FrameHeader seed = CreateHeader(
            Av1FrameType.Key, 0, 255, false, true, false);
        seed.CurrentFrameId = 17;
        ((seed.GlobalMotion ?? throw new InvalidOperationException("Invalid internal state.")).Parameters ?? throw new InvalidOperationException("Invalid internal state."))[1][0] = 123;
        (seed.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled = true;
        seed.Segmentation.UpdateMap = true;
        (seed.Segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[2][0] = true;
        (seed.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[2][0] = 5;
        ((seed.LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceDeltas ?? throw new InvalidOperationException("Invalid internal state."))[0] = 7;
        Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, seed, CreatePlane(8, 8),
                new bool[] { true }));
        (manager.HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).PreviousGlobalMotionParameters[1][0] = 999;
        manager.HeaderState.SegmentFeatureData[2][0] = 99;
        manager.HeaderState.LoopFilterReferenceDeltas[0] = -3;

        ParsedAv1FrameHeader existing = CreateShowExisting(
            Av1FrameType.Key, 0, 255, 17);
        AvifResultCode code = Av1DecodeWrapup.Complete(
            manager, CreateExistingRequest(sequence, existing));

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(manager.HeaderState.PreviousGlobalMotionParameters[1][0],
                Is.EqualTo(123));
            Assert.That(manager.HeaderState.SegmentFeatureEnabled[2][0], Is.True);
            Assert.That(manager.HeaderState.SegmentFeatureData[2][0], Is.EqualTo(5));
            Assert.That(manager.HeaderState.LoopFilterReferenceDeltas[0],
                Is.EqualTo(7));
        });
        Av1OutputFrame? output = manager.TakeOutput();
        manager.ReleaseOutput(output);
        manager.Close();
    }

    [Test]
    public void ShowExistingRejectsMismatchedDisplayFrameId()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(true, 0);
        ParsedAv1FrameHeader seed = CreateHeader(
            Av1FrameType.Key, 0, 255, false, true, false);
        seed.CurrentFrameId = 6;
        Av1DecodeWrapup.Complete(
            manager,
            CreateRequest(sequence, seed, CreatePlane(8, 8),
                new bool[] { true }));
        ParsedAv1FrameHeader existing = CreateShowExisting(
            Av1FrameType.Key, 0, 255, 5);

        AvifResultCode code = Av1DecodeWrapup.Complete(
            manager, CreateExistingRequest(sequence, existing));

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(manager.GetPendingOutputCount(), Is.Zero);
        });
        manager.Close();
    }

    static Av1DecodeWrapupRequest CreateRequest(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        PlaneBuffer plane,
        bool[] completedTiles)
    {
        Av1DecodeWrapupRequest request = new Av1DecodeWrapupRequest();
        request.Sequence = sequence;
        request.Header = header;
        request.Planes = new PlaneBuffer[] { plane };
        request.CompletedTiles = completedTiles;
        request.SegmentIds = new int[(header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows * header.Size.MiColumns];
        return request;
    }

    static Av1DecodeWrapupRequest CreateExistingRequest(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header)
    {
        Av1DecodeWrapupRequest request = new Av1DecodeWrapupRequest();
        request.Sequence = sequence;
        request.Header = header;
        return request;
    }

    static ParsedAv1FrameHeader CreateHeader(
        Av1FrameType frameType,
        int orderHint,
        int refreshFlags,
        bool showFrame,
        bool showableFrame,
        bool errorResilient)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.FrameType = frameType;
        header.FrameIsIntra = frameType == Av1FrameType.Key
            || frameType == Av1FrameType.IntraOnly;
        header.OrderHint = orderHint;
        header.RefreshFrameFlags = refreshFlags;
        header.ShowFrame = showFrame;
        header.ShowableFrame = showableFrame;
        header.ErrorResilientMode = errorResilient;
        header.PrimaryReferenceFrame = 7;
        header.Size = new Av1FrameSize();
        header.Size.UpscaledWidth = 8;
        header.Size.FrameWidth = 8;
        header.Size.FrameHeight = 8;
        header.Size.RenderWidth = 8;
        header.Size.RenderHeight = 8;
        header.Size.MiColumns = 2;
        header.Size.MiRows = 2;
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = 1;
        header.Tiles.TileRows = 1;
        header.Tiles.MiColumnStarts = new int[] { 0,2 };
        header.Tiles.MiRowStarts = new int[] { 0,2 };
        header.GlobalMotion = CreateGlobalMotion();
        header.Segmentation = CreateSegmentation();
        header.LoopFilter = CreateLoopFilter();
        header.FilmGrain = new Av1FilmGrainParameters();
        return header;
    }

    static ParsedAv1FrameHeader CreateShowExisting(
        Av1FrameType frameType,
        int slot,
        int refreshFlags,
        int displayFrameId)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.ShowExistingFrame = true;
        header.ShowFrame = true;
        header.FrameType = frameType;
        header.FrameIsIntra = frameType == Av1FrameType.Key
            || frameType == Av1FrameType.IntraOnly;
        header.FrameToShowMapIndex = slot;
        header.RefreshFrameFlags = refreshFlags;
        header.DisplayFrameId = displayFrameId;
        return header;
    }

    static void FillReferences(ParsedAv1FrameHeader header, int slot)
    {
        int index = 0;
        while (index < header.ReferenceFrameIndexes.Length)
        {
            header.ReferenceFrameIndexes[index] = slot;
            index++;
        }
    }

    static Av1GlobalMotionParameters CreateGlobalMotion()
    {
        Av1GlobalMotionParameters global = new Av1GlobalMotionParameters();
        global.Types = new Av1WarpModelType[Av1SyntaxConstants.TotalRefsPerFrame];
        global.Parameters = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        int index = 0;
        while (index < global.Parameters.Length)
        {
            global.Parameters[index] =
                Av1FrameHeaderState.CreateIdentityGlobalParameters();
            index++;
        }
        return global;
    }

    static Av1SegmentationParameters CreateSegmentation()
    {
        Av1SegmentationParameters segmentation =
            new Av1SegmentationParameters();
        segmentation.FeatureEnabled =
            new bool[Av1SyntaxConstants.MaxSegments][];
        segmentation.FeatureData =
            new int[Av1SyntaxConstants.MaxSegments][];
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            segmentation.FeatureEnabled[segment] =
                new bool[Av1SyntaxConstants.SegmentFeatureCount];
            segmentation.FeatureData[segment] =
                new int[Av1SyntaxConstants.SegmentFeatureCount];
            segment++;
        }
        return segmentation;
    }

    static Av1LoopFilterParameters CreateLoopFilter()
    {
        Av1LoopFilterParameters filter = new Av1LoopFilterParameters();
        filter.Levels = new int[4];
        filter.ReferenceDeltas = new int[] { 1,0,0,0,-1,0,-1,-1 };
        filter.ModeDeltas = new int[2];
        return filter;
    }

    static PlaneBuffer CreatePlane(int width, int height)
    {
        return new PlaneBuffer(width, height, 1, 1, 1 << 20, true);
    }

    static ParsedAv1SequenceHeader CreateSequence(
        bool frameIds,
        int orderHintBits)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameIdNumbersPresent = frameIds;
        features.DeltaFrameIdLength = frameIds ? 2 : 0;
        features.AdditionalFrameIdLength = frameIds ? 2 : 0;
        features.EnableOrderHint = orderHintBits > 0;
        features.OrderHintBits = orderHintBits;
        return new ParsedAv1SequenceHeader(
            0,
            false,
            false,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 0, 0) },
            8,
            8,
            features,
            false,
            false,
            true,
            false,
            false,
            0,
            2,
            2,
            2,
            true,
            true,
            false,
            false,
            System.Array.Empty<byte>());
    }
}
