namespace config;

/// <summary>Defines explicit mappings from target fields to configuration paths.</summary>
public interface IConfigFieldMap
{
    /// <summary>Adds or replaces a field mapping.</summary>
    ConfigFieldMap Add(string fieldPath, string configPath);
    /// <summary>Checks whether a field path is mapped.</summary>
    bool Has(string fieldPath);
    /// <summary>Gets the mapped configuration path.</summary>
    string Get(string fieldPath);
}
