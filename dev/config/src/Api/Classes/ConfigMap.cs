using System;
using System.Collections.Generic;
using System.Text;

namespace config;

/// <summary>Stores named configuration values and supports delimiter-aware path traversal.</summary>
/// <remarks>Direct keys are compared using the case mode chosen when the map is created. Paths support a backslash escape before delimiters.</remarks>
public class ConfigMap : IConfigMap
{
    internal Dictionary<string, ConfigValue> Values;
    internal ConfigCaseMode CaseMode;

    /// <summary>Creates an empty map with case-sensitive, ordinal keys.</summary>
    public ConfigMap()
    {
        CaseMode = ConfigCaseMode.Sensitive;
        Values = new Dictionary<string, ConfigValue>(Comparer(CaseMode));
    }

    /// <summary>Sets key case sensitivity on an empty map.</summary>
    /// <param name="caseMode">Whether key casing is significant.</param>
    /// <returns>This map for fluent construction.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the map already contains values.</exception>
    public ConfigMap SetCaseMode(ConfigCaseMode caseMode)
    {
        if (Values.Count > 0)
        {
            throw new InvalidOperationException("Case mode can only be changed on an empty map.");
        }

        CaseMode = caseMode;
        Values = new Dictionary<string, ConfigValue>(Comparer(CaseMode));
        return this;
    }

    /// <summary>Creates an empty map using the requested key comparison mode.</summary>
    /// <param name="caseMode">Whether key casing is significant.</param>
    /// <returns>A new empty map.</returns>
    internal static ConfigMap Create(ConfigCaseMode caseMode)
    {
        ConfigMap map = new ConfigMap();
        map.SetCaseMode(caseMode);
        return map;
    }

    internal static StringComparer Comparer(ConfigCaseMode caseMode)
    {
        if (caseMode == ConfigCaseMode.Insensitive)
        {
            return StringComparer.OrdinalIgnoreCase;
        }

        return StringComparer.Ordinal;
    }

    /// <summary>Sets or replaces one direct child key.</summary>
    /// <param name="key">The direct key; delimiters are treated as ordinary characters.</param>
    /// <param name="value">The value to store.</param>
    public void Set(string key, ConfigValue value)
    {
        Values[key] = value;
    }

    /// <summary>Reads one direct child key.</summary>
    /// <param name="key">The direct key to read.</param>
    /// <returns>The stored value, or a missing value when the key is absent.</returns>
    public ConfigValue Get(string key)
    {
        if (Values.ContainsKey(key))
        {
            return Values[key];
        }

        return ConfigValue.Missing();
    }

    /// <summary>Checks for one direct child key.</summary>
    /// <param name="key">The direct key to test.</param>
    /// <returns>True when the map contains the key.</returns>
    public bool Has(string key)
    {
        return Values.ContainsKey(key);
    }

    /// <summary>Removes one direct child key when it exists.</summary>
    /// <param name="key">The direct key to remove.</param>
    public void Remove(string key)
    {
        Values.Remove(key);
    }

    /// <summary>Reads a path using <c>.</c> as the delimiter.</summary>
    /// <param name="path">The hierarchical path to read.</param>
    /// <returns>The stored value, or a missing value when traversal cannot reach the path.</returns>
    public ConfigValue GetPathDefault(string path)
    {
        return GetPath(path, ".");
    }

    internal ConfigValue GetPathDefaultWithAliases(string path, ConfigAliasTable aliases)
    {
        return GetPathWithAliases(path, ".", aliases);
    }

    /// <summary>Reads a hierarchical path using the supplied delimiter.</summary>
    /// <param name="path">The hierarchical path to read.</param>
    /// <param name="delimiter">The segment delimiter; an empty delimiter is treated as <c>.</c>.</param>
    /// <returns>The stored value, or a missing value when traversal cannot reach the path.</returns>
    public ConfigValue GetPath(string path, string delimiter)
    {
        ConfigPath parsed = ConfigPath.Parse(path, delimiter);
        if (parsed.Count() == 0)
        {
            return ConfigValue.Object(this);
        }

        ConfigMap current = this;
        int index = 0;
        while (index < parsed.Count())
        {
            ConfigValue value = current.Get(parsed.Segment(index));
            if (index == parsed.Count() - 1)
            {
                return value;
            }

            if (value.Kind != ConfigValueKind.Object)
            {
                return ConfigValue.Missing();
            }

            if (value.ObjectValue == null)
            {
                return ConfigValue.Missing();
            }

            current = value.ObjectValue;
            index = index + 1;
        }

        return ConfigValue.Missing();
    }

    internal ConfigValue GetPathWithAliases(string path, string delimiter, ConfigAliasTable aliases)
    {
        return GetPath(aliases.Resolve(path, delimiter), delimiter);
    }

    /// <summary>Sets a path using <c>.</c> as the delimiter, creating intermediate objects as needed.</summary>
    /// <param name="path">The hierarchical path to set.</param>
    /// <param name="value">The value to store.</param>
    public void SetPathDefault(string path, ConfigValue value)
    {
        SetPath(path, value, ".");
    }

    internal void SetPathDefaultWithAliases(string path, ConfigValue value, ConfigAliasTable aliases)
    {
        SetPathWithAliases(path, value, ".", aliases);
    }

    /// <summary>Sets a hierarchical path, replacing non-object intermediate values with objects as needed.</summary>
    /// <param name="path">The hierarchical path to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="delimiter">The segment delimiter; an empty delimiter is treated as <c>.</c>.</param>
    public void SetPath(string path, ConfigValue value, string delimiter)
    {
        ConfigPath parsed = ConfigPath.Parse(path, delimiter);
        if (parsed.Count() == 0)
        {
            return;
        }

        ConfigMap current = this;
        int index = 0;
        while (index < parsed.Count() - 1)
        {
            string key = parsed.Segment(index);
            ConfigValue existing = current.Get(key);
            ConfigMap? next = null;

            if (existing.Kind == ConfigValueKind.Object && existing.ObjectValue != null)
            {
                next = existing.ObjectValue;
            }

            if (next == null)
            {
                next = Create(CaseMode);
                current.Set(key, ConfigValue.Object(next));
            }

            current = next;
            index = index + 1;
        }

        current.Set(parsed.Segment(parsed.Count() - 1), value);
    }

    internal void SetPathWithAliases(string path, ConfigValue value, string delimiter, ConfigAliasTable aliases)
    {
        SetPath(aliases.Resolve(path, delimiter), value, delimiter);
    }

    /// <summary>Checks a path using <c>.</c> as the delimiter.</summary>
    /// <param name="path">The hierarchical path to test.</param>
    /// <returns>True when the path resolves to any value, including null.</returns>
    public bool HasPathDefault(string path)
    {
        return HasPath(path, ".");
    }

    internal bool HasPathDefaultWithAliases(string path, ConfigAliasTable aliases)
    {
        return HasPathWithAliases(path, ".", aliases);
    }

    /// <summary>Checks a hierarchical path using the supplied delimiter.</summary>
    /// <param name="path">The hierarchical path to test.</param>
    /// <param name="delimiter">The segment delimiter.</param>
    /// <returns>True when the path resolves to any value, including null.</returns>
    public bool HasPath(string path, string delimiter)
    {
        ConfigValue value = GetPath(path, delimiter);
        return !value.IsMissing();
    }

    internal bool HasPathWithAliases(string path, string delimiter, ConfigAliasTable aliases)
    {
        ConfigValue value = GetPathWithAliases(path, delimiter, aliases);
        return !value.IsMissing();
    }

    /// <summary>Lists direct child keys below a path using <c>.</c> as the delimiter.</summary>
    /// <param name="path">The object path, or an empty string for this map.</param>
    /// <returns>Sorted child keys, or an empty array when the path is not an object.</returns>
    public string[] ChildKeysDefault(string path)
    {
        return ChildKeys(path, ".");
    }

    internal string[] ChildKeysDefaultWithAliases(string path, ConfigAliasTable aliases)
    {
        return ChildKeysWithAliases(path, ".", aliases);
    }

    /// <summary>Lists direct child keys below a path.</summary>
    /// <param name="path">The object path, or an empty string for this map.</param>
    /// <param name="delimiter">The segment delimiter.</param>
    /// <returns>Sorted child keys, or an empty array when the path is not an object.</returns>
    public string[] ChildKeys(string path, string delimiter)
    {
        ConfigPath parsed = ConfigPath.Parse(path, delimiter);
        if (parsed.Count() == 0)
        {
            return Keys();
        }

        ConfigValue value = GetPath(path, delimiter);
        if (value.Kind != ConfigValueKind.Object)
        {
            return new string[0];
        }

        if (value.ObjectValue == null)
        {
            return new string[0];
        }

        return value.ObjectValue.Keys();
    }

    internal string[] ChildKeysWithAliases(string path, string delimiter, ConfigAliasTable aliases)
    {
        return ChildKeys(aliases.Resolve(path, delimiter), delimiter);
    }

    /// <summary>Gets the number of direct keys in the map.</summary>
    /// <returns>The direct key count.</returns>
    public int Count()
    {
        return Values.Count;
    }

    /// <summary>Lists direct keys in deterministic order using the map's case comparer.</summary>
    /// <returns>A new array of sorted keys.</returns>
    public string[] Keys()
    {
        string[] keys = new string[Values.Keys.Count];
        Values.Keys.CopyTo(keys, 0);
        Array.Sort(keys, Comparer(CaseMode));
        return keys;
    }

    /// <summary>Creates a deep copy that preserves key comparison behavior.</summary>
    /// <returns>An independent map and independent nested values.</returns>
    public ConfigMap Copy()
    {
        string[] keys = Keys();
        ConfigMap map = Create(CaseMode);

        int index = 0;
        while (index < keys.Length)
        {
            string key = keys[index];
            map.Set(key, Values[key].Copy());
            index = index + 1;
        }

        return map;
    }

    /// <summary>Formats the object as compact, deterministic diagnostic text.</summary>
    /// <returns>A brace-delimited list of sorted keys and display values.</returns>
    public string ToDisplayString()
    {
        string[] keys = Keys();
        StringBuilder builder = new StringBuilder();
        builder.Append("{");

        int index = 0;
        while (index < keys.Length)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            string key = keys[index];
            builder.Append(key);
            builder.Append(": ");
            builder.Append(Values[key].ToDisplayString());
            index = index + 1;
        }

        builder.Append("}");
        return builder.ToString();
    }
}
