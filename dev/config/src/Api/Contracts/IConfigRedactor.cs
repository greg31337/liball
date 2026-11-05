namespace config;

/// <summary>Defines secret-path detection and value redaction operations.</summary>
public interface IConfigRedactor
{
    /// <summary>Adds a case-insensitive path token.</summary>
    void AddToken(string token);
    /// <summary>Gets the number of configured tokens.</summary>
    int TokenCount();
    /// <summary>Sets the replacement text.</summary>
    ConfigRedactor SetReplacement(string replacement);
    /// <summary>Gets the replacement text.</summary>
    string ReplacementText();
    /// <summary>Checks whether a path is considered secret.</summary>
    bool IsSecretPath(string path);
    /// <summary>Redacts a value when its path is secret.</summary>
    string RedactValue(string path, string value);
}
