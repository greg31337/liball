namespace sdp;

/// <summary>Represents a media-stream and optional track association.</summary>
public class SdpMediaStreamId
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the media-stream identifier.</summary>
    public string StreamId = string.Empty;
    /// <summary>Indicates whether a track identifier was present.</summary>
    public bool HasTrackId = false;
    /// <summary>Contains the track identifier.</summary>
    public string TrackId = string.Empty;
    /// <summary>Indicates whether this association came from source-level signaling.</summary>
    public bool SourceLevel = false;
    /// <summary>Contains the source identifier for source-level signaling.</summary>
    public uint SynchronizationSource = 0;
}
