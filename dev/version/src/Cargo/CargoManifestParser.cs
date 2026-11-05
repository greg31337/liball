namespace version;

internal class CargoManifestParser
{
    internal string source;
    internal System.Collections.Generic.List<CargoDependency> dependencies;
    internal System.Collections.Generic.List<CargoFeature> features;
    internal System.Collections.Generic.List<CargoRegistrySource> registrySources;
    internal System.Collections.Generic.List<CargoPendingSourceReplacement> pendingSourceReplacements;
    internal CargoManifestSection section;
    internal CargoDependencyKind dependencyKind;
    internal string targetTriple;
    internal string registryName;
    internal string sourceName;

    internal CargoManifestParser(string sourceValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        source = sourceValue;
        dependencies = new System.Collections.Generic.List<CargoDependency>();
        features = new System.Collections.Generic.List<CargoFeature>();
        registrySources = new System.Collections.Generic.List<CargoRegistrySource>();
        pendingSourceReplacements = new System.Collections.Generic.List<CargoPendingSourceReplacement>();
        section = CargoManifestSection.None;
        dependencyKind = CargoDependencyKind.Normal;
        targetTriple = "";
        registryName = "";
        sourceName = "";
    }

    internal static CargoManifest Parse(string manifestText)
    {
        CargoManifestParser parser = new CargoManifestParser(manifestText);
        return parser.ParseManifest();
    }

    internal CargoManifest ParseManifest()
    {
        int lineStartIndex = 0;

        while (lineStartIndex <= source.Length)
        {
            int lineEndIndex = FindLineEnd(lineStartIndex);
            string line = TrimText(RemoveComment(source.Substring(lineStartIndex, lineEndIndex - lineStartIndex)));
            ParseLine(line);

            if (lineEndIndex >= source.Length)
            {
                break;
            }

            lineStartIndex = lineEndIndex + 1;
        }

        return new CargoManifest(
            dependencies.ToArray(),
            features.ToArray(),
            registrySources.ToArray(),
            CreateSourceReplacements());
    }

    internal void ParseLine(string line)
    {
        if (line.Length == 0)
        {
            return;
        }

        if (line[0] == '[')
        {
            ParseSection(line);
            return;
        }

        int equalsIndex = FindEquals(line);
        if (equalsIndex < 0)
        {
            throw new System.ArgumentException("Cargo manifest entry is missing an equals sign.");
        }

        string key = StripQuotes(TrimText(line.Substring(0, equalsIndex)));
        string value = TrimText(line.Substring(equalsIndex + 1));

        if (section == CargoManifestSection.Dependency)
        {
            dependencies.Add(ParseDependency(key, value));
        }
        else if (section == CargoManifestSection.Feature)
        {
            features.Add(new CargoFeature(key, ParseStringArray(value)));
        }
        else if (section == CargoManifestSection.Registry)
        {
            if (key == "index")
            {
                AddOrReplaceRegistrySource(new CargoRegistrySource(registryName, ParseString(value)));
            }
        }
        else if (section == CargoManifestSection.Source)
        {
            if (key == "replace-with")
            {
                pendingSourceReplacements.Add(new CargoPendingSourceReplacement(sourceName, ParseString(value)));
            }
            else if (key == "registry")
            {
                AddOrReplaceRegistrySource(new CargoRegistrySource(sourceName, ParseString(value)));
            }
        }
    }

    internal void ParseSection(string line)
    {
        if (line.Length < 2 || line[line.Length - 1] != ']')
        {
            throw new System.ArgumentException("Cargo manifest section header is malformed.");
        }

        string table = line.Substring(1, line.Length - 2);
        section = CargoManifestSection.None;
        dependencyKind = CargoDependencyKind.Normal;
        targetTriple = "";
        registryName = "";
        sourceName = "";

        if (table == "dependencies")
        {
            section = CargoManifestSection.Dependency;
            dependencyKind = CargoDependencyKind.Normal;
        }
        else if (table == "dev-dependencies")
        {
            section = CargoManifestSection.Dependency;
            dependencyKind = CargoDependencyKind.Development;
        }
        else if (table == "build-dependencies")
        {
            section = CargoManifestSection.Dependency;
            dependencyKind = CargoDependencyKind.Build;
        }
        else if (table == "features")
        {
            section = CargoManifestSection.Feature;
        }
        else if (StartsWithText(table, "registries."))
        {
            section = CargoManifestSection.Registry;
            registryName = StripQuotes(table.Substring("registries.".Length));
        }
        else if (StartsWithText(table, "source."))
        {
            section = CargoManifestSection.Source;
            sourceName = StripQuotes(table.Substring("source.".Length));
        }
        else if (StartsWithText(table, "target."))
        {
            ParseTargetSection(table);
        }
    }

    internal void ParseTargetSection(string table)
    {
        string suffix = ".dependencies";

        if (EndsWithText(table, ".dev-dependencies"))
        {
            suffix = ".dev-dependencies";
            dependencyKind = CargoDependencyKind.Development;
        }
        else if (EndsWithText(table, ".build-dependencies"))
        {
            suffix = ".build-dependencies";
            dependencyKind = CargoDependencyKind.Build;
        }
        else if (EndsWithText(table, ".dependencies"))
        {
            dependencyKind = CargoDependencyKind.Normal;
        }
        else
        {
            return;
        }

        string targetText = table.Substring("target.".Length, table.Length - "target.".Length - suffix.Length);
        section = CargoManifestSection.Dependency;
        targetTriple = StripQuotes(targetText);
    }

    internal CargoDependency ParseDependency(string packageNameText, string value)
    {
        if (IsQuotedString(value))
        {
            return CreateDependency(packageNameText, ParseString(value), false, true, new string[0], null);
        }

        if (!IsInlineTable(value))
        {
            throw new System.ArgumentException("Cargo dependency must be a version string or inline table.");
        }

        string versionRequirementText = "";
        bool optional = false;
        bool defaultFeatures = true;
        string[] dependencyFeatures = new string[0];
        string? dependencyRegistryName = null;
        string body = value.Substring(1, value.Length - 2);
        string[] entries = SplitTopLevel(body, ',');
        int index = 0;

        while (index < entries.Length)
        {
            string entry = TrimText(entries[index]);
            if (entry.Length > 0)
            {
                int equalsIndex = FindEquals(entry);
                if (equalsIndex < 0)
                {
                    throw new System.ArgumentException("Cargo dependency inline table entry is missing an equals sign.");
                }

                string key = StripQuotes(TrimText(entry.Substring(0, equalsIndex)));
                string entryValue = TrimText(entry.Substring(equalsIndex + 1));

                if (key == "version")
                {
                    versionRequirementText = ParseString(entryValue);
                }
                else if (key == "optional")
                {
                    optional = ParseBoolean(entryValue);
                }
                else if (key == "default-features")
                {
                    defaultFeatures = ParseBoolean(entryValue);
                }
                else if (key == "features")
                {
                    dependencyFeatures = ParseStringArray(entryValue);
                }
                else if (key == "registry")
                {
                    dependencyRegistryName = ParseString(entryValue);
                }
            }

            index = index + 1;
        }

        return CreateDependency(packageNameText, versionRequirementText, optional, defaultFeatures, dependencyFeatures, dependencyRegistryName);
    }

    internal CargoDependency CreateDependency(
        string packageNameText,
        string versionRequirementText,
        bool optional,
        bool defaultFeatures,
        string[] dependencyFeatures,
        string? dependencyRegistryName)
    {
        string? dependencyTargetTriple = null;
        if (targetTriple.Length > 0)
        {
            dependencyTargetTriple = targetTriple;
        }

        return new CargoDependency(
            new CargoPackageName(packageNameText),
            versionRequirementText,
            dependencyKind,
            optional,
            defaultFeatures,
            dependencyFeatures,
            dependencyRegistryName,
            dependencyTargetTriple);
    }

    internal CargoSourceReplacement[] CreateSourceReplacements()
    {
        System.Collections.Generic.List<CargoSourceReplacement> replacements = new System.Collections.Generic.List<CargoSourceReplacement>();
        int index = 0;

        while (index < pendingSourceReplacements.Count)
        {
            CargoPendingSourceReplacement pending = pendingSourceReplacements[index];
            CargoRegistrySource? replacementSource = FindRegistrySource(pending.GetReplacementName());
            if (replacementSource == null)
            {
                throw new System.ArgumentException("Cargo source replacement target does not have a registry source declaration.");
            }

            replacements.Add(new CargoSourceReplacement(pending.GetSourceName(), replacementSource));
            index = index + 1;
        }

        return replacements.ToArray();
    }

    internal void AddOrReplaceRegistrySource(CargoRegistrySource registrySource)
    {
        int index = 0;

        while (index < registrySources.Count)
        {
            if (registrySources[index].MatchesName(registrySource.GetName()))
            {
                registrySources[index] = registrySource;
                return;
            }

            index = index + 1;
        }

        registrySources.Add(registrySource);
    }

    internal CargoRegistrySource? FindRegistrySource(string name)
    {
        int index = 0;

        while (index < registrySources.Count)
        {
            if (registrySources[index].MatchesName(name))
            {
                return registrySources[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal string ParseString(string value)
    {
        if (!IsQuotedString(value))
        {
            throw new System.ArgumentException("Cargo manifest value must be a quoted string.");
        }

        return value.Substring(1, value.Length - 2);
    }

    internal bool ParseBoolean(string value)
    {
        if (value == "true")
        {
            return true;
        }

        if (value == "false")
        {
            return false;
        }

        throw new System.ArgumentException("Cargo manifest boolean value must be true or false.");
    }

    internal string[] ParseStringArray(string value)
    {
        if (value.Length < 2 || value[0] != '[' || value[value.Length - 1] != ']')
        {
            throw new System.ArgumentException("Cargo manifest array value is malformed.");
        }

        string body = value.Substring(1, value.Length - 2);
        string[] entries = SplitTopLevel(body, ',');
        System.Collections.Generic.List<string> values = new System.Collections.Generic.List<string>();
        int index = 0;

        while (index < entries.Length)
        {
            string entry = TrimText(entries[index]);
            if (entry.Length > 0)
            {
                values.Add(ParseString(entry));
            }

            index = index + 1;
        }

        return values.ToArray();
    }

    internal string[] SplitTopLevel(string text, char delimiter)
    {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        bool inString = false;
        int depth = 0;
        int startIndex = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '"')
            {
                inString = !inString;
            }
            else if (!inString && (text[index] == '[' || text[index] == '{'))
            {
                depth = depth + 1;
            }
            else if (!inString && (text[index] == ']' || text[index] == '}'))
            {
                depth = depth - 1;
            }
            else if (!inString && depth == 0 && text[index] == delimiter)
            {
                parts.Add(text.Substring(startIndex, index - startIndex));
                startIndex = index + 1;
            }

            index = index + 1;
        }

        parts.Add(text.Substring(startIndex));
        return parts.ToArray();
    }

    internal string RemoveComment(string text)
    {
        bool inString = false;
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '"')
            {
                inString = !inString;
            }
            else if (!inString && text[index] == '#')
            {
                return text.Substring(0, index);
            }

            index = index + 1;
        }

        return text;
    }

    internal int FindLineEnd(int startIndex)
    {
        int index = startIndex;

        while (index < source.Length)
        {
            if (source[index] == '\n')
            {
                if (index > startIndex && source[index - 1] == '\r')
                {
                    return index - 1;
                }

                return index;
            }

            index = index + 1;
        }

        return source.Length;
    }

    internal int FindEquals(string text)
    {
        bool inString = false;
        int depth = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (text[index] == '"')
            {
                inString = !inString;
            }
            else if (!inString && (text[index] == '[' || text[index] == '{'))
            {
                depth = depth + 1;
            }
            else if (!inString && (text[index] == ']' || text[index] == '}'))
            {
                depth = depth - 1;
            }
            else if (!inString && depth == 0 && text[index] == '=')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsQuotedString(string value)
    {
        return value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
    }

    internal bool IsInlineTable(string value)
    {
        return value.Length >= 2 && value[0] == '{' && value[value.Length - 1] == '}';
    }

    internal string StripQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\''))
            {
                return value.Substring(1, value.Length - 2);
            }
        }

        return value;
    }

    internal string TrimText(string text)
    {
        int startIndex = 0;
        int endIndex = text.Length;

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        return text.Substring(startIndex, endIndex - startIndex);
    }

    internal bool StartsWithText(string text, string value)
    {
        if (text.Length < value.Length)
        {
            return false;
        }

        return text.Substring(0, value.Length) == value;
    }

    internal bool EndsWithText(string text, string value)
    {
        if (text.Length < value.Length)
        {
            return false;
        }

        return text.Substring(text.Length - value.Length) == value;
    }
}

internal enum CargoManifestSection
{
    None = 0,
    Dependency = 1,
    Feature = 2,
    Registry = 3,
    Source = 4
}

internal class CargoPendingSourceReplacement
{
    internal string sourceName;
    internal string replacementName;

    internal CargoPendingSourceReplacement(string sourceNameValue, string replacementNameValue)
    {
        CargoPackageName.ValidateName(sourceNameValue, "Cargo source replacement source name cannot be empty or contain invalid characters.");
        CargoPackageName.ValidateName(replacementNameValue, "Cargo source replacement target name cannot be empty or contain invalid characters.");

        sourceName = sourceNameValue;
        replacementName = replacementNameValue;
    }

    internal string GetSourceName()
    {
        return sourceName;
    }

    internal string GetReplacementName()
    {
        return replacementName;
    }
}
