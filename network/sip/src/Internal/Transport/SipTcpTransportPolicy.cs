namespace sip;

internal static class SipTcpTransportPolicy
{
    internal static bool IsOversizedUdpFallback(SipTransportKind requestedTransport,
        int byteCount)
    {
        return requestedTransport == SipTransportKind.Udp
            && byteCount > SipDefaults.UdpSwitchThresholdBytes;
    }
}
