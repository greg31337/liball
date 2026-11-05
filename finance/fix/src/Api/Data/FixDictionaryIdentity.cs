namespace fix;

/// <summary>Identifies one transport or application FIX dictionary.</summary>
public class FixDictionaryIdentity
{
    /// <summary>A human-readable dictionary name.</summary>
    public string Name = "";
    /// <summary>The FIX BeginString for a transport dictionary.</summary>
    public string BeginString = "";
    /// <summary>The standard ApplVerID for an application dictionary.</summary>
    public string ApplicationVersion = "";
    /// <summary>Whether an extension-pack identifier is present.</summary>
    public bool HasExtensionPack;
    /// <summary>The application extension-pack identifier.</summary>
    public int ExtensionPack;
    /// <summary>A bilateral custom application version identifier.</summary>
    public string CustomVersion = "";
    /// <summary>The scenario selected within the dictionary.</summary>
    public string Scenario = "";
    /// <summary>The source repository or overlay that supplied the identity.</summary>
    public string Source = "";
}
