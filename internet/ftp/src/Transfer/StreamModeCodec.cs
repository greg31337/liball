namespace ftp;

internal static class StreamModeCodec
{
    internal static FtpStreamModeState CreateFramingState(
        FtpTransferOptions? options,
        bool hasRestartOffset,
        long restartOffset)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (options.Mode != FtpTransferMode.Stream &&
            options.Mode != FtpTransferMode.Deflate)
        {
            throw new FtpConfigurationException("Stream-based framing requires MODE S or MODE Z.", null);
        }

        return new FtpStreamModeState(options.Structure, hasRestartOffset, restartOffset);
    }
}
