namespace ftp;

internal class FtpSizeValue
{
    internal bool HasValue;
    internal long Value;
    internal string RawValue;

    internal FtpSizeValue()
    {
        HasValue = false;
        Value = 0;
        RawValue = "";
    }
}
