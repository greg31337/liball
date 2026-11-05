namespace ftp;

internal static class FtpClientMarkerRestart
{
    internal static bool IsMarkerMode(FtpTransferOptions options)
    {
        return options.Mode == FtpTransferMode.Block ||
            options.Mode == FtpTransferMode.Compressed;
    }

    internal static FtpRestartCheckpoint RequireLocalCheckpoint(
        FtpClientState state)
    {
        if (state.LatestLocalRestartMarker == null)
        {
            throw new FtpConfigurationException(
                "A local block/compressed restart checkpoint is unavailable.", null);
        }
        return new RestartMarkerCodec().ParseMarker(
            state.LatestLocalRestartMarker);
    }

    internal static string RequireServerMarker(FtpClientState state)
    {
        if (state.LatestRestartMarker == null)
        {
            throw new FtpConfigurationException(
                "A server block/compressed restart marker is unavailable.", null);
        }
        return new RestartMarkerCodec().AcceptOpaqueMarker(
            state.LatestRestartMarker, 127);
    }

    internal static string RecordMarkerReply(
        FtpClientState state,
        FtpReply reply)
    {
        string text = FtpReplyExtractor.ExtractRestartMarker(reply);
        if (!text.StartsWith("MARK ", System.StringComparison.Ordinal))
        {
            throw new FtpProtocolException(
                "An FTP restart marker reply must begin with 'MARK '.", null);
        }
        string markers = text.Substring(5);
        int separator = markers.IndexOf(" = ", System.StringComparison.Ordinal);
        if (separator <= 0 || separator + 3 >= markers.Length)
        {
            throw new FtpProtocolException(
                "An FTP restart marker reply must contain local and server markers.",
                null);
        }

        RestartMarkerCodec codec = new RestartMarkerCodec();
        string localMarker = codec.AcceptOpaqueMarker(
            markers.Substring(0, separator), 127);
        string serverMarker = codec.AcceptOpaqueMarker(
            markers.Substring(separator + 3), 127);
        state.LatestLocalRestartMarker = localMarker;
        state.LatestRestartMarker = serverMarker;
        return serverMarker;
    }
}
