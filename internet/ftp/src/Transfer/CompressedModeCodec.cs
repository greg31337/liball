namespace ftp;

internal static class CompressedModeCodec
{
    internal static FtpCompressedModeState CreateFramingState(FtpTransferOptions? options)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (options.Mode != FtpTransferMode.Compressed)
        {
            throw new FtpConfigurationException("Compressed-mode framing requires MODE C.", null);
        }

        return new FtpCompressedModeState(options.Structure, options.Type);
    }
}
