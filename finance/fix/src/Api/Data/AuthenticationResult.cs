namespace fix;

/// <summary>Contains the explicit outcome of session authentication.</summary>
public class AuthenticationResult
{
    /// <summary>Whether authentication succeeded.</summary>
    public bool Authenticated;
    /// <summary>Whether the transport should be disconnected.</summary>
    public bool Disconnect;
    /// <summary>A stable machine-readable outcome code.</summary>
    public string Code = "";
    /// <summary>Text suitable for a logout or operational diagnostic.</summary>
    public string Text = "";
}
