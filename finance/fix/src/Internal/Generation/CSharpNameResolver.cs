using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal static class CSharpNameResolver
{
    internal static readonly string[] ReservedIdentifiers =
    [
        "abstract", "add", "alias", "and", "as", "ascending", "async", "await", "base", "bool", "break", "by", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "descending", "do", "double", "dynamic", "else", "enum", "equals", "event", "explicit", "extern", "false", "file", "finally", "fixed", "float", "for", "foreach", "from", "get", "global", "goto", "group", "if", "implicit", "in", "init", "int", "interface", "internal", "into", "is", "join", "let", "lock", "long", "managed", "nameof", "namespace", "new", "nint", "not", "notnull", "nuint", "null", "object", "on", "operator", "or", "orderby", "out", "override", "params", "partial", "private", "protected", "public", "readonly", "record", "ref", "remove", "required", "return", "sbyte", "scoped", "sealed", "select", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unmanaged", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile", "when", "where", "while", "with", "yield"
    ];

    internal static CSharpNamePlan ResolveNames(FixDictionary dictionary, ModelGenerationOptions options)
    {
        CSharpNamePlan plan = new CSharpNamePlan();
        if (dictionary == null)
        {
            AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-name-dictionary", "The dictionary for C# name resolution is missing.", "/");
            return plan;
        }
        if (options == null)
        {
            AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Error, "missing-name-options", "The C# name resolution options are missing.", "/options");
            return plan;
        }

        plan.RootNamespace = ResolveRootNamespace(options.Namespace, plan.Diagnostics);
        string versionName = ResolveVersionIdentifier(dictionary.Identity, plan.Diagnostics);
        plan.VersionNamespace = versionName.Length == 0 ? plan.RootNamespace : AppendNamespaceSegment(plan.RootNamespace, versionName);
        List<string> typeNames = [];
        ResolveScenarioNames(dictionary, plan);
        ResolveFieldNames(dictionary, plan);
        ResolveCodeSetAndValueNames(dictionary, plan, typeNames);
        ResolveComponentNames(dictionary, plan, typeNames);
        ResolveGroupNames(dictionary, plan, typeNames);
        ResolveMessageNames(dictionary, plan, typeNames);
        ResolveScenarioNamespaces(plan);
        return plan;
    }

    internal static void ResolveScenarioNames(FixDictionary dictionary, CSharpNamePlan plan)
    {
        List<FixScenarioDefinition> definitions = new List<FixScenarioDefinition>(dictionary.Scenarios);
        definitions.Sort((left, right) => CompareScenarioDefinitions(left, right));
        List<string> used = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            FixScenarioDefinition definition = definitions[index];
            string key = NormalizeNameKey(definition.Name);
            if (key.Length == 0 || plan.ScenarioNames.ContainsKey(key)) continue;
            string source = "/scenarios/" + definition.Name;
            string identifier = CreateSymbolIdentifier(definition.Name, "Scenario", "Scenario", plan.Diagnostics, source);
            string unique = AssignUniqueIdentifier(identifier, "Scenario" + EncodeStableValue(definition.Name), used);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A scenario identifier required a deterministic collision suffix.", source);
            plan.ScenarioNames.Add(key, unique);
        }
        if (!plan.ScenarioNames.ContainsKey("BASE")) plan.ScenarioNames.Add("BASE", "BaseScenario");
    }

    internal static void ResolveFieldNames(FixDictionary dictionary, CSharpNamePlan plan)
    {
        List<FixFieldDefinition> definitions = new List<FixFieldDefinition>(dictionary.Fields);
        definitions.Sort((left, right) => CompareFieldDefinitions(left, right));
        List<string> used = [];
        for (int index = 0; index < definitions.Count; index++)
        {
            FixFieldDefinition definition = definitions[index];
            if (plan.FieldNames.ContainsKey(definition.Tag))
            {
                AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "duplicate-field-tag-name", "A duplicate field tag cannot receive a second generated identifier.", "/fields/" + IntegerNameText(definition.Tag));
                continue;
            }
            string source = "/fields/" + IntegerNameText(definition.Tag);
            string identifier = CreateSymbolIdentifier(definition.Name, "Field" + IntegerNameText(definition.Tag), "Value", plan.Diagnostics, source);
            string unique = AssignUniqueIdentifier(identifier, "Tag" + IntegerNameText(definition.Tag), used);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A field identifier required its tag collision suffix.", source);
            plan.FieldNames.Add(definition.Tag, unique);
        }
    }

    internal static void ResolveCodeSetAndValueNames(FixDictionary dictionary, CSharpNamePlan plan, List<string> typeNames)
    {
        List<FixCodeSetDefinition> definitions = new List<FixCodeSetDefinition>(dictionary.CodeSets);
        definitions.Sort((left, right) => CompareCodeSetDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixCodeSetDefinition definition = definitions[index];
            string key = NormalizeNameKey(definition.Name);
            if (key.Length == 0 || plan.CodeSetNames.ContainsKey(key)) continue;
            string source = "/code-sets/" + definition.Name;
            string identifier = CreateSymbolIdentifier(definition.Name, "CodeSet" + IntegerNameText(definition.Id), "Type", plan.Diagnostics, source);
            string unique = AssignUniqueIdentifier(identifier, "CodeSet" + IntegerNameText(definition.Id), typeNames);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A code-set type identifier required a deterministic collision suffix.", source);
            plan.CodeSetNames.Add(key, unique);
            ResolveCodeNames(definition, plan);
        }
    }

    internal static void ResolveCodeNames(FixCodeSetDefinition definition, CSharpNamePlan plan)
    {
        List<FixCodeDefinition> codes = new List<FixCodeDefinition>(definition.Codes);
        codes.Sort((left, right) => CompareCodeDefinitions(left, right));
        List<string> used = [];
        for (int index = 0; index < codes.Count; index++)
        {
            FixCodeDefinition code = codes[index];
            string key = CreateCodeKey(definition.Name, code.Value);
            if (plan.CodeNames.ContainsKey(key))
            {
                AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "duplicate-code-value-name", "A duplicate code value cannot receive a second generated identifier.", "/code-sets/" + definition.Name + "/" + code.Value);
                continue;
            }
            string source = "/code-sets/" + definition.Name + "/" + code.Value;
            string identifier = CreateSymbolIdentifier(code.Name, "Value" + EncodeStableValue(code.Value), "Value", plan.Diagnostics, source);
            string unique = AssignUniqueIdentifier(identifier, "Value" + EncodeStableValue(code.Value), used);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A code identifier required its value collision suffix.", source);
            plan.CodeNames.Add(key, unique);
        }
    }

    internal static void ResolveComponentNames(FixDictionary dictionary, CSharpNamePlan plan, List<string> typeNames)
    {
        List<FixComponentDefinition> definitions = new List<FixComponentDefinition>(dictionary.Components);
        definitions.Sort((left, right) => CompareComponentDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixComponentDefinition definition = definitions[index];
            if (plan.ComponentNames.ContainsKey(definition.Id)) continue;
            string source = "/components/" + IntegerNameText(definition.Id);
            string identifier = CreateSymbolIdentifier(definition.Name, "Component" + IntegerNameText(definition.Id), "Type", plan.Diagnostics, source);
            string unique = AssignUniqueIdentifier(identifier, "Component" + IntegerNameText(definition.Id), typeNames);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A component type identifier required its identifier suffix.", source);
            plan.ComponentNames.Add(definition.Id, unique);
        }
    }

    internal static void ResolveGroupNames(FixDictionary dictionary, CSharpNamePlan plan, List<string> typeNames)
    {
        List<FixGroupDefinition> definitions = new List<FixGroupDefinition>(dictionary.Groups);
        definitions.Sort((left, right) => CompareGroupDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixGroupDefinition definition = definitions[index];
            if (plan.GroupNames.ContainsKey(definition.Id)) continue;
            string source = "/groups/" + IntegerNameText(definition.Id);
            string identifier = CreateSymbolIdentifier(definition.Name, "Group" + IntegerNameText(definition.Id), "Type", plan.Diagnostics, source);
            string unique = AssignUniqueIdentifier(identifier, "Group" + IntegerNameText(definition.Id), typeNames);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A group type identifier required its identifier suffix.", source);
            plan.GroupNames.Add(definition.Id, unique);
        }
    }

    internal static void ResolveMessageNames(FixDictionary dictionary, CSharpNamePlan plan, List<string> typeNames)
    {
        List<FixMessageDefinition> definitions = new List<FixMessageDefinition>(dictionary.Messages);
        definitions.Sort((left, right) => CompareMessageDefinitions(left, right));
        for (int index = 0; index < definitions.Count; index++)
        {
            FixMessageDefinition definition = definitions[index];
            string key = CreateMessageKey(definition.MsgType, definition.Scenario);
            if (plan.MessageNames.ContainsKey(key))
            {
                AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "duplicate-message-key-name", "A duplicate MsgType and scenario cannot receive a second generated identifier.", "/messages/" + definition.MsgType + "/" + definition.Scenario);
                continue;
            }
            string source = "/messages/" + definition.MsgType + "/" + definition.Scenario;
            string identifier = CreateSymbolIdentifier(definition.Name, "Message" + EncodeStableValue(definition.MsgType), "Type", plan.Diagnostics, source);
            string suffix = "MsgType" + EncodeStableValue(definition.MsgType);
            if (definition.Scenario.Length != 0 && NormalizeNameKey(definition.Scenario) != "BASE") suffix += "Scenario" + CreateRawSymbolIdentifier(definition.Scenario, "Scenario", "Scenario");
            string unique = AssignUniqueIdentifier(identifier, suffix, typeNames);
            if (unique != identifier) AddNameDiagnostic(plan.Diagnostics, ValidationSeverity.Warning, "identifier-collision", "A message type identifier required its MsgType or scenario collision suffix.", source);
            plan.MessageNames.Add(key, unique);
        }
    }

    internal static void ResolveScenarioNamespaces(CSharpNamePlan plan)
    {
        List<string> keys = new List<string>(plan.ScenarioNames.Keys);
        keys.Sort((left, right) => string.CompareOrdinal(left, right));
        for (int index = 0; index < keys.Count; index++)
        {
            string key = keys[index];
            string name = plan.ScenarioNames[key];
            string value = key == "BASE" ? plan.VersionNamespace : AppendNamespaceSegment(plan.VersionNamespace, name);
            plan.ScenarioNamespaces.Add(key, value);
        }
    }

    internal static string ResolveRootNamespace(string source, List<GenerationDiagnostic> diagnostics)
    {
        string value = source == null ? "" : source.Trim();
        if (value.Length == 0)
        {
            AddNameDiagnostic(diagnostics, ValidationSeverity.Warning, "namespace-normalized", "An empty generated namespace was replaced with Fix.Generated.", "/options/namespace");
            return "Fix.Generated";
        }
        string[] segments = value.Split('.');
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < segments.Length; index++)
        {
            string segment = CreateNamespaceIdentifier(segments[index], "Generated", diagnostics, "/options/namespace/" + IntegerNameText(index));
            if (builder.Length != 0) builder.Append('.');
            builder.Append(segment);
        }
        return builder.ToString();
    }

    internal static string ResolveVersionIdentifier(FixDictionaryIdentity identity, List<GenerationDiagnostic> diagnostics)
    {
        string source = identity.BeginString;
        if (source.Length == 0) source = StandardApplicationVersionName(identity.ApplicationVersion);
        if (source.Length == 0) source = identity.Name;
        if (source.Length == 0) return "";
        string identifier = CreateSymbolIdentifier(source, "Version", "Version", diagnostics, "/identity/version");
        identifier = NormalizeFixVersionCasing(identifier);
        if (identity.HasExtensionPack && identity.ExtensionPack > 0 && !EndsWithExtensionPack(identifier, identity.ExtensionPack)) identifier += "Ep" + IntegerNameText(identity.ExtensionPack);
        if (identity.CustomVersion.Length != 0) identifier += CreateSymbolIdentifier(identity.CustomVersion, "Custom", "Version", diagnostics, "/identity/custom-version");
        return identifier;
    }

    internal static string StandardApplicationVersionName(string version)
    {
        if (version == FixApplicationVersions.Fix27 || string.Equals(version, "FIX.2.7", StringComparison.OrdinalIgnoreCase)) return "Fix27";
        if (version == FixApplicationVersions.Fix30 || string.Equals(version, "FIX.3.0", StringComparison.OrdinalIgnoreCase)) return "Fix30";
        if (version == FixApplicationVersions.Fix40 || string.Equals(version, "FIX.4.0", StringComparison.OrdinalIgnoreCase)) return "Fix40";
        if (version == FixApplicationVersions.Fix41 || string.Equals(version, "FIX.4.1", StringComparison.OrdinalIgnoreCase)) return "Fix41";
        if (version == FixApplicationVersions.Fix42 || string.Equals(version, "FIX.4.2", StringComparison.OrdinalIgnoreCase)) return "Fix42";
        if (version == FixApplicationVersions.Fix43 || string.Equals(version, "FIX.4.3", StringComparison.OrdinalIgnoreCase)) return "Fix43";
        if (version == FixApplicationVersions.Fix44 || string.Equals(version, "FIX.4.4", StringComparison.OrdinalIgnoreCase)) return "Fix44";
        if (version == FixApplicationVersions.Fix50 || string.Equals(version, "FIX.5.0", StringComparison.OrdinalIgnoreCase)) return "Fix50";
        if (version == FixApplicationVersions.Fix50Sp1 || string.Equals(version, "FIX.5.0SP1", StringComparison.OrdinalIgnoreCase) || string.Equals(version, "FIX.5.0 SP1", StringComparison.OrdinalIgnoreCase)) return "Fix50Sp1";
        if (version == FixApplicationVersions.Fix50Sp2 || string.Equals(version, "FIX.5.0SP2", StringComparison.OrdinalIgnoreCase) || string.Equals(version, "FIX.5.0 SP2", StringComparison.OrdinalIgnoreCase)) return "Fix50Sp2";
        if (version == FixApplicationVersions.FixLatest || string.Equals(version, "FIX.Latest", StringComparison.OrdinalIgnoreCase)) return "FixLatest";
        return version;
    }

    internal static string CreateSymbolIdentifier(string source, string fallback, string reservedSuffix, List<GenerationDiagnostic> diagnostics, string path)
    {
        string value = CreateRawSymbolIdentifier(source, fallback, reservedSuffix);
        bool reserved = IsReservedIdentifier(source) || IsReservedIdentifier(value);
        if (reserved) AddNameDiagnostic(diagnostics, ValidationSeverity.Warning, "reserved-identifier", "A C# reserved word required a deterministic identifier suffix.", path);
        else if (source != value) AddNameDiagnostic(diagnostics, ValidationSeverity.Warning, "identifier-normalized", "A source name was normalized to a portable C# identifier.", path);
        return value;
    }

    internal static string CreateRawSymbolIdentifier(string source, string fallback, string reservedSuffix)
    {
        string clean = SanitizeIdentifier(source, fallback);
        if (clean.Length != 0 && IsAsciiLower(clean[0])) clean = char.ToUpperInvariant(clean[0]) + clean.Substring(1);
        if (IsReservedIdentifier(source) || IsReservedIdentifier(clean)) clean += reservedSuffix;
        return clean;
    }

    internal static string CreateNamespaceIdentifier(string source, string fallback, List<GenerationDiagnostic> diagnostics, string path)
    {
        string clean = SanitizeIdentifier(source, fallback);
        if (IsReservedIdentifier(source) || IsReservedIdentifier(clean)) clean += "Namespace";
        if (clean != source) AddNameDiagnostic(diagnostics, ValidationSeverity.Warning, "namespace-normalized", "A namespace segment was normalized to a portable C# identifier.", path);
        return clean;
    }

    internal static string SanitizeIdentifier(string source, string fallback)
    {
        string value = source == null ? "" : source.Trim();
        StringBuilder builder = new StringBuilder();
        bool capitalize = false;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (IsAsciiLetter(character) || IsAsciiDigit(character) || character == '_')
            {
                if (builder.Length == 0 && IsAsciiDigit(character)) builder.Append(fallback);
                if (capitalize && IsAsciiLower(character)) character = char.ToUpperInvariant(character);
                builder.Append(character);
                capitalize = false;
            }
            else
            {
                capitalize = builder.Length != 0;
            }
        }
        if (builder.Length == 0) builder.Append(fallback);
        if (builder.Length != 0 && IsAsciiDigit(builder[0])) builder.Insert(0, fallback);
        return builder.ToString();
    }

    internal static string AssignUniqueIdentifier(string identifier, string suffix, List<string> used)
    {
        if (!ContainsIdentifier(used, identifier))
        {
            used.Add(identifier);
            return identifier;
        }
        string candidate = identifier + suffix;
        int sequence = 2;
        while (ContainsIdentifier(used, candidate))
        {
            candidate = identifier + suffix + IntegerNameText(sequence);
            sequence++;
        }
        used.Add(candidate);
        return candidate;
    }

    internal static bool ContainsIdentifier(List<string> used, string value)
    {
        for (int index = 0; index < used.Count; index++) if (string.Equals(used[index], value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static string EncodeStableValue(string value)
    {
        if (value == null || value.Length == 0) return "Empty";
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (IsAsciiLetter(character) || IsAsciiDigit(character)) builder.Append(character);
            else
            {
                builder.Append('X');
                builder.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            }
        }
        return builder.ToString();
    }

    internal static string CreateMessageKey(string msgType, string scenario)
    {
        return msgType + "\u001f" + NormalizeNameKey(scenario);
    }

    internal static string CreateCodeKey(string codeSetName, string value)
    {
        return NormalizeNameKey(codeSetName) + "\u001f" + value;
    }

    internal static string NormalizeNameKey(string value)
    {
        return value == null ? "" : value.Trim().ToUpperInvariant();
    }

    internal static string AppendNamespaceSegment(string root, string segment)
    {
        return root.Length == 0 ? segment : root + "." + segment;
    }

    internal static string NormalizeFixVersionCasing(string value)
    {
        string result = value.Replace("_", "", StringComparison.Ordinal);
        if (result.StartsWith("FIXT", StringComparison.OrdinalIgnoreCase)) result = "Fixt" + result.Substring(4);
        else if (result.StartsWith("FIX", StringComparison.OrdinalIgnoreCase)) result = "Fix" + result.Substring(3);
        return NormalizeVersionMarkers(result);
    }

    internal static string NormalizeVersionMarkers(string value)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (index + 2 < value.Length && (value[index] == 'E' || value[index] == 'e') && (value[index + 1] == 'P' || value[index + 1] == 'p') && IsAsciiDigit(value[index + 2]))
            {
                builder.Append("Ep");
                index++;
            }
            else if (index + 2 < value.Length && (value[index] == 'S' || value[index] == 's') && (value[index + 1] == 'P' || value[index + 1] == 'p') && IsAsciiDigit(value[index + 2]))
            {
                builder.Append("Sp");
                index++;
            }
            else builder.Append(value[index]);
            index++;
        }
        return builder.ToString();
    }

    internal static bool EndsWithExtensionPack(string value, int extensionPack)
    {
        string suffix = "EP" + IntegerNameText(extensionPack);
        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsReservedIdentifier(string value)
    {
        if (value == null) return false;
        for (int index = 0; index < ReservedIdentifiers.Length; index++) if (string.Equals(ReservedIdentifiers[index], value, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    internal static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal static bool IsAsciiLower(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static int CompareScenarioDefinitions(FixScenarioDefinition left, FixScenarioDefinition right)
    {
        return string.CompareOrdinal(NormalizeNameKey(left.Name), NormalizeNameKey(right.Name));
    }

    internal static int CompareFieldDefinitions(FixFieldDefinition left, FixFieldDefinition right)
    {
        int comparison = left.Tag.CompareTo(right.Tag);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareCodeSetDefinitions(FixCodeSetDefinition left, FixCodeSetDefinition right)
    {
        int comparison = string.CompareOrdinal(NormalizeNameKey(left.Name), NormalizeNameKey(right.Name));
        return comparison != 0 ? comparison : left.Id.CompareTo(right.Id);
    }

    internal static int CompareCodeDefinitions(FixCodeDefinition left, FixCodeDefinition right)
    {
        int comparison = string.CompareOrdinal(left.Value, right.Value);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareComponentDefinitions(FixComponentDefinition left, FixComponentDefinition right)
    {
        int comparison = left.Id.CompareTo(right.Id);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareGroupDefinitions(FixGroupDefinition left, FixGroupDefinition right)
    {
        int comparison = left.Id.CompareTo(right.Id);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Name, right.Name);
    }

    internal static int CompareMessageDefinitions(FixMessageDefinition left, FixMessageDefinition right)
    {
        int comparison = string.CompareOrdinal(left.MsgType, right.MsgType);
        if (comparison != 0) return comparison;
        comparison = ScenarioSortRank(left.Scenario).CompareTo(ScenarioSortRank(right.Scenario));
        if (comparison != 0) return comparison;
        comparison = string.CompareOrdinal(NormalizeNameKey(left.Scenario), NormalizeNameKey(right.Scenario));
        if (comparison != 0) return comparison;
        comparison = string.CompareOrdinal(left.Name, right.Name);
        return comparison != 0 ? comparison : left.Id.CompareTo(right.Id);
    }

    internal static int ScenarioSortRank(string scenario)
    {
        string key = NormalizeNameKey(scenario);
        return key.Length == 0 || key == "BASE" ? 0 : 1;
    }

    internal static string IntegerNameText(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static void AddNameDiagnostic(List<GenerationDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        diagnostics.Add(diagnostic);
    }
}
