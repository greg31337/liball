using System;

namespace avif;

// AV1 specification sections 5.9.2 through 5.9.30.
internal static class Av1FrameHeaderParser
{
    internal static Av1FrameHeaderParseResult ParsePayload(
        ReadOnlySpan<byte> payload,
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        int temporalId,
        int spatialId,
        bool validateTrailingBits)
    {
        if (sequence == null || state == null
            || temporalId < 0 || temporalId >= Av1SyntaxConstants.MaxTemporalLayers
            || spatialId < 0 || spatialId >= Av1SyntaxConstants.MaxSpatialLayers)
        {
            return new Av1FrameHeaderParseResult(AvifResultCode.InvalidArgument, null);
        }

        BitReader reader = BitReader.Create(payload.Length);
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.TemporalId = temporalId;
        header.SpatialId = spatialId;
        int allFrames = (1 << Av1SyntaxConstants.StoredReferenceFrames) - 1;
        if (sequence.HasReducedStillPictureHeader())
        {
            header.ShowExistingFrame = false;
            header.FrameType = Av1FrameType.Key;
            header.FrameIsIntra = true;
            header.ShowFrame = true;
            header.ShowableFrame = false;
            header.ErrorResilientMode = true;
        }
        else
        {
            header.ShowExistingFrame = reader.ReadBit(payload) != 0;
            if (header.ShowExistingFrame)
            {
                header.FrameToShowMapIndex = (int)reader.ReadBits(payload, 3);
                ReadTemporalPoint(payload, reader, sequence, header);
                if (sequence.HasFrameIdNumbers())
                {
                    header.DisplayFrameId =
                        (int)reader.ReadBits(payload, sequence.GetFrameIdLength());
                }

                if (reader.Code != AvifResultCode.Success)
                {
                    return Fail(reader.Code);
                }

                if (!state.ReferenceValid[header.FrameToShowMapIndex])
                {
                    return Fail(AvifResultCode.InvalidAv1Bitstream);
                }

                header.FrameType = state.ReferenceFrameTypes[header.FrameToShowMapIndex];
                header.FrameIsIntra = header.FrameType == Av1FrameType.Key
                    || header.FrameType == Av1FrameType.IntraOnly;
                header.ShowFrame = true;
                header.ShowableFrame = false;
                header.RefreshFrameFlags = header.FrameType == Av1FrameType.Key
                    ? allFrames
                    : 0;
                return Finish(payload, reader, header, validateTrailingBits);
            }

            header.FrameType = (Av1FrameType)reader.ReadBits(payload, 2);
            header.FrameIsIntra = header.FrameType == Av1FrameType.Key
                || header.FrameType == Av1FrameType.IntraOnly;
            header.ShowFrame = reader.ReadBit(payload) != 0;
            if (header.ShowFrame)
            {
                ReadTemporalPoint(payload, reader, sequence, header);
                header.ShowableFrame = header.FrameType != Av1FrameType.Key;
            }
            else
            {
                header.ShowableFrame = reader.ReadBit(payload) != 0;
            }

            if (header.FrameType == Av1FrameType.Switch
                || (header.FrameType == Av1FrameType.Key && header.ShowFrame))
            {
                header.ErrorResilientMode = true;
            }
            else
            {
                header.ErrorResilientMode = reader.ReadBit(payload) != 0;
            }
        }

        if (header.FrameType == Av1FrameType.Key && header.ShowFrame)
        {
            state.ClearReferences();
        }

        header.DisableCdfUpdate = reader.ReadBit(payload) != 0;
        int screenMode = sequence.GetScreenContentToolsMode();
        header.AllowScreenContentTools = screenMode ==
            Av1SyntaxConstants.SelectScreenContentTools
            ? reader.ReadBit(payload) != 0
            : screenMode != 0;
        if (header.AllowScreenContentTools)
        {
            int integerMode = sequence.GetIntegerMotionVectorMode();
            header.ForceIntegerMotionVectors = integerMode ==
                Av1SyntaxConstants.SelectIntegerMotionVector
                ? reader.ReadBit(payload) != 0
                : integerMode != 0;
        }

        if (header.FrameIsIntra)
        {
            header.ForceIntegerMotionVectors = true;
        }

        if (sequence.HasFrameIdNumbers())
        {
            header.CurrentFrameId =
                (int)reader.ReadBits(payload, sequence.GetFrameIdLength());
            MarkReferenceFrames(sequence, state, header.CurrentFrameId);
        }

        if (header.FrameType == Av1FrameType.Switch)
        {
            header.FrameSizeOverride = true;
        }
        else if (!sequence.HasReducedStillPictureHeader())
        {
            header.FrameSizeOverride = reader.ReadBit(payload) != 0;
        }

        header.OrderHint = (int)reader.ReadBits(payload, sequence.GetOrderHintBits());
        if (header.FrameIsIntra || header.ErrorResilientMode)
        {
            header.PrimaryReferenceFrame = 7;
        }
        else
        {
            header.PrimaryReferenceFrame = (int)reader.ReadBits(payload, 3);
        }

        ReadBufferRemovalTimes(
            payload,
            reader,
            sequence,
            header,
            temporalId,
            spatialId);

        if (header.FrameType == Av1FrameType.Switch
            || (header.FrameType == Av1FrameType.Key && header.ShowFrame))
        {
            header.RefreshFrameFlags = allFrames;
        }
        else
        {
            header.RefreshFrameFlags = (int)reader.ReadBits(payload, 8);
        }

        if ((!header.FrameIsIntra || header.RefreshFrameFlags != allFrames)
            && header.ErrorResilientMode
            && sequence.IsOrderHintEnabled())
        {
            int index = 0;
            while (index < Av1SyntaxConstants.StoredReferenceFrames)
            {
                int hint = (int)reader.ReadBits(payload, sequence.GetOrderHintBits());
                if (hint != state.ReferenceOrderHints[index])
                {
                    state.ReferenceValid[index] = false;
                }

                index++;
            }
        }

        if (header.FrameIsIntra)
        {
            header.Size = ReadFrameSize(payload, reader, sequence, header.FrameSizeOverride);
            ReadRenderSize(payload, reader, header.Size);
            if (header.AllowScreenContentTools
                && header.Size.UpscaledWidth == header.Size.FrameWidth)
            {
                header.AllowIntraBlockCopy = reader.ReadBit(payload) != 0;
            }
        }
        else
        {
            ReadReferences(payload, reader, sequence, state, header);
            if (header.FrameSizeOverride && !header.ErrorResilientMode)
            {
                header.Size = ReadFrameSizeWithReferences(
                    payload,
                    reader,
                    sequence,
                    state,
                    header);
            }
            else
            {
                header.Size = ReadFrameSize(payload, reader, sequence, header.FrameSizeOverride);
                ReadRenderSize(payload, reader, header.Size);
            }

            if (!header.ForceIntegerMotionVectors)
            {
                header.AllowHighPrecisionMotionVectors = reader.ReadBit(payload) != 0;
            }

            bool filterSwitchable = reader.ReadBit(payload) != 0;
            header.InterpolationFilter = filterSwitchable
                ? Av1InterpolationFilter.Switchable
                : (Av1InterpolationFilter)reader.ReadBits(payload, 2);
            header.MotionModeSwitchable = reader.ReadBit(payload) != 0;
            if (!header.ErrorResilientMode
                && sequence.AreReferenceFrameMotionVectorsEnabled())
            {
                header.UseReferenceFrameMotionVectors = reader.ReadBit(payload) != 0;
            }
        }

        if (sequence.HasReducedStillPictureHeader() || header.DisableCdfUpdate)
        {
            header.DisableFrameEndUpdateCdf = true;
        }
        else
        {
            header.DisableFrameEndUpdateCdf = reader.ReadBit(payload) != 0;
        }

        if (reader.Code != AvifResultCode.Success || header.Size == null)
        {
            return Fail(reader.Code);
        }

        if (header.PrimaryReferenceFrame != 7)
        {
            int primarySlot = header.ReferenceFrameIndexes[
                header.PrimaryReferenceFrame];
            if (!state.LoadReferenceState(primarySlot))
            {
                return Fail(AvifResultCode.InvalidAv1Bitstream);
            }
        }

        header.Tiles = ReadTileInfo(payload, reader, sequence, header.Size);
        header.Quantization = ReadQuantization(payload, reader, sequence);
        header.Segmentation = ReadSegmentation(
            payload,
            reader,
            state,
            header.PrimaryReferenceFrame);
        ReadDeltaQuantizerAndLoopFilter(payload, reader, header);
        ComputeLossless(header);
        header.LoopFilter = ReadLoopFilter(payload, reader, sequence, state, header);
        header.Cdef = ReadCdef(payload, reader, sequence, header);
        header.LoopRestoration = ReadLoopRestoration(payload, reader, sequence, header);
        header.TransformMode = header.CodedLossless
            ? Av1TransformMode.Only4x4
            : (reader.ReadBit(payload) != 0
                ? Av1TransformMode.Select
                : Av1TransformMode.Largest);
        if (!header.FrameIsIntra)
        {
            header.ReferenceSelect = reader.ReadBit(payload) != 0;
        }

        ReadSkipMode(payload, reader, sequence, state, header);
        if (!header.FrameIsIntra
            && !header.ErrorResilientMode
            && sequence.IsWarpedMotionEnabled())
        {
            header.AllowWarpedMotion = reader.ReadBit(payload) != 0;
        }

        header.ReducedTransformSet = reader.ReadBit(payload) != 0;
        header.GlobalMotion = ReadGlobalMotion(payload, reader, state, header);
        header.FilmGrain = ReadFilmGrain(payload, reader, sequence, state, header);

        if (reader.Code != AvifResultCode.Success)
        {
            return Fail(reader.Code);
        }

        return Finish(payload, reader, header, validateTrailingBits);
    }

    static void ReadTemporalPoint(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header)
    {
        if (!sequence.HasDecoderModelInfo() || !sequence.HasTimingInfo())
        {
            return;
        }

        if (!(sequence.GetTimingInfo() ?? throw new InvalidOperationException("Invalid internal state.")).EqualPictureInterval)
        {
            header.FramePresentationTimePresent = true;
            header.FramePresentationTime = reader.ReadBits(
                payload,
                (sequence.GetDecoderModelInfo() ?? throw new InvalidOperationException("Invalid internal state.")).FramePresentationTimeLength);
        }
    }

    static void ReadBufferRemovalTimes(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        int temporalId,
        int spatialId)
    {
        if (!sequence.HasDecoderModelInfo())
        {
            return;
        }

        header.BufferRemovalTimePresent = reader.ReadBit(payload) != 0;
        if (!header.BufferRemovalTimePresent)
        {
            return;
        }

        int index = 0;
        while (index < sequence.GetOperatingPointCount())
        {
            Av1OperatingPoint? point = sequence.GetOperatingPoint(index);
            if ((point ?? throw new InvalidOperationException("Invalid internal state.")).DecoderModelPresent)
            {
                bool inTemporalLayer = (point.Idc & (1 << temporalId)) != 0;
                bool inSpatialLayer = (point.Idc & (1 << (spatialId + 8))) != 0;
                if (point.Idc == 0 || (inTemporalLayer && inSpatialLayer))
                {
                    header.BufferRemovalTimes[index] = reader.ReadBits(
                        payload,
                        (sequence.GetDecoderModelInfo() ?? throw new InvalidOperationException("Invalid internal state.")).BufferRemovalTimeLength);
                }
            }

            index++;
        }
    }

    static void MarkReferenceFrames(
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        int currentFrameId)
    {
        int idLength = sequence.GetFrameIdLength();
        int differenceLength = sequence.GetDeltaFrameIdLength();
        int idRange = 1 << idLength;
        int differenceRange = 1 << differenceLength;
        int index = 0;
        while (index < state.ReferenceValid.Length)
        {
            int difference = currentFrameId - state.ReferenceFrameIds[index];
            if (difference < 0)
            {
                difference += idRange;
            }

            if (difference == 0 || difference >= differenceRange)
            {
                state.ReferenceValid[index] = false;
            }

            index++;
        }
    }

    static void ReadReferences(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        if (sequence.IsOrderHintEnabled())
        {
            header.FrameReferencesShortSignaling = reader.ReadBit(payload) != 0;
        }

        if (header.FrameReferencesShortSignaling)
        {
            header.LastFrameIndex = (int)reader.ReadBits(payload, 3);
            header.GoldenFrameIndex = (int)reader.ReadBits(payload, 3);
            SetShortReferences(sequence, state, header);
        }

        int index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            if (!header.FrameReferencesShortSignaling)
            {
                header.ReferenceFrameIndexes[index] = (int)reader.ReadBits(payload, 3);
            }

            if (sequence.HasFrameIdNumbers())
            {
                header.DeltaFrameIds[index] = (int)reader.ReadBits(
                    payload,
                    sequence.GetDeltaFrameIdLength()) + 1;
            }

            index++;
        }
    }

    static void SetShortReferences(
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        int reference = 0;
        while (reference < header.ReferenceFrameIndexes.Length)
        {
            header.ReferenceFrameIndexes[reference] = -1;
            reference++;
        }

        bool[] used = new bool[Av1SyntaxConstants.StoredReferenceFrames];
        header.ReferenceFrameIndexes[0] = header.LastFrameIndex;
        header.ReferenceFrameIndexes[3] = header.GoldenFrameIndex;
        used[header.LastFrameIndex] = true;
        used[header.GoldenFrameIndex] = true;

        int orderHintBits = sequence.GetOrderHintBits();
        int currentHint = 1 << (orderHintBits - 1);
        int[] shiftedHints = new int[Av1SyntaxConstants.StoredReferenceFrames];
        int index = 0;
        while (index < shiftedHints.Length)
        {
            shiftedHints[index] = currentHint
                + RelativeDistance(
                    state.ReferenceOrderHints[index],
                    header.OrderHint,
                    orderHintBits);
            index++;
        }

        int selected = FindBackwardReference(shiftedHints, used, currentHint, true);
        SetSelectedReference(header, used, 6, selected);
        selected = FindBackwardReference(shiftedHints, used, currentHint, false);
        SetSelectedReference(header, used, 4, selected);
        selected = FindBackwardReference(shiftedHints, used, currentHint, false);
        SetSelectedReference(header, used, 5, selected);

        int[] forwardOrder = new int[] { 1, 2, 4, 5, 6 };
        index = 0;
        while (index < forwardOrder.Length)
        {
            int frame = forwardOrder[index];
            if (header.ReferenceFrameIndexes[frame] < 0)
            {
                selected = FindForwardReference(shiftedHints, used, currentHint);
                SetSelectedReference(header, used, frame, selected);
            }

            index++;
        }

        selected = FindEarliestReference(shiftedHints);
        index = 0;
        while (index < header.ReferenceFrameIndexes.Length)
        {
            if (header.ReferenceFrameIndexes[index] < 0)
            {
                header.ReferenceFrameIndexes[index] = selected;
            }

            index++;
        }
    }

    static int FindBackwardReference(
        int[] shiftedHints,
        bool[] used,
        int currentHint,
        bool latest)
    {
        int selected = -1;
        int index = 0;
        while (index < shiftedHints.Length)
        {
            int hint = shiftedHints[index];
            bool better = selected < 0
                || (latest ? hint >= shiftedHints[selected] : hint < shiftedHints[selected]);
            if (!used[index] && hint >= currentHint && better)
            {
                selected = index;
            }

            index++;
        }

        return selected;
    }

    static int FindForwardReference(int[] shiftedHints, bool[] used, int currentHint)
    {
        int selected = -1;
        int index = 0;
        while (index < shiftedHints.Length)
        {
            int hint = shiftedHints[index];
            if (!used[index] && hint < currentHint
                && (selected < 0 || hint >= shiftedHints[selected]))
            {
                selected = index;
            }

            index++;
        }

        return selected;
    }

    static int FindEarliestReference(int[] shiftedHints)
    {
        int selected = 0;
        int index = 1;
        while (index < shiftedHints.Length)
        {
            if (shiftedHints[index] < shiftedHints[selected])
            {
                selected = index;
            }

            index++;
        }

        return selected;
    }

    static void SetSelectedReference(
        ParsedAv1FrameHeader header,
        bool[] used,
        int frame,
        int selected)
    {
        if (selected >= 0)
        {
            header.ReferenceFrameIndexes[frame] = selected;
            used[selected] = true;
        }
    }

    static Av1FrameSize ReadFrameSize(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        bool overrideSize)
    {
        Av1FrameSize size = new Av1FrameSize();
        int upscaledWidth = (int)sequence.GetMaximumWidth();
        int frameHeight = (int)sequence.GetMaximumHeight();
        if (overrideSize)
        {
            upscaledWidth = (int)reader.ReadBits(payload, sequence.GetFrameWidthBits()) + 1;
            frameHeight = (int)reader.ReadBits(payload, sequence.GetFrameHeightBits()) + 1;
        }

        size.UpscaledWidth = upscaledWidth;
        size.FrameHeight = frameHeight;
        ReadSuperres(payload, reader, sequence, size);
        ComputeImageSize(size);
        return size;
    }

    static Av1FrameSize ReadFrameSizeWithReferences(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        int index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            bool found = reader.ReadBit(payload) != 0;
            if (found)
            {
                int reference = header.ReferenceFrameIndexes[index];
                Av1FrameSize size = new Av1FrameSize();
                size.UpscaledWidth = state.ReferenceUpscaledWidths[reference];
                size.FrameWidth = size.UpscaledWidth;
                size.FrameHeight = state.ReferenceFrameHeights[reference];
                size.RenderWidth = state.ReferenceRenderWidths[reference];
                size.RenderHeight = state.ReferenceRenderHeights[reference];
                ReadSuperres(payload, reader, sequence, size);
                ComputeImageSize(size);
                return size;
            }

            index++;
        }

        Av1FrameSize explicitSize = ReadFrameSize(payload, reader, sequence, true);
        ReadRenderSize(payload, reader, explicitSize);
        return explicitSize;
    }

    static void ReadSuperres(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameSize size)
    {
        size.UsesSuperres = sequence.IsSuperresEnabled()
            && reader.ReadBit(payload) != 0;
        size.SuperresDenominator = size.UsesSuperres
            ? (int)reader.ReadBits(payload, Av1SyntaxConstants.SuperresDenominatorBits)
                + Av1SyntaxConstants.SuperresMinimumDenominator
            : Av1SyntaxConstants.SuperresNumerator;
        size.FrameWidth = (size.UpscaledWidth * Av1SyntaxConstants.SuperresNumerator
            + size.SuperresDenominator / 2) / size.SuperresDenominator;
    }

    static void ReadRenderSize(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        Av1FrameSize size)
    {
        bool different = reader.ReadBit(payload) != 0;
        if (different)
        {
            size.RenderWidth = (int)reader.ReadBits(payload, 16) + 1;
            size.RenderHeight = (int)reader.ReadBits(payload, 16) + 1;
        }
        else
        {
            size.RenderWidth = size.UpscaledWidth;
            size.RenderHeight = size.FrameHeight;
        }
    }

    static void ComputeImageSize(Av1FrameSize size)
    {
        size.MiColumns = 2 * ((size.FrameWidth + 7) >> 3);
        size.MiRows = 2 * ((size.FrameHeight + 7) >> 3);
    }

    static Av1TileInfo ReadTileInfo(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameSize size)
    {
        Av1TileInfo tiles = new Av1TileInfo();
        int superblockShift = sequence.Uses128x128Superblocks() ? 5 : 4;
        int superblockSizeLog2 = superblockShift + 2;
        int superblockColumns = sequence.Uses128x128Superblocks()
            ? (size.MiColumns + 31) >> 5
            : (size.MiColumns + 15) >> 4;
        int superblockRows = sequence.Uses128x128Superblocks()
            ? (size.MiRows + 31) >> 5
            : (size.MiRows + 15) >> 4;
        int maxTileWidthSb = Av1SyntaxConstants.MaxTileWidth >> superblockSizeLog2;
        int maxTileAreaSb = Av1SyntaxConstants.MaxTileArea >> (2 * superblockSizeLog2);
        int minLog2TileColumns = TileLog2(maxTileWidthSb, superblockColumns);
        int maxLog2TileColumns = TileLog2(
            1,
            Minimum(superblockColumns, Av1SyntaxConstants.MaxTileColumns));
        int maxLog2TileRows = TileLog2(
            1,
            Minimum(superblockRows, Av1SyntaxConstants.MaxTileRows));
        int minLog2Tiles = Maximum(
            minLog2TileColumns,
            TileLog2(maxTileAreaSb, superblockRows * superblockColumns));
        int[] columnStarts = new int[Av1SyntaxConstants.MaxTileColumns + 1];
        int[] rowStarts = new int[Av1SyntaxConstants.MaxTileRows + 1];
        tiles.UniformSpacing = reader.ReadBit(payload) != 0;
        if (tiles.UniformSpacing)
        {
            tiles.TileColumnsLog2 = minLog2TileColumns;
            while (tiles.TileColumnsLog2 < maxLog2TileColumns
                && reader.ReadBit(payload) != 0)
            {
                tiles.TileColumnsLog2++;
            }

            int tileWidthSb = (superblockColumns + (1 << tiles.TileColumnsLog2) - 1)
                >> tiles.TileColumnsLog2;
            int start = 0;
            int count = 0;
            while (start < superblockColumns)
            {
                columnStarts[count] = start << superblockShift;
                count++;
                start += tileWidthSb;
            }

            columnStarts[count] = size.MiColumns;
            tiles.TileColumns = count;
            int minLog2TileRows = Maximum(minLog2Tiles - tiles.TileColumnsLog2, 0);
            tiles.TileRowsLog2 = minLog2TileRows;
            while (tiles.TileRowsLog2 < maxLog2TileRows
                && reader.ReadBit(payload) != 0)
            {
                tiles.TileRowsLog2++;
            }

            int tileHeightSb = (superblockRows + (1 << tiles.TileRowsLog2) - 1)
                >> tiles.TileRowsLog2;
            start = 0;
            count = 0;
            while (start < superblockRows)
            {
                rowStarts[count] = start << superblockShift;
                count++;
                start += tileHeightSb;
            }

            rowStarts[count] = size.MiRows;
            tiles.TileRows = count;
        }
        else
        {
            int widestTileSb = 0;
            int start = 0;
            int count = 0;
            while (start < superblockColumns)
            {
                columnStarts[count] = start << superblockShift;
                int maxWidth = Minimum(superblockColumns - start, maxTileWidthSb);
                int tileWidth = (int)reader.ReadNs(payload, (uint)maxWidth) + 1;
                widestTileSb = Maximum(widestTileSb, tileWidth);
                start += tileWidth;
                count++;
            }

            columnStarts[count] = size.MiColumns;
            tiles.TileColumns = count;
            tiles.TileColumnsLog2 = TileLog2(1, count);
            if (minLog2Tiles > 0)
            {
                maxTileAreaSb = superblockRows * superblockColumns
                    >> (minLog2Tiles + 1);
            }
            else
            {
                maxTileAreaSb = superblockRows * superblockColumns;
            }

            int maxTileHeightSb = Maximum(maxTileAreaSb / widestTileSb, 1);
            start = 0;
            count = 0;
            while (start < superblockRows)
            {
                rowStarts[count] = start << superblockShift;
                int maxHeight = Minimum(superblockRows - start, maxTileHeightSb);
                int tileHeight = (int)reader.ReadNs(payload, (uint)maxHeight) + 1;
                start += tileHeight;
                count++;
            }

            rowStarts[count] = size.MiRows;
            tiles.TileRows = count;
            tiles.TileRowsLog2 = TileLog2(1, count);
        }

        tiles.MiColumnStarts = CopyPrefix(columnStarts, tiles.TileColumns + 1);
        tiles.MiRowStarts = CopyPrefix(rowStarts, tiles.TileRows + 1);
        if (tiles.TileColumnsLog2 > 0 || tiles.TileRowsLog2 > 0)
        {
            tiles.ContextUpdateTileId = (int)reader.ReadBits(
                payload,
                tiles.TileColumnsLog2 + tiles.TileRowsLog2);
            tiles.TileSizeBytes = (int)reader.ReadBits(payload, 2) + 1;
        }

        return tiles;
    }

    static Av1QuantizationParameters ReadQuantization(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence)
    {
        Av1QuantizationParameters quantization = new Av1QuantizationParameters();
        quantization.BaseQIndex = (int)reader.ReadBits(payload, 8);
        quantization.DeltaYDc = ReadDeltaQ(payload, reader);
        if (sequence.GetPlaneCount() > 1)
        {
            if (sequence.HasSeparateUvDeltaQuantizer())
            {
                quantization.DifferentUvDelta = reader.ReadBit(payload) != 0;
            }

            quantization.DeltaUDc = ReadDeltaQ(payload, reader);
            quantization.DeltaUAc = ReadDeltaQ(payload, reader);
            if (quantization.DifferentUvDelta)
            {
                quantization.DeltaVDc = ReadDeltaQ(payload, reader);
                quantization.DeltaVAc = ReadDeltaQ(payload, reader);
            }
            else
            {
                quantization.DeltaVDc = quantization.DeltaUDc;
                quantization.DeltaVAc = quantization.DeltaUAc;
            }
        }

        quantization.UsesQuantizerMatrix = reader.ReadBit(payload) != 0;
        if (quantization.UsesQuantizerMatrix)
        {
            quantization.MatrixLevelY = (int)reader.ReadBits(payload, 4);
            quantization.MatrixLevelU = (int)reader.ReadBits(payload, 4);
            quantization.MatrixLevelV = sequence.HasSeparateUvDeltaQuantizer()
                ? (int)reader.ReadBits(payload, 4)
                : quantization.MatrixLevelU;
        }

        return quantization;
    }

    static int ReadDeltaQ(ReadOnlySpan<byte> payload, BitReader reader)
    {
        return reader.ReadBit(payload) != 0
            ? (int)reader.ReadSignedBits(payload, 7)
            : 0;
    }

    static Av1SegmentationParameters ReadSegmentation(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        Av1FrameHeaderState state,
        int primaryReferenceFrame)
    {
        Av1SegmentationParameters segmentation = new Av1SegmentationParameters();
        segmentation.FeatureEnabled = new bool[Av1SyntaxConstants.MaxSegments][];
        segmentation.FeatureData = new int[Av1SyntaxConstants.MaxSegments][];
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            segmentation.FeatureEnabled[segment] =
                new bool[Av1SyntaxConstants.SegmentFeatureCount];
            segmentation.FeatureData[segment] =
                new int[Av1SyntaxConstants.SegmentFeatureCount];
            segment++;
        }

        segmentation.Enabled = reader.ReadBit(payload) != 0;
        if (segmentation.Enabled)
        {
            if (primaryReferenceFrame == 7)
            {
                segmentation.UpdateMap = true;
                segmentation.TemporalUpdate = false;
                segmentation.UpdateData = true;
            }
            else
            {
                segmentation.UpdateMap = reader.ReadBit(payload) != 0;
                if (segmentation.UpdateMap)
                {
                    segmentation.TemporalUpdate = reader.ReadBit(payload) != 0;
                }

                segmentation.UpdateData = reader.ReadBit(payload) != 0;
            }

            if (segmentation.UpdateData)
            {
                ReadSegmentationFeatures(payload, reader, segmentation);
            }
            else
            {
                CopySegmentationState(state, segmentation);
            }
        }

        UpdateSegmentationSummary(segmentation);
        return segmentation;
    }

    static void ReadSegmentationFeatures(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        Av1SegmentationParameters segmentation)
    {
        int[] bits = new int[] { 8, 6, 6, 6, 6, 3, 0, 0 };
        bool[] signed = new bool[]
        {
            true, true, true, true, true, false, false, false
        };
        int[] maximums = new int[] { 255, 63, 63, 63, 63, 7, 0, 0 };
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            int feature = 0;
            while (feature < Av1SyntaxConstants.SegmentFeatureCount)
            {
                bool enabled = reader.ReadBit(payload) != 0;
                (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] = enabled;
                if (enabled)
                {
                    int value = signed[feature]
                        ? (int)reader.ReadSignedBits(payload, bits[feature] + 1)
                        : (int)reader.ReadBits(payload, bits[feature]);
                    (segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] = signed[feature]
                        ? Clip(value, -maximums[feature], maximums[feature])
                        : Clip(value, 0, maximums[feature]);
                }

                feature++;
            }

            segment++;
        }
    }

    static void CopySegmentationState(
        Av1FrameHeaderState state,
        Av1SegmentationParameters segmentation)
    {
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            int feature = 0;
            while (feature < Av1SyntaxConstants.SegmentFeatureCount)
            {
                (segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] =
                    state.SegmentFeatureEnabled[segment][feature];
                (segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature] =
                    state.SegmentFeatureData[segment][feature];
                feature++;
            }

            segment++;
        }
    }

    static void UpdateSegmentationSummary(Av1SegmentationParameters segmentation)
    {
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            int feature = 0;
            while (feature < Av1SyntaxConstants.SegmentFeatureCount)
            {
                if ((segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment][feature])
                {
                    segmentation.LastActiveSegmentId = segment;
                    if (feature >= (int)Av1SegmentFeature.ReferenceFrame)
                    {
                        segmentation.SegmentIdPreSkip = true;
                    }
                }

                feature++;
            }

            segment++;
        }
    }

    static void ReadDeltaQuantizerAndLoopFilter(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1FrameHeader header)
    {
        if ((header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).BaseQIndex > 0)
        {
            header.Quantization.DeltaQPresent = reader.ReadBit(payload) != 0;
        }

        if (header.Quantization.DeltaQPresent)
        {
            header.Quantization.DeltaQResolution = (int)reader.ReadBits(payload, 2);
            if (!header.AllowIntraBlockCopy)
            {
                header.Quantization.DeltaLoopFilterPresent = reader.ReadBit(payload) != 0;
            }

            if (header.Quantization.DeltaLoopFilterPresent)
            {
                header.Quantization.DeltaLoopFilterResolution =
                    (int)reader.ReadBits(payload, 2);
                header.Quantization.DeltaLoopFilterMulti = reader.ReadBit(payload) != 0;
            }
        }
    }

    static void ComputeLossless(ParsedAv1FrameHeader header)
    {
        header.CodedLossless = true;
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            int qindex = (header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).BaseQIndex;
            if (((header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment]
                [(int)Av1SegmentFeature.AlternateQuantizer])
            {
                qindex += (header.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segment]
                    [(int)Av1SegmentFeature.AlternateQuantizer];
            }

            qindex = Clip(qindex, 0, Av1SyntaxConstants.MaxQuantizerIndex);
            bool lossless = qindex == 0
                && header.Quantization.DeltaYDc == 0
                && header.Quantization.DeltaUDc == 0
                && header.Quantization.DeltaUAc == 0
                && header.Quantization.DeltaVDc == 0
                && header.Quantization.DeltaVAc == 0;
            if (!lossless)
            {
                header.CodedLossless = false;
            }

            segment++;
        }

        header.AllLossless = header.CodedLossless
            && (header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameWidth == header.Size.UpscaledWidth;
    }

    static Av1LoopFilterParameters ReadLoopFilter(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        Av1LoopFilterParameters filter = new Av1LoopFilterParameters();
        filter.Levels = new int[4];
        filter.ReferenceDeltas = header.PrimaryReferenceFrame == 7
            ? new int[] { 1, 0, 0, 0, -1, 0, -1, -1 }
            : (int[])state.LoopFilterReferenceDeltas.Clone();
        filter.ModeDeltas = header.PrimaryReferenceFrame == 7
            ? new int[2]
            : (int[])state.LoopFilterModeDeltas.Clone();
        if (header.CodedLossless || header.AllowIntraBlockCopy)
        {
            return filter;
        }

        filter.Levels[0] = (int)reader.ReadBits(payload, 6);
        filter.Levels[1] = (int)reader.ReadBits(payload, 6);
        if (sequence.GetPlaneCount() > 1
            && (filter.Levels[0] != 0 || filter.Levels[1] != 0))
        {
            filter.Levels[2] = (int)reader.ReadBits(payload, 6);
            filter.Levels[3] = (int)reader.ReadBits(payload, 6);
        }

        filter.Sharpness = (int)reader.ReadBits(payload, 3);
        filter.DeltaEnabled = reader.ReadBit(payload) != 0;
        if (filter.DeltaEnabled)
        {
            filter.DeltaUpdate = reader.ReadBit(payload) != 0;
            if (filter.DeltaUpdate)
            {
                int index = 0;
                while (index < filter.ReferenceDeltas.Length)
                {
                    if (reader.ReadBit(payload) != 0)
                    {
                        filter.ReferenceDeltas[index] =
                            (int)reader.ReadSignedBits(payload, 7);
                    }

                    index++;
                }

                index = 0;
                while (index < filter.ModeDeltas.Length)
                {
                    if (reader.ReadBit(payload) != 0)
                    {
                        filter.ModeDeltas[index] =
                            (int)reader.ReadSignedBits(payload, 7);
                    }

                    index++;
                }
            }
        }

        return filter;
    }

    static Av1CdefParameters ReadCdef(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header)
    {
        Av1CdefParameters cdef = new Av1CdefParameters();
        cdef.Damping = 3;
        cdef.YPrimaryStrengths = new int[1];
        cdef.YSecondaryStrengths = new int[1];
        cdef.UvPrimaryStrengths = new int[1];
        cdef.UvSecondaryStrengths = new int[1];
        if (header.CodedLossless || header.AllowIntraBlockCopy || !sequence.IsCdefEnabled())
        {
            return cdef;
        }

        cdef.Damping = (int)reader.ReadBits(payload, 2) + 3;
        cdef.Bits = (int)reader.ReadBits(payload, 2);
        int count = 1 << cdef.Bits;
        cdef.YPrimaryStrengths = new int[count];
        cdef.YSecondaryStrengths = new int[count];
        cdef.UvPrimaryStrengths = new int[count];
        cdef.UvSecondaryStrengths = new int[count];
        int index = 0;
        while (index < count)
        {
            cdef.YPrimaryStrengths[index] = (int)reader.ReadBits(payload, 4);
            int secondary = (int)reader.ReadBits(payload, 2);
            cdef.YSecondaryStrengths[index] = secondary == 3 ? 4 : secondary;
            if (sequence.GetPlaneCount() > 1)
            {
                cdef.UvPrimaryStrengths[index] = (int)reader.ReadBits(payload, 4);
                secondary = (int)reader.ReadBits(payload, 2);
                cdef.UvSecondaryStrengths[index] = secondary == 3 ? 4 : secondary;
            }

            index++;
        }

        return cdef;
    }

    static Av1LoopRestorationParameters ReadLoopRestoration(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header)
    {
        Av1LoopRestorationParameters restoration = new Av1LoopRestorationParameters();
        restoration.Types = new Av1RestorationType[]
        {
            Av1RestorationType.None,
            Av1RestorationType.None,
            Av1RestorationType.None
        };
        restoration.UnitSizes = new int[3];
        if (header.AllLossless || header.AllowIntraBlockCopy
            || !sequence.IsRestorationEnabled())
        {
            return restoration;
        }

        Av1RestorationType[] remap = new Av1RestorationType[]
        {
            Av1RestorationType.None,
            Av1RestorationType.Switchable,
            Av1RestorationType.Wiener,
            Av1RestorationType.Sgrproj
        };
        bool usesChroma = false;
        int plane = 0;
        while (plane < sequence.GetPlaneCount())
        {
            restoration.Types[plane] = remap[(int)reader.ReadBits(payload, 2)];
            if (restoration.Types[plane] != Av1RestorationType.None)
            {
                restoration.UsesRestoration = true;
                if (plane > 0)
                {
                    usesChroma = true;
                }
            }

            plane++;
        }

        if (restoration.UsesRestoration)
        {
            int unitShift = reader.ReadBit(payload);
            if (sequence.Uses128x128Superblocks())
            {
                unitShift++;
            }
            else if (unitShift != 0)
            {
                unitShift += reader.ReadBit(payload);
            }

            restoration.UnitSizes[0] = Av1SyntaxConstants.MaxRestorationTileSize
                >> (2 - unitShift);
            int uvShift = sequence.HasChromaSubsamplingX()
                && sequence.HasChromaSubsamplingY()
                && usesChroma
                ? reader.ReadBit(payload)
                : 0;
            restoration.UnitSizes[1] = restoration.UnitSizes[0] >> uvShift;
            restoration.UnitSizes[2] = restoration.UnitSizes[0] >> uvShift;
        }

        return restoration;
    }

    static void ReadSkipMode(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        if (header.FrameIsIntra || !header.ReferenceSelect || !sequence.IsOrderHintEnabled())
        {
            return;
        }

        int forwardIndex = -1;
        int backwardIndex = -1;
        int forwardHint = 0;
        int backwardHint = 0;
        int index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            int hint = state.ReferenceOrderHints[header.ReferenceFrameIndexes[index]];
            int distance = RelativeDistance(hint, header.OrderHint, sequence.GetOrderHintBits());
            if (distance < 0
                && (forwardIndex < 0
                    || RelativeDistance(hint, forwardHint, sequence.GetOrderHintBits()) > 0))
            {
                forwardIndex = index;
                forwardHint = hint;
            }
            else if (distance > 0
                && (backwardIndex < 0
                    || RelativeDistance(hint, backwardHint, sequence.GetOrderHintBits()) < 0))
            {
                backwardIndex = index;
                backwardHint = hint;
            }

            index++;
        }

        int secondForwardIndex = -1;
        int secondForwardHint = 0;
        if (forwardIndex >= 0 && backwardIndex < 0)
        {
            index = 0;
            while (index < Av1SyntaxConstants.RefsPerFrame)
            {
                int hint = state.ReferenceOrderHints[header.ReferenceFrameIndexes[index]];
                if (RelativeDistance(hint, forwardHint, sequence.GetOrderHintBits()) < 0
                    && (secondForwardIndex < 0
                        || RelativeDistance(
                            hint,
                            secondForwardHint,
                            sequence.GetOrderHintBits()) > 0))
                {
                    secondForwardIndex = index;
                    secondForwardHint = hint;
                }

                index++;
            }
        }

        bool allowed = forwardIndex >= 0
            && (backwardIndex >= 0 || secondForwardIndex >= 0);
        if (allowed)
        {
            int otherIndex = backwardIndex >= 0 ? backwardIndex : secondForwardIndex;
            header.SkipModeFrames[0] = 1 + Minimum(forwardIndex, otherIndex);
            header.SkipModeFrames[1] = 1 + Maximum(forwardIndex, otherIndex);
            header.SkipModePresent = reader.ReadBit(payload) != 0;
        }
    }

    static Av1GlobalMotionParameters ReadGlobalMotion(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        Av1GlobalMotionParameters global = new Av1GlobalMotionParameters();
        global.Types = new Av1WarpModelType[Av1SyntaxConstants.TotalRefsPerFrame];
        global.Parameters = new int[Av1SyntaxConstants.TotalRefsPerFrame][];
        int reference = 0;
        while (reference < Av1SyntaxConstants.TotalRefsPerFrame)
        {
            global.Types[reference] = Av1WarpModelType.Identity;
            global.Parameters[reference] = Av1FrameHeaderState.CreateIdentityGlobalParameters();
            reference++;
        }

        if (header.FrameIsIntra)
        {
            return global;
        }

        reference = 1;
        while (reference < Av1SyntaxConstants.TotalRefsPerFrame)
        {
            bool isGlobal = reader.ReadBit(payload) != 0;
            Av1WarpModelType type = Av1WarpModelType.Identity;
            if (isGlobal)
            {
                bool rotZoom = reader.ReadBit(payload) != 0;
                if (rotZoom)
                {
                    type = Av1WarpModelType.RotZoom;
                }
                else
                {
                    type = reader.ReadBit(payload) != 0
                        ? Av1WarpModelType.Translation
                        : Av1WarpModelType.Affine;
                }
            }

            global.Types[reference] = type;
            int[] parameters = global.Parameters[reference];
            if (type == Av1WarpModelType.RotZoom || type == Av1WarpModelType.Affine)
            {
                parameters[2] = ReadGlobalParameter(
                    payload, reader, state, header, type, reference, 2);
                parameters[3] = ReadGlobalParameter(
                    payload, reader, state, header, type, reference, 3);
                if (type == Av1WarpModelType.Affine)
                {
                    parameters[4] = ReadGlobalParameter(
                        payload, reader, state, header, type, reference, 4);
                    parameters[5] = ReadGlobalParameter(
                        payload, reader, state, header, type, reference, 5);
                }
                else
                {
                    parameters[4] = -parameters[3];
                    parameters[5] = parameters[2];
                }
            }

            if (type != Av1WarpModelType.Identity)
            {
                parameters[0] = ReadGlobalParameter(
                    payload, reader, state, header, type, reference, 0);
                parameters[1] = ReadGlobalParameter(
                    payload, reader, state, header, type, reference, 1);
            }

            reference++;
        }

        return global;
    }

    static int ReadGlobalParameter(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header,
        Av1WarpModelType type,
        int reference,
        int parameterIndex)
    {
        int absoluteBits = Av1SyntaxConstants.GlobalMotionAbsoluteAlphaBits;
        int precisionBits = Av1SyntaxConstants.GlobalMotionAlphaPrecisionBits;
        if (parameterIndex < 2)
        {
            if (type == Av1WarpModelType.Translation)
            {
                int precisionReduction = header.AllowHighPrecisionMotionVectors ? 0 : 1;
                absoluteBits = Av1SyntaxConstants.GlobalMotionTranslationOnlyBits
                    - precisionReduction;
                precisionBits = Av1SyntaxConstants.GlobalMotionTranslationOnlyPrecisionBits
                    - precisionReduction;
            }
            else
            {
                absoluteBits = Av1SyntaxConstants.GlobalMotionAbsoluteTranslationBits;
                precisionBits = Av1SyntaxConstants.GlobalMotionTranslationPrecisionBits;
            }
        }

        int precisionDifference = Av1SyntaxConstants.WarpedModelPrecisionBits - precisionBits;
        int round = parameterIndex % 3 == 2
            ? 1 << Av1SyntaxConstants.WarpedModelPrecisionBits
            : 0;
        int subtract = parameterIndex % 3 == 2 ? 1 << precisionBits : 0;
        int maximum = 1 << absoluteBits;
        int referenceValue = (state.PreviousGlobalMotionParameters[reference][parameterIndex]
            >> precisionDifference) - subtract;
        int value = DecodeSignedSubexpWithReference(
            payload,
            reader,
            -maximum,
            maximum + 1,
            referenceValue);
        return (value << precisionDifference) + round;
    }

    static int DecodeSignedSubexpWithReference(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        int low,
        int high,
        int reference)
    {
        int value = DecodeUnsignedSubexpWithReference(
            payload,
            reader,
            high - low,
            reference - low);
        return value + low;
    }

    static int DecodeUnsignedSubexpWithReference(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        int maximum,
        int reference)
    {
        int value = DecodeSubexp(payload, reader, maximum);
        if ((reference << 1) <= maximum)
        {
            return InverseRecenter(reference, value);
        }

        return maximum - 1 - InverseRecenter(maximum - 1 - reference, value);
    }

    static int DecodeSubexp(ReadOnlySpan<byte> payload, BitReader reader, int symbolCount)
    {
        int index = 0;
        int consumed = 0;
        while (true)
        {
            int bitCount = index == 0 ? 3 : 3 + index - 1;
            int partSize = 1 << bitCount;
            if (symbolCount <= consumed + 3 * partSize)
            {
                return (int)reader.ReadNs(payload, (uint)(symbolCount - consumed)) + consumed;
            }

            if (reader.ReadBit(payload) != 0)
            {
                index++;
                consumed += partSize;
            }
            else
            {
                return (int)reader.ReadBits(payload, bitCount) + consumed;
            }
        }
    }

    static int InverseRecenter(int reference, int value)
    {
        if (value > 2 * reference)
        {
            return value;
        }

        if ((value & 1) != 0)
        {
            return reference - ((value + 1) >> 1);
        }

        return reference + (value >> 1);
    }

    static Av1FilmGrainParameters ReadFilmGrain(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1SequenceHeader sequence,
        Av1FrameHeaderState state,
        ParsedAv1FrameHeader header)
    {
        Av1FilmGrainParameters grain = new Av1FilmGrainParameters();
        grain.YPoints = System.Array.Empty<int[]>();
        grain.CbPoints = System.Array.Empty<int[]>();
        grain.CrPoints = System.Array.Empty<int[]>();
        grain.ArCoefficientsY = System.Array.Empty<int>();
        grain.ArCoefficientsCb = System.Array.Empty<int>();
        grain.ArCoefficientsCr = System.Array.Empty<int>();
        if (!sequence.HasFilmGrainParameters()
            || (!header.ShowFrame && !header.ShowableFrame))
        {
            return grain;
        }

        grain.ApplyGrain = reader.ReadBit(payload) != 0;
        if (!grain.ApplyGrain)
        {
            return grain;
        }

        grain.GrainSeed = (int)reader.ReadBits(payload, 16);
        grain.UpdateGrain = header.FrameType != Av1FrameType.Inter
            || reader.ReadBit(payload) != 0;
        if (!grain.UpdateGrain)
        {
            grain.ReferenceIndex = (int)reader.ReadBits(payload, 3);
            Av1FilmGrainParameters? stored =
                state.ReferenceFilmGrainParameters[grain.ReferenceIndex];
            if (stored != null)
            {
                int seed = grain.GrainSeed;
                int referenceIndex = grain.ReferenceIndex;
                grain = CopyFilmGrain(stored);
                grain.ApplyGrain = true;
                grain.GrainSeed = seed;
                grain.UpdateGrain = false;
                grain.ReferenceIndex = referenceIndex;
            }

            return grain;
        }

        grain.YPoints = ReadFilmGrainPoints(payload, reader, 14);
        if (!sequence.IsMonochrome())
        {
            grain.ChromaScalingFromLuma = reader.ReadBit(payload) != 0;
        }

        bool inferNoChromaPoints = sequence.IsMonochrome()
            || grain.ChromaScalingFromLuma
            || (sequence.HasChromaSubsamplingX()
                && sequence.HasChromaSubsamplingY()
                && grain.YPoints.Length == 0);
        if (!inferNoChromaPoints)
        {
            grain.CbPoints = ReadFilmGrainPoints(payload, reader, 10);
            grain.CrPoints = ReadFilmGrainPoints(payload, reader, 10);
        }

        grain.ScalingShift = (int)reader.ReadBits(payload, 2) + 8;
        grain.ArCoefficientLag = (int)reader.ReadBits(payload, 2);
        int lumaCoefficientCount = 2 * grain.ArCoefficientLag
            * (grain.ArCoefficientLag + 1);
        if (grain.YPoints.Length > 0)
        {
            grain.ArCoefficientsY = ReadSignedArray(
                payload,
                reader,
                lumaCoefficientCount,
                8);
        }

        int chromaCoefficientCount = lumaCoefficientCount
            + (grain.YPoints.Length > 0 ? 1 : 0);
        if (!sequence.IsMonochrome())
        {
            if (grain.CbPoints.Length > 0 || grain.ChromaScalingFromLuma)
            {
                grain.ArCoefficientsCb = ReadSignedArray(
                    payload,
                    reader,
                    chromaCoefficientCount,
                    8);
            }

            if (grain.CrPoints.Length > 0 || grain.ChromaScalingFromLuma)
            {
                grain.ArCoefficientsCr = ReadSignedArray(
                    payload,
                    reader,
                    chromaCoefficientCount,
                    8);
            }
        }

        grain.ArCoefficientShift = (int)reader.ReadBits(payload, 2) + 6;
        grain.GrainScaleShift = (int)reader.ReadBits(payload, 2);
        if (grain.CbPoints.Length > 0)
        {
            grain.CbMultiplier = (int)reader.ReadBits(payload, 8);
            grain.CbLumaMultiplier = (int)reader.ReadBits(payload, 8);
            grain.CbOffset = (int)reader.ReadBits(payload, 9);
        }

        if (grain.CrPoints.Length > 0)
        {
            grain.CrMultiplier = (int)reader.ReadBits(payload, 8);
            grain.CrLumaMultiplier = (int)reader.ReadBits(payload, 8);
            grain.CrOffset = (int)reader.ReadBits(payload, 9);
        }

        grain.Overlap = reader.ReadBit(payload) != 0;
        grain.ClipToRestrictedRange = reader.ReadBit(payload) != 0;
        return grain;
    }

    static Av1FilmGrainParameters CopyFilmGrain(Av1FilmGrainParameters source)
    {
        Av1FilmGrainParameters copy = new Av1FilmGrainParameters();
        copy.ApplyGrain = source.ApplyGrain;
        copy.GrainSeed = source.GrainSeed;
        copy.UpdateGrain = source.UpdateGrain;
        copy.ReferenceIndex = source.ReferenceIndex;
        copy.YPoints = CopyPointArray(source.YPoints);
        copy.CbPoints = CopyPointArray(source.CbPoints);
        copy.CrPoints = CopyPointArray(source.CrPoints);
        copy.ChromaScalingFromLuma = source.ChromaScalingFromLuma;
        copy.ScalingShift = source.ScalingShift;
        copy.ArCoefficientLag = source.ArCoefficientLag;
        copy.ArCoefficientsY = (int[])source.ArCoefficientsY.Clone();
        copy.ArCoefficientsCb = (int[])source.ArCoefficientsCb.Clone();
        copy.ArCoefficientsCr = (int[])source.ArCoefficientsCr.Clone();
        copy.ArCoefficientShift = source.ArCoefficientShift;
        copy.GrainScaleShift = source.GrainScaleShift;
        copy.CbMultiplier = source.CbMultiplier;
        copy.CbLumaMultiplier = source.CbLumaMultiplier;
        copy.CbOffset = source.CbOffset;
        copy.CrMultiplier = source.CrMultiplier;
        copy.CrLumaMultiplier = source.CrLumaMultiplier;
        copy.CrOffset = source.CrOffset;
        copy.Overlap = source.Overlap;
        copy.ClipToRestrictedRange = source.ClipToRestrictedRange;
        return copy;
    }

    static int[][] CopyPointArray(int[][] source)
    {
        int[][] copy = new int[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = (int[])source[index].Clone();
            index++;
        }

        return copy;
    }

    static int[][] ReadFilmGrainPoints(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        int countBits)
    {
        int count = (int)reader.ReadBits(payload, 4);
        if (count > countBits)
        {
            reader.Code = AvifResultCode.InvalidData;
            return System.Array.Empty<int[]>();
        }

        int[][] points = new int[count][];
        int index = 0;
        while (index < count)
        {
            points[index] = new int[]
            {
                (int)reader.ReadBits(payload, 8),
                (int)reader.ReadBits(payload, 8)
            };
            index++;
        }

        return points;
    }

    static int[] ReadSignedArray(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        int count,
        int bitCount)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = (int)reader.ReadBits(payload, bitCount) - 128;
            index++;
        }

        return values;
    }

    static Av1FrameHeaderParseResult Finish(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1FrameHeader header,
        bool validateTrailingBits)
    {
        if (reader.Code != AvifResultCode.Success)
        {
            return Fail(reader.Code);
        }

        header.BitsConsumed = reader.BitPosition;
        header.SourcePayload = payload.ToArray();
        if (validateTrailingBits)
        {
            AvifResultCode trailingCode =
                Av1SequenceHeaderParser.ReadTrailingBits(payload, reader);
            if (trailingCode != AvifResultCode.Success)
            {
                return Fail(trailingCode);
            }
        }

        return new Av1FrameHeaderParseResult(AvifResultCode.Success, header);
    }

    static Av1FrameHeaderParseResult Fail(AvifResultCode code)
    {
        AvifResultCode normalized = code == AvifResultCode.TruncatedData
            ? AvifResultCode.TruncatedData
            : AvifResultCode.InvalidAv1Bitstream;
        return new Av1FrameHeaderParseResult(normalized, null);
    }

    static int RelativeDistance(int first, int second, int orderHintBits)
    {
        if (orderHintBits == 0)
        {
            return 0;
        }

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

    static int[] CopyPrefix(int[] source, int count)
    {
        int[] result = new int[count];
        System.Array.Copy(source, result, count);
        return result;
    }

    static int Clip(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }

    static int Maximum(int first, int second)
    {
        return first > second ? first : second;
    }
}
