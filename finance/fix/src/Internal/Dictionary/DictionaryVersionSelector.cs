using System.Collections.Generic;
using System.Text;

namespace fix;

internal static class DictionaryVersionSelector
{
    internal static DictionarySelectionResult SelectDictionaryIdentities(FixMessage message, FixDictionaryIdentity configuredTransport, FixDictionaryIdentity configuredApplication, string configuredDefaultApplicationVersion)
    {
        DictionarySelectionResult result = new DictionarySelectionResult();
        result.TransportIdentity = DictionaryComposition.CloneIdentity(configuredTransport);
        result.ApplicationIdentity = DictionaryComposition.CloneIdentity(configuredApplication);

        DictionaryTagValue beginField = ReadTagValue(message, FixTags.BeginString);
        DictionaryTagValue applicationField = ReadTagValue(message, FixTags.ApplVerID);
        DictionaryTagValue defaultApplicationField = ReadTagValue(message, FixTags.DefaultApplVerID);
        DictionaryTagValue extensionField = ReadTagValue(message, FixTags.ApplExtID);
        DictionaryTagValue customField = ReadTagValue(message, FixTags.CstmApplVerID);
        ReportDuplicate(result.Diagnostics, beginField, FixTags.BeginString);

        string beginString = ResolveBeginString(message, beginField, configuredTransport, result.Diagnostics);
        result.TransportIdentity.BeginString = beginString;
        ClearApplicationIdentity(result.TransportIdentity);

        if (beginString.Length == 0)
        {
            AddSelectionDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-begin-string", "A message or configured transport dictionary must supply BeginString.", FixTags.BeginString);
        }
        else if (IsTransportIndependent(beginString))
        {
            ReportDuplicate(result.Diagnostics, applicationField, FixTags.ApplVerID);
            ReportDuplicate(result.Diagnostics, defaultApplicationField, FixTags.DefaultApplVerID);
            ReportDuplicate(result.Diagnostics, extensionField, FixTags.ApplExtID);
            ReportDuplicate(result.Diagnostics, customField, FixTags.CstmApplVerID);
            SelectFixtApplicationIdentity(result.ApplicationIdentity, applicationField, defaultApplicationField, extensionField, customField, configuredDefaultApplicationVersion, result.Diagnostics);
        }
        else
        {
            result.ApplicationIdentity.BeginString = beginString;
            result.ApplicationIdentity.ApplicationVersion = "";
            result.ApplicationIdentity.HasExtensionPack = false;
            result.ApplicationIdentity.ExtensionPack = 0;
            result.ApplicationIdentity.CustomVersion = "";
        }

        result.Success = !HasSelectionErrors(result.Diagnostics);
        return result;
    }

    internal static void SelectFixtApplicationIdentity(FixDictionaryIdentity identity, DictionaryTagValue applicationField, DictionaryTagValue defaultApplicationField, DictionaryTagValue extensionField, DictionaryTagValue customField, string configuredDefaultApplicationVersion, List<DictionaryDiagnostic> diagnostics)
    {
        identity.BeginString = "";
        string configuredVersion = identity.ApplicationVersion;
        string selectedVersion = configuredVersion;
        bool versionWasOverridden = false;

        string applicationVersion = ReadTextValue(applicationField, FixTags.ApplVerID, diagnostics);
        string defaultApplicationVersion = ReadTextValue(defaultApplicationField, FixTags.DefaultApplVerID, diagnostics);
        if (applicationVersion.Length != 0)
        {
            selectedVersion = applicationVersion;
            versionWasOverridden = true;
        }
        else if (defaultApplicationVersion.Length != 0)
        {
            selectedVersion = defaultApplicationVersion;
            versionWasOverridden = true;
        }
        else if (configuredDefaultApplicationVersion.Length != 0)
        {
            selectedVersion = configuredDefaultApplicationVersion;
            versionWasOverridden = configuredDefaultApplicationVersion != configuredVersion;
        }

        identity.ApplicationVersion = selectedVersion;
        if (versionWasOverridden)
        {
            identity.Name = "";
            identity.HasExtensionPack = false;
            identity.ExtensionPack = 0;
            identity.CustomVersion = "";
        }
        if (selectedVersion.Length == 0)
        {
            AddSelectionDiagnostic(diagnostics, ValidationSeverity.Error, "missing-application-version", "A FIXT message requires ApplVerID, DefaultApplVerID, or a configured application version.", FixTags.ApplVerID);
        }

        if (extensionField.IsPresent)
        {
            int extensionPack = ParseExtensionPack(extensionField.Value);
            if (extensionPack <= 0)
            {
                AddSelectionDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-application-extension", "ApplExtID must contain a positive decimal integer.", FixTags.ApplExtID);
            }
            else
            {
                identity.HasExtensionPack = true;
                identity.ExtensionPack = extensionPack;
            }
        }
        string customVersion = ReadTextValue(customField, FixTags.CstmApplVerID, diagnostics);
        if (customVersion.Length != 0)
        {
            identity.CustomVersion = customVersion;
        }
    }

    internal static string ResolveBeginString(FixMessage message, DictionaryTagValue beginField, FixDictionaryIdentity configuredTransport, List<DictionaryDiagnostic> diagnostics)
    {
        string fieldValue = ReadTextValue(beginField, FixTags.BeginString, diagnostics);
        if (message.BeginString.Length != 0)
        {
            if (fieldValue.Length != 0 && fieldValue != message.BeginString)
            {
                AddSelectionDiagnostic(diagnostics, ValidationSeverity.Error, "conflicting-begin-string", "The decoded BeginString does not match the first BeginString field.", FixTags.BeginString);
            }
            return message.BeginString;
        }
        if (fieldValue.Length != 0)
        {
            return fieldValue;
        }
        return configuredTransport.BeginString;
    }

    internal static DictionaryTagValue ReadTagValue(FixMessage message, int tag)
    {
        DictionaryTagValue value = new DictionaryTagValue();
        ReadTagFromFields(message.HeaderFields, tag, value);
        ReadTagFromFields(message.BodyFields, tag, value);
        ReadTagFromFields(message.TrailerFields, tag, value);
        return value;
    }

    internal static void ReadTagFromFields(List<FixField> fields, int tag, DictionaryTagValue value)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                value.Count++;
                if (!value.IsPresent)
                {
                    value.IsPresent = true;
                    value.Value = fields[index].Value;
                }
            }
        }
    }

    internal static string ReadTextValue(DictionaryTagValue value, int tag, List<DictionaryDiagnostic> diagnostics)
    {
        if (!value.IsPresent)
        {
            return "";
        }
        if (value.Value.Length == 0)
        {
            AddSelectionDiagnostic(diagnostics, ValidationSeverity.Error, "empty-dictionary-version-field", "A dictionary version selection field must not be empty.", tag);
            return "";
        }
        return Encoding.Latin1.GetString(value.Value);
    }

    internal static int ParseExtensionPack(byte[] value)
    {
        if (value.Length == 0)
        {
            return -1;
        }
        int parsed = 0;
        for (int index = 0; index < value.Length; index++)
        {
            byte digit = value[index];
            if (digit < 48 || digit > 57)
            {
                return -1;
            }
            int numericDigit = digit - 48;
            if (parsed > (int.MaxValue - numericDigit) / 10)
            {
                return -1;
            }
            parsed = parsed * 10 + numericDigit;
        }
        return parsed;
    }

    internal static bool IsTransportIndependent(string beginString)
    {
        return beginString.StartsWith("FIXT.", System.StringComparison.Ordinal);
    }

    internal static void ClearApplicationIdentity(FixDictionaryIdentity identity)
    {
        identity.ApplicationVersion = "";
        identity.HasExtensionPack = false;
        identity.ExtensionPack = 0;
        identity.CustomVersion = "";
    }

    internal static void ReportDuplicate(List<DictionaryDiagnostic> diagnostics, DictionaryTagValue value, int tag)
    {
        if (value.Count > 1)
        {
            AddSelectionDiagnostic(diagnostics, ValidationSeverity.Error, "duplicate-dictionary-version-field", "A dictionary version selection field appears more than once.", tag);
        }
    }

    internal static void AddSelectionDiagnostic(List<DictionaryDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, int tag)
    {
        if (diagnostics.Count >= FixDefaults.MaxDiagnostics)
        {
            return;
        }
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.HasTag = true;
        diagnostic.Tag = tag;
        diagnostics.Add(diagnostic);
    }

    internal static bool HasSelectionErrors(List<DictionaryDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal)
            {
                return true;
            }
        }
        return false;
    }
}
