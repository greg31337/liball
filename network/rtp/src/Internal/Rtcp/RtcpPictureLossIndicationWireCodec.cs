using System;

namespace rtp;

internal static class RtcpPictureLossIndicationWireCodec
{
    internal static string? GetPictureLossIndicationValidationError(
        RtcpPictureLossIndication? pictureLossIndication)
    {
        if (pictureLossIndication == null)
        {
            return "The picture loss indication body is required.";
        }

        if (!pictureLossIndication.Requested)
        {
            return "A picture loss indication must request a picture refresh.";
        }

        return null;
    }

    internal static string? GetPictureLossIndicationDecodeError(
        ReadOnlySpan<byte> source)
    {
        if (source.Length != 0)
        {
            return "A picture loss indication must not contain feedback control information.";
        }

        return null;
    }

    internal static RtcpPictureLossIndication DecodePictureLossIndication()
    {
        RtcpPictureLossIndication pictureLossIndication =
            new RtcpPictureLossIndication();
        pictureLossIndication.Requested = true;
        return pictureLossIndication;
    }
}
