namespace svg;

internal class SvgUriComponents
{
    internal string Scheme;
    internal bool HasAuthority;
    internal bool HasUserInfo;
    internal string UserInfo;
    internal string Host;
    internal bool HasPort;
    internal string Port;
    internal bool HostIsLiteral;
    internal string Path;
    internal bool HasQuery;
    internal string Query;
    internal bool HasFragment;
    internal string Fragment;
    internal bool Valid;
    internal SvgUriError Error;

    internal SvgUriComponents()
    {
        Scheme = "";
        HasAuthority = false;
        HasUserInfo = false;
        UserInfo = "";
        Host = "";
        HasPort = false;
        Port = "";
        HostIsLiteral = false;
        Path = "";
        HasQuery = false;
        Query = "";
        HasFragment = false;
        Fragment = "";
        Valid = false;
        Error = SvgUriError.InvalidSyntax;
    }
}
