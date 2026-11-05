using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Exposes validated movie timing and timed image tracks.</summary>
public class AvifMovie : IAvifMovie
{
    ulong creationTime;
    ulong modificationTime;
    uint timescale;
    ulong duration;
    uint nextTrackId;
    List<AvifTrack> tracks;

    internal AvifMovie(ParsedMovie? parsed)
    {
        creationTime = (parsed ?? throw new InvalidOperationException("Invalid internal state.")).CreationTime;
        modificationTime = parsed.ModificationTime;
        timescale = parsed.Timescale;
        duration = parsed.Duration;
        nextTrackId = parsed.NextTrackId;
        tracks = new List<AvifTrack>();
        int index = 0;
        while (index < parsed.Tracks.Count)
        {
            tracks.Add(new AvifTrack(parsed.Tracks[index], timescale));
            index++;
        }
    }

    /// <summary>Returns the movie creation time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned creation time.</returns>
    public ulong GetCreationTime()
    {
        return creationTime;
    }

    /// <summary>Returns the movie modification time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned modification time.</returns>
    public ulong GetModificationTime()
    {
        return modificationTime;
    }

    /// <summary>Returns the number of movie time units per second.</summary>
    /// <returns>The positive movie timescale.</returns>
    public uint GetTimescale()
    {
        return timescale;
    }

    /// <summary>Returns the movie presentation duration.</summary>
    /// <returns>The duration in movie-timescale units.</returns>
    public ulong GetDuration()
    {
        return duration;
    }

    /// <summary>Reports whether the movie duration is indefinite.</summary>
    /// <returns><c>true</c> for an all-ones duration.</returns>
    public bool IsDurationIndefinite()
    {
        return duration == ulong.MaxValue;
    }

    /// <summary>Returns the movie duration in seconds.</summary>
    /// <returns>The duration, zero when the timescale is unavailable, or positive infinity when indefinite.</returns>
    public double GetDurationSeconds()
    {
        if (IsDurationIndefinite())
        {
            return double.PositiveInfinity;
        }
        return timescale == 0 ? 0 : duration / (double)timescale;
    }

    /// <summary>Returns the next unused track identifier declared by the movie.</summary>
    /// <returns>The nonzero next track identifier.</returns>
    public uint GetNextTrackId()
    {
        return nextTrackId;
    }

    /// <summary>Returns the number of parsed tracks.</summary>
    /// <returns>The track count.</returns>
    public int GetTrackCount()
    {
        return tracks.Count;
    }

    /// <summary>Returns one parsed track.</summary>
    /// <param name="index">The zero-based track index.</param>
    /// <returns>The track, or null for an invalid index.</returns>
    public AvifTrack? GetTrack(int index)
    {
        if (index < 0 || index >= tracks.Count)
        {
            return null;
        }
        return tracks[index];
    }

    /// <summary>Finds a track by its nonzero identifier.</summary>
    /// <param name="trackId">The track identifier.</param>
    /// <returns>The track, or null when absent.</returns>
    public AvifTrack? FindTrack(uint trackId)
    {
        int index = 0;
        while (index < tracks.Count)
        {
            if (tracks[index].GetTrackId() == trackId)
            {
                return tracks[index];
            }
            index++;
        }
        return null;
    }

    /// <summary>Returns the number of non-auxiliary visual tracks.</summary>
    /// <returns>The visual-track count.</returns>
    public int GetVisualTrackCount()
    {
        int count = 0;
        int index = 0;
        while (index < tracks.Count)
        {
            if (tracks[index].IsVisual() && !tracks[index].IsAuxiliary())
            {
                count++;
            }
            index++;
        }
        return count;
    }

    /// <summary>Returns one non-auxiliary visual track in source order.</summary>
    /// <param name="index">The zero-based visual-track index.</param>
    /// <returns>The track, or null for an invalid index.</returns>
    public AvifTrack? GetVisualTrack(int index)
    {
        if (index < 0)
        {
            return null;
        }
        int visualIndex = 0;
        int trackIndex = 0;
        while (trackIndex < tracks.Count)
        {
            AvifTrack track = tracks[trackIndex];
            if (track.IsVisual() && !track.IsAuxiliary())
            {
                if (visualIndex == index)
                {
                    return track;
                }
                visualIndex++;
            }
            trackIndex++;
        }
        return null;
    }

    /// <summary>Returns the number of alternate visual tracks for one track.</summary>
    /// <param name="trackId">The selected visual track identifier.</param>
    /// <returns>The count of other tracks in the same nonzero alternate group.</returns>
    public int GetAlternateTrackCount(uint trackId)
    {
        AvifTrack? selected = FindTrack(trackId);
        if (selected == null || selected.GetAlternateGroup() == 0)
        {
            return 0;
        }
        int count = 0;
        int index = 0;
        while (index < tracks.Count)
        {
            AvifTrack candidate = tracks[index];
            if (candidate.GetTrackId() != trackId
                && candidate.IsVisual()
                && !candidate.IsAuxiliary()
                && candidate.GetAlternateGroup() == selected.GetAlternateGroup())
            {
                count++;
            }
            index++;
        }
        return count;
    }

    /// <summary>Returns one alternate visual track for a selected track.</summary>
    /// <param name="trackId">The selected visual track identifier.</param>
    /// <param name="index">The zero-based alternate index.</param>
    /// <returns>The alternate track, or null for an invalid identifier or index.</returns>
    public AvifTrack? GetAlternateTrack(uint trackId, int index)
    {
        AvifTrack? selected = FindTrack(trackId);
        if (selected == null || selected.GetAlternateGroup() == 0 || index < 0)
        {
            return null;
        }
        int alternateIndex = 0;
        int trackIndex = 0;
        while (trackIndex < tracks.Count)
        {
            AvifTrack candidate = tracks[trackIndex];
            if (candidate.GetTrackId() != trackId
                && candidate.IsVisual()
                && !candidate.IsAuxiliary()
                && candidate.GetAlternateGroup() == selected.GetAlternateGroup())
            {
                if (alternateIndex == index)
                {
                    return candidate;
                }
                alternateIndex++;
            }
            trackIndex++;
        }
        return null;
    }

    /// <summary>Returns the number of auxiliary tracks associated with one master track.</summary>
    /// <param name="masterTrackId">The master visual track identifier.</param>
    /// <returns>The associated auxiliary-track count.</returns>
    public int GetAuxiliaryTrackCount(uint masterTrackId)
    {
        int count = 0;
        int index = 0;
        while (index < tracks.Count)
        {
            if (tracks[index].IsAuxiliaryFor(masterTrackId))
            {
                count++;
            }
            index++;
        }
        return count;
    }

    /// <summary>Returns one auxiliary track associated with a master track.</summary>
    /// <param name="masterTrackId">The master visual track identifier.</param>
    /// <param name="index">The zero-based auxiliary index.</param>
    /// <returns>The auxiliary track, or null for an invalid identifier or index.</returns>
    public AvifTrack? GetAuxiliaryTrack(uint masterTrackId, int index)
    {
        if (index < 0)
        {
            return null;
        }
        int auxiliaryIndex = 0;
        int trackIndex = 0;
        while (trackIndex < tracks.Count)
        {
            AvifTrack candidate = tracks[trackIndex];
            if (candidate.IsAuxiliaryFor(masterTrackId))
            {
                if (auxiliaryIndex == index)
                {
                    return candidate;
                }
                auxiliaryIndex++;
            }
            trackIndex++;
        }
        return null;
    }
}
