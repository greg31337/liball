namespace avif;

/// <summary>Reports one frame decision from an AV1 sequence authoring plan.</summary>
public class AvifFrameEncodingDecision
{
    /// <summary>The source frame index in presentation order.</summary>
    public int PresentationIndex;

    /// <summary>The zero-based coded order index.</summary>
    public int EncodeIndex;

    /// <summary>Whether the frame is a random-access key frame.</summary>
    public bool KeyFrame;

    /// <summary>Whether the frame reuses a stored reference for display.</summary>
    public bool ShowExistingFrame;

    /// <summary>The stored reference slot, or negative one when not reused.</summary>
    public int ShowExistingSlot;

    /// <summary>The eight-bit stored-reference refresh mask.</summary>
    public int RefreshFrameFlags;

    /// <summary>The decode-to-presentation time offset in sequence units.</summary>
    public long CompositionOffset;

    internal AvifFrameEncodingDecision()
    {
        PresentationIndex = -1;
        EncodeIndex = -1;
        ShowExistingSlot = -1;
    }
}
