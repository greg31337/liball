namespace rtp;

/// <summary>
/// Contains an RFC 6464 client-to-mixer audio level.
/// </summary>
public class RtpAudioLevelExtension
{
    /// <summary>Whether the voice-activity flag is set.</summary>
    public bool VoiceActivity;

    /// <summary>The audio level in negative decibels relative to overload.</summary>
    public byte Level;

    internal RtpAudioLevelExtension Copy()
    {
        RtpAudioLevelExtension copy = new RtpAudioLevelExtension();
        copy.VoiceActivity = VoiceActivity;
        copy.Level = Level;
        return copy;
    }
}
