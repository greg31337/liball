using System;
using System.Collections.Generic;

namespace config;

internal class ConfigAliasTable
{
    internal Dictionary<string, string> Paths;
    internal ConfigCaseMode CaseMode;

    internal ConfigAliasTable(ConfigCaseMode caseMode)
    {
        CaseMode = caseMode;
        Paths = new Dictionary<string, string>(ConfigMap.Comparer(CaseMode));
    }

    internal void AddDefault(string oldPath, string newPath)
    {
        Add(oldPath, newPath, ".");
    }

    internal void Add(string oldPath, string newPath, string delimiter)
    {
        string normalizedOldPath = ConfigPath.Normalize(oldPath, delimiter);
        string normalizedNewPath = ConfigPath.Normalize(newPath, delimiter);
        Paths[normalizedOldPath] = normalizedNewPath;
    }

    internal bool HasDefault(string path)
    {
        return Has(path, ".");
    }

    internal bool Has(string path, string delimiter)
    {
        string normalizedPath = ConfigPath.Normalize(path, delimiter);
        return Paths.ContainsKey(normalizedPath);
    }

    internal string ResolveDefault(string path)
    {
        return Resolve(path, ".");
    }

    internal string Resolve(string path, string delimiter)
    {
        string current = ConfigPath.Normalize(path, delimiter);
        HashSet<string> seen = new HashSet<string>(ConfigMap.Comparer(CaseMode));

        while (Paths.ContainsKey(current))
        {
            if (!seen.Add(current))
            {
                return current;
            }

            current = Paths[current];
        }

        return current;
    }

    internal int Count()
    {
        return Paths.Count;
    }

    internal ConfigAliasTable Copy()
    {
        ConfigAliasTable copy = new ConfigAliasTable(CaseMode);
        string[] keys = new string[Paths.Keys.Count];
        Paths.Keys.CopyTo(keys, 0);
        Array.Sort(keys, ConfigMap.Comparer(CaseMode));

        int index = 0;
        while (index < keys.Length)
        {
            copy.Paths[keys[index]] = Paths[keys[index]];
            index = index + 1;
        }

        return copy;
    }
}
