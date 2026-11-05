using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifMovie"/>.</summary>
public interface IAvifMovie
{
    /// <summary>Returns the movie creation time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned creation time.</returns>
    ulong GetCreationTime();

    /// <summary>Returns the movie modification time in seconds since the ISO epoch.</summary>
    /// <returns>The unsigned modification time.</returns>
    ulong GetModificationTime();

    /// <summary>Returns the number of movie time units per second.</summary>
    /// <returns>The positive movie timescale.</returns>
    uint GetTimescale();

    /// <summary>Returns the movie presentation duration.</summary>
    /// <returns>The duration in movie-timescale units.</returns>
    ulong GetDuration();

    /// <summary>Reports whether the movie duration is indefinite.</summary>
    /// <returns><c>true</c> for an all-ones duration.</returns>
    bool IsDurationIndefinite();

    /// <summary>Returns the movie duration in seconds.</summary>
    /// <returns>The duration or positive infinity when indefinite.</returns>
    double GetDurationSeconds();

    /// <summary>Returns the next unused track identifier declared by the movie.</summary>
    /// <returns>The nonzero next track identifier.</returns>
    uint GetNextTrackId();

    /// <summary>Returns the number of parsed tracks.</summary>
    /// <returns>The track count.</returns>
    int GetTrackCount();

    /// <summary>Returns one parsed track.</summary>
    /// <param name="index">The zero-based track index.</param>
    /// <returns>The track, or null for an invalid index.</returns>
    AvifTrack? GetTrack(int index);

    /// <summary>Finds a track by its nonzero identifier.</summary>
    /// <param name="trackId">The track identifier.</param>
    /// <returns>The track, or null when absent.</returns>
    AvifTrack? FindTrack(uint trackId);

    /// <summary>Returns the number of non-auxiliary visual tracks.</summary>
    /// <returns>The visual-track count.</returns>
    int GetVisualTrackCount();

    /// <summary>Returns one non-auxiliary visual track in source order.</summary>
    /// <param name="index">The zero-based visual-track index.</param>
    /// <returns>The track, or null for an invalid index.</returns>
    AvifTrack? GetVisualTrack(int index);

    /// <summary>Returns the number of alternate visual tracks for one track.</summary>
    /// <param name="trackId">The selected visual track identifier.</param>
    /// <returns>The count of other tracks in the same nonzero alternate group.</returns>
    int GetAlternateTrackCount(uint trackId);

    /// <summary>Returns one alternate visual track for a selected track.</summary>
    /// <param name="trackId">The selected visual track identifier.</param>
    /// <param name="index">The zero-based alternate index.</param>
    /// <returns>The alternate track, or null for an invalid identifier or index.</returns>
    AvifTrack? GetAlternateTrack(uint trackId, int index);

    /// <summary>Returns the number of auxiliary tracks associated with one master track.</summary>
    /// <param name="masterTrackId">The master visual track identifier.</param>
    /// <returns>The associated auxiliary-track count.</returns>
    int GetAuxiliaryTrackCount(uint masterTrackId);

    /// <summary>Returns one auxiliary track associated with a master track.</summary>
    /// <param name="masterTrackId">The master visual track identifier.</param>
    /// <param name="index">The zero-based auxiliary index.</param>
    /// <returns>The auxiliary track, or null for an invalid identifier or index.</returns>
    AvifTrack? GetAuxiliaryTrack(uint masterTrackId, int index);
}
