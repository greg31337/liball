namespace rtp;

/// <summary>
/// Contains the type and format discriminators of an RTCP feedback packet.
/// </summary>
public class RtcpFeedbackPacket
{
    /// <summary>The RTCP feedback packet category.</summary>
    public RtcpFeedbackPacketType PacketType;

    /// <summary>The original five-bit feedback-message format.</summary>
    public byte Format;

    /// <summary>The transport-layer feedback format when applicable.</summary>
    public RtcpTransportFeedbackFormat TransportFormat;

    /// <summary>The payload-specific feedback format when applicable.</summary>
    public RtcpPayloadFeedbackFormat PayloadFormat;

    /// <summary>The SSRC of the feedback packet sender.</summary>
    public uint SenderSourceIdentifier;

    /// <summary>The SSRC of the media source described by the feedback.</summary>
    public uint MediaSourceIdentifier;

    /// <summary>The Generic NACK data when carried by the packet.</summary>
    public RtcpGenericNack? GenericNack;

    /// <summary>The picture loss indication when carried by the packet.</summary>
    public RtcpPictureLossIndication? PictureLossIndication;

    /// <summary>The slice loss indication when carried by the packet.</summary>
    public RtcpSliceLossIndication? SliceLossIndication;

    /// <summary>The reference picture selection indication when carried by the packet.</summary>
    public RtcpReferencePictureSelectionIndication? ReferencePictureSelectionIndication;

    /// <summary>The application-layer feedback when carried by the packet.</summary>
    public RtcpApplicationFeedback? ApplicationFeedback;

    /// <summary>The full intra request when carried by the packet.</summary>
    public RtcpFullIntraRequest? FullIntraRequest;

    /// <summary>The temporary maximum bitrate request or notification when carried.</summary>
    public RtcpBitrateControl? BitrateControl;

    /// <summary>The temporal-spatial trade-off entries carried by the packet.</summary>
    public RtcpTemporalSpatialTradeoff[] TemporalSpatialTradeoffs = [];

    /// <summary>The rapid resynchronization request when carried by the packet.</summary>
    public RtcpRapidResynchronizationRequest? RapidResynchronizationRequest;

    /// <summary>The congestion-control feedback when carried by the packet.</summary>
    public RtcpCongestionControlFeedback? CongestionControlFeedback;

    /// <summary>The losslessly preserved raw feedback control information bytes.</summary>
    public byte[] RawFeedbackControlInformation = [];

    internal RtcpFeedbackPacket Copy()
    {
        RtcpFeedbackPacket copy = new RtcpFeedbackPacket();
        copy.PacketType = PacketType;
        copy.Format = Format;
        copy.TransportFormat = TransportFormat;
        copy.PayloadFormat = PayloadFormat;
        copy.SenderSourceIdentifier = SenderSourceIdentifier;
        copy.MediaSourceIdentifier = MediaSourceIdentifier;
        if (GenericNack != null)
        {
            copy.GenericNack = GenericNack.Copy();
        }

        if (PictureLossIndication != null)
        {
            copy.PictureLossIndication = PictureLossIndication.Copy();
        }

        if (SliceLossIndication != null)
        {
            copy.SliceLossIndication = SliceLossIndication.Copy();
        }

        if (ReferencePictureSelectionIndication != null)
        {
            copy.ReferencePictureSelectionIndication = ReferencePictureSelectionIndication.Copy();
        }

        if (ApplicationFeedback != null)
        {
            copy.ApplicationFeedback = ApplicationFeedback.Copy();
        }

        if (FullIntraRequest != null)
        {
            copy.FullIntraRequest = FullIntraRequest.Copy();
        }

        if (BitrateControl != null)
        {
            copy.BitrateControl = BitrateControl.Copy();
        }

        copy.TemporalSpatialTradeoffs =
            new RtcpTemporalSpatialTradeoff[TemporalSpatialTradeoffs.Length];
        int tradeoffIndex = 0;
        while (tradeoffIndex < TemporalSpatialTradeoffs.Length)
        {
            copy.TemporalSpatialTradeoffs[tradeoffIndex] =
                TemporalSpatialTradeoffs[tradeoffIndex].Copy();
            tradeoffIndex = tradeoffIndex + 1;
        }

        if (RapidResynchronizationRequest != null)
        {
            copy.RapidResynchronizationRequest = RapidResynchronizationRequest.Copy();
        }

        if (CongestionControlFeedback != null)
        {
            copy.CongestionControlFeedback = CongestionControlFeedback.Copy();
        }

        copy.RawFeedbackControlInformation = (byte[])RawFeedbackControlInformation.Clone();
        return copy;
    }
}
