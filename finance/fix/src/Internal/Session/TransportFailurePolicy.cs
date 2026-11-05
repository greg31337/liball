namespace fix;

internal static class TransportFailurePolicy
{
    internal static bool IsRetryableFailure(TransportState state, string code)
    {
        if (code.StartsWith("invalid-") || code.StartsWith("missing-") || code.StartsWith("tls-") || code.StartsWith("stream-") || code == "acceptor-connection-consumed" || code == "transport-faulted")
        {
            return false;
        }
        if (state == TransportState.Closed || state == TransportState.Disconnected)
        {
            return true;
        }
        return code == "tcp-connect-failed" ||
            code == "tcp-connect-timeout" ||
            code == "tcp-name-resolution-failed" ||
            code == "tcp-accept-failed" ||
            code == "tcp-accept-timeout" ||
            code == "tcp-bind-name-resolution-failed" ||
            code == "transport-closed" ||
            code == "transport-not-connected" ||
            code == "transport-end-of-stream" ||
            code == "transport-disconnected" ||
            code == "connect-timeout" ||
            code == "logon-timeout";
    }
}
