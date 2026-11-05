using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifSample"/>.</summary>
public interface IAvifSample
{
    /// <summary>Returns the zero-based sample index.</summary>
    /// <returns>The sample index.</returns>
    int GetIndex();

    /// <summary>Returns the one-based sample-description index.</summary>
    /// <returns>The description index.</returns>
    int GetDescriptionIndex();

    /// <summary>Returns the absolute encoded sample offset.</summary>
    /// <returns>The file offset.</returns>
    ulong GetOffset();

    /// <summary>Returns the encoded sample size.</summary>
    /// <returns>The size in bytes.</returns>
    uint GetSize();

    /// <summary>Returns the sample decode time in media-timescale units.</summary>
    /// <returns>The decode time.</returns>
    ulong GetDecodeTime();

    /// <summary>Returns the sample duration in media-timescale units.</summary>
    /// <returns>The positive duration.</returns>
    uint GetDuration();

    /// <summary>Reports whether the sample is a random-access synchronization sample.</summary>
    /// <returns><c>true</c> for a synchronization sample.</returns>
    bool IsSync();

    /// <summary>Returns the ISO sample-leading dependency value.</summary>
    /// <returns>A value from zero through two.</returns>
    int GetLeadingDependency();

    /// <summary>Returns whether this sample depends on other samples.</summary>
    /// <returns>The ISO dependency value from zero through two.</returns>
    int GetDependsOn();

    /// <summary>Returns whether other samples depend on this sample.</summary>
    /// <returns>The ISO dependency value from zero through two.</returns>
    int GetIsDependedOn();

    /// <summary>Returns whether the sample contains redundant coding.</summary>
    /// <returns>The ISO redundancy value from zero through two.</returns>
    int GetRedundancy();

    /// <summary>Reports whether this sample contains a delayed random-access point.</summary>
    /// <returns><c>true</c> for an <c>av1f</c> group member.</returns>
    bool IsForwardKeyFrame();

    /// <summary>Returns the forward recovery distance.</summary>
    /// <returns>The number of intervening samples, or minus one when unavailable.</returns>
    int GetForwardKeyFrameDistance();

    /// <summary>Returns the zero-based dependent recovery-point sample index.</summary>
    /// <returns>The recovery index, or minus one when unavailable.</returns>
    int GetRecoverySampleIndex();

    /// <summary>Reports whether the temporal unit contains multiple frames.</summary>
    /// <returns><c>true</c> for an <c>av1m</c> group member.</returns>
    bool HasMultipleFrames();

    /// <summary>Reports whether the sample starts with an AV1 switch frame.</summary>
    /// <returns><c>true</c> for an <c>av1s</c> group member.</returns>
    bool IsSwitchFrame();

    /// <summary>Returns the number of AV1 metadata group memberships.</summary>
    /// <returns>The metadata group count.</returns>
    int GetMetadataGroupCount();

    /// <summary>Returns one AV1 metadata group membership.</summary>
    /// <param name="indexValue">The zero-based membership index.</param>
    /// <returns>The membership, or null for an invalid index.</returns>
    AvifSampleMetadataGroup? GetMetadataGroup(int indexValue);
}
