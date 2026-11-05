using System.Collections.Generic;

namespace avif;

/// <summary>Describes one expanded timed-media sample.</summary>
public class AvifSample : IAvifSample
{
    int index;
    int descriptionIndex;
    ulong offset;
    uint size;
    ulong decodeTime;
    uint duration;
    bool sync;
    int leading;
    int dependsOn;
    int isDependedOn;
    int redundancy;
    bool forwardKeyFrame;
    int forwardKeyFrameDistance;
    int recoverySampleIndex;
    bool multipleFrames;
    bool switchFrame;
    List<AvifSampleMetadataGroup> metadataGroups;

    internal AvifSample(ParsedTrackSample parsed)
    {
        index = parsed.Index;
        descriptionIndex = parsed.DescriptionIndex;
        offset = parsed.Offset;
        size = parsed.Size;
        decodeTime = parsed.DecodeTime;
        duration = parsed.Duration;
        sync = parsed.Sync;
        leading = parsed.Leading;
        dependsOn = parsed.DependsOn;
        isDependedOn = parsed.IsDependedOn;
        redundancy = parsed.Redundancy;
        forwardKeyFrame = parsed.ForwardKeyFrame;
        forwardKeyFrameDistance = parsed.ForwardKeyFrameDistance;
        recoverySampleIndex = parsed.RecoverySampleIndex;
        multipleFrames = parsed.MultipleFrames;
        switchFrame = parsed.SwitchFrame;
        metadataGroups = new List<AvifSampleMetadataGroup>();
        int metadataIndex = 0;
        while (metadataIndex < parsed.MetadataGroups.Count)
        {
            metadataGroups.Add(new AvifSampleMetadataGroup(
                parsed.MetadataGroups[metadataIndex]));
            metadataIndex++;
        }
    }

    /// <summary>Returns the zero-based sample index.</summary>
    /// <returns>The sample index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>Returns the one-based sample-description index.</summary>
    /// <returns>The description index.</returns>
    public int GetDescriptionIndex()
    {
        return descriptionIndex;
    }

    /// <summary>Returns the absolute encoded sample offset.</summary>
    /// <returns>The file offset.</returns>
    public ulong GetOffset()
    {
        return offset;
    }

    /// <summary>Returns the encoded sample size.</summary>
    /// <returns>The size in bytes.</returns>
    public uint GetSize()
    {
        return size;
    }

    /// <summary>Returns the sample decode time in media-timescale units.</summary>
    /// <returns>The decode time.</returns>
    public ulong GetDecodeTime()
    {
        return decodeTime;
    }

    /// <summary>Returns the sample duration in media-timescale units.</summary>
    /// <returns>The positive duration.</returns>
    public uint GetDuration()
    {
        return duration;
    }

    /// <summary>Reports whether the sample is a random-access synchronization sample.</summary>
    /// <returns><c>true</c> for a synchronization sample.</returns>
    public bool IsSync()
    {
        return sync;
    }

    /// <summary>Returns the ISO sample-leading dependency value.</summary>
    /// <returns>A value from zero through two.</returns>
    public int GetLeadingDependency()
    {
        return leading;
    }

    /// <summary>Returns whether this sample depends on other samples.</summary>
    /// <returns>The ISO dependency value from zero through two.</returns>
    public int GetDependsOn()
    {
        return dependsOn;
    }

    /// <summary>Returns whether other samples depend on this sample.</summary>
    /// <returns>The ISO dependency value from zero through two.</returns>
    public int GetIsDependedOn()
    {
        return isDependedOn;
    }

    /// <summary>Returns whether the sample contains redundant coding.</summary>
    /// <returns>The ISO redundancy value from zero through two.</returns>
    public int GetRedundancy()
    {
        return redundancy;
    }

    /// <summary>Reports whether this sample contains a delayed random-access point.</summary>
    /// <returns><c>true</c> for an <c>av1f</c> group member.</returns>
    public bool IsForwardKeyFrame()
    {
        return forwardKeyFrame;
    }

    /// <summary>Returns the forward recovery distance.</summary>
    /// <returns>The number of intervening samples, or minus one when unavailable.</returns>
    public int GetForwardKeyFrameDistance()
    {
        return forwardKeyFrameDistance;
    }

    /// <summary>Returns the zero-based dependent recovery-point sample index.</summary>
    /// <returns>The recovery index, or minus one when unavailable.</returns>
    public int GetRecoverySampleIndex()
    {
        return recoverySampleIndex;
    }

    /// <summary>Reports whether the temporal unit contains multiple frames.</summary>
    /// <returns><c>true</c> for an <c>av1m</c> group member.</returns>
    public bool HasMultipleFrames()
    {
        return multipleFrames;
    }

    /// <summary>Reports whether the sample starts with an AV1 switch frame.</summary>
    /// <returns><c>true</c> for an <c>av1s</c> group member.</returns>
    public bool IsSwitchFrame()
    {
        return switchFrame;
    }

    /// <summary>Returns the number of AV1 metadata group memberships.</summary>
    /// <returns>The metadata group count.</returns>
    public int GetMetadataGroupCount()
    {
        return metadataGroups.Count;
    }

    /// <summary>Returns one AV1 metadata group membership.</summary>
    /// <param name="indexValue">The zero-based membership index.</param>
    /// <returns>The membership, or null for an invalid index.</returns>
    public AvifSampleMetadataGroup? GetMetadataGroup(int indexValue)
    {
        if (indexValue < 0 || indexValue >= metadataGroups.Count)
        {
            return null;
        }
        return metadataGroups[indexValue];
    }
}
