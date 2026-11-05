namespace ftp;

internal interface IFtpTransferPumpTransform
{
    FtpTransferPumpChunk[] TransformChunk(byte[] data, int count, bool finalChunk);
}
