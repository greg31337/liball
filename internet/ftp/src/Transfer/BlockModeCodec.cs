namespace ftp;

internal static class BlockModeCodec
{
    internal static FtpBlockModeState CreateFramingState(FtpTransferOptions? options)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (options.Mode != FtpTransferMode.Block)
        {
            throw new FtpConfigurationException("Block-mode framing requires MODE B.", null);
        }

        return new FtpBlockModeState(options.Structure);
    }
}
