namespace rtp;

/// <summary>
/// Contains RFC 6465 mixer-to-client CSRC audio levels.
/// </summary>
public class RtpMixerAudioLevelExtension
{
    /// <summary>The audio levels in the same order as the RTP CSRC list.</summary>
    public byte[] Levels = [];

    internal RtpMixerAudioLevelExtension Copy()
    {
        RtpMixerAudioLevelExtension copy = new RtpMixerAudioLevelExtension();
        copy.Levels = (byte[])Levels.Clone();
        return copy;
    }
}
