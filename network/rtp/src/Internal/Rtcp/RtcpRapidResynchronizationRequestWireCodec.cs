using System;

namespace rtp;

internal static class RtcpRapidResynchronizationRequestWireCodec
{
    internal static string? GetRapidResynchronizationRequestValidationError(
        RtcpRapidResynchronizationRequest? request)
    {
        if (request == null)
        {
            return "The rapid resynchronization request body is required.";
        }

        if (!request.Requested)
        {
            return "A rapid resynchronization request must request synchronization.";
        }

        return null;
    }

    internal static string? GetRapidResynchronizationRequestDecodeError(
        ReadOnlySpan<byte> source)
    {
        if (source.Length != 0)
        {
            return "A rapid resynchronization request must not contain feedback control information.";
        }

        return null;
    }

    internal static RtcpRapidResynchronizationRequest
        DecodeRapidResynchronizationRequest()
    {
        RtcpRapidResynchronizationRequest request =
            new RtcpRapidResynchronizationRequest();
        request.Requested = true;
        return request;
    }
}
