using System;
using System.Collections.Generic;

namespace config;

/// <summary>Maps target field paths to configuration paths for reflection-based binding.</summary>
/// <remarks>Field paths use <c>.</c> between nested public field names. Configuration paths are interpreted relative to the section being bound.</remarks>
public class ConfigFieldMap : IConfigFieldMap
{
    internal Dictionary<string, string> Paths;

    /// <summary>Creates an empty field mapping table.</summary>
    public ConfigFieldMap()
    {
        Paths = new Dictionary<string, string>();
    }

    /// <summary>Adds or replaces the configuration path for a target field.</summary>
    /// <param name="fieldPath">The target field path, such as <c>Database.Host</c>.</param>
    /// <param name="configPath">The configuration path relative to the bound section.</param>
    /// <returns>This mapping table for fluent calls.</returns>
    public ConfigFieldMap Add(string fieldPath, string configPath)
    {
        Paths[NormalizeFieldPath(fieldPath)] = configPath;
        return this;
    }

    /// <summary>Checks whether a target field has an explicit mapping.</summary>
    /// <param name="fieldPath">The target field path to test.</param>
    /// <returns>True when a mapping is registered.</returns>
    public bool Has(string fieldPath)
    {
        return Paths.ContainsKey(NormalizeFieldPath(fieldPath));
    }

    /// <summary>Gets the configuration path registered for a target field.</summary>
    /// <param name="fieldPath">The target field path to look up.</param>
    /// <returns>The mapped path, or an empty string when no mapping exists.</returns>
    public string Get(string fieldPath)
    {
        string normalized = NormalizeFieldPath(fieldPath);
        if (Paths.ContainsKey(normalized))
        {
            return Paths[normalized];
        }

        return "";
    }

    internal string Resolve(string fieldPath, string fallback)
    {
        string normalized = NormalizeFieldPath(fieldPath);
        if (Paths.ContainsKey(normalized))
        {
            return Paths[normalized];
        }

        return fallback;
    }

    internal ConfigFieldMap Copy()
    {
        ConfigFieldMap copy = new ConfigFieldMap();
        string[] keys = new string[Paths.Keys.Count];
        Paths.Keys.CopyTo(keys, 0);
        Array.Sort(keys, StringComparer.Ordinal);

        int index = 0;
        while (index < keys.Length)
        {
            copy.Paths[keys[index]] = Paths[keys[index]];
            index = index + 1;
        }

        return copy;
    }

    internal static string NormalizeFieldPath(string fieldPath)
    {
        return ConfigPath.Normalize(fieldPath, ".");
    }
}
