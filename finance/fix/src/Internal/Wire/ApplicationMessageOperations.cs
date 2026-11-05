using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal static class ApplicationMessageOperations
{
    internal static ApplicationMessageResult CreateApplicationMessage(FixDictionary dictionary, string msgType, string scenario)
    {
        ApplicationMessageResult result = ResolveApplicationDefinition(dictionary, msgType, scenario);
        if (!result.Success) return result;

        FixMessage message = new FixMessage();
        message.BeginString = result.Identity.BeginString;
        message.MsgType = result.Definition == null ? msgType : result.Definition.MsgType;
        if (message.BeginString.Length != 0) AddApplicationHeaderField(message.HeaderFields, FixTags.BeginString, message.BeginString);
        AddApplicationHeaderField(message.HeaderFields, FixTags.MsgType, message.MsgType);
        if (result.Identity.ApplicationVersion.Length != 0 && UsesApplicationVersionHeader(message.BeginString)) AddApplicationHeaderField(message.HeaderFields, FixTags.ApplVerID, result.Identity.ApplicationVersion);
        if (result.Identity.HasExtensionPack)
        {
            if (result.Identity.ExtensionPack <= 0) return CreateApplicationFailure(null, "invalid-application-extension", "The selected application extension pack must be positive.");
            AddApplicationHeaderField(message.HeaderFields, FixTags.ApplExtID, result.Identity.ExtensionPack.ToString(CultureInfo.InvariantCulture));
        }
        if (result.Identity.CustomVersion.Length != 0) AddApplicationHeaderField(message.HeaderFields, FixTags.CstmApplVerID, result.Identity.CustomVersion);
        result.Message = message;
        return result;
    }

    internal static bool UsesApplicationVersionHeader(string beginString)
    {
        return beginString.Length == 0 || beginString.StartsWith("FIXT.", System.StringComparison.Ordinal);
    }

    internal static ApplicationMessageResult IdentifyApplicationMessage(FixMessage message, FixDictionary dictionary, string scenario)
    {
        if (message == null) return CreateApplicationFailure(null, "missing-application-message", "The application message is missing.");
        DictionaryTagValue typeField = ReadApplicationTag(message, FixTags.MsgType);
        if (typeField.Count > 1) return CreateApplicationFailure(message, "duplicate-message-type", "MsgType occurs more than once.");
        string fieldType = ReadApplicationText(typeField);
        string msgType = message.MsgType == null ? "" : message.MsgType;
        if (msgType.Length == 0) msgType = fieldType;
        else if (fieldType.Length != 0 && fieldType != msgType) return CreateApplicationFailure(message, "conflicting-message-type", "The decoded MsgType does not match the MsgType field.");

        ApplicationMessageResult result = ResolveApplicationDefinition(dictionary, msgType, scenario);
        result.Message = message;
        if (!result.Success) return result;

        DictionaryTagValue version = ReadApplicationTag(message, FixTags.ApplVerID);
        DictionaryTagValue extension = ReadApplicationTag(message, FixTags.ApplExtID);
        DictionaryTagValue custom = ReadApplicationTag(message, FixTags.CstmApplVerID);
        if (version.Count > 1 || extension.Count > 1 || custom.Count > 1) return CreateApplicationFailure(message, "duplicate-application-version-metadata", "Application version metadata occurs more than once.");
        if (version.IsPresent)
        {
            string value = ReadApplicationText(version);
            if (value.Length == 0) return CreateApplicationFailure(message, "empty-application-version", "ApplVerID must not be empty.");
            result.Identity.ApplicationVersion = value;
            result.Identity.HasExtensionPack = false;
            result.Identity.ExtensionPack = 0;
            result.Identity.CustomVersion = "";
        }
        if (extension.IsPresent)
        {
            int value = ParseApplicationExtension(extension.Value);
            if (value <= 0) return CreateApplicationFailure(message, "invalid-application-extension", "ApplExtID must contain a positive decimal integer.");
            result.Identity.HasExtensionPack = true;
            result.Identity.ExtensionPack = value;
        }
        if (custom.IsPresent)
        {
            string value = ReadApplicationText(custom);
            if (value.Length == 0) return CreateApplicationFailure(message, "empty-custom-application-version", "CstmApplVerID must not be empty.");
            result.Identity.CustomVersion = value;
        }
        if (message.BeginString != null && message.BeginString.Length != 0) result.Identity.BeginString = message.BeginString;
        return result;
    }

    internal static ApplicationMessageResult ResolveApplicationDefinition(FixDictionary dictionary, string msgType, string scenario)
    {
        if (dictionary == null) return CreateApplicationFailure(null, "missing-application-dictionary", "The application dictionary is missing.");
        if (msgType == null || msgType.Length == 0) return CreateApplicationFailure(null, "missing-application-message-type", "The application MsgType is missing.");
        if (scenario == null) return CreateApplicationFailure(null, "missing-application-scenario", "The application scenario selection is missing.");

        string selectedScenario = SelectApplicationScenario(dictionary, msgType, scenario);
        ApplicationMessageResult result = new ApplicationMessageResult();
        result.Identity = DictionaryComposition.CloneIdentity(dictionary.Identity);
        result.Identity.Scenario = selectedScenario;
        List<string> candidates = BuildApplicationScenarioCandidates(dictionary, selectedScenario);
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            FixMessageDefinition? found = null;
            int matches = 0;
            for (int index = 0; index < dictionary.Messages.Count; index++)
            {
                FixMessageDefinition definition = dictionary.Messages[index];
                if (!definition.IsRemoved && definition.MsgType == msgType && ApplicationNamesEqual(definition.Scenario, candidates[candidateIndex], dictionary.Options.NormalizeNames))
                {
                    if (found == null) found = definition;
                    matches++;
                }
            }
            if (matches > 1) return CreateApplicationFailure(null, "ambiguous-application-message-definition", "More than one message definition matches the selected MsgType and scenario.");
            if (found != null)
            {
                result.Success = true;
                result.Definition = found;
                return result;
            }
        }

        bool knownType = false;
        for (int index = 0; index < dictionary.Messages.Count; index++) if (!dictionary.Messages[index].IsRemoved && dictionary.Messages[index].MsgType == msgType) knownType = true;
        if (knownType) return CreateApplicationFailure(null, "unknown-application-message-scenario", "No message definition matches the selected scenario.");
        return CreateApplicationFailure(null, "unknown-application-message-type", "No message definition has the requested MsgType.");
    }

    internal static string SelectApplicationScenario(FixDictionary dictionary, string msgType, string scenario)
    {
        string selected = scenario.Trim();
        if (selected.Length == 0) selected = dictionary.Identity.Scenario.Trim();
        if (selected.Length != 0) return selected;
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            if (!definition.IsRemoved && definition.MsgType == msgType && ApplicationNamesEqual(definition.Scenario, "base", dictionary.Options.NormalizeNames)) return "base";
        }
        return "";
    }

    internal static List<string> BuildApplicationScenarioCandidates(FixDictionary dictionary, string selectedScenario)
    {
        List<string> result = [];
        AddApplicationScenarioCandidate(result, selectedScenario, dictionary.Options.NormalizeNames);
        string current = selectedScenario;
        int remaining = dictionary.Scenarios.Count;
        while (current.Length != 0 && remaining > 0)
        {
            string parent = FindApplicationScenarioParent(dictionary, current);
            if (parent.Length == 0 || ContainsApplicationScenarioCandidate(result, parent, dictionary.Options.NormalizeNames)) break;
            AddApplicationScenarioCandidate(result, parent, dictionary.Options.NormalizeNames);
            current = parent;
            remaining--;
        }
        AddApplicationScenarioCandidate(result, "", dictionary.Options.NormalizeNames);
        return result;
    }

    internal static string FindApplicationScenarioParent(FixDictionary dictionary, string scenario)
    {
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            if (ApplicationNamesEqual(dictionary.Scenarios[index].Name, scenario, dictionary.Options.NormalizeNames)) return dictionary.Scenarios[index].ParentName;
        }
        return "";
    }

    internal static void AddApplicationScenarioCandidate(List<string> destination, string scenario, bool normalizeNames)
    {
        if (!ContainsApplicationScenarioCandidate(destination, scenario, normalizeNames)) destination.Add(scenario);
    }

    internal static bool ContainsApplicationScenarioCandidate(List<string> source, string scenario, bool normalizeNames)
    {
        for (int index = 0; index < source.Count; index++) if (ApplicationNamesEqual(source[index], scenario, normalizeNames)) return true;
        return false;
    }

    internal static bool ApplicationNamesEqual(string left, string right, bool normalizeNames)
    {
        if (!normalizeNames) return left == right;
        return left.Trim().ToUpperInvariant() == right.Trim().ToUpperInvariant();
    }

    internal static DictionaryTagValue ReadApplicationTag(FixMessage message, int tag)
    {
        DictionaryTagValue result = new DictionaryTagValue();
        ReadApplicationFields(message.HeaderFields, tag, result);
        ReadApplicationFields(message.BodyFields, tag, result);
        ReadApplicationFields(message.TrailerFields, tag, result);
        return result;
    }

    internal static void ReadApplicationFields(List<FixField> fields, int tag, DictionaryTagValue result)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                result.Count++;
                if (!result.IsPresent)
                {
                    result.IsPresent = true;
                    result.Value = fields[index].Value == null ? [] : fields[index].Value;
                }
            }
        }
    }

    internal static string ReadApplicationText(DictionaryTagValue value)
    {
        return value.IsPresent ? Encoding.Latin1.GetString(value.Value) : "";
    }

    internal static int ParseApplicationExtension(byte[] value)
    {
        if (value.Length == 0) return -1;
        int result = 0;
        for (int index = 0; index < value.Length; index++)
        {
            int digit = value[index] - 48;
            if (digit < 0 || digit > 9 || result > (int.MaxValue - digit) / 10) return -1;
            result = result * 10 + digit;
        }
        return result;
    }

    internal static void AddApplicationHeaderField(List<FixField> destination, int tag, string value)
    {
        FixField field = new FixField();
        field.Tag = tag;
        field.Value = Encoding.Latin1.GetBytes(value);
        field.WireIndex = destination.Count;
        destination.Add(field);
    }

    internal static ApplicationMessageResult CreateApplicationFailure(FixMessage? message, string code, string description)
    {
        ApplicationMessageResult result = new ApplicationMessageResult();
        result.Message = message;
        result.ErrorCode = code;
        result.ErrorMessage = description;
        return result;
    }
}
