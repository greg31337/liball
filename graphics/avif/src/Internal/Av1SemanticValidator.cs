namespace avif;

internal enum Av1RandomAccessKind
{
    None = 0,
    KeyFrame = 1,
    Delayed = 2
}

internal class Av1SemanticValidationState
{
    internal Av1RandomAccessKind RequiredRandomAccessKind;
    internal bool HasPreviousFrameId;
    internal int PreviousFrameId;
    internal int ValidatedFrameCount;
    internal bool[] ShownKeyFrames;

    internal Av1SemanticValidationState(Av1RandomAccessKind requiredRandomAccessKind)
    {
        RequiredRandomAccessKind = requiredRandomAccessKind;
        ShownKeyFrames = new bool[Av1SyntaxConstants.StoredReferenceFrames];
    }

    internal void Reset(Av1RandomAccessKind requiredRandomAccessKind)
    {
        RequiredRandomAccessKind = requiredRandomAccessKind;
        HasPreviousFrameId = false;
        PreviousFrameId = 0;
        ValidatedFrameCount = 0;
        System.Array.Clear(ShownKeyFrames);
    }
}

internal class Av1SemanticValidationResult
{
    internal AvifResultCode Code;
    internal DiagnosticLog Diagnostics;

    internal Av1SemanticValidationResult(AvifResultCode code, DiagnosticLog diagnostics)
    {
        Code = code;
        Diagnostics = diagnostics;
    }

    internal int GetDiagnosticCount()
    {
        return Diagnostics.Diagnostics.Count;
    }

    internal AvifDiagnostic? GetDiagnostic(int index)
    {
        return Diagnostics.Get(index);
    }
}

// AV1 specification sections 6.4, 6.8, 7.5, and 7.6.
internal static class Av1SemanticValidator
{
    internal static Av1SemanticValidationResult ValidateSequence(
        ParsedAv1SequenceHeader? sequence)
    {
        DiagnosticLog diagnostics = new DiagnosticLog();
        if (sequence == null)
        {
            ReportArgument(diagnostics, "A sequence header is required.");
            return CreateResult(diagnostics);
        }

        ValidateSequenceFields(sequence, diagnostics);
        return CreateResult(diagnostics);
    }

    internal static Av1SemanticValidationResult ValidateFrame(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1FrameHeaderState references,
        Av1SemanticValidationState state,
        int temporalId)
    {
        DiagnosticLog diagnostics = new DiagnosticLog();
        if (sequence == null || frame == null || references == null || state == null
            || temporalId < 0 || temporalId >= Av1SyntaxConstants.MaxTemporalLayers)
        {
            ReportArgument(diagnostics, "Sequence, frame, reference state, and temporal ID are required.");
            return CreateResult(diagnostics);
        }

        if (!HasValidReferenceState(references) || !HasValidFrameState(frame, state))
        {
            ReportArgument(diagnostics, "The frame or validation state arrays have invalid lengths.");
            return CreateResult(diagnostics);
        }

        ValidateSequenceFields(sequence, diagnostics);
        if (diagnostics.Code != AvifResultCode.Success)
        {
            return CreateResult(diagnostics);
        }

        ValidateRandomAccess(frame, state.RequiredRandomAccessKind, temporalId, diagnostics);
        ValidateFrameFlags(frame, diagnostics);
        if (frame.ShowExistingFrame)
        {
            ValidateShowExisting(sequence, frame, references, state, diagnostics);
        }
        else
        {
            ValidateFrameIds(sequence, frame, references, state, diagnostics);
            ValidateReferences(sequence, frame, references, diagnostics);
            ValidateDimensions(sequence, frame, references, diagnostics);
            ValidateTiles(sequence, frame, diagnostics);
        }

        if (diagnostics.Code == AvifResultCode.Success)
        {
            AcceptFrame(sequence, frame, state);
        }

        return CreateResult(diagnostics);
    }

    static void ValidateSequenceFields(
        ParsedAv1SequenceHeader sequence,
        DiagnosticLog diagnostics)
    {
        int profile = sequence.GetSequenceProfile();
        int bitDepth = sequence.GetBitDepth();
        if (profile < 0 || profile > 2)
        {
            Report(diagnostics, "seq_profile", "The sequence profile is outside the AV1 range.",
                "AV1 section 6.4.2");
            return;
        }

        if (sequence.GetMaximumWidth() == 0 || sequence.GetMaximumWidth() > 65536
            || sequence.GetMaximumHeight() == 0 || sequence.GetMaximumHeight() > 65536)
        {
            Report(diagnostics, "max_frame_size", "The maximum frame dimensions are outside the AV1 range.",
                "AV1 section 6.4.2");
        }

        if (bitDepth != 8 && bitDepth != 10 && bitDepth != 12)
        {
            Report(diagnostics, "BitDepth", "The bit depth is not an AV1 bit depth.",
                "AV1 section 6.4.2");
        }

        if (sequence.IsTwelveBit() && (profile != 2 || !sequence.IsHighBitDepth()))
        {
            Report(diagnostics, "twelve_bit", "Twelve-bit coding is restricted to profile 2.",
                "AV1 section 6.4.2");
        }

        if (profile == 0)
        {
            if (bitDepth == 12 || (!sequence.IsMonochrome()
                && (!sequence.HasChromaSubsamplingX()
                    || !sequence.HasChromaSubsamplingY())))
            {
                Report(diagnostics, "color_config", "Profile 0 requires 8/10-bit monochrome or 4:2:0 video.",
                    "AV1 section 6.4.2");
            }
        }
        else if (profile == 1)
        {
            if (bitDepth == 12 || sequence.IsMonochrome()
                || sequence.HasChromaSubsamplingX()
                || sequence.HasChromaSubsamplingY())
            {
                Report(diagnostics, "color_config", "Profile 1 requires 8/10-bit 4:4:4 video.",
                    "AV1 section 6.4.2");
            }
        }
        else if (bitDepth < 12 && !sequence.IsMonochrome()
            && (!sequence.HasChromaSubsamplingX()
                || sequence.HasChromaSubsamplingY()))
        {
            Report(diagnostics, "color_config", "Profile 2 below 12 bits requires 4:2:2 video.",
                "AV1 section 6.4.2");
        }

        if (!sequence.IsMonochrome() && sequence.GetMatrixCoefficients() == 0
            && (sequence.HasChromaSubsamplingX() || sequence.HasChromaSubsamplingY()))
        {
            Report(diagnostics, "matrix_coefficients", "Identity RGB coding cannot use chroma subsampling.",
                "AV1 section 6.4.2");
        }

        if (sequence.IsMonochrome() && sequence.HasSeparateUvDeltaQuantizer())
        {
            Report(diagnostics, "separate_uv_delta_q", "Monochrome video cannot signal UV quantizers.",
                "AV1 section 6.4.2");
        }

        if (sequence.HasChromaSubsamplingX() && sequence.HasChromaSubsamplingY()
            && sequence.GetChromaSamplePosition() == 3)
        {
            Report(diagnostics, "chroma_sample_position", "Chroma sample position 3 is reserved.",
                "AV1 section 6.4.2");
        }

        if (sequence.HasFrameIdNumbers())
        {
            int idLength = sequence.GetFrameIdLength();
            if (sequence.GetDeltaFrameIdLength() < 2
                || sequence.GetAdditionalFrameIdLength() < 1
                || idLength > 16)
            {
                Report(diagnostics, "frame_id_numbers", "The frame ID lengths are not conforming.",
                    "AV1 section 6.8.2");
            }
        }

        if ((sequence.IsOrderHintEnabled()
                && (sequence.GetOrderHintBits() < 1 || sequence.GetOrderHintBits() > 8))
            || (!sequence.IsOrderHintEnabled() && sequence.GetOrderHintBits() != 0))
        {
            Report(diagnostics, "OrderHintBits", "The order hint width is inconsistent with enable_order_hint.",
                "AV1 section 6.4.2");
        }

        if (sequence.HasReducedStillPictureHeader() && !sequence.IsStillPicture())
        {
            Report(diagnostics, "reduced_still_picture_header",
                "A reduced still-picture header requires still_picture.", "AV1 section 6.4.2");
        }

        int pointIndex = 0;
        while (pointIndex < sequence.GetOperatingPointCount())
        {
            Av1OperatingPoint? point = sequence.GetOperatingPoint(pointIndex);
            if (point == null || point.Idc < 0 || point.Idc > 4095
                || (point.Idc != 0 && ((point.Idc & 1) == 0 || (point.Idc & 256) == 0))
                || !Av1SequenceHeaderParser.IsValidLevel(point.Level)
                || (point.Level <= 7 && point.Tier != 0))
            {
                Report(diagnostics, "operating_point", "An operating point is not conforming.",
                    "AV1 section 6.4.2");
                break;
            }

            pointIndex++;
        }
    }

    static void ValidateRandomAccess(
        ParsedAv1FrameHeader frame,
        Av1RandomAccessKind requirement,
        int temporalId,
        DiagnosticLog diagnostics)
    {
        if (requirement == Av1RandomAccessKind.None)
        {
            return;
        }

        bool expectedShowFrame = requirement == Av1RandomAccessKind.KeyFrame;
        if (frame.ShowExistingFrame || frame.FrameType != Av1FrameType.Key
            || frame.ShowFrame != expectedShowFrame || temporalId != 0)
        {
            Report(diagnostics, "random_access_point",
                requirement == Av1RandomAccessKind.KeyFrame
                    ? "A key-frame random access point requires a shown temporal-layer-zero key frame."
                    : "A delayed random access point requires a hidden temporal-layer-zero key frame.",
                "AV1 section 7.6");
        }
    }

    static void ValidateFrameFlags(
        ParsedAv1FrameHeader frame,
        DiagnosticLog diagnostics)
    {
        bool expectedIntra = frame.FrameType == Av1FrameType.Key
            || frame.FrameType == Av1FrameType.IntraOnly;
        if (frame.FrameIsIntra != expectedIntra)
        {
            Report(diagnostics, "FrameIsIntra", "FrameIsIntra does not match frame_type.",
                "AV1 section 6.8.2");
        }

        if (frame.RefreshFrameFlags < 0 || frame.RefreshFrameFlags > 255)
        {
            Report(diagnostics, "refresh_frame_flags", "The refresh mask is not eight bits.",
                "AV1 section 6.8.2");
        }

        if (!frame.ShowExistingFrame
            && (frame.FrameType == Av1FrameType.Switch
                || (frame.FrameType == Av1FrameType.Key && frame.ShowFrame))
            && (!frame.ErrorResilientMode || frame.RefreshFrameFlags != 255))
        {
            Report(diagnostics, "refresh_frame_flags",
                "Shown key frames and switch frames must be error resilient and refresh every slot.",
                "AV1 section 6.8.2");
        }

        if (frame.FrameType == Av1FrameType.IntraOnly && frame.RefreshFrameFlags == 255)
        {
            Report(diagnostics, "refresh_frame_flags", "An intra-only frame cannot refresh every slot.",
                "AV1 section 6.8.2");
        }

        if ((frame.FrameIsIntra || frame.ErrorResilientMode)
            && frame.PrimaryReferenceFrame != 7)
        {
            Report(diagnostics, "primary_ref_frame",
                "Intra and error-resilient frames require PRIMARY_REF_NONE.",
                "AV1 section 6.8.2");
        }

        if (frame.AllowIntraBlockCopy && (!frame.FrameIsIntra
            || !frame.AllowScreenContentTools || frame.Size == null
            || frame.Size.UsesSuperres))
        {
            Report(diagnostics, "allow_intrabc", "Intra block copy has incompatible frame settings.",
                "AV1 section 6.8.2");
        }
    }

    static void ValidateShowExisting(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1FrameHeaderState references,
        Av1SemanticValidationState state,
        DiagnosticLog diagnostics)
    {
        int slot = frame.FrameToShowMapIndex;
        if (slot < 0 || slot >= Av1SyntaxConstants.StoredReferenceFrames)
        {
            Report(diagnostics, "frame_to_show_map_idx", "The displayed reference slot is outside the buffer.",
                "AV1 section 6.8.2");
            return;
        }

        if (!references.ReferenceValid[slot])
        {
            Report(diagnostics, "frame_to_show_map_idx", "The displayed reference slot is unavailable.",
                "AV1 section 6.8.2");
        }

        if (!references.ReferenceShowable[slot])
        {
            Report(diagnostics, "show_existing_frame", "The referenced frame was not marked showable.",
                "AV1 section 6.8.2");
        }

        if (frame.FrameType != references.ReferenceFrameTypes[slot])
        {
            Report(diagnostics, "frame_type", "The displayed frame type does not match the reference slot.",
                "AV1 section 6.8.2");
        }

        if (sequence.HasFrameIdNumbers()
            && frame.DisplayFrameId != references.ReferenceFrameIds[slot])
        {
            Report(diagnostics, "display_frame_id", "The display frame ID does not match the reference slot.",
                "AV1 section 6.8.2");
        }

        if (references.ReferenceFrameTypes[slot] == Av1FrameType.Key
            && state.ShownKeyFrames[slot])
        {
            Report(diagnostics, "show_existing_frame", "A stored key frame can be shown only once.",
                "AV1 section 6.8.2");
        }
    }

    static void ValidateFrameIds(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1FrameHeaderState references,
        Av1SemanticValidationState state,
        DiagnosticLog diagnostics)
    {
        if (!sequence.HasFrameIdNumbers())
        {
            if (frame.CurrentFrameId != 0)
            {
                Report(diagnostics, "current_frame_id", "Frame IDs are disabled for this sequence.",
                    "AV1 section 6.8.2");
            }

            return;
        }

        int idLength = sequence.GetFrameIdLength();
        int idRange = 1 << idLength;
        if (frame.CurrentFrameId < 0 || frame.CurrentFrameId >= idRange)
        {
            Report(diagnostics, "current_frame_id", "The current frame ID exceeds idLen.",
                "AV1 section 6.8.2");
            return;
        }

        if (state.HasPreviousFrameId
            && (frame.FrameType != Av1FrameType.Key || !frame.ShowFrame))
        {
            int difference = frame.CurrentFrameId - state.PreviousFrameId;
            if (difference <= 0)
            {
                difference += idRange;
            }

            if (frame.CurrentFrameId == state.PreviousFrameId
                || difference >= (1 << (idLength - 1)))
            {
                Report(diagnostics, "current_frame_id", "The frame ID progression is not conforming.",
                    "AV1 section 6.8.2");
            }
        }

        if (!frame.FrameIsIntra)
        {
            int index = 0;
            int deltaMaximum = 1 << sequence.GetDeltaFrameIdLength();
            while (index < Av1SyntaxConstants.RefsPerFrame)
            {
                int slot = frame.ReferenceFrameIndexes[index];
                int delta = frame.DeltaFrameIds[index];
                if (slot >= 0 && slot < Av1SyntaxConstants.StoredReferenceFrames
                    && delta >= 1 && delta <= deltaMaximum)
                {
                    int expected = frame.CurrentFrameId - delta;
                    if (expected < 0)
                    {
                        expected += idRange;
                    }

                    if (references.ReferenceFrameIds[slot] != expected)
                    {
                        Report(diagnostics, "expectedFrameId",
                            "A reference frame ID does not match delta_frame_id.",
                            "AV1 section 6.8.2");
                        break;
                    }
                }
                else
                {
                    Report(diagnostics, "delta_frame_id", "A delta frame ID is outside its coded range.",
                        "AV1 section 6.8.2");
                    break;
                }

                index++;
            }
        }
    }

    static void ValidateReferences(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1FrameHeaderState references,
        DiagnosticLog diagnostics)
    {
        if (frame.FrameIsIntra)
        {
            return;
        }

        int index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            int slot = frame.ReferenceFrameIndexes[index];
            if (slot < 0 || slot >= Av1SyntaxConstants.StoredReferenceFrames
                || !references.ReferenceValid[slot])
            {
                Report(diagnostics, "ref_frame_idx", "An inter reference slot is unavailable.",
                    "AV1 section 6.8.2");
                break;
            }

            index++;
        }

        if (frame.PrimaryReferenceFrame < 0 || frame.PrimaryReferenceFrame > 7)
        {
            Report(diagnostics, "primary_ref_frame", "The primary reference index is outside its range.",
                "AV1 section 6.8.2");
        }
        else if (frame.PrimaryReferenceFrame != 7)
        {
            int slot = frame.ReferenceFrameIndexes[frame.PrimaryReferenceFrame];
            if (slot < 0 || slot >= Av1SyntaxConstants.StoredReferenceFrames
                || !references.ReferenceValid[slot])
            {
                Report(diagnostics, "primary_ref_frame", "The primary reference is unavailable.",
                    "AV1 section 6.8.2");
            }
        }

        if (!sequence.IsOrderHintEnabled())
        {
            if (frame.OrderHint != 0 || frame.FrameReferencesShortSignaling
                || frame.SkipModePresent || frame.SkipModeFrames[0] >= 0
                || frame.SkipModeFrames[1] >= 0)
            {
                Report(diagnostics, "order_hint", "Order-hint tools are present while order hints are disabled.",
                    "AV1 sections 6.4.2 and 6.8.2");
            }
        }
        else
        {
            int hintRange = 1 << sequence.GetOrderHintBits();
            if (frame.OrderHint < 0 || frame.OrderHint >= hintRange)
            {
                Report(diagnostics, "order_hint", "The frame order hint exceeds OrderHintBits.",
                    "AV1 section 6.8.2");
            }

            index = 0;
            while (index < Av1SyntaxConstants.RefsPerFrame)
            {
                int slot = frame.ReferenceFrameIndexes[index];
                if (slot >= 0 && slot < Av1SyntaxConstants.StoredReferenceFrames
                    && references.ReferenceValid[slot]
                    && (references.ReferenceOrderHints[slot] < 0
                        || references.ReferenceOrderHints[slot] >= hintRange))
                {
                    Report(diagnostics, "RefOrderHint",
                        "A reference order hint exceeds OrderHintBits.",
                        "AV1 section 6.8.2");
                    break;
                }

                index++;
            }

            if (frame.FrameReferencesShortSignaling)
            {
                ValidateShortReferences(sequence, frame, references, diagnostics);
            }
        }

        if (frame.FilmGrain != null && frame.FilmGrain.ApplyGrain
            && !frame.FilmGrain.UpdateGrain)
        {
            bool found = false;
            index = 0;
            while (index < Av1SyntaxConstants.RefsPerFrame)
            {
                if (frame.ReferenceFrameIndexes[index] == frame.FilmGrain.ReferenceIndex)
                {
                    found = true;
                }

                index++;
            }

            if (!found)
            {
                Report(diagnostics, "film_grain_params_ref_idx",
                    "Predicted film grain must use one of the current references.",
                    "AV1 section 6.8.20");
            }
        }
    }

    static void ValidateShortReferences(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1FrameHeaderState references,
        DiagnosticLog diagnostics)
    {
        if (frame.LastFrameIndex < 0 || frame.LastFrameIndex >= 8
            || frame.GoldenFrameIndex < 0 || frame.GoldenFrameIndex >= 8
            || frame.ReferenceFrameIndexes[0] != frame.LastFrameIndex
            || frame.ReferenceFrameIndexes[3] != frame.GoldenFrameIndex)
        {
            Report(diagnostics, "frame_refs_short_signaling",
                "Short reference signaling is inconsistent with the selected references.",
                "AV1 section 7.8");
            return;
        }

        int lastDistance = RelativeDistance(
            references.ReferenceOrderHints[frame.LastFrameIndex],
            frame.OrderHint,
            sequence.GetOrderHintBits());
        int goldenDistance = RelativeDistance(
            references.ReferenceOrderHints[frame.GoldenFrameIndex],
            frame.OrderHint,
            sequence.GetOrderHintBits());
        if (lastDistance >= 0 || goldenDistance >= 0)
        {
            Report(diagnostics, "frame_refs_short_signaling",
                "LAST and GOLDEN must be forward references under short signaling.",
                "AV1 section 7.8");
        }
    }

    static void ValidateDimensions(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1FrameHeaderState references,
        DiagnosticLog diagnostics)
    {
        Av1FrameSize? size = frame.Size;
        if (size == null)
        {
            Report(diagnostics, "frame_size", "A decoded frame requires dimensions.",
                "AV1 sections 6.8.3 through 6.8.7");
            return;
        }

        if (size.UpscaledWidth < 1 || size.UpscaledWidth > sequence.GetMaximumWidth()
            || size.FrameHeight < 1 || size.FrameHeight > sequence.GetMaximumHeight()
            || size.FrameWidth < 1 || size.RenderWidth < 1 || size.RenderWidth > 65536
            || size.RenderHeight < 1 || size.RenderHeight > 65536)
        {
            Report(diagnostics, "frame_size", "Frame or render dimensions are outside their allowed range.",
                "AV1 sections 6.8.3 and 6.8.4");
            return;
        }

        int expectedDenominator = size.UsesSuperres
            ? size.SuperresDenominator
            : Av1SyntaxConstants.SuperresNumerator;
        int expectedWidth = (size.UpscaledWidth * Av1SyntaxConstants.SuperresNumerator
            + expectedDenominator / 2) / expectedDenominator;
        if ((size.UsesSuperres && (!sequence.IsSuperresEnabled()
                || size.SuperresDenominator < Av1SyntaxConstants.SuperresMinimumDenominator
                || size.SuperresDenominator > 16))
            || (!size.UsesSuperres
                && size.SuperresDenominator != Av1SyntaxConstants.SuperresNumerator)
            || size.FrameWidth != expectedWidth)
        {
            Report(diagnostics, "superres_params", "The super-resolution dimensions are inconsistent.",
                "AV1 section 6.8.7");
        }

        int expectedMiColumns = 2 * ((size.FrameWidth + 7) >> 3);
        int expectedMiRows = 2 * ((size.FrameHeight + 7) >> 3);
        if (size.MiColumns != expectedMiColumns || size.MiRows != expectedMiRows)
        {
            Report(diagnostics, "compute_image_size", "The MI dimensions do not match the frame size.",
                "AV1 section 6.8.8");
        }

        if (!frame.FrameIsIntra)
        {
            int index = 0;
            while (index < Av1SyntaxConstants.RefsPerFrame)
            {
                int slot = frame.ReferenceFrameIndexes[index];
                if (slot >= 0 && slot < Av1SyntaxConstants.StoredReferenceFrames
                    && references.ReferenceValid[slot])
                {
                    int referenceWidth = references.ReferenceUpscaledWidths[slot];
                    int referenceHeight = references.ReferenceFrameHeights[slot];
                    if (referenceWidth < 1 || referenceHeight < 1
                        || 2L * size.FrameWidth < referenceWidth
                        || 2L * size.FrameHeight < referenceHeight
                        || size.FrameWidth > 16L * referenceWidth
                        || size.FrameHeight > 16L * referenceHeight)
                    {
                        Report(diagnostics, "frame_size_with_refs",
                            "The frame size is incompatible with a selected reference.",
                            "AV1 section 6.8.6");
                        break;
                    }
                }

                index++;
            }
        }
    }

    static void ValidateTiles(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        DiagnosticLog diagnostics)
    {
        if (frame.Size == null || frame.Tiles == null)
        {
            Report(diagnostics, "tile_info", "A decoded frame requires tile layout information.",
                "AV1 section 6.8.14");
            return;
        }

        Av1TileInfo tiles = frame.Tiles;
        if (tiles.TileColumns < 1 || tiles.TileColumns > Av1SyntaxConstants.MaxTileColumns
            || tiles.TileRows < 1 || tiles.TileRows > Av1SyntaxConstants.MaxTileRows
            || tiles.MiColumnStarts == null || tiles.MiRowStarts == null
            || tiles.MiColumnStarts.Length != tiles.TileColumns + 1
            || tiles.MiRowStarts.Length != tiles.TileRows + 1)
        {
            Report(diagnostics, "tile_info", "Tile counts or boundary arrays are invalid.",
                "AV1 section 6.8.14");
            return;
        }

        if (tiles.TileColumnsLog2 != TileLog2(1, tiles.TileColumns)
            || tiles.TileRowsLog2 != TileLog2(1, tiles.TileRows)
            || tiles.MiColumnStarts[0] != 0 || tiles.MiRowStarts[0] != 0
            || tiles.MiColumnStarts[tiles.TileColumns] != frame.Size.MiColumns
            || tiles.MiRowStarts[tiles.TileRows] != frame.Size.MiRows)
        {
            Report(diagnostics, "tile_info", "Tile logarithms or outer boundaries are inconsistent.",
                "AV1 section 6.8.14");
            return;
        }

        int superblockShift = sequence.Uses128x128Superblocks() ? 5 : 4;
        int superblockSizeLog2 = superblockShift + 2;
        int superblockColumns = (frame.Size.MiColumns + (1 << superblockShift) - 1)
            >> superblockShift;
        int superblockRows = (frame.Size.MiRows + (1 << superblockShift) - 1)
            >> superblockShift;
        int maxTileWidthSb = Av1SyntaxConstants.MaxTileWidth >> superblockSizeLog2;
        int maxTileAreaSb = Av1SyntaxConstants.MaxTileArea >> (2 * superblockSizeLog2);
        if (tiles.UniformSpacing
            && (!HasUniformTileStarts(
                    tiles.MiColumnStarts,
                    tiles.TileColumns,
                    superblockColumns,
                    tiles.TileColumnsLog2,
                    superblockShift,
                    frame.Size.MiColumns)
                || !HasUniformTileStarts(
                    tiles.MiRowStarts,
                    tiles.TileRows,
                    superblockRows,
                    tiles.TileRowsLog2,
                    superblockShift,
                    frame.Size.MiRows)))
        {
            Report(diagnostics, "uniform_tile_spacing_flag",
                "Uniform tile boundaries do not match their logarithmic tile counts.",
                "AV1 section 6.8.14");
            return;
        }

        int column = 0;
        while (column < tiles.TileColumns)
        {
            int startMi = tiles.MiColumnStarts[column];
            int endMi = tiles.MiColumnStarts[column + 1];
            if (startMi < 0 || endMi <= startMi
                || (column > 0 && (startMi & ((1 << superblockShift) - 1)) != 0))
            {
                Report(diagnostics, "MiColStarts", "Tile column boundaries are invalid.",
                    "AV1 section 6.8.14");
                return;
            }

            int startSb = startMi >> superblockShift;
            int endSb = column + 1 == tiles.TileColumns
                ? superblockColumns
                : endMi >> superblockShift;
            int widthSb = endSb - startSb;
            if (widthSb < 1 || widthSb > maxTileWidthSb)
            {
                Report(diagnostics, "tileWidthSb", "A tile column exceeds the maximum width.",
                    "AV1 section 6.8.14");
                return;
            }

            int row = 0;
            while (row < tiles.TileRows)
            {
                int rowStartMi = tiles.MiRowStarts[row];
                int rowEndMi = tiles.MiRowStarts[row + 1];
                if (rowStartMi < 0 || rowEndMi <= rowStartMi
                    || (row > 0 && (rowStartMi & ((1 << superblockShift) - 1)) != 0))
                {
                    Report(diagnostics, "MiRowStarts", "Tile row boundaries are invalid.",
                        "AV1 section 6.8.14");
                    return;
                }

                int rowStartSb = rowStartMi >> superblockShift;
                int rowEndSb = row + 1 == tiles.TileRows
                    ? superblockRows
                    : rowEndMi >> superblockShift;
                int heightSb = rowEndSb - rowStartSb;
                if (heightSb < 1 || widthSb * heightSb > maxTileAreaSb)
                {
                    Report(diagnostics, "tileHeightSb", "A tile exceeds the maximum area.",
                        "AV1 section 6.8.14");
                    return;
                }

                row++;
            }

            column++;
        }

        int tileCount = tiles.TileColumns * tiles.TileRows;
        if (tiles.ContextUpdateTileId < 0 || tiles.ContextUpdateTileId >= tileCount
            || (tileCount == 1 && tiles.TileSizeBytes != 0)
            || (tileCount > 1 && (tiles.TileSizeBytes < 1 || tiles.TileSizeBytes > 4)))
        {
            Report(diagnostics, "context_update_tile_id", "Tile context or size-byte signaling is invalid.",
                "AV1 section 6.8.14");
        }
    }

    static void AcceptFrame(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader frame,
        Av1SemanticValidationState state)
    {
        if (frame.ShowExistingFrame)
        {
            int slot = frame.FrameToShowMapIndex;
            if (frame.FrameType == Av1FrameType.Key)
            {
                state.ShownKeyFrames[slot] = true;
            }
        }
        else if (sequence.HasFrameIdNumbers())
        {
            state.PreviousFrameId = frame.CurrentFrameId;
            state.HasPreviousFrameId = true;
        }

        state.RequiredRandomAccessKind = Av1RandomAccessKind.None;
        state.ValidatedFrameCount++;
    }

    static bool HasValidReferenceState(Av1FrameHeaderState references)
    {
        return references.ReferenceValid != null && references.ReferenceValid.Length == 8
            && references.ReferenceFrameTypes != null && references.ReferenceFrameTypes.Length == 8
            && references.ReferenceShowable != null && references.ReferenceShowable.Length == 8
            && references.ReferenceOrderHints != null && references.ReferenceOrderHints.Length == 8
            && references.ReferenceFrameIds != null && references.ReferenceFrameIds.Length == 8
            && references.ReferenceUpscaledWidths != null
            && references.ReferenceUpscaledWidths.Length == 8
            && references.ReferenceFrameHeights != null
            && references.ReferenceFrameHeights.Length == 8;
    }

    static bool HasValidFrameState(
        ParsedAv1FrameHeader frame,
        Av1SemanticValidationState state)
    {
        return frame.ReferenceFrameIndexes != null
            && frame.ReferenceFrameIndexes.Length == Av1SyntaxConstants.RefsPerFrame
            && frame.DeltaFrameIds != null
            && frame.DeltaFrameIds.Length == Av1SyntaxConstants.RefsPerFrame
            && frame.SkipModeFrames != null && frame.SkipModeFrames.Length == 2
            && state.ShownKeyFrames != null
            && state.ShownKeyFrames.Length == Av1SyntaxConstants.StoredReferenceFrames;
    }

    static bool HasUniformTileStarts(
        int[] starts,
        int count,
        int superblockCount,
        int countLog2,
        int superblockShift,
        int finalMi)
    {
        int tileSize = (superblockCount + (1 << countLog2) - 1) >> countLog2;
        int start = 0;
        int index = 0;
        while (start < superblockCount && index < count)
        {
            if (starts[index] != start << superblockShift)
            {
                return false;
            }

            start += tileSize;
            index++;
        }

        return index == count && start >= superblockCount && starts[count] == finalMi;
    }

    static int RelativeDistance(int first, int second, int orderHintBits)
    {
        int difference = first - second;
        int sign = 1 << (orderHintBits - 1);
        return (difference & (sign - 1)) - (difference & sign);
    }

    static int TileLog2(int blockSize, int target)
    {
        int value = 0;
        while ((blockSize << value) < target)
        {
            value++;
        }

        return value;
    }

    static void Report(
        DiagnosticLog diagnostics,
        string syntax,
        string message,
        string standardReference)
    {
        diagnostics.Context.PushAv1Syntax(syntax);
        diagnostics.Context.SetStandardReference(standardReference);
        diagnostics.Add(
            AvifResultCode.InvalidAv1Bitstream,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Validation,
            message);
        diagnostics.Context.PopAv1Syntax();
        diagnostics.Context.SetStandardReference(string.Empty);
    }

    static void ReportArgument(DiagnosticLog diagnostics, string message)
    {
        diagnostics.Add(
            AvifResultCode.InvalidArgument,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Api,
            message);
    }

    static Av1SemanticValidationResult CreateResult(DiagnosticLog diagnostics)
    {
        return new Av1SemanticValidationResult(diagnostics.Code, diagnostics);
    }
}
