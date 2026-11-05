namespace config;

/// <summary>Defines delimiter-aware operations for constructing and comparing configuration paths.</summary>
public interface IConfigPath
{
    /// <summary>Returns the effective delimiter, substituting the default for an empty value.</summary>
    string Clean(string delimiter);
    /// <summary>Escapes one literal path segment.</summary>
    string Escape(string segment, string delimiter);
    /// <summary>Parses a path into unescaped nonempty segments.</summary>
    string[] ParseSegments(string path, string delimiter);
    /// <summary>Returns the canonical escaped representation of a path.</summary>
    string NormalizePath(string path, string delimiter);
    /// <summary>Escapes and joins literal path segments.</summary>
    string JoinPath(string[] segments, string delimiter);
    /// <summary>Compares two paths by their parsed segments.</summary>
    int Compare(string leftPath, string rightPath, string delimiter, ConfigCaseMode caseMode);
}
