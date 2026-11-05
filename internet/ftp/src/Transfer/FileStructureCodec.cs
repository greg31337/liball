namespace ftp;

internal static class FileStructureCodec
{
    internal static FtpFileStructureState CreateFramingState(FtpTransferOptions? options)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (options.Structure != FtpFileStructure.File)
        {
            throw new FtpConfigurationException("File-structure framing requires STRU F.", null);
        }

        return new FtpFileStructureState(options.Mode);
    }
}
