using System;
using System.Collections.Generic;

namespace avif;

// AV1 specification sections 7.4.1, 8.3.2, 8.8.4, and 8.8.6.
internal class Av1BlockMotionField
{
    internal int MiRows;
    internal int MiColumns;
    internal int[] ReferenceFrames0;
    internal int[] ReferenceFrames1;
    internal int[] MotionVectorRows0;
    internal int[] MotionVectorColumns0;
    internal int[] MotionVectorRows1;
    internal int[] MotionVectorColumns1;
    internal AvifResultCode Code;

    internal Av1BlockMotionField(int miRows, int miColumns)
    {
        MiRows = miRows;
        MiColumns = miColumns;
        Code = ValidateDimensions(miRows, miColumns);
        if (Code != AvifResultCode.Success)
        {
            ReferenceFrames0 = System.Array.Empty<int>();
            ReferenceFrames1 = System.Array.Empty<int>();
            MotionVectorRows0 = System.Array.Empty<int>();
            MotionVectorColumns0 = System.Array.Empty<int>();
            MotionVectorRows1 = System.Array.Empty<int>();
            MotionVectorColumns1 = System.Array.Empty<int>();
            return;
        }

        int count = miRows * miColumns;
        ReferenceFrames0 = new int[count];
        ReferenceFrames1 = new int[count];
        MotionVectorRows0 = new int[count];
        MotionVectorColumns0 = new int[count];
        MotionVectorRows1 = new int[count];
        MotionVectorColumns1 = new int[count];
        int index = 0;
        while (index < count)
        {
            ReferenceFrames0[index] = (int)Av1ReferenceFrame.None;
            ReferenceFrames1[index] = (int)Av1ReferenceFrame.None;
            index++;
        }
    }

    internal AvifResultCode Set(
        int row,
        int column,
        int list,
        Av1ReferenceFrame referenceFrame,
        int motionVectorRow,
        int motionVectorColumn)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (row < 0 || row >= MiRows || column < 0 || column >= MiColumns
            || list < 0 || list > 1
            || referenceFrame < Av1ReferenceFrame.None
            || referenceFrame > Av1ReferenceFrame.Alternate)
        {
            return AvifResultCode.InvalidArgument;
        }

        int index = row * MiColumns + column;
        if (list == 0)
        {
            ReferenceFrames0[index] = (int)referenceFrame;
            MotionVectorRows0[index] = motionVectorRow;
            MotionVectorColumns0[index] = motionVectorColumn;
        }
        else
        {
            ReferenceFrames1[index] = (int)referenceFrame;
            MotionVectorRows1[index] = motionVectorRow;
            MotionVectorColumns1[index] = motionVectorColumn;
        }

        return AvifResultCode.Success;
    }

    internal int GetReferenceFrame(int index, int list)
    {
        if (index < 0 || index >= ReferenceFrames0.Length || list < 0 || list > 1)
        {
            return (int)Av1ReferenceFrame.None;
        }

        return list == 0 ? ReferenceFrames0[index] : ReferenceFrames1[index];
    }

    internal int GetMotionVectorRow(int index, int list)
    {
        if (index < 0 || index >= ReferenceFrames0.Length || list < 0 || list > 1)
        {
            return 0;
        }

        return list == 0 ? MotionVectorRows0[index] : MotionVectorRows1[index];
    }

    internal int GetMotionVectorColumn(int index, int list)
    {
        if (index < 0 || index >= ReferenceFrames0.Length || list < 0 || list > 1)
        {
            return 0;
        }

        return list == 0 ? MotionVectorColumns0[index] : MotionVectorColumns1[index];
    }

    static AvifResultCode ValidateDimensions(int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        long count = (long)rows * columns;
        return count > System.Array.MaxLength
            ? AvifResultCode.LimitExceeded
            : AvifResultCode.Success;
    }
}

internal class Av1StoredMotionField
{
    internal int MiRows;
    internal int MiColumns;
    internal int[] ReferenceFrames;
    internal int[] MotionVectorRows;
    internal int[] MotionVectorColumns;

    internal Av1StoredMotionField(int miRows, int miColumns)
    {
        MiRows = miRows;
        MiColumns = miColumns;
        int count = miRows * miColumns;
        ReferenceFrames = new int[count];
        MotionVectorRows = new int[count];
        MotionVectorColumns = new int[count];
        int index = 0;
        while (index < count)
        {
            ReferenceFrames[index] = (int)Av1ReferenceFrame.None;
            index++;
        }
    }

    internal Av1ReferenceFrame GetReferenceFrame(int row, int column)
    {
        if (row < 0 || row >= MiRows || column < 0 || column >= MiColumns)
        {
            return Av1ReferenceFrame.None;
        }

        return (Av1ReferenceFrame)ReferenceFrames[row * MiColumns + column];
    }

    internal int GetMotionVectorRow(int row, int column)
    {
        if (row < 0 || row >= MiRows || column < 0 || column >= MiColumns)
        {
            return 0;
        }

        return MotionVectorRows[row * MiColumns + column];
    }

    internal int GetMotionVectorColumn(int row, int column)
    {
        if (row < 0 || row >= MiRows || column < 0 || column >= MiColumns)
        {
            return 0;
        }

        return MotionVectorColumns[row * MiColumns + column];
    }
}

internal class Av1DecodedFrame
{
    internal int TemporalId;
    internal int SpatialId;
    internal int Identity;
    internal Av1FrameType FrameType;
    internal int FrameId;
    internal int OrderHint;
    internal bool ShowableFrame;
    internal bool KeyShownUsingShowExisting;
    internal int UpscaledWidth;
    internal int FrameWidth;
    internal int FrameHeight;
    internal int RenderWidth;
    internal int RenderHeight;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal PlaneBuffer[]? Planes;
    internal Av1StoredMotionField? MotionField;
    internal int[]? ReferenceOrderHints;
    internal Av1GlobalMotionParameters? GlobalMotion;
    internal Av1SegmentationParameters? Segmentation;
    internal Av1LoopFilterParameters? LoopFilter;
    internal int[]? SegmentIds;
    internal Av1FilmGrainParameters? FilmGrain;
    internal long RetainedBytes;
    internal int HoldCount;
    internal bool Closed;
}

internal class Av1OutputFrame
{
    internal long OutputIndex;
    internal Av1DecodedFrame? Frame;
    internal bool FromShowExisting;
    internal bool PresentationTimePresent;
    internal ulong PresentationTime;
    internal int TemporalId;
    internal int SpatialId;
    internal int State;

    internal Av1OutputFrame(
        long outputIndex,
        Av1DecodedFrame frame,
        bool fromShowExisting,
        bool presentationTimePresent,
        ulong presentationTime,
        int temporalId,
        int spatialId)
    {
        OutputIndex = outputIndex;
        Frame = frame;
        FromShowExisting = fromShowExisting;
        PresentationTimePresent = presentationTimePresent;
        PresentationTime = presentationTime;
        TemporalId = temporalId;
        SpatialId = spatialId;
        State = 0;
    }
}

internal class Av1FrameBufferManager
{
    internal ResourceGuard? Guard;
    internal Av1FrameHeaderState? HeaderState;
    internal Av1DecodedFrame?[]? ReferenceFrames;
    internal Queue<Av1OutputFrame>? OutputQueue;
    internal List<Av1DecodedFrame>? LiveFrames;
    internal int NextFrameIdentity;
    internal long NextOutputIndex;
    internal bool Closed;

    internal static Av1FrameBufferManager Create(AvifSecurityLimits limits)
    {
        return new Av1FrameBufferManager(limits, new Av1FrameHeaderState());
    }

    internal Av1FrameBufferManager(AvifSecurityLimits limits, Av1FrameHeaderState headerState)
    {
        Initialize(limits, headerState);
    }

    internal static Av1FrameBufferManager Create(
        AvifSecurityLimits limits,
        Av1FrameHeaderState headerState)
    {
        return new Av1FrameBufferManager(limits, headerState);
    }

    void Initialize(AvifSecurityLimits limits, Av1FrameHeaderState headerState)
    {
        Guard = new ResourceGuard(limits);
        if (headerState == null)
        {
            Guard.Code = AvifResultCode.InvalidArgument;
            HeaderState = new Av1FrameHeaderState();
        }
        else
        {
            HeaderState = headerState;
        }
        ReferenceFrames = new Av1DecodedFrame[Av1SyntaxConstants.StoredReferenceFrames];
        OutputQueue = new Queue<Av1OutputFrame>();
        LiveFrames = new List<Av1DecodedFrame>();
        NextFrameIdentity = 1;
        NextOutputIndex = 0;
        Closed = false;
    }

    internal AvifResultCode StoreDecodedFrame(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] planes,
        Av1BlockMotionField? blockMotion)
    {
        return StoreDecodedFrameWithSegments(
            sequence, header, planes, blockMotion, null);
    }

    internal AvifResultCode StoreDecodedFrameWithSegments(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] planes,
        Av1BlockMotionField? blockMotion,
        int[]? segmentIds)
    {
        AvifResultCode validation = ValidateNewFrame(
            sequence, header, planes, blockMotion, segmentIds);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        long retainedBytes = ComputeRetainedBytes(planes, (header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows, header.Size.MiColumns);
        if (!(Guard ?? throw new InvalidOperationException("Invalid internal state.")).FrameCount.CanAdd(1) || !Guard.DecodedBytes.CanAdd(retainedBytes))
        {
            return AvifResultCode.LimitExceeded;
        }

        Guard.FrameCount.AddUnchecked(1);
        Guard.DecodedBytes.AddUnchecked(retainedBytes);
        Av1DecodedFrame frame = CreateFrame(
            sequence, header, planes, blockMotion, segmentIds, retainedBytes);
        (LiveFrames ?? throw new InvalidOperationException("Invalid internal state.")).Add(frame);
        ApplyRefreshFlags(frame, header.RefreshFrameFlags);
        if (header.ShowFrame)
        {
            QueueOutput(frame, false, header);
        }

        ReleaseIfUnused(frame);
        return AvifResultCode.Success;
    }

    internal AvifResultCode ShowExistingFrame(ParsedAv1FrameHeader header)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if ((Guard ?? throw new InvalidOperationException("Invalid internal state.")).Code != AvifResultCode.Success)
        {
            return Guard.Code;
        }

        if (header == null || !header.ShowExistingFrame
            || header.FrameToShowMapIndex < 0
            || header.FrameToShowMapIndex >= (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || header.RefreshFrameFlags < 0 || header.RefreshFrameFlags > 255)
        {
            return AvifResultCode.InvalidArgument;
        }

        Av1DecodedFrame? frame = ReferenceFrames[header.FrameToShowMapIndex];
        if (!(HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceValid[header.FrameToShowMapIndex]
            || frame == null || frame.Closed || !frame.ShowableFrame)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        if (frame.FrameType == Av1FrameType.Key && frame.KeyShownUsingShowExisting)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        if (!Guard.FrameCount.CanAdd(1))
        {
            return AvifResultCode.LimitExceeded;
        }

        Guard.FrameCount.AddUnchecked(1);
        if (frame.FrameType == Av1FrameType.Key)
        {
            frame.KeyShownUsingShowExisting = true;
            LoadFrameState(frame);
        }

        ApplyRefreshFlags(frame, header.RefreshFrameFlags);
        QueueOutput(frame, true, header);
        return AvifResultCode.Success;
    }

    internal Av1DecodedFrame? GetReferenceFrame(int slot)
    {
        if (Closed || slot < 0 || slot >= (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || !(HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceValid[slot])
        {
            return null;
        }

        return ReferenceFrames[slot];
    }

    internal int GetPendingOutputCount()
    {
        return Closed ? 0 : (OutputQueue ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    internal Av1OutputFrame? TakeOutput()
    {
        if (Closed || (OutputQueue ?? throw new InvalidOperationException("Invalid internal state.")).Count == 0)
        {
            return null;
        }

        Av1OutputFrame output = OutputQueue.Dequeue();
        output.State = 1;
        return output;
    }

    internal AvifResultCode ReleaseOutput(Av1OutputFrame? output)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (output == null || output.State != 1 || output.Frame == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        output.State = 2;
        Av1DecodedFrame frame = output.Frame;
        output.Frame = null;
        frame.HoldCount--;
        ReleaseIfUnused(frame);
        return AvifResultCode.Success;
    }

    internal long GetRetainedByteCount()
    {
        return (Guard ?? throw new InvalidOperationException("Invalid internal state.")).DecodedBytes.Current;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        Closed = true;
        int index = 0;
        while (index < (LiveFrames ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            CloseFrame(LiveFrames[index]);
            index++;
        }

        LiveFrames.Clear();
        while ((OutputQueue ?? throw new InvalidOperationException("Invalid internal state.")).Count > 0)
        {
            Av1OutputFrame output = OutputQueue.Dequeue();
            output.State = 2;
            output.Frame = null;
        }

        index = 0;
        while (index < (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            ReferenceFrames[index] = null;
            (HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).SetReferenceShowable(index, false);
            index++;
        }

        (HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ClearReferences();
        return AvifResultCode.Success;
    }

    AvifResultCode ValidateNewFrame(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] planes,
        Av1BlockMotionField? blockMotion,
        int[]? segmentIds)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if ((Guard ?? throw new InvalidOperationException("Invalid internal state.")).Code != AvifResultCode.Success)
        {
            return Guard.Code;
        }

        if (sequence == null || header == null || header.ShowExistingFrame
            || header.Size == null || planes == null
            || planes.Length != sequence.GetPlaneCount()
            || header.RefreshFrameFlags < 0 || header.RefreshFrameFlags > 255
            || header.ReferenceFrameIndexes == null
            || header.ReferenceFrameIndexes.Length != Av1SyntaxConstants.RefsPerFrame)
        {
            return AvifResultCode.InvalidArgument;
        }

        AvifResultCode dimensions = Guard.CheckImageDimensions(
            header.Size.UpscaledWidth,
            header.Size.FrameHeight);
        if (dimensions != AvifResultCode.Success)
        {
            return dimensions;
        }

        dimensions = Guard.CheckImageDimensions(
            header.Size.FrameWidth,
            header.Size.FrameHeight);
        if (dimensions != AvifResultCode.Success)
        {
            return dimensions;
        }

        dimensions = Guard.CheckImageDimensions(
            header.Size.RenderWidth,
            header.Size.RenderHeight);
        if (dimensions != AvifResultCode.Success)
        {
            return dimensions;
        }

        if (header.Size.MiRows <= 0
            || header.Size.MiColumns <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        long miCount = (long)header.Size.MiRows * header.Size.MiColumns;
        if (miCount > System.Array.MaxLength)
        {
            return AvifResultCode.LimitExceeded;
        }

        if (blockMotion != null
            && (blockMotion.Code != AvifResultCode.Success
                || blockMotion.MiRows != header.Size.MiRows
                || blockMotion.MiColumns != header.Size.MiColumns))
        {
            return AvifResultCode.InvalidArgument;
        }

        if (segmentIds != null)
        {
            if (segmentIds.Length != miCount)
            {
                return AvifResultCode.InvalidArgument;
            }
            int segmentIndex = 0;
            while (segmentIndex < segmentIds.Length)
            {
                if (segmentIds[segmentIndex] < 0
                    || segmentIds[segmentIndex] >= Av1SyntaxConstants.MaxSegments)
                {
                    return AvifResultCode.InvalidArgument;
                }
                segmentIndex++;
            }
        }

        return Av1PixelConformance.Validate(
            sequence,
            header.Size.UpscaledWidth,
            header.Size.FrameHeight,
            planes);
    }

    long ComputeRetainedBytes(PlaneBuffer[] planes, int miRows, int miColumns)
    {
        long bytes = (long)miRows * miColumns * 12;
        int plane = 0;
        while (plane < planes.Length)
        {
            bytes += (long)planes[plane].Stride * planes[plane].Height;
            plane++;
        }

        return bytes;
    }

    Av1DecodedFrame CreateFrame(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        PlaneBuffer[] planes,
        Av1BlockMotionField? blockMotion,
        int[]? segmentIds,
        long retainedBytes)
    {
        Av1DecodedFrame frame = new Av1DecodedFrame();
        frame.Identity = NextFrameIdentity++;
        frame.TemporalId = header.TemporalId;
        frame.SpatialId = header.SpatialId;
        frame.FrameType = header.FrameType;
        frame.FrameId = header.CurrentFrameId;
        frame.OrderHint = header.OrderHint;
        frame.ShowableFrame = header.ShowableFrame;
        frame.UpscaledWidth = (header.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth;
        frame.FrameWidth = header.Size.FrameWidth;
        frame.FrameHeight = header.Size.FrameHeight;
        frame.RenderWidth = header.Size.RenderWidth;
        frame.RenderHeight = header.Size.RenderHeight;
        frame.BitDepth = sequence.GetBitDepth();
        frame.SubsamplingX = sequence.HasChromaSubsamplingX();
        frame.SubsamplingY = sequence.HasChromaSubsamplingY();
        frame.Planes = planes;
        frame.RetainedBytes = retainedBytes;
        frame.ReferenceOrderHints = CopyReferenceOrderHints(header);
        frame.GlobalMotion = CopyGlobalMotion(header.GlobalMotion);
        frame.Segmentation = CopySegmentation(header.Segmentation);
        frame.LoopFilter = CopyLoopFilter(header.LoopFilter);
        frame.SegmentIds = segmentIds == null
            ? System.Array.Empty<int>()
            : (int[])segmentIds.Clone();
        frame.FilmGrain = CopyFilmGrain(header.FilmGrain);
        frame.MotionField = CreateStoredMotionField(sequence, header, blockMotion);
        return frame;
    }

    Av1StoredMotionField CreateStoredMotionField(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        Av1BlockMotionField? blockMotion)
    {
        Av1StoredMotionField stored =
            new Av1StoredMotionField((header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows, header.Size.MiColumns);
        if (blockMotion == null)
        {
            return stored;
        }

        int count = header.Size.MiRows * header.Size.MiColumns;
        int index = 0;
        while (index < count)
        {
            int list = 0;
            while (list < 2)
            {
                int referenceType = blockMotion.GetReferenceFrame(index, list);
                int referenceIndex = referenceType - (int)Av1ReferenceFrame.Last;
                if (referenceIndex >= 0 && referenceIndex < header.ReferenceFrameIndexes.Length)
                {
                    int slot = header.ReferenceFrameIndexes[referenceIndex];
                    Av1DecodedFrame? reference = slot >= 0
                        && slot < (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length
                        && (HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceValid[slot]
                        ? ReferenceFrames[slot]
                        : null;
                    int rowMv = blockMotion.GetMotionVectorRow(index, list);
                    int columnMv = blockMotion.GetMotionVectorColumn(index, list);
                    if (reference != null
                        && RelativeDistance(
                            reference.OrderHint,
                            header.OrderHint,
                            sequence.GetOrderHintBits()) < 0
                        && rowMv >= -Av1SyntaxConstants.ReferenceMotionVectorLimit
                        && rowMv <= Av1SyntaxConstants.ReferenceMotionVectorLimit
                        && columnMv >= -Av1SyntaxConstants.ReferenceMotionVectorLimit
                        && columnMv <= Av1SyntaxConstants.ReferenceMotionVectorLimit)
                    {
                        stored.ReferenceFrames[index] = referenceType;
                        stored.MotionVectorRows[index] = rowMv;
                        stored.MotionVectorColumns[index] = columnMv;
                    }
                }

                list++;
            }

            index++;
        }

        return stored;
    }

    void ApplyRefreshFlags(Av1DecodedFrame frame, int refreshFlags)
    {
        int slot = 0;
        while (slot < (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if ((refreshFlags & 1 << slot) != 0)
            {
                SetReferenceFrame(slot, frame);
            }

            slot++;
        }
    }

    void SetReferenceFrame(int slot, Av1DecodedFrame frame)
    {
        Av1DecodedFrame? previous = (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state."))[slot];
        if (object.ReferenceEquals(previous, frame))
        {
            SyncHeaderReference(slot, frame);
            return;
        }

        ReferenceFrames[slot] = frame;
        frame.HoldCount++;
        if (previous != null)
        {
            previous.HoldCount--;
            ReleaseIfUnused(previous);
        }

        SyncHeaderReference(slot, frame);
    }

    void SyncHeaderReference(int slot, Av1DecodedFrame frame)
    {
        (HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).SetReference(
            slot,
            frame.FrameType,
            frame.OrderHint,
            frame.FrameId,
            frame.UpscaledWidth,
            frame.FrameWidth,
            frame.FrameHeight,
            frame.RenderWidth,
            frame.RenderHeight);
        HeaderState.SetReferenceShowable(slot, frame.ShowableFrame);
        HeaderState.SetReferenceFilmGrain(slot, CopyFilmGrain(frame.FilmGrain));
        HeaderState.SaveReferenceState(
            slot,
            frame.GlobalMotion,
            frame.Segmentation,
            frame.LoopFilter,
            frame.SegmentIds,
            (frame.MotionField ?? throw new InvalidOperationException("Invalid internal state.")).MiRows,
            frame.MotionField.MiColumns);
    }

    void LoadFrameState(Av1DecodedFrame frame)
    {
        (HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).LoadFrameState(
            frame.GlobalMotion, frame.Segmentation, frame.LoopFilter);
    }

    void QueueOutput(
        Av1DecodedFrame frame,
        bool fromShowExisting,
        ParsedAv1FrameHeader header)
    {
        Av1OutputFrame output = new Av1OutputFrame(
            NextOutputIndex++,
            frame,
            fromShowExisting,
            header.FramePresentationTimePresent,
            header.FramePresentationTime,
            header.TemporalId,
            header.SpatialId);
        frame.HoldCount++;
        (OutputQueue ?? throw new InvalidOperationException("Invalid internal state.")).Enqueue(output);
    }

    void ReleaseIfUnused(Av1DecodedFrame frame)
    {
        if (frame == null || frame.Closed || frame.HoldCount != 0)
        {
            return;
        }

        CloseFrame(frame);
        (LiveFrames ?? throw new InvalidOperationException("Invalid internal state.")).Remove(frame);
    }

    void CloseFrame(Av1DecodedFrame frame)
    {
        if (frame == null || frame.Closed)
        {
            return;
        }

        frame.Closed = true;
        int plane = 0;
        while (plane < (frame.Planes ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            frame.Planes[plane].Close();
            plane++;
        }

        (Guard ?? throw new InvalidOperationException("Invalid internal state.")).DecodedBytes.Release(frame.RetainedBytes);
        frame.HoldCount = 0;
    }

    int[] CopyReferenceOrderHints(ParsedAv1FrameHeader header)
    {
        int[] hints = new int[Av1SyntaxConstants.RefsPerFrame];
        int index = 0;
        while (index < hints.Length)
        {
            int slot = header.ReferenceFrameIndexes[index];
            Av1DecodedFrame? reference = slot >= 0 && slot < (ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length
                ? ReferenceFrames[slot]
                : null;
            hints[index] = reference == null ? 0 : reference.OrderHint;
            index++;
        }

        return hints;
    }

    static Av1GlobalMotionParameters? CopyGlobalMotion(Av1GlobalMotionParameters? source)
    {
        if (source == null || source.Types == null || source.Parameters == null
            || source.Types.Length != Av1SyntaxConstants.TotalRefsPerFrame
            || source.Parameters.Length != Av1SyntaxConstants.TotalRefsPerFrame)
        {
            return null;
        }

        Av1GlobalMotionParameters copy = new Av1GlobalMotionParameters();
        copy.Types = (Av1WarpModelType[])source.Types.Clone();
        copy.Parameters = new int[source.Parameters.Length][];
        int index = 0;
        while (index < copy.Parameters.Length)
        {
            copy.Parameters[index] = source.Parameters[index] == null
                ? Av1FrameHeaderState.CreateIdentityGlobalParameters()
                : (int[])source.Parameters[index].Clone();
            index++;
        }

        return copy;
    }

    static Av1SegmentationParameters? CopySegmentation(
        Av1SegmentationParameters? source)
    {
        if (source == null)
        {
            return null;
        }

        Av1SegmentationParameters copy = new Av1SegmentationParameters();
        copy.Enabled = source.Enabled;
        copy.UpdateMap = source.UpdateMap;
        copy.TemporalUpdate = source.TemporalUpdate;
        copy.UpdateData = source.UpdateData;
        copy.FeatureEnabled = CopyBoolJagged(source.FeatureEnabled);
        copy.FeatureData = CopyJagged(source.FeatureData);
        copy.SegmentIdPreSkip = source.SegmentIdPreSkip;
        copy.LastActiveSegmentId = source.LastActiveSegmentId;
        return copy;
    }

    static Av1LoopFilterParameters? CopyLoopFilter(Av1LoopFilterParameters? source)
    {
        if (source == null)
        {
            return null;
        }

        Av1LoopFilterParameters copy = new Av1LoopFilterParameters();
        copy.Levels = CopyArray(source.Levels);
        copy.Sharpness = source.Sharpness;
        copy.DeltaEnabled = source.DeltaEnabled;
        copy.DeltaUpdate = source.DeltaUpdate;
        copy.ReferenceDeltas = CopyArray(source.ReferenceDeltas);
        copy.ModeDeltas = CopyArray(source.ModeDeltas);
        return copy;
    }

    static Av1FilmGrainParameters? CopyFilmGrain(Av1FilmGrainParameters? source)
    {
        if (source == null)
        {
            return null;
        }

        Av1FilmGrainParameters copy = new Av1FilmGrainParameters();
        copy.ApplyGrain = source.ApplyGrain;
        copy.GrainSeed = source.GrainSeed;
        copy.UpdateGrain = source.UpdateGrain;
        copy.ReferenceIndex = source.ReferenceIndex;
        copy.YPoints = CopyJagged(source.YPoints);
        copy.CbPoints = CopyJagged(source.CbPoints);
        copy.CrPoints = CopyJagged(source.CrPoints);
        copy.ChromaScalingFromLuma = source.ChromaScalingFromLuma;
        copy.ScalingShift = source.ScalingShift;
        copy.ArCoefficientLag = source.ArCoefficientLag;
        copy.ArCoefficientsY = CopyArray(source.ArCoefficientsY);
        copy.ArCoefficientsCb = CopyArray(source.ArCoefficientsCb);
        copy.ArCoefficientsCr = CopyArray(source.ArCoefficientsCr);
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

    static int[] CopyArray(int[]? source)
    {
        return source == null ? System.Array.Empty<int>() : (int[])source.Clone();
    }

    static int[][] CopyJagged(int[][]? source)
    {
        if (source == null)
        {
            return System.Array.Empty<int[]>();
        }

        int[][] copy = new int[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CopyArray(source[index]);
            index++;
        }

        return copy;
    }

    static bool[][] CopyBoolJagged(bool[][]? source)
    {
        if (source == null)
        {
            return System.Array.Empty<bool[]>();
        }

        bool[][] copy = new bool[source.Length][];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index] == null
                ? System.Array.Empty<bool>()
                : (bool[])source[index].Clone();
            index++;
        }
        return copy;
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
}
