namespace rtp;

/// <summary>
/// Contains an exception-less RTP wire-codec outcome and owned result data.
/// </summary>
public class RtpCodecResult
{
    /// <summary>Whether the codec operation completed successfully.</summary>
    public bool Success;

    /// <summary>The number of input bytes consumed.</summary>
    public int ConsumedCount;

    /// <summary>The number of output bytes written.</summary>
    public int WrittenCount;

    /// <summary>The decoded RTP packet when decoding succeeds.</summary>
    public RtpPacket? Packet;

    /// <summary>The decoded RTP header extension when extension decoding succeeds.</summary>
    public RtpHeaderExtension? HeaderExtension;

    /// <summary>The decoded client audio-level value when known-extension decoding succeeds.</summary>
    public RtpAudioLevelExtension? AudioLevelExtension;

    /// <summary>The decoded mixer audio-level values when known-extension decoding succeeds.</summary>
    public RtpMixerAudioLevelExtension? MixerAudioLevelExtension;

    /// <summary>The decoded NTP-64 value when known-extension decoding succeeds.</summary>
    public RtpNtp64Extension? Ntp64Extension;

    /// <summary>The decoded stream identifier when known-extension decoding succeeds.</summary>
    public RtpStreamIdentifierExtension? StreamIdentifierExtension;

    /// <summary>The owned byte data produced by an operation.</summary>
    public byte[] Data = [];

    /// <summary>A description of a recoverable codec failure.</summary>
    public string? ErrorMessage;

    internal RtpCodecResult Copy()
    {
        RtpCodecResult copy = new RtpCodecResult();
        copy.Success = Success;
        copy.ConsumedCount = ConsumedCount;
        copy.WrittenCount = WrittenCount;
        if (Packet != null)
        {
            copy.Packet = Packet.Copy();
        }

        if (HeaderExtension != null)
        {
            copy.HeaderExtension = HeaderExtension.Copy();
        }

        if (AudioLevelExtension != null)
        {
            copy.AudioLevelExtension = AudioLevelExtension.Copy();
        }

        if (MixerAudioLevelExtension != null)
        {
            copy.MixerAudioLevelExtension = MixerAudioLevelExtension.Copy();
        }

        if (Ntp64Extension != null)
        {
            copy.Ntp64Extension = Ntp64Extension.Copy();
        }

        if (StreamIdentifierExtension != null)
        {
            copy.StreamIdentifierExtension = StreamIdentifierExtension.Copy();
        }

        copy.Data = (byte[])Data.Clone();
        copy.ErrorMessage = ErrorMessage;
        return copy;
    }
}
