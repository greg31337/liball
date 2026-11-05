using System;

namespace rtp;

internal static class RtcpApplicationFeedbackWireCodec
{
    internal static string? GetApplicationFeedbackValidationError(
        RtcpApplicationFeedback? applicationFeedback)
    {
        if (applicationFeedback == null)
        {
            return "The application feedback body is required.";
        }

        if (applicationFeedback.Data == null)
        {
            return "The application feedback data is required.";
        }

        if (applicationFeedback.Data.Length %
            RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "Application feedback data must be 32-bit aligned.";
        }

        return null;
    }

    internal static string? GetApplicationFeedbackDecodeError(
        ReadOnlySpan<byte> source)
    {
        if (source.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "Application feedback data must be 32-bit aligned.";
        }

        return null;
    }

    internal static RtcpApplicationFeedback DecodeApplicationFeedback(
        ReadOnlySpan<byte> source)
    {
        RtcpApplicationFeedback applicationFeedback =
            new RtcpApplicationFeedback();
        applicationFeedback.Data = source.ToArray();
        return applicationFeedback;
    }

    internal static void EncodeApplicationFeedback(
        RtcpApplicationFeedback applicationFeedback,
        Span<byte> destination)
    {
        NetworkBinaryWriter.WriteBytes(
            destination,
            0,
            applicationFeedback.Data);
    }
}
