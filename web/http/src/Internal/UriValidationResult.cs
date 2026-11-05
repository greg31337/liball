namespace http;

internal class UriValidationResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
    internal bool IsAbsolute;
    internal bool HasAuthority;
    internal bool HasUserInfo;
    internal bool HasPort;
    internal bool HasPortValue;
    internal bool IsPortInRange;
    internal bool HasQuery;
    internal bool HasFragment;
    internal bool IsIpLiteral;
    internal string Scheme = "";
    internal string Authority = "";
    internal string UserInfo = "";
    internal string Host = "";
    internal string PortText = "";
    internal int Port;
    internal string Path = "";
    internal string Query = "";
    internal string Fragment = "";
}
