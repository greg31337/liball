namespace avif.Tests;

public class Av1SemanticValidatorTests
{
    [Test]
    public void ValidKeyAndDelayedRandomAccessFramesAreAccepted()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            0, false, false, true, true, true, false, false, false);
        ParsedAv1FrameHeader keyFrame = CreateKeyFrame(true);
        Av1SemanticValidationState keyState =
            new Av1SemanticValidationState(Av1RandomAccessKind.KeyFrame);

        Av1SemanticValidationResult sequenceResult =
            Av1SemanticValidator.ValidateSequence(sequence);
        Av1SemanticValidationResult keyResult = Av1SemanticValidator.ValidateFrame(
            sequence,
            keyFrame,
            new Av1FrameHeaderState(),
            keyState,
            0);

        ParsedAv1FrameHeader delayedFrame = CreateKeyFrame(false);
        Av1SemanticValidationState delayedState =
            new Av1SemanticValidationState(Av1RandomAccessKind.Delayed);
        Av1SemanticValidationResult delayedResult = Av1SemanticValidator.ValidateFrame(
            sequence,
            delayedFrame,
            new Av1FrameHeaderState(),
            delayedState,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(sequenceResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(sequenceResult.GetDiagnosticCount(), Is.Zero);
            Assert.That(keyResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(keyState.RequiredRandomAccessKind, Is.EqualTo(Av1RandomAccessKind.None));
            Assert.That(keyState.ValidatedFrameCount, Is.EqualTo(1));
            Assert.That(delayedResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(delayedState.ValidatedFrameCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SequenceProfilesFrameIdWidthsAndChromaRulesAreValidated()
    {
        ParsedAv1SequenceHeader invalidProfileOne = CreateSequence(
            1, false, false, true, true, true, false, false, false);
        ParsedAv1SequenceHeader invalidProfileTwo = CreateSequence(
            2, true, false, false, true, true, false, false, false);
        ParsedAv1SequenceHeader invalidFrameIds = CreateSequence(
            0, false, false, true, true, true, true, false, false, 12, 8);

        Av1SemanticValidationResult profileOne =
            Av1SemanticValidator.ValidateSequence(invalidProfileOne);
        Av1SemanticValidationResult profileTwo =
            Av1SemanticValidator.ValidateSequence(invalidProfileTwo);
        Av1SemanticValidationResult frameIds =
            Av1SemanticValidator.ValidateSequence(invalidFrameIds);

        Assert.Multiple(() =>
        {
            Assert.That(profileOne.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That((profileOne.GetDiagnostic(0) ?? throw new InvalidOperationException("Invalid internal state.")).Av1SyntaxPath, Is.EqualTo("color_config"));
            Assert.That(profileTwo.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(frameIds.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(frameIds, "frame_id_numbers"), Is.True);
            Assert.That(Av1SemanticValidator.ValidateSequence(null).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [Test]
    public void FrameIdsAndReferenceAvailabilityAreValidatedAcrossFrames()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            0, false, false, true, true, true, true, false, false);
        Av1FrameHeaderState references = CreateReferences(64, 64);
        ParsedAv1FrameHeader validFrame = CreateInterFrame(false);
        validFrame.CurrentFrameId = 20;
        int index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            validFrame.DeltaFrameIds[index] = index + 1;
            references.ReferenceFrameIds[index] = 19 - index;
            index++;
        }

        Av1SemanticValidationState validState =
            new Av1SemanticValidationState(Av1RandomAccessKind.None);
        validState.HasPreviousFrameId = true;
        validState.PreviousFrameId = 16;
        Av1SemanticValidationResult valid = Av1SemanticValidator.ValidateFrame(
            sequence, validFrame, references, validState, 0);

        Av1FrameHeaderState missingReferences = CreateReferences(64, 64);
        missingReferences.ReferenceValid[4] = false;
        Av1SemanticValidationState missingState =
            new Av1SemanticValidationState(Av1RandomAccessKind.None);
        ParsedAv1FrameHeader missingFrame = CreateInterFrame(false);
        missingFrame.CurrentFrameId = 20;
        index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            missingFrame.DeltaFrameIds[index] = index + 1;
            missingReferences.ReferenceFrameIds[index] = 19 - index;
            index++;
        }

        Av1SemanticValidationResult missing = Av1SemanticValidator.ValidateFrame(
            sequence, missingFrame, missingReferences, missingState, 0);

        Av1FrameHeaderState wrongIdReferences = CreateReferences(64, 64);
        ParsedAv1FrameHeader wrongIdFrame = CreateInterFrame(false);
        wrongIdFrame.CurrentFrameId = 20;
        index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            wrongIdFrame.DeltaFrameIds[index] = index + 1;
            wrongIdReferences.ReferenceFrameIds[index] = 19 - index;
            index++;
        }

        wrongIdReferences.ReferenceFrameIds[2] = 4;
        Av1SemanticValidationResult wrongId = Av1SemanticValidator.ValidateFrame(
            sequence,
            wrongIdFrame,
            wrongIdReferences,
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);
        ParsedAv1FrameHeader malformedFrame = CreateInterFrame(false);
        malformedFrame.ReferenceFrameIndexes = System.Array.Empty<int>();
        Av1SemanticValidationResult malformed = Av1SemanticValidator.ValidateFrame(
            sequence,
            malformedFrame,
            CreateReferences(64, 64),
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(validState.PreviousFrameId, Is.EqualTo(20));
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(missing, "ref_frame_idx"), Is.True);
            Assert.That(wrongId.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(wrongId, "expectedFrameId"), Is.True);
            Assert.That(malformed.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [Test]
    public void OrderHintsAndShortReferenceDirectionsAreValidated()
    {
        ParsedAv1SequenceHeader orderedSequence = CreateSequence(
            0, false, false, true, true, true, false, true, false);
        ParsedAv1FrameHeader orderedFrame = CreateInterFrame(true);
        orderedFrame.OrderHint = 8;
        orderedFrame.FrameReferencesShortSignaling = true;
        orderedFrame.LastFrameIndex = 0;
        orderedFrame.GoldenFrameIndex = 3;
        Av1FrameHeaderState references = CreateReferences(64, 64);
        int[] hints = new int[] { 7, 6, 5, 4, 9, 10, 11, 12 };
        int index = 0;
        while (index < hints.Length)
        {
            references.ReferenceOrderHints[index] = hints[index];
            index++;
        }

        Av1SemanticValidationResult valid = Av1SemanticValidator.ValidateFrame(
            orderedSequence,
            orderedFrame,
            references,
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);
        references.ReferenceOrderHints[0] = 9;
        Av1SemanticValidationResult backwardLast = Av1SemanticValidator.ValidateFrame(
            orderedSequence,
            orderedFrame,
            references,
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);
        references.ReferenceOrderHints[0] = 7;
        orderedFrame.ReferenceFrameIndexes[0] = 1;
        Av1SemanticValidationResult mismatchedShortReferences =
            Av1SemanticValidator.ValidateFrame(
                orderedSequence,
                orderedFrame,
                references,
                new Av1SemanticValidationState(Av1RandomAccessKind.None),
                0);
        orderedFrame.ReferenceFrameIndexes[0] = 0;
        references.ReferenceOrderHints[2] = 16;
        Av1SemanticValidationResult oversizedReferenceHint =
            Av1SemanticValidator.ValidateFrame(
                orderedSequence,
                orderedFrame,
                references,
                new Av1SemanticValidationState(Av1RandomAccessKind.None),
                0);

        ParsedAv1SequenceHeader unorderedSequence = CreateSequence(
            0, false, false, true, true, true, false, false, false);
        ParsedAv1FrameHeader unorderedFrame = CreateInterFrame(false);
        unorderedFrame.OrderHint = 1;
        Av1SemanticValidationResult disabled = Av1SemanticValidator.ValidateFrame(
            unorderedSequence,
            unorderedFrame,
            CreateReferences(64, 64),
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(backwardLast.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(backwardLast, "frame_refs_short_signaling"), Is.True);
            Assert.That(mismatchedShortReferences.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(mismatchedShortReferences, "frame_refs_short_signaling"),
                Is.True);
            Assert.That(oversizedReferenceHint.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(oversizedReferenceHint, "RefOrderHint"), Is.True);
            Assert.That(disabled.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(disabled, "order_hint"), Is.True);
        });
    }

    [Test]
    public void DimensionsSuperResolutionAndTileLayoutsAreValidated()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            0, false, false, true, true, true, false, false, true);
        ParsedAv1FrameHeader validFrame = CreateKeyFrame(true);
        (validFrame.Size ?? throw new InvalidOperationException("Invalid internal state.")).UsesSuperres = true;
        validFrame.Size.SuperresDenominator = 10;
        validFrame.Size.FrameWidth = 51;
        validFrame.Size.MiColumns = 14;
        (validFrame.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiColumnStarts = new int[] { 0, 14 };
        Av1SemanticValidationResult valid = Av1SemanticValidator.ValidateFrame(
            sequence,
            validFrame,
            new Av1FrameHeaderState(),
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);

        ParsedAv1FrameHeader oversized = CreateKeyFrame(true);
        (oversized.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth = 65;
        Av1SemanticValidationResult size = Av1SemanticValidator.ValidateFrame(
            sequence,
            oversized,
            new Av1FrameHeaderState(),
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);

        ParsedAv1FrameHeader badSuperres = CreateKeyFrame(true);
        (badSuperres.Size ?? throw new InvalidOperationException("Invalid internal state.")).UsesSuperres = true;
        badSuperres.Size.SuperresDenominator = 10;
        Av1SemanticValidationResult superres = Av1SemanticValidator.ValidateFrame(
            sequence,
            badSuperres,
            new Av1FrameHeaderState(),
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);

        ParsedAv1FrameHeader badTiles = CreateKeyFrame(true);
        (badTiles.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns = 2;
        badTiles.Tiles.TileColumnsLog2 = 1;
        badTiles.Tiles.MiColumnStarts = new int[] { 0, 8, 16 };
        badTiles.Tiles.TileSizeBytes = 1;
        Av1SemanticValidationResult tiles = Av1SemanticValidator.ValidateFrame(
            sequence,
            badTiles,
            new Av1FrameHeaderState(),
            new Av1SemanticValidationState(Av1RandomAccessKind.None),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(size.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(size, "frame_size"), Is.True);
            Assert.That(superres.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(superres, "superres_params"), Is.True);
            Assert.That(tiles.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(tiles, "uniform_tile_spacing_flag"), Is.True);
        });
    }

    [Test]
    public void RandomAccessAndShowExistingRequirementsAreEnforced()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            0, false, false, true, true, true, false, false, false);
        ParsedAv1FrameHeader interFrame = CreateInterFrame(false);
        Av1SemanticValidationResult randomAccess = Av1SemanticValidator.ValidateFrame(
            sequence,
            interFrame,
            CreateReferences(64, 64),
            new Av1SemanticValidationState(Av1RandomAccessKind.KeyFrame),
            1);

        Av1FrameHeaderState references = new Av1FrameHeaderState();
        references.SetReference(2, Av1FrameType.Key, 0, 0, 64, 64, 64, 64, 64);
        references.SetReferenceShowable(2, true);
        ParsedAv1FrameHeader showExisting = new ParsedAv1FrameHeader();
        showExisting.ShowExistingFrame = true;
        showExisting.FrameToShowMapIndex = 2;
        showExisting.FrameType = Av1FrameType.Key;
        showExisting.FrameIsIntra = true;
        showExisting.ShowFrame = true;
        showExisting.RefreshFrameFlags = 255;
        Av1SemanticValidationState showState =
            new Av1SemanticValidationState(Av1RandomAccessKind.None);
        Av1SemanticValidationResult first = Av1SemanticValidator.ValidateFrame(
            sequence, showExisting, references, showState, 0);
        Av1SemanticValidationResult second = Av1SemanticValidator.ValidateFrame(
            sequence, showExisting, references, showState, 0);

        Assert.Multiple(() =>
        {
            Assert.That(randomAccess.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(randomAccess, "random_access_point"), Is.True);
            Assert.That(first.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(HasDiagnostic(second, "show_existing_frame"), Is.True);
            Assert.That((second.GetDiagnostic(0) ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Validation));
        });
    }

    static ParsedAv1SequenceHeader CreateSequence(
        int profile,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        bool frameIds,
        bool orderHints,
        bool superres)
    {
        return CreateSequence(
            profile,
            highBitDepth,
            twelveBit,
            monochrome,
            subsamplingX,
            subsamplingY,
            frameIds,
            orderHints,
            superres,
            frameIds ? 4 : 0,
            frameIds ? 4 : 0);
    }

    static ParsedAv1SequenceHeader CreateSequence(
        int profile,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        bool frameIds,
        bool orderHints,
        bool superres,
        int deltaFrameIdLength,
        int additionalFrameIdLength)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameWidthBits = 6;
        features.FrameHeightBits = 6;
        features.FrameIdNumbersPresent = frameIds;
        features.DeltaFrameIdLength = deltaFrameIdLength;
        features.AdditionalFrameIdLength = additionalFrameIdLength;
        features.EnableOrderHint = orderHints;
        features.OrderHintBits = orderHints ? 4 : 0;
        features.EnableSuperres = superres;
        features.ScreenContentToolsMode = 0;
        features.IntegerMotionVectorMode = Av1SyntaxConstants.SelectIntegerMotionVector;
        return new ParsedAv1SequenceHeader(
            profile,
            false,
            false,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 4, 0) },
            64,
            64,
            features,
            highBitDepth,
            twelveBit,
            monochrome,
            subsamplingX,
            subsamplingY,
            subsamplingX && subsamplingY ? 1 : 0,
            2,
            2,
            2,
            true,
            false,
            false,
            false,
            System.Array.Empty<byte>());
    }

    static ParsedAv1FrameHeader CreateKeyFrame(bool showFrame)
    {
        ParsedAv1FrameHeader frame = new ParsedAv1FrameHeader();
        frame.FrameType = Av1FrameType.Key;
        frame.FrameIsIntra = true;
        frame.ShowFrame = showFrame;
        frame.ShowableFrame = !showFrame;
        frame.ErrorResilientMode = true;
        frame.PrimaryReferenceFrame = 7;
        frame.RefreshFrameFlags = 255;
        SetDimensionsAndTiles(frame, 64, 64);
        return frame;
    }

    static ParsedAv1FrameHeader CreateInterFrame(bool orderHints)
    {
        ParsedAv1FrameHeader frame = new ParsedAv1FrameHeader();
        frame.FrameType = Av1FrameType.Inter;
        frame.FrameIsIntra = false;
        frame.ShowFrame = true;
        frame.ShowableFrame = true;
        frame.PrimaryReferenceFrame = 0;
        frame.RefreshFrameFlags = 1;
        int index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            frame.ReferenceFrameIndexes[index] = index;
            index++;
        }

        if (!orderHints)
        {
            frame.OrderHint = 0;
        }

        SetDimensionsAndTiles(frame, 64, 64);
        return frame;
    }

    static void SetDimensionsAndTiles(ParsedAv1FrameHeader frame, int width, int height)
    {
        Av1FrameSize size = new Av1FrameSize();
        size.UpscaledWidth = width;
        size.FrameWidth = width;
        size.FrameHeight = height;
        size.RenderWidth = width;
        size.RenderHeight = height;
        size.SuperresDenominator = Av1SyntaxConstants.SuperresNumerator;
        size.MiColumns = 2 * ((width + 7) >> 3);
        size.MiRows = 2 * ((height + 7) >> 3);
        frame.Size = size;

        Av1TileInfo tiles = new Av1TileInfo();
        tiles.UniformSpacing = true;
        tiles.TileColumns = 1;
        tiles.TileRows = 1;
        tiles.MiColumnStarts = new int[] { 0, size.MiColumns };
        tiles.MiRowStarts = new int[] { 0, size.MiRows };
        frame.Tiles = tiles;
    }

    static Av1FrameHeaderState CreateReferences(int width, int height)
    {
        Av1FrameHeaderState references = new Av1FrameHeaderState();
        int index = 0;
        while (index < Av1SyntaxConstants.StoredReferenceFrames)
        {
            references.SetReference(
                index,
                Av1FrameType.Inter,
                0,
                0,
                width,
                width,
                height,
                width,
                height);
            references.SetReferenceShowable(index, true);
            index++;
        }

        return references;
    }

    static bool HasDiagnostic(Av1SemanticValidationResult result, string syntaxPath)
    {
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if ((result.GetDiagnostic(index) ?? throw new InvalidOperationException("Invalid internal state.")).Av1SyntaxPath == syntaxPath)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
