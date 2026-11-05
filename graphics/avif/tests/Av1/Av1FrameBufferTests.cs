namespace avif.Tests;

public class Av1FrameBufferTests
{
    [Test]
    public void RefreshFlagsShareOneFrameAndOutputsRemainInDecodeOrder()
    {
        Av1FrameHeaderState state = new Av1FrameHeaderState();
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits(), state);
        ParsedAv1SequenceHeader sequence = CreateSequence(0);
        ParsedAv1FrameHeader firstHeader = CreateHeader(
            Av1FrameType.Inter, 3, 1 << 1 | 1 << 6, true, false);
        firstHeader.TemporalId = 1;
        firstHeader.SpatialId = 2;
        PlaneBuffer firstPlane = CreatePlane();
        ParsedAv1FrameHeader secondHeader = CreateHeader(
            Av1FrameType.Inter, 4, 1 << 1, true, false);
        PlaneBuffer secondPlane = CreatePlane();

        AvifResultCode firstResult = manager.StoreDecodedFrame(
            sequence, firstHeader, new PlaneBuffer[] { firstPlane }, null);
        Av1DecodedFrame? first = manager.GetReferenceFrame(1);
        AvifResultCode secondResult = manager.StoreDecodedFrame(
            sequence, secondHeader, new PlaneBuffer[] { secondPlane }, null);
        Av1OutputFrame? firstOutput = manager.TakeOutput();
        Av1OutputFrame? secondOutput = manager.TakeOutput();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That((manager.GetReferenceFrame(1) ?? throw new InvalidOperationException("Invalid internal state.")).OrderHint, Is.EqualTo(4));
            Assert.That(manager.GetReferenceFrame(6), Is.SameAs(first));
            Assert.That(state.ReferenceOrderHints[1], Is.EqualTo(4));
            Assert.That(state.ReferenceOrderHints[6], Is.EqualTo(3));
            Assert.That((firstOutput ?? throw new InvalidOperationException("Invalid internal state.")).OutputIndex, Is.EqualTo(0));
            Assert.That(firstOutput.Frame, Is.SameAs(first));
            Assert.That((first ?? throw new InvalidOperationException("Invalid internal state.")).TemporalId, Is.EqualTo(1));
            Assert.That(first.SpatialId, Is.EqualTo(2));
            Assert.That(firstOutput.TemporalId, Is.EqualTo(1));
            Assert.That(firstOutput.SpatialId, Is.EqualTo(2));
            Assert.That((secondOutput ?? throw new InvalidOperationException("Invalid internal state.")).OutputIndex, Is.EqualTo(1));
            Assert.That(secondOutput.Frame, Is.SameAs(manager.GetReferenceFrame(1)));
            Assert.That(manager.GetPendingOutputCount(), Is.Zero);
        });

        manager.ReleaseOutput(firstOutput);
        manager.ReleaseOutput(secondOutput);
        manager.Close();
    }

    [Test]
    public void ShowExistingKeyFrameUsesSharedIdentityOnlyOnce()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(0);
        ParsedAv1FrameHeader key = CreateHeader(
            Av1FrameType.Key, 0, 255, false, true);
        key.CurrentFrameId = 27;
        PlaneBuffer plane = CreatePlane();
        manager.StoreDecodedFrame(sequence, key, new PlaneBuffer[] { plane }, null);
        Av1DecodedFrame? stored = manager.GetReferenceFrame(0);
        ParsedAv1FrameHeader show = CreateShowExistingHeader(3, 255, 101);

        AvifResultCode first = manager.ShowExistingFrame(show);
        Av1OutputFrame? output = manager.TakeOutput();
        AvifResultCode duplicate = manager.ShowExistingFrame(
            CreateShowExistingHeader(6, 255, 102));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(AvifResultCode.Success));
            Assert.That((output ?? throw new InvalidOperationException("Invalid internal state.")).Frame, Is.SameAs(stored));
            Assert.That(output.FromShowExisting, Is.True);
            Assert.That(output.PresentationTimePresent, Is.True);
            Assert.That(output.PresentationTime, Is.EqualTo(101));
            Assert.That(manager.GetReferenceFrame(7), Is.SameAs(stored));
            Assert.That(duplicate, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });

        manager.ReleaseOutput(output);
        manager.Close();
    }

    [Test]
    public void StoredMotionFieldKeepsPastReferencesAndRejectsInvalidProjection()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(4);
        ParsedAv1FrameHeader past = CreateHeader(
            Av1FrameType.Inter, 2, 1 << 2, false, true);
        ParsedAv1FrameHeader future = CreateHeader(
            Av1FrameType.Inter, 8, 1 << 3, false, true);
        manager.StoreDecodedFrame(sequence, past, new PlaneBuffer[] { CreatePlane() }, null);
        manager.StoreDecodedFrame(sequence, future, new PlaneBuffer[] { CreatePlane() }, null);

        ParsedAv1FrameHeader current = CreateHeader(
            Av1FrameType.Inter, 5, 1 << 4, false, true);
        (current.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiColumns = 3;
        current.ReferenceFrameIndexes[0] = 2;
        current.ReferenceFrameIndexes[1] = 3;
        Av1BlockMotionField motion = new Av1BlockMotionField(1, 3);
        motion.Set(0, 0, 0, Av1ReferenceFrame.Last, -7, 11);
        motion.Set(0, 1, 0, Av1ReferenceFrame.Last, 13, -17);
        motion.Set(0, 1, 1, Av1ReferenceFrame.Last2, 21, 22);
        motion.Set(
            0,
            2,
            0,
            Av1ReferenceFrame.Last,
            Av1SyntaxConstants.ReferenceMotionVectorLimit + 1,
            0);

        AvifResultCode result = manager.StoreDecodedFrame(
            sequence, current, new PlaneBuffer[] { CreatePlane() }, motion);
        Av1StoredMotionField? stored = (manager.GetReferenceFrame(4) ?? throw new InvalidOperationException("Invalid internal state.")).MotionField;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That((stored ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceFrame(0, 0), Is.EqualTo(Av1ReferenceFrame.Last));
            Assert.That(stored.GetMotionVectorRow(0, 0), Is.EqualTo(-7));
            Assert.That(stored.GetMotionVectorColumn(0, 0), Is.EqualTo(11));
            Assert.That(stored.GetReferenceFrame(0, 1), Is.EqualTo(Av1ReferenceFrame.Last));
            Assert.That(stored.GetMotionVectorRow(0, 1), Is.EqualTo(13));
            Assert.That(stored.GetReferenceFrame(0, 2), Is.EqualTo(Av1ReferenceFrame.None));
        });

        manager.Close();
    }

    [Test]
    public void OutputOwnershipKeepsOverwrittenFrameAliveUntilRelease()
    {
        Av1FrameBufferManager manager =
            Av1FrameBufferManager.Create(new AvifSecurityLimits());
        ParsedAv1SequenceHeader sequence = CreateSequence(0);
        PlaneBuffer firstPlane = CreatePlane();
        PlaneBuffer replacementPlane = CreatePlane();
        manager.StoreDecodedFrame(
            sequence,
            CreateHeader(Av1FrameType.Inter, 1, 1, true, false),
            new PlaneBuffer[] { firstPlane },
            null);
        Av1OutputFrame? output = manager.TakeOutput();
        manager.StoreDecodedFrame(
            sequence,
            CreateHeader(Av1FrameType.Inter, 2, 1, false, true),
            new PlaneBuffer[] { replacementPlane },
            null);

        Assert.Multiple(() =>
        {
            Assert.That(firstPlane.Closed, Is.False);
            Assert.That(replacementPlane.Closed, Is.False);
            Assert.That(manager.GetRetainedByteCount(), Is.EqualTo(56));
        });

        AvifResultCode release = manager.ReleaseOutput(output);

        Assert.Multiple(() =>
        {
            Assert.That(release, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstPlane.Closed, Is.True);
            Assert.That(replacementPlane.Closed, Is.False);
            Assert.That(manager.GetRetainedByteCount(), Is.EqualTo(28));
            Assert.That(manager.ReleaseOutput(output), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        manager.Close();
        Assert.That(replacementPlane.Closed, Is.True);
    }

    [Test]
    public void DecodedByteAndFrameLimitsFailWithoutTakingPlaneOwnership()
    {
        AvifSecurityLimits byteLimits = new AvifSecurityLimits();
        byteLimits.MaxDecodedBytes = 27;
        Av1FrameBufferManager byteManager = Av1FrameBufferManager.Create(byteLimits);
        PlaneBuffer byteLimitedPlane = CreatePlane();
        AvifResultCode byteResult = byteManager.StoreDecodedFrame(
            CreateSequence(0),
            CreateHeader(Av1FrameType.Inter, 0, 1, false, true),
            new PlaneBuffer[] { byteLimitedPlane },
            null);

        AvifSecurityLimits frameLimits = new AvifSecurityLimits();
        frameLimits.MaxFrameCount = 1;
        Av1FrameBufferManager frameManager = Av1FrameBufferManager.Create(frameLimits);
        frameManager.StoreDecodedFrame(
            CreateSequence(0),
            CreateHeader(Av1FrameType.Inter, 0, 1, false, true),
            new PlaneBuffer[] { CreatePlane() },
            null);
        PlaneBuffer frameLimitedPlane = CreatePlane();
        AvifResultCode frameResult = frameManager.StoreDecodedFrame(
            CreateSequence(0),
            CreateHeader(Av1FrameType.Inter, 1, 2, false, true),
            new PlaneBuffer[] { frameLimitedPlane },
            null);

        Assert.Multiple(() =>
        {
            Assert.That(byteResult, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(byteLimitedPlane.Closed, Is.False);
            Assert.That(byteManager.GetRetainedByteCount(), Is.Zero);
            Assert.That(frameResult, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(frameLimitedPlane.Closed, Is.False);
        });

        byteLimitedPlane.Close();
        frameLimitedPlane.Close();
        byteManager.Close();
        frameManager.Close();
    }

    static PlaneBuffer CreatePlane()
    {
        return new PlaneBuffer(4, 4, 1, 1, 1024, true);
    }

    static ParsedAv1FrameHeader CreateHeader(
        Av1FrameType frameType,
        int orderHint,
        int refreshFlags,
        bool showFrame,
        bool showableFrame)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.FrameType = frameType;
        header.FrameIsIntra = frameType == Av1FrameType.Key
            || frameType == Av1FrameType.IntraOnly;
        header.OrderHint = orderHint;
        header.RefreshFrameFlags = refreshFlags;
        header.ShowFrame = showFrame;
        header.ShowableFrame = showableFrame;
        header.Size = new Av1FrameSize();
        header.Size.UpscaledWidth = 4;
        header.Size.FrameWidth = 4;
        header.Size.FrameHeight = 4;
        header.Size.RenderWidth = 4;
        header.Size.RenderHeight = 4;
        header.Size.MiRows = 1;
        header.Size.MiColumns = 1;
        header.GlobalMotion = CreateGlobalMotion();
        header.FilmGrain = new Av1FilmGrainParameters();
        return header;
    }

    static ParsedAv1FrameHeader CreateShowExistingHeader(
        int slot,
        int refreshFlags,
        ulong presentationTime)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.ShowExistingFrame = true;
        header.FrameToShowMapIndex = slot;
        header.RefreshFrameFlags = refreshFlags;
        header.FramePresentationTimePresent = true;
        header.FramePresentationTime = presentationTime;
        return header;
    }

    static Av1GlobalMotionParameters CreateGlobalMotion()
    {
        Av1GlobalMotionParameters global = new Av1GlobalMotionParameters();
        global.Types = new Av1WarpModelType[Av1SyntaxConstants.TotalRefsPerFrame];
        global.Parameters = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        int index = 0;
        while (index < global.Parameters.Length)
        {
            global.Parameters[index] = Av1FrameHeaderState.CreateIdentityGlobalParameters();
            index++;
        }

        return global;
    }

    static ParsedAv1SequenceHeader CreateSequence(int orderHintBits)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
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
            4,
            4,
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
