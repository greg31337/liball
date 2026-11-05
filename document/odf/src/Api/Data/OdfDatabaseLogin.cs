namespace odf;

/// <summary>
/// Describes database login settings without storing a password.
/// </summary>
public class OdfDatabaseLogin
{
    /// <summary>The configured user name.</summary>
    public string UserName = "";
    /// <summary>The preserved system-user flag.</summary>
    public string UseSystemUser = "";
    /// <summary>The preserved password-required flag.</summary>
    public string IsPasswordRequired = "";
    /// <summary>The lexical login timeout.</summary>
    public string LoginTimeout = "";
    /// <summary>The underlying login element.</summary>
    public OdfNode? Node;
    internal string OriginalUserName = "";
    internal string OriginalUseSystemUser = "";
    internal string OriginalIsPasswordRequired = "";
    internal string OriginalLoginTimeout = "";
}
