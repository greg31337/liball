namespace ftp;

internal class FtpCommandDefinition
{
    internal FtpCommandKind Kind;
    internal string Verb;
    internal bool ArgumentRequired;
    internal bool ArgumentAllowed;
    internal bool RequiresAuthentication;
    internal bool AvailableDuringTransfer;
    internal bool Standard;
    internal bool SecurityCommand;
    internal bool Obsolete;
    internal bool HistoricAlias;
    internal bool Compatibility;
    internal bool HasAliasTarget;
    internal FtpCommandKind AliasTarget;

    internal FtpCommandDefinition()
    {
        Kind = FtpCommandKind.Unknown;
        Verb = "";
        ArgumentRequired = false;
        ArgumentAllowed = true;
        RequiresAuthentication = false;
        AvailableDuringTransfer = false;
        Standard = false;
        SecurityCommand = false;
        Obsolete = false;
        HistoricAlias = false;
        Compatibility = false;
        HasAliasTarget = false;
        AliasTarget = FtpCommandKind.Unknown;
    }
}
