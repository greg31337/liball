using System;

namespace avif;

internal class Av1DecodeWrapupRequest
{
    internal ParsedAv1SequenceHeader? Sequence;
    internal ParsedAv1FrameHeader? Header;
    internal PlaneBuffer[] Planes;
    internal Av1BlockMotionField? MotionField;
    internal int[] SegmentIds;
    internal bool[] CompletedTiles;

    internal Av1DecodeWrapupRequest()
    {
        Planes = System.Array.Empty<PlaneBuffer>();
        SegmentIds = System.Array.Empty<int>();
        CompletedTiles = System.Array.Empty<bool>();
    }
}

// AV1 specification sections 7.4, 7.18, 7.19, 7.20, and 7.21.
internal static class Av1DecodeWrapup
{
    internal static AvifResultCode Complete(
        Av1FrameBufferManager manager,
        Av1DecodeWrapupRequest request)
    {
        if (manager == null || request == null || request.Sequence == null
            || request.Header == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (manager.Closed)
        {
            return AvifResultCode.Closed;
        }

        if (request.Header.ShowExistingFrame)
        {
            AvifResultCode existing = ValidateExisting(manager, request);
            return existing == AvifResultCode.Success
                ? manager.ShowExistingFrame(request.Header)
                : existing;
        }

        AvifResultCode validation = ValidateDecoded(manager, request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        int[]? segmentIds = ResolveSegmentIds(manager, request);
        if (segmentIds == null)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }
        return manager.StoreDecodedFrameWithSegments(
            request.Sequence,
            request.Header,
            request.Planes,
            request.MotionField,
            segmentIds);
    }

    static AvifResultCode ValidateExisting(
        Av1FrameBufferManager manager,
        Av1DecodeWrapupRequest request)
    {
        ParsedAv1FrameHeader? header = request.Header;
        if (!(header ?? throw new InvalidOperationException("Invalid internal state.")).ShowFrame || request.Planes == null
            || request.Planes.Length != 0
            || request.CompletedTiles == null
            || request.CompletedTiles.Length != 0
            || request.MotionField != null
            || request.SegmentIds == null || request.SegmentIds.Length != 0
            || header.FrameToShowMapIndex < 0
            || header.FrameToShowMapIndex
                >= Av1SyntaxConstants.StoredReferenceFrames)
        {
            return AvifResultCode.InvalidArgument;
        }
        Av1DecodedFrame? frame = (manager.ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state."))[
            header.FrameToShowMapIndex];
        if (!(manager.HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceValid[header.FrameToShowMapIndex]
            || frame == null || frame.Closed || !frame.ShowableFrame
            || header.FrameType != frame.FrameType
            || (request.Sequence ?? throw new InvalidOperationException("Invalid internal state.")).HasFrameIdNumbers()
                && header.DisplayFrameId != frame.FrameId
            || frame.FrameType == Av1FrameType.Key
                && header.RefreshFrameFlags != 255
            || frame.FrameType != Av1FrameType.Key
                && header.RefreshFrameFlags != 0)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateDecoded(
        Av1FrameBufferManager manager,
        Av1DecodeWrapupRequest request)
    {
        ParsedAv1FrameHeader? header = request.Header;
        if ((header ?? throw new InvalidOperationException("Invalid internal state.")).Size == null || header.Tiles == null
            || request.Planes == null || request.CompletedTiles == null
            || header.Tiles.TileColumns <= 0 || header.Tiles.TileRows <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        long tileCount = (long)header.Tiles.TileColumns * header.Tiles.TileRows;
        if (tileCount > System.Array.MaxLength
            || request.CompletedTiles.Length != tileCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        int tile = 0;
        while (tile < request.CompletedTiles.Length)
        {
            if (!request.CompletedTiles[tile])
            {
                return AvifResultCode.InvalidState;
            }
            tile++;
        }

        bool resetFrame = header.FrameType == Av1FrameType.Switch
            || header.FrameType == Av1FrameType.Key && header.ShowFrame;
        if (resetFrame
            && (!header.ErrorResilientMode
                || header.RefreshFrameFlags != 255)
            || header.FrameType == Av1FrameType.IntraOnly
                && header.RefreshFrameFlags == 255
            || (header.FrameIsIntra || header.ErrorResilientMode)
                && header.PrimaryReferenceFrame != 7)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        if (!header.FrameIsIntra)
        {
            if (header.ReferenceFrameIndexes == null
                || header.ReferenceFrameIndexes.Length
                    != Av1SyntaxConstants.RefsPerFrame)
            {
                return AvifResultCode.InvalidArgument;
            }
            int reference = 0;
            while (reference < header.ReferenceFrameIndexes.Length)
            {
                int slot = header.ReferenceFrameIndexes[reference];
                if (slot < 0 || slot >= Av1SyntaxConstants.StoredReferenceFrames
                    || !(manager.HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceValid[slot]
                    || (manager.ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state."))[slot] == null
                    || (manager.ReferenceFrames[slot] ?? throw new InvalidOperationException("Invalid internal state.")).Closed)
                {
                    return AvifResultCode.InvalidAv1Bitstream;
                }
                reference++;
            }
        }

        long miCount = (long)(header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows * header.Size.MiColumns;
        if (miCount <= 0 || miCount > System.Array.MaxLength
            || request.SegmentIds == null
            || request.SegmentIds.Length != miCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static int[]? ResolveSegmentIds(
        Av1FrameBufferManager manager,
        Av1DecodeWrapupRequest request)
    {
        ParsedAv1FrameHeader? header = request.Header;
        if ((header ?? throw new InvalidOperationException("Invalid internal state.")).Segmentation == null || !(header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled
            || header.Segmentation.UpdateMap)
        {
            return ValidateSegmentIds(request.SegmentIds)
                ? (int[])request.SegmentIds.Clone()
                : null;
        }
        if (header.PrimaryReferenceFrame < 0
            || header.PrimaryReferenceFrame
                >= Av1SyntaxConstants.RefsPerFrame)
        {
            return null;
        }
        int slot = header.ReferenceFrameIndexes[header.PrimaryReferenceFrame];
        return (manager.HeaderState ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceSegmentIds(
            slot, (header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows, header.Size.MiColumns);
    }

    static bool ValidateSegmentIds(int[] segmentIds)
    {
        int index = 0;
        while (index < segmentIds.Length)
        {
            if (segmentIds[index] < 0
                || segmentIds[index] >= Av1SyntaxConstants.MaxSegments)
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
