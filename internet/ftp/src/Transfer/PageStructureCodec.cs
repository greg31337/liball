namespace ftp;

internal static class PageStructureCodec
{
    internal static FtpPageStructureState CreateFramingState(FtpTransferOptions? options)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (options.Structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("Page-structure framing requires STRU P.", null);
        }

        int logicalByteSize = 8;
        if (options.Type == FtpTransferType.LocalByte)
        {
            logicalByteSize = options.LocalByteSize;
        }

        return new FtpPageStructureState(logicalByteSize);
    }
}
