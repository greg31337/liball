namespace ftp;

internal class FtpServerLanguageInfo
{
    internal bool Available;
    internal string[] Languages;
    internal string DefaultLanguage;

    internal FtpServerLanguageInfo()
    {
        Available = false;
        Languages = new string[0];
        DefaultLanguage = "";
    }
}
