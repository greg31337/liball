using System;

namespace avif;

/// <summary>Describes the resolved primary-item or timed-sequence image source.</summary>
public class AvifImageSource : IAvifImageSource
{
    AvifSourceSelection selection;
    AvifImageItem? primaryItem;
    AvifMovie? movie;
    AvifTrack? sequenceTrack;

    internal static AvifImageSource FromPrimaryItem(AvifImageItem primaryItem)
    {
        AvifImageSource result = new AvifImageSource(null, null);
        result.selection = AvifSourceSelection.PrimaryItem;
        result.primaryItem = primaryItem;
        return result;
    }

    internal AvifImageSource(AvifMovie? movie, AvifTrack? sequenceTrack)
    {
        selection = AvifSourceSelection.ImageSequence;
        this.movie = movie;
        this.sequenceTrack = sequenceTrack;
    }

    /// <summary>Returns the source kind selected after resolving automatic selection.</summary>
    /// <returns>The primary-item or image-sequence selection.</returns>
    public AvifSourceSelection GetSelection()
    {
        return selection;
    }

    /// <summary>Returns the selected primary image item.</summary>
    /// <returns>The item, or null when a timed sequence was selected.</returns>
    public AvifImageItem? GetPrimaryItem()
    {
        return primaryItem;
    }

    /// <summary>Returns the selected timed image-sequence track.</summary>
    /// <returns>The track, or null when a primary item was selected.</returns>
    public AvifTrack? GetSequenceTrack()
    {
        return sequenceTrack;
    }

    /// <summary>Returns the number of available non-auxiliary visual tracks.</summary>
    /// <returns>The track count, or zero for a primary-item source.</returns>
    public int GetVisualTrackCount()
    {
        return movie == null ? 0 : movie.GetVisualTrackCount();
    }

    /// <summary>Returns one available non-auxiliary visual track.</summary>
    /// <param name="index">The zero-based visual-track index.</param>
    /// <returns>The track, or null for a primary-item source or invalid index.</returns>
    public AvifTrack? GetVisualTrack(int index)
    {
        return movie == null ? null : movie.GetVisualTrack(index);
    }

    /// <summary>Returns the number of alternatives to the selected sequence track.</summary>
    /// <returns>The alternate-track count.</returns>
    public int GetAlternateTrackCount()
    {
        return movie == null || sequenceTrack == null
            ? 0 : movie.GetAlternateTrackCount(sequenceTrack.GetTrackId());
    }

    /// <summary>Returns one alternative to the selected sequence track.</summary>
    /// <param name="index">The zero-based alternate index.</param>
    /// <returns>The alternate track, or null when unavailable.</returns>
    public AvifTrack? GetAlternateTrack(int index)
    {
        return movie == null || sequenceTrack == null
            ? null : movie.GetAlternateTrack(sequenceTrack.GetTrackId(), index);
    }

    /// <summary>Returns the number of auxiliary tracks associated with the selected sequence.</summary>
    /// <returns>The associated auxiliary-track count.</returns>
    public int GetAuxiliaryTrackCount()
    {
        return movie == null || sequenceTrack == null
            ? 0 : movie.GetAuxiliaryTrackCount(sequenceTrack.GetTrackId());
    }

    /// <summary>Returns one auxiliary track associated with the selected sequence.</summary>
    /// <param name="index">The zero-based auxiliary-track index.</param>
    /// <returns>The auxiliary track, or null when unavailable.</returns>
    public AvifTrack? GetAuxiliaryTrack(int index)
    {
        return movie == null || sequenceTrack == null
            ? null : movie.GetAuxiliaryTrack(sequenceTrack.GetTrackId(), index);
    }

    /// <summary>Returns the number of frames in the selected timed sequence.</summary>
    /// <returns>The color-track sample count, or zero for a primary-item source.</returns>
    public int GetFrameCount()
    {
        return sequenceTrack == null ? 0 : sequenceTrack.GetSampleCount();
    }

    /// <summary>Reports whether every associated track can begin decoding at a frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns><c>true</c> when the color and all auxiliary samples are synchronization samples.</returns>
    public bool IsFrameKeyFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= GetFrameCount())
        {
            return false;
        }
        int trackIndex = 0;
        while (trackIndex < GetDecodeTrackCount())
        {
            AvifTrack? track = GetDecodeTrack(trackIndex);
            AvifSample? sample = (track ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(frameIndex);
            if (sample == null || !sample.IsSync())
            {
                return false;
            }
            trackIndex++;
        }
        return true;
    }

    /// <summary>Returns the nearest synchronization frame at or before a frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The nearest keyframe index, or minus one for an invalid index.</returns>
    public int GetNearestKeyFrameIndex(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= GetFrameCount())
        {
            return -1;
        }
        int candidate = frameIndex;
        while (candidate > 0 && !IsFrameKeyFrame(candidate))
        {
            candidate--;
        }
        return candidate;
    }

    /// <summary>Returns timing for one sequence frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The frame timing, or null for a primary item or invalid index.</returns>
    public AvifFrameTiming? GetFrameTiming(int frameIndex)
    {
        if (sequenceTrack == null)
        {
            return null;
        }
        AvifSample? sample = sequenceTrack.GetSample(frameIndex);
        if (sample == null)
        {
            return null;
        }
        return new AvifFrameTiming(
            sequenceTrack.GetMediaTimescale(),
            sample.GetDecodeTime(),
            sample.GetDuration());
    }

    /// <summary>Returns the maximal encoded range containing one frame on all associated tracks.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The encompassing range, or null for a primary item or invalid index.</returns>
    public AvifByteRange? GetFrameEncodedRange(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= GetFrameCount())
        {
            return null;
        }
        return GetRange(frameIndex, frameIndex);
    }

    /// <summary>Returns the maximal encoded range needed to decode one frame from its keyframe.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The encompassing dependency range, or null for an invalid index.</returns>
    public AvifByteRange? GetFrameDecodeRange(int frameIndex)
    {
        int startIndex = GetNearestKeyFrameIndex(frameIndex);
        return startIndex < 0 ? null : GetRange(startIndex, frameIndex);
    }

    /// <summary>Creates an independent stateful decode cursor for this timed source.</summary>
    /// <returns>The sequence cursor, or null for a primary-item source.</returns>
    public AvifSequence? CreateSequence()
    {
        return sequenceTrack == null ? null : AvifSequence.FromSource(this);
    }

    int GetDecodeTrackCount()
    {
        return sequenceTrack == null ? 0 : 1 + GetAuxiliaryTrackCount();
    }

    AvifTrack? GetDecodeTrack(int index)
    {
        if (sequenceTrack == null || index < 0)
        {
            return null;
        }
        return index == 0 ? sequenceTrack : GetAuxiliaryTrack(index - 1);
    }

    AvifByteRange? GetRange(int startFrameIndex, int endFrameIndex)
    {
        ulong minimum = ulong.MaxValue;
        ulong maximum = 0;
        int frameIndex = startFrameIndex;
        while (frameIndex <= endFrameIndex)
        {
            int trackIndex = 0;
            while (trackIndex < GetDecodeTrackCount())
            {
                AvifTrack? track = GetDecodeTrack(trackIndex);
                AvifSample? sample = (track ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(frameIndex);
                if (sample == null
                    || sample.GetOffset() > ulong.MaxValue - sample.GetSize())
                {
                    return null;
                }
                ulong end = sample.GetOffset() + sample.GetSize();
                if (sample.GetOffset() < minimum)
                {
                    minimum = sample.GetOffset();
                }
                if (end > maximum)
                {
                    maximum = end;
                }
                trackIndex++;
            }
            frameIndex++;
        }
        return minimum == ulong.MaxValue
            ? null : new AvifByteRange(minimum, maximum - minimum);
    }
}
