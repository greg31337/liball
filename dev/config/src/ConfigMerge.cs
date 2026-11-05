namespace config;

internal class ConfigMergeResult
{
    internal ConfigValue Root;
    internal ConfigDiagnostics Diagnostics;

    internal ConfigMergeResult(ConfigValue root, ConfigDiagnostics diagnostics)
    {
        Root = root;
        Diagnostics = diagnostics;
    }
}

internal class ConfigMerger
{
    internal static ConfigMergeResult Merge(ConfigSourceResult[] results)
    {
        return MergeWithArrayMode(results, ConfigArrayMergeMode.Replace);
    }

    internal static ConfigMergeResult MergeWithArrayMode(ConfigSourceResult[] results, ConfigArrayMergeMode arrayMergeMode)
    {
        return MergeWithOptions(results, ConfigMergeMode.Override, arrayMergeMode, ConfigNullMergeMode.Replace);
    }

    internal static ConfigMergeResult MergeWithNullMode(ConfigSourceResult[] results, ConfigNullMergeMode nullMergeMode)
    {
        return MergeWithOptions(results, ConfigMergeMode.Override, ConfigArrayMergeMode.Replace, nullMergeMode);
    }

    internal static ConfigMergeResult MergeWithMergeMode(ConfigSourceResult[] results, ConfigMergeMode mergeMode)
    {
        return MergeWithOptions(results, mergeMode, ConfigArrayMergeMode.Replace, ConfigNullMergeMode.Replace);
    }

    internal static ConfigMergeResult MergeWithOptions(ConfigSourceResult[] results, ConfigMergeMode mergeMode, ConfigArrayMergeMode arrayMergeMode, ConfigNullMergeMode nullMergeMode)
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        ConfigValue root = ConfigValue.Missing();

        int index = 0;
        while (index < results.Length)
        {
            ConfigSourceLoader.AppendDiagnostics(diagnostics, results[index].Diagnostics);
            if (root.Kind == ConfigValueKind.Missing)
            {
                root = results[index].Root.Copy();
            }
            else
            {
                root = MergeRoot(root, results[index].Root, mergeMode, arrayMergeMode, nullMergeMode, diagnostics, results[index].SourceGroup);
            }

            index = index + 1;
        }

        if (root.Kind == ConfigValueKind.Missing)
        {
            root = ConfigValue.Object(new ConfigMap()).SetSource("merged", 0, "", 0, 0);
        }

        AddMergeTraces(diagnostics, root, "", results);
        return new ConfigMergeResult(root, diagnostics);
    }

    internal static ConfigValue MergeRoot(ConfigValue currentRoot, ConfigValue nextRoot, ConfigMergeMode mergeMode, ConfigArrayMergeMode arrayMergeMode, ConfigNullMergeMode nullMergeMode, ConfigDiagnostics diagnostics, string sourceGroup)
    {
        if (nextRoot.Kind == ConfigValueKind.Null && nullMergeMode == ConfigNullMergeMode.Ignore)
        {
            AddDecisionTrace(diagnostics, nextRoot, sourceGroup, "", ConfigMergeDecision.Ignored, "ignored null value");
            return currentRoot.Copy();
        }

        if (mergeMode == ConfigMergeMode.Fallback)
        {
            if (currentRoot.Kind == ConfigValueKind.Object && currentRoot.ObjectValue != null && nextRoot.Kind == ConfigValueKind.Object && nextRoot.ObjectValue != null)
            {
                ConfigMap map = currentRoot.ObjectValue.Copy();
                FallbackDeep(map, nextRoot.ObjectValue, nullMergeMode, diagnostics, "", sourceGroup);
                ConfigValue root = ConfigValue.Object(map);
                currentRoot.CopyMetadataTo(root);
                return root;
            }

            AddDecisionTrace(diagnostics, nextRoot, sourceGroup, "", ConfigMergeDecision.FallbackKept, "fallback kept existing value");
            return currentRoot.Copy();
        }

        if (mergeMode == ConfigMergeMode.Strict && IsTypeConflict(currentRoot, nextRoot))
        {
            AddStrictConflict(diagnostics, "", nextRoot, sourceGroup);
            return currentRoot.Copy();
        }

        if (currentRoot.Kind == ConfigValueKind.Object && currentRoot.ObjectValue != null && nextRoot.Kind == ConfigValueKind.Object && nextRoot.ObjectValue != null)
        {
            ConfigMap map = currentRoot.ObjectValue.Copy();
            OverrideDeep(map, nextRoot.ObjectValue, mergeMode, arrayMergeMode, nullMergeMode, diagnostics, "", sourceGroup);
            ConfigValue root = ConfigValue.Object(map);
            currentRoot.CopyMetadataTo(root);
            return root;
        }

        if (currentRoot.Kind == ConfigValueKind.Array && currentRoot.ArrayValue != null && nextRoot.Kind == ConfigValueKind.Array && nextRoot.ArrayValue != null && arrayMergeMode == ConfigArrayMergeMode.Append)
        {
            AddDecisionTrace(diagnostics, nextRoot, sourceGroup, "", ConfigMergeDecision.Appended, "appended array value");
            return AppendArrayValue(currentRoot, nextRoot);
        }

        AddDecisionTrace(diagnostics, nextRoot, sourceGroup, "", ConfigMergeDecision.Replaced, "replaced existing value");
        return nextRoot.Copy();
    }

    internal static void OverrideDeep(ConfigMap target, ConfigMap source, ConfigMergeMode mergeMode, ConfigArrayMergeMode arrayMergeMode, ConfigNullMergeMode nullMergeMode, ConfigDiagnostics diagnostics, string parentPath, string sourceGroup)
    {
        string[] keys = source.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string key = keys[index];
            string path = ConfigJsonParser.ChildPath(parentPath, key);
            ConfigValue existing = target.Get(key);
            ConfigValue incoming = source.Get(key);

            if (incoming.Kind == ConfigValueKind.Null && nullMergeMode == ConfigNullMergeMode.Ignore)
            {
                AddDecisionTrace(diagnostics, incoming, sourceGroup, path, ConfigMergeDecision.Ignored, "ignored null value");
                index = index + 1;
                continue;
            }

            if (mergeMode == ConfigMergeMode.Strict && IsTypeConflict(existing, incoming))
            {
                AddStrictConflict(diagnostics, path, incoming, sourceGroup);
                index = index + 1;
                continue;
            }

            if (existing.Kind == ConfigValueKind.Object && existing.ObjectValue != null && incoming.Kind == ConfigValueKind.Object && incoming.ObjectValue != null)
            {
                OverrideDeep(existing.ObjectValue, incoming.ObjectValue, mergeMode, arrayMergeMode, nullMergeMode, diagnostics, path, sourceGroup);
            }
            else if (existing.Kind == ConfigValueKind.Array && existing.ArrayValue != null && incoming.Kind == ConfigValueKind.Array && incoming.ArrayValue != null && arrayMergeMode == ConfigArrayMergeMode.Append)
            {
                AddDecisionTrace(diagnostics, incoming, sourceGroup, path, ConfigMergeDecision.Appended, "appended array value");
                target.Set(key, AppendArrayValue(existing, incoming));
            }
            else
            {
                if (existing.Kind != ConfigValueKind.Missing)
                {
                    AddDecisionTrace(diagnostics, incoming, sourceGroup, path, ConfigMergeDecision.Replaced, "replaced existing value");
                }

                target.Set(key, incoming.Copy());
            }

            index = index + 1;
        }
    }

    internal static void FallbackDeep(ConfigMap target, ConfigMap source, ConfigNullMergeMode nullMergeMode, ConfigDiagnostics diagnostics, string parentPath, string sourceGroup)
    {
        string[] keys = source.Keys();

        int index = 0;
        while (index < keys.Length)
        {
            string key = keys[index];
            string path = ConfigJsonParser.ChildPath(parentPath, key);
            ConfigValue existing = target.Get(key);
            ConfigValue incoming = source.Get(key);

            if (incoming.Kind == ConfigValueKind.Null && nullMergeMode == ConfigNullMergeMode.Ignore)
            {
                AddDecisionTrace(diagnostics, incoming, sourceGroup, path, ConfigMergeDecision.Ignored, "ignored null value");
                index = index + 1;
                continue;
            }

            if (existing.Kind == ConfigValueKind.Missing)
            {
                target.Set(key, incoming.Copy());
            }
            else if (existing.Kind == ConfigValueKind.Object && existing.ObjectValue != null && incoming.Kind == ConfigValueKind.Object && incoming.ObjectValue != null)
            {
                FallbackDeep(existing.ObjectValue, incoming.ObjectValue, nullMergeMode, diagnostics, path, sourceGroup);
            }
            else
            {
                AddDecisionTrace(diagnostics, incoming, sourceGroup, path, ConfigMergeDecision.FallbackKept, "fallback kept existing value");
            }

            index = index + 1;
        }
    }

    internal static ConfigValue AppendArrayValue(ConfigValue existing, ConfigValue incoming)
    {
        ConfigArray array = new ConfigArray();
        if (existing.ArrayValue != null)
        {
            array = existing.ArrayValue.Copy();
        }

        ConfigArray incomingArray = new ConfigArray();
        if (incoming.ArrayValue != null)
        {
            incomingArray = incoming.ArrayValue;
        }

        int index = 0;
        while (index < incomingArray.Count())
        {
            array.Add(incomingArray.Get(index).Copy());
            index = index + 1;
        }

        ConfigValue value = ConfigValue.Array(array);
        incoming.CopyMetadataTo(value);
        return value;
    }

    internal static bool IsTypeConflict(ConfigValue existing, ConfigValue incoming)
    {
        if (existing.Kind == ConfigValueKind.Missing)
        {
            return false;
        }

        if (incoming.Kind == ConfigValueKind.Null)
        {
            return false;
        }

        if (existing.Kind == ConfigValueKind.Null)
        {
            return false;
        }

        return existing.Kind != incoming.Kind;
    }

    internal static void AddStrictConflict(ConfigDiagnostics diagnostics, string path, ConfigValue incoming, string sourceGroup)
    {
        diagnostics.AddMergeError(ValueSourceName(incoming), path, "strict merge type conflict", incoming.LineNumber, incoming.ColumnNumber);
        AddDecisionTrace(diagnostics, incoming, sourceGroup, path, ConfigMergeDecision.StrictConflict, "strict conflict kept existing value");
    }

    internal static void AddMergeTraces(ConfigDiagnostics diagnostics, ConfigValue value, string path, ConfigSourceResult[] results)
    {
        if (value.Kind == ConfigValueKind.Object && value.ObjectValue != null)
        {
            string[] keys = value.ObjectValue.Keys();

            int index = 0;
            while (index < keys.Length)
            {
                string childPath = ConfigJsonParser.ChildPath(path, keys[index]);
                AddMergeTraces(diagnostics, value.ObjectValue.Get(keys[index]), childPath, results);
                index = index + 1;
            }

            return;
        }

        if (value.Kind == ConfigValueKind.Missing)
        {
            return;
        }

        AddDecisionTrace(diagnostics, value, SourceGroupForRank(results, value.PrecedenceRank), path, ConfigMergeDecision.FinalSupplier, "supplied final value");
    }

    internal static void AddDecisionTrace(ConfigDiagnostics diagnostics, ConfigValue value, string sourceGroup, string path, ConfigMergeDecision decision, string message)
    {
        ConfigTrace trace = diagnostics.AddTrace("merge", ValueSourceName(value), path, message);
        trace.SourceGroup = sourceGroup;
        trace.PrecedenceRank = value.PrecedenceRank;
        trace.Decision = decision;
    }

    internal static string SourceGroupForRank(ConfigSourceResult[] results, int precedenceRank)
    {
        int index = precedenceRank - 1;
        if (index < 0 || index >= results.Length)
        {
            return "";
        }

        return results[index].SourceGroup;
    }

    internal static string ValueSourceName(ConfigValue value)
    {
        if (value.SourceName == null)
        {
            return "";
        }

        return value.SourceName;
    }
}
