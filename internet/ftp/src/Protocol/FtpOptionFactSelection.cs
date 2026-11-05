namespace ftp;

internal class FtpOptionFactSelection
{
    internal bool Valid;
    internal string[] Facts;
    internal string FailureMessage;

    internal FtpOptionFactSelection()
    {
        Valid = false;
        Facts = new string[0];
        FailureMessage = "";
    }
}
