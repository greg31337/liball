namespace fix;

/// <summary>Contains logon credentials and context presented for authentication.</summary>
public class AuthenticationRequest
{
    /// <summary>The requested session identity.</summary>
    public SessionId Id = new SessionId();
    /// <summary>The supplied username.</summary>
    public string Username = "";
    /// <summary>The supplied password.</summary>
    public string Password = "";
    /// <summary>The supplied raw authentication bytes.</summary>
    public byte[] RawData = [];
    /// <summary>The peer network address.</summary>
    public string RemoteAddress = "";
    /// <summary>The decoded logon message.</summary>
    public FixMessage? LogonMessage;
}
