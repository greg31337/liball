namespace ftp;

internal static class RecordStructureCodec
{
    internal static FtpRecordStructureState CreateFramingState(FtpTransferOptions? options)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        if (options == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (options.Structure != FtpFileStructure.Record)
        {
            throw new FtpConfigurationException("Record-structure framing requires STRU R.", null);
        }

        return new FtpRecordStructureState(options.Mode, options.Type);
    }
}
