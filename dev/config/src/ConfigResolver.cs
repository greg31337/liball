using System;
using System.Collections.Generic;
using System.Text;

namespace config;

internal class ConfigResolver
{
    internal static ConfigValue ResolveValue(Config config, ConfigValue value, int depth)
    {
        List<string> stack = new List<string>();
        return ResolveValueAt(config, value, depth, stack, "", null);
    }

    internal static ConfigValue ResolvePath(Config config, string path)
    {
        List<string> stack = new List<string>();
        string normalizedPath = ConfigPath.Normalize(path, config.Delimiter);
        return ResolveValueAt(config, config.RawValue(path), 0, stack, normalizedPath, null);
    }

    internal static void AddDiagnostics(Config config, ConfigDiagnostics diagnostics)
    {
        List<string> stack = new List<string>();
        ResolveValueAt(config, config.Root, 0, stack, "", diagnostics);
    }

    internal static ConfigValue ResolveValueAt(Config config, ConfigValue value, int depth, List<string> stack, string path, ConfigDiagnostics? diagnostics)
    {
        if (depth > config.ResolutionOptions.SubstitutionDepthLimit)
        {
            AddResolutionError(config, diagnostics, stack, path, "substitution depth limit exceeded");
            return EmptyStringValue(value);
        }

        if (value.Kind == ConfigValueKind.String)
        {
            bool added = false;
            if (path.Length > 0)
            {
                if (ContainsPath(stack, path))
                {
                    AddResolutionError(config, diagnostics, stack, path, "substitution cycle: " + path);
                    return EmptyStringValue(value);
                }

                stack.Add(path);
                added = true;
            }

            ConfigValue resolved = value.Copy();
            string text = "";
            if (value.StringValue != null)
            {
                text = value.StringValue;
            }

            resolved.StringValue = ResolveString(config, text, depth, stack, diagnostics);
            if (added)
            {
                stack.RemoveAt(stack.Count - 1);
            }

            return resolved;
        }

        if (value.Kind == ConfigValueKind.Array && value.ArrayValue != null)
        {
            ConfigArray array = new ConfigArray();

            int index = 0;
            while (index < value.ArrayValue.Count())
            {
                array.Add(ResolveValueAt(config, value.ArrayValue.Get(index), depth, stack, path, diagnostics));
                index = index + 1;
            }

            ConfigValue resolved = ConfigValue.Array(array);
            value.CopyMetadataTo(resolved);
            return resolved;
        }

        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            ConfigMap map = ConfigMap.Create(value.ObjectValue.CaseMode);
            string[] keys = value.ObjectValue.Keys();

            int index = 0;
            while (index < keys.Length)
            {
                string childPath = ChildPath(path, keys[index], config.Delimiter);
                map.Set(keys[index], ResolveValueAt(config, value.ObjectValue.Get(keys[index]), depth, stack, childPath, diagnostics));
                index = index + 1;
            }

            ConfigValue resolved = ConfigValue.Object(map);
            value.CopyMetadataTo(resolved);
            return resolved;
        }

        return value.Copy();
    }

    internal static string ResolveString(Config config, string text, int depth, List<string> stack, ConfigDiagnostics? diagnostics)
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '$' && index + 1 < text.Length && text[index + 1] == '{')
            {
                int end = FindClosingBrace(text, index + 2);
                if (end >= 0)
                {
                    string reference = text.Substring(index + 2, end - index - 2);
                    builder.Append(ResolveReference(config, reference, depth + 1, stack, diagnostics));
                    index = end + 1;
                    continue;
                }
            }

            builder.Append(text[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string ResolveReference(Config config, string reference, int depth, List<string> stack, ConfigDiagnostics? diagnostics)
    {
        bool optional = IsOptionalReference(reference);
        string lookupReference = ReferenceName(reference);
        string path = ConfigPath.Normalize(lookupReference, config.Delimiter);
        if (path.Length == 0)
        {
            AddMissingReferenceError(config, diagnostics, stack, lookupReference, optional);
            return "";
        }

        if (ContainsPath(stack, path))
        {
            AddResolutionError(config, diagnostics, stack, path, "substitution cycle: " + path);
            return "";
        }

        if (depth > config.ResolutionOptions.SubstitutionDepthLimit)
        {
            AddResolutionError(config, diagnostics, stack, path, "substitution depth limit exceeded");
            return "";
        }

        ConfigValue value = ConfigValue.Missing();
        if (config.ResolutionOptions.ConfigPathLookup == ConfigSubstitutionLookupMode.Enabled)
        {
            value = config.RawValue(path);
        }

        if (value.Kind == ConfigValueKind.Missing && config.ResolutionOptions.EnvironmentFallback == ConfigSubstitutionLookupMode.Enabled)
        {
            string? environmentValue = Environment.GetEnvironmentVariable(lookupReference);
            if (environmentValue != null)
            {
                return environmentValue;
            }
        }

        if (value.Kind == ConfigValueKind.Missing)
        {
            AddMissingReferenceError(config, diagnostics, stack, lookupReference, optional);
            return "";
        }

        if (value.Kind == ConfigValueKind.Null)
        {
            return "";
        }

        ConfigValue resolved = ResolveValueAt(config, value, depth, stack, path, diagnostics);
        return ConfigConversions.StringValue(resolved);
    }

    internal static void AddMissingReferenceError(Config config, ConfigDiagnostics? diagnostics, List<string> stack, string reference, bool optional)
    {
        if (optional)
        {
            return;
        }

        if (config.ResolutionOptions.MissingSubstitution != ConfigSubstitutionMissingMode.Error)
        {
            return;
        }

        AddResolutionError(config, diagnostics, stack, reference, "missing substitution reference: " + reference);
    }

    internal static void AddResolutionError(Config config, ConfigDiagnostics? diagnostics, List<string> stack, string fallbackPath, string message)
    {
        if (diagnostics == null)
        {
            return;
        }

        string path = fallbackPath;
        if (stack.Count > 0)
        {
            path = stack[stack.Count - 1];
        }

        ConfigValue owner = config.Root;
        if (path.Length > 0)
        {
            owner = config.RawValue(path);
        }

        string sourceName = "";
        if (owner.SourceName != null)
        {
            sourceName = owner.SourceName;
        }

        int index = 0;
        while (index < diagnostics.Errors.Count)
        {
            ConfigError existing = diagnostics.Errors[index];
            if (existing.SourceName == sourceName && existing.Path == path && existing.Message == message)
            {
                return;
            }

            index = index + 1;
        }

        diagnostics.AddError(sourceName, path, message, owner.LineNumber, owner.ColumnNumber);
    }

    internal static bool IsOptionalReference(string reference)
    {
        return reference.StartsWith("?", StringComparison.Ordinal);
    }

    internal static string ReferenceName(string reference)
    {
        if (IsOptionalReference(reference))
        {
            return reference.Substring(1);
        }

        return reference;
    }

    internal static ConfigValue EmptyStringValue(ConfigValue value)
    {
        ConfigValue resolved = ConfigValue.String("");
        value.CopyMetadataTo(resolved);
        return resolved;
    }

    internal static string ChildPath(string parentPath, string childSegment, string delimiter)
    {
        if (parentPath.Length == 0)
        {
            return ConfigPath.Normalize(childSegment, delimiter);
        }

        return parentPath + delimiter + ConfigPath.Normalize(childSegment, delimiter);
    }

    internal static bool ContainsPath(List<string> paths, string path)
    {
        int index = 0;
        while (index < paths.Count)
        {
            if (paths[index] == path)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int FindClosingBrace(string text, int start)
    {
        int index = start;
        while (index < text.Length)
        {
            if (text[index] == '}')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
