namespace config;

/// <summary>Defines operations on named and hierarchical configuration values.</summary>
public interface IConfigMap
{
    /// <summary>Sets key case sensitivity before values are added.</summary>
    ConfigMap SetCaseMode(ConfigCaseMode caseMode);
    /// <summary>Sets one direct child key.</summary>
    void Set(string key, ConfigValue value);
    /// <summary>Reads one direct child key.</summary>
    ConfigValue Get(string key);
    /// <summary>Checks for one direct child key.</summary>
    bool Has(string key);
    /// <summary>Removes one direct child key.</summary>
    void Remove(string key);
    /// <summary>Reads a path using the default delimiter.</summary>
    ConfigValue GetPathDefault(string path);
    /// <summary>Reads a path using a supplied delimiter.</summary>
    ConfigValue GetPath(string path, string delimiter);
    /// <summary>Sets a path using the default delimiter.</summary>
    void SetPathDefault(string path, ConfigValue value);
    /// <summary>Sets a path using a supplied delimiter.</summary>
    void SetPath(string path, ConfigValue value, string delimiter);
    /// <summary>Checks a path using the default delimiter.</summary>
    bool HasPathDefault(string path);
    /// <summary>Checks a path using a supplied delimiter.</summary>
    bool HasPath(string path, string delimiter);
    /// <summary>Lists child keys below a path using the default delimiter.</summary>
    string[] ChildKeysDefault(string path);
    /// <summary>Lists child keys below a path using a supplied delimiter.</summary>
    string[] ChildKeys(string path, string delimiter);
    /// <summary>Gets the number of direct keys.</summary>
    int Count();
    /// <summary>Lists direct keys in deterministic order.</summary>
    string[] Keys();
    /// <summary>Creates a deep copy.</summary>
    ConfigMap Copy();
    /// <summary>Formats the map for diagnostics.</summary>
    string ToDisplayString();
}
