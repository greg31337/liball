using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal static class DictionaryDifference
{
    internal static DictionaryDiff CompareGraphs(FixDictionary beforeDictionary, FixDictionary afterDictionary)
    {
        DictionaryDiff diff = new DictionaryDiff();
        diff.BeforeIdentity = DictionaryComposition.CloneIdentity(beforeDictionary.Identity);
        diff.AfterIdentity = DictionaryComposition.CloneIdentity(afterDictionary.Identity);
        bool normalizeNames = beforeDictionary.Options.NormalizeNames || afterDictionary.Options.NormalizeNames;
        CompareIdentity(beforeDictionary.Identity, afterDictionary.Identity, diff.Changes);
        CompareDatatypes(beforeDictionary.Datatypes, afterDictionary.Datatypes, diff.Changes, normalizeNames);
        CompareCodeSets(beforeDictionary.CodeSets, afterDictionary.CodeSets, diff.Changes, normalizeNames);
        CompareFields(beforeDictionary.Fields, afterDictionary.Fields, diff.Changes, normalizeNames);
        CompareMembers(beforeDictionary.Header, afterDictionary.Header, "header", diff.Changes, normalizeNames);
        CompareMembers(beforeDictionary.Trailer, afterDictionary.Trailer, "trailer", diff.Changes, normalizeNames);
        CompareComponents(beforeDictionary.Components, afterDictionary.Components, diff.Changes, normalizeNames);
        CompareGroups(beforeDictionary.Groups, afterDictionary.Groups, diff.Changes, normalizeNames);
        CompareMessages(beforeDictionary.Messages, afterDictionary.Messages, diff.Changes, normalizeNames);
        CompareScenarios(beforeDictionary.Scenarios, afterDictionary.Scenarios, diff.Changes, normalizeNames);
        return diff;
    }

    internal static void CompareIdentity(FixDictionaryIdentity before, FixDictionaryIdentity after, List<DictionaryChange> changes)
    {
        AddTextReplacement(changes, "identity/name", before.Name, after.Name, false);
        AddTextReplacement(changes, "identity/begin-string", before.BeginString, after.BeginString, true);
        AddTextReplacement(changes, "identity/application-version", before.ApplicationVersion, after.ApplicationVersion, true);
        AddTextReplacement(changes, "identity/has-extension-pack", BooleanText(before.HasExtensionPack), BooleanText(after.HasExtensionPack), true);
        if (before.HasExtensionPack || after.HasExtensionPack)
        {
            AddTextReplacement(changes, "identity/extension-pack", IntegerText(before.ExtensionPack), IntegerText(after.ExtensionPack), true);
        }
        AddTextReplacement(changes, "identity/custom-version", before.CustomVersion, after.CustomVersion, true);
        AddTextReplacement(changes, "identity/scenario", before.Scenario, after.Scenario, true);
    }

    internal static void CompareDatatypes(List<FixDatatypeDefinition> before, List<FixDatatypeDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixDatatypeDefinition definition = before[index];
            int match = DictionaryComposition.FindDatatypeIndex(after, definition.Name, normalizeNames);
            string path = "datatypes/" + EscapePathSegment(definition.Name);
            if (match < 0)
            {
                AddChange(changes, "remove", path, DatatypeText(definition), "", true);
            }
            else if (!DatatypeEquals(definition, after[match]))
            {
                bool breaking = definition.FieldType != after[match].FieldType || definition.BaseTypeName != after[match].BaseTypeName;
                AddChange(changes, "replace", path, DatatypeText(definition), DatatypeText(after[match]), breaking);
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixDatatypeDefinition definition = after[index];
            if (DictionaryComposition.FindDatatypeIndex(before, definition.Name, normalizeNames) < 0)
            {
                AddChange(changes, "add", "datatypes/" + EscapePathSegment(definition.Name), "", DatatypeText(definition), false);
            }
        }
    }

    internal static void CompareCodeSets(List<FixCodeSetDefinition> before, List<FixCodeSetDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixCodeSetDefinition definition = before[index];
            int match = DictionaryComposition.FindCodeSetIndex(after, definition.Name, normalizeNames);
            string path = "code-sets/" + EscapePathSegment(definition.Name);
            if (match < 0)
            {
                AddChange(changes, "remove", path, CodeSetText(definition), "", true);
            }
            else
            {
                FixCodeSetDefinition replacement = after[match];
                if (!CodeSetEquals(definition, replacement))
                {
                    AddChange(changes, "replace", path, CodeSetText(definition), CodeSetText(replacement), definition.FieldType != replacement.FieldType);
                }
                CompareCodes(definition.Codes, replacement.Codes, path + "/codes", changes, normalizeNames);
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixCodeSetDefinition definition = after[index];
            if (DictionaryComposition.FindCodeSetIndex(before, definition.Name, normalizeNames) < 0)
            {
                AddChange(changes, "add", "code-sets/" + EscapePathSegment(definition.Name), "", CodeSetText(definition), false);
            }
        }
    }

    internal static void CompareCodes(List<FixCodeDefinition> before, List<FixCodeDefinition> after, string ownerPath, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixCodeDefinition definition = before[index];
            int match = DictionaryComposition.FindCodeIndex(after, definition.Value);
            string path = ownerPath + "/" + EscapePathSegment(definition.Value);
            if (match < 0)
            {
                AddChange(changes, "remove", path, CodeText(definition), "", true);
            }
            else if (!CodeEquals(definition, after[match]))
            {
                AddChange(changes, "replace", path, CodeText(definition), CodeText(after[match]), false);
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixCodeDefinition definition = after[index];
            if (DictionaryComposition.FindCodeIndex(before, definition.Value) < 0)
            {
                AddChange(changes, "add", ownerPath + "/" + EscapePathSegment(definition.Value), "", CodeText(definition), false);
            }
        }
    }

    internal static void CompareFields(List<FixFieldDefinition> before, List<FixFieldDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixFieldDefinition definition = before[index];
            int match = DictionaryComposition.FindFieldIndex(after, definition.Tag);
            string path = "fields/" + IntegerText(definition.Tag);
            if (match < 0)
            {
                AddChange(changes, "remove", path, FieldText(definition), "", true);
            }
            else if (!FieldEquals(definition, after[match]))
            {
                AddChange(changes, "replace", path, FieldText(definition), FieldText(after[match]), FieldChangeBreaks(definition, after[match]));
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixFieldDefinition definition = after[index];
            if (DictionaryComposition.FindFieldIndex(before, definition.Tag) < 0)
            {
                AddChange(changes, "add", "fields/" + IntegerText(definition.Tag), "", FieldText(definition), false);
            }
        }
    }

    internal static void CompareComponents(List<FixComponentDefinition> before, List<FixComponentDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixComponentDefinition definition = before[index];
            int match = DictionaryComposition.FindComponentIndex(after, definition.Id);
            string path = "components/" + IntegerText(definition.Id);
            if (match < 0)
            {
                AddChange(changes, "remove", path, ComponentText(definition), "", true);
            }
            else
            {
                FixComponentDefinition replacement = after[match];
                if (!ComponentEquals(definition, replacement))
                {
                    AddChange(changes, "replace", path, ComponentText(definition), ComponentText(replacement), false);
                }
                CompareMembers(definition.Members, replacement.Members, path, changes, normalizeNames);
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixComponentDefinition definition = after[index];
            if (DictionaryComposition.FindComponentIndex(before, definition.Id) < 0)
            {
                AddChange(changes, "add", "components/" + IntegerText(definition.Id), "", ComponentText(definition), false);
            }
        }
    }

    internal static void CompareGroups(List<FixGroupDefinition> before, List<FixGroupDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixGroupDefinition definition = before[index];
            int match = DictionaryComposition.FindGroupIndex(after, definition.Id);
            string path = "groups/" + IntegerText(definition.Id);
            if (match < 0)
            {
                AddChange(changes, "remove", path, GroupText(definition), "", true);
            }
            else
            {
                FixGroupDefinition replacement = after[match];
                if (!GroupEquals(definition, replacement))
                {
                    bool breaking = definition.CountTag != replacement.CountTag || definition.DelimiterTag != replacement.DelimiterTag;
                    AddChange(changes, "replace", path, GroupText(definition), GroupText(replacement), breaking);
                }
                CompareMembers(definition.Members, replacement.Members, path, changes, normalizeNames);
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixGroupDefinition definition = after[index];
            if (DictionaryComposition.FindGroupIndex(before, definition.Id) < 0)
            {
                AddChange(changes, "add", "groups/" + IntegerText(definition.Id), "", GroupText(definition), false);
            }
        }
    }

    internal static void CompareMessages(List<FixMessageDefinition> before, List<FixMessageDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixMessageDefinition definition = before[index];
            int match = DictionaryComposition.FindMessageIndex(after, definition.MsgType, definition.Scenario, normalizeNames);
            string path = MessagePath(definition.MsgType, definition.Scenario);
            if (match < 0)
            {
                AddChange(changes, "remove", path, MessageText(definition), "", true);
            }
            else
            {
                FixMessageDefinition replacement = after[match];
                if (!MessageEquals(definition, replacement))
                {
                    AddChange(changes, "replace", path, MessageText(definition), MessageText(replacement), false);
                }
                CompareMembers(definition.Members, replacement.Members, path, changes, normalizeNames);
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixMessageDefinition definition = after[index];
            if (DictionaryComposition.FindMessageIndex(before, definition.MsgType, definition.Scenario, normalizeNames) < 0)
            {
                AddChange(changes, "add", MessagePath(definition.MsgType, definition.Scenario), "", MessageText(definition), false);
            }
        }
    }

    internal static void CompareScenarios(List<FixScenarioDefinition> before, List<FixScenarioDefinition> after, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixScenarioDefinition definition = before[index];
            int match = DictionaryComposition.FindScenarioIndex(after, definition.Name, normalizeNames);
            string path = "scenarios/" + EscapePathSegment(definition.Name);
            if (match < 0)
            {
                AddChange(changes, "remove", path, ScenarioText(definition), "", true);
            }
            else
            {
                FixScenarioDefinition replacement = after[match];
                if (!ScenarioEquals(definition, replacement))
                {
                    AddChange(changes, "replace", path, ScenarioText(definition), ScenarioText(replacement), definition.ParentName != replacement.ParentName);
                }
                CompareMembers(definition.Members, replacement.Members, path, changes, normalizeNames);
                if (!RuleListsEqual(definition.Rules, replacement.Rules))
                {
                    AddChange(changes, "replace", path + "/rules", RuleListText(definition.Rules), RuleListText(replacement.Rules), true);
                }
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixScenarioDefinition definition = after[index];
            if (DictionaryComposition.FindScenarioIndex(before, definition.Name, normalizeNames) < 0)
            {
                AddChange(changes, "add", "scenarios/" + EscapePathSegment(definition.Name), "", ScenarioText(definition), false);
            }
        }
    }

    internal static void CompareMembers(List<FixMemberDefinition> before, List<FixMemberDefinition> after, string ownerPath, List<DictionaryChange> changes, bool normalizeNames)
    {
        for (int index = 0; index < before.Count; index++)
        {
            FixMemberDefinition definition = before[index];
            int match = DictionaryComposition.FindMemberIndex(after, definition, normalizeNames);
            string path = ownerPath + "/members/" + MemberPathSegment(definition);
            if (match < 0)
            {
                AddChange(changes, "remove", path, MemberText(definition), "", true);
            }
            else
            {
                FixMemberDefinition replacement = after[match];
                if (definition.Presence != replacement.Presence)
                {
                    AddChange(changes, "replace", path + "/presence", PresenceText(definition.Presence), PresenceText(replacement.Presence), PresenceChangeBreaks(definition.Presence, replacement.Presence));
                }
                if (!MemberMetadataEquals(definition, replacement))
                {
                    bool breaking = definition.Position != replacement.Position || definition.PresenceRule != replacement.PresenceRule || definition.RulePresence != replacement.RulePresence;
                    AddChange(changes, "replace", path + "/definition", MemberMetadataText(definition), MemberMetadataText(replacement), breaking);
                }
            }
        }
        for (int index = 0; index < after.Count; index++)
        {
            FixMemberDefinition definition = after[index];
            if (DictionaryComposition.FindMemberIndex(before, definition, normalizeNames) < 0)
            {
                AddChange(changes, "add", ownerPath + "/members/" + MemberPathSegment(definition), "", MemberText(definition), PresenceRestricts(definition.Presence));
            }
        }
    }

    internal static void AddTextReplacement(List<DictionaryChange> changes, string path, string before, string after, bool breaking)
    {
        if (before != after)
        {
            AddChange(changes, "replace", path, CanonicalText(before), CanonicalText(after), breaking);
        }
    }

    internal static void AddChange(List<DictionaryChange> changes, string kind, string path, string before, string after, bool breaking)
    {
        DictionaryChange change = new DictionaryChange();
        change.Kind = kind;
        change.Path = path;
        change.Before = before;
        change.After = after;
        change.IsBreaking = breaking;
        changes.Add(change);
    }

    internal static bool DatatypeEquals(FixDatatypeDefinition before, FixDatatypeDefinition after)
    {
        return before.Name == after.Name && before.FieldType == after.FieldType && before.BaseTypeName == after.BaseTypeName && before.Description == after.Description;
    }

    internal static bool CodeSetEquals(FixCodeSetDefinition before, FixCodeSetDefinition after)
    {
        return before.Id == after.Id && before.Name == after.Name && before.FieldType == after.FieldType && before.Description == after.Description;
    }

    internal static bool CodeEquals(FixCodeDefinition before, FixCodeDefinition after)
    {
        return before.Id == after.Id && before.Name == after.Name && before.Value == after.Value && before.SortOrder == after.SortOrder && before.Description == after.Description;
    }

    internal static bool FieldEquals(FixFieldDefinition before, FixFieldDefinition after)
    {
        return before.Tag == after.Tag && before.Name == after.Name && before.Abbreviation == after.Abbreviation && before.DatatypeName == after.DatatypeName && before.FieldType == after.FieldType && before.CodeSetName == after.CodeSetName && before.HasDiscriminatorTag == after.HasDiscriminatorTag && before.DiscriminatorTag == after.DiscriminatorTag && before.HasLengthTag == after.HasLengthTag && before.LengthTag == after.LengthTag && before.HasDataTag == after.HasDataTag && before.DataTag == after.DataTag && before.Description == after.Description;
    }

    internal static bool ComponentEquals(FixComponentDefinition before, FixComponentDefinition after)
    {
        return before.Id == after.Id && before.Name == after.Name && before.Abbreviation == after.Abbreviation && before.Description == after.Description;
    }

    internal static bool GroupEquals(FixGroupDefinition before, FixGroupDefinition after)
    {
        return before.Id == after.Id && before.Name == after.Name && before.CountTag == after.CountTag && before.DelimiterTag == after.DelimiterTag && before.Description == after.Description;
    }

    internal static bool MessageEquals(FixMessageDefinition before, FixMessageDefinition after)
    {
        return before.Id == after.Id && before.Name == after.Name && before.MsgType == after.MsgType && before.Category == after.Category && before.Scenario == after.Scenario && before.Description == after.Description;
    }

    internal static bool ScenarioEquals(FixScenarioDefinition before, FixScenarioDefinition after)
    {
        return before.Name == after.Name && before.ParentName == after.ParentName && before.Description == after.Description;
    }

    internal static bool MemberMetadataEquals(FixMemberDefinition before, FixMemberDefinition after)
    {
        return before.Name == after.Name && before.Kind == after.Kind && before.ReferenceId == after.ReferenceId && before.Position == after.Position && before.Scenario == after.Scenario && before.PresenceRule == after.PresenceRule && before.RulePresence == after.RulePresence;
    }

    internal static bool RuleListsEqual(List<string> before, List<string> after)
    {
        if (before.Count != after.Count)
        {
            return false;
        }
        for (int index = 0; index < before.Count; index++)
        {
            if (before[index] != after[index])
            {
                return false;
            }
        }
        return true;
    }

    internal static bool FieldChangeBreaks(FixFieldDefinition before, FixFieldDefinition after)
    {
        return before.DatatypeName != after.DatatypeName || before.FieldType != after.FieldType || before.CodeSetName != after.CodeSetName || before.HasDiscriminatorTag != after.HasDiscriminatorTag || before.DiscriminatorTag != after.DiscriminatorTag || before.HasLengthTag != after.HasLengthTag || before.LengthTag != after.LengthTag || before.HasDataTag != after.HasDataTag || before.DataTag != after.DataTag;
    }

    internal static bool PresenceChangeBreaks(FixPresence before, FixPresence after)
    {
        if (before == after)
        {
            return false;
        }
        return PresenceRestricts(after);
    }

    internal static bool PresenceRestricts(FixPresence presence)
    {
        return presence == FixPresence.Required || presence == FixPresence.Forbidden || presence == FixPresence.Constant || presence == FixPresence.Conditional;
    }

    internal static string DatatypeText(FixDatatypeDefinition definition)
    {
        return "type=" + definition.FieldType.ToString() + ";base=" + CanonicalText(definition.BaseTypeName) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string CodeSetText(FixCodeSetDefinition definition)
    {
        return "id=" + IntegerText(definition.Id) + ";type=" + definition.FieldType.ToString() + ";description=" + CanonicalText(definition.Description);
    }

    internal static string CodeText(FixCodeDefinition definition)
    {
        return "id=" + IntegerText(definition.Id) + ";name=" + CanonicalText(definition.Name) + ";value=" + CanonicalText(definition.Value) + ";sort=" + IntegerText(definition.SortOrder) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string FieldText(FixFieldDefinition definition)
    {
        return "name=" + CanonicalText(definition.Name) + ";abbreviation=" + CanonicalText(definition.Abbreviation) + ";datatype=" + CanonicalText(definition.DatatypeName) + ";type=" + definition.FieldType.ToString() + ";code-set=" + CanonicalText(definition.CodeSetName) + ";discriminator-tag=" + OptionalIntegerText(definition.HasDiscriminatorTag, definition.DiscriminatorTag) + ";length-tag=" + OptionalIntegerText(definition.HasLengthTag, definition.LengthTag) + ";data-tag=" + OptionalIntegerText(definition.HasDataTag, definition.DataTag) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string ComponentText(FixComponentDefinition definition)
    {
        return "name=" + CanonicalText(definition.Name) + ";abbreviation=" + CanonicalText(definition.Abbreviation) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string GroupText(FixGroupDefinition definition)
    {
        return "name=" + CanonicalText(definition.Name) + ";count-tag=" + IntegerText(definition.CountTag) + ";delimiter-tag=" + IntegerText(definition.DelimiterTag) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string MessageText(FixMessageDefinition definition)
    {
        return "id=" + IntegerText(definition.Id) + ";name=" + CanonicalText(definition.Name) + ";msg-type=" + CanonicalText(definition.MsgType) + ";category=" + CanonicalText(definition.Category) + ";scenario=" + CanonicalText(definition.Scenario) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string ScenarioText(FixScenarioDefinition definition)
    {
        return "parent=" + CanonicalText(definition.ParentName) + ";description=" + CanonicalText(definition.Description);
    }

    internal static string MemberText(FixMemberDefinition definition)
    {
        return MemberMetadataText(definition) + ";presence=" + PresenceText(definition.Presence);
    }

    internal static string MemberMetadataText(FixMemberDefinition definition)
    {
        return "name=" + CanonicalText(definition.Name) + ";kind=" + definition.Kind.ToString() + ";reference=" + IntegerText(definition.ReferenceId) + ";position=" + IntegerText(definition.Position) + ";scenario=" + CanonicalText(definition.Scenario) + ";rule=" + CanonicalText(definition.PresenceRule) + ";rule-presence=" + definition.RulePresence.ToString();
    }

    internal static string RuleListText(List<string> rules)
    {
        StringBuilder builder = new StringBuilder();
        for (int index = 0; index < rules.Count; index++)
        {
            if (index != 0)
            {
                builder.Append(',');
            }
            builder.Append(CanonicalText(rules[index]));
        }
        return builder.ToString();
    }

    internal static string MessagePath(string msgType, string scenario)
    {
        string path = "messages/" + EscapePathSegment(msgType);
        if (scenario.Length != 0)
        {
            path += "/scenarios/" + EscapePathSegment(scenario);
        }
        return path;
    }

    internal static string MemberPathSegment(FixMemberDefinition member)
    {
        string segment;
        if (member.Kind == FixMemberKind.Field)
        {
            segment = IntegerText(member.ReferenceId);
        }
        else if (member.Kind == FixMemberKind.Component)
        {
            segment = "component-" + IntegerText(member.ReferenceId);
        }
        else if (member.Kind == FixMemberKind.Group)
        {
            segment = "group-" + IntegerText(member.ReferenceId);
        }
        else
        {
            segment = "unknown-" + IntegerText(member.ReferenceId);
        }
        if (member.Scenario.Length != 0)
        {
            segment += "/scenarios/" + EscapePathSegment(member.Scenario);
        }
        return segment;
    }

    internal static string PresenceText(FixPresence presence)
    {
        return presence.ToString().ToLowerInvariant();
    }

    internal static string OptionalIntegerText(bool hasValue, int value)
    {
        return hasValue ? IntegerText(value) : "";
    }

    internal static string IntegerText(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string BooleanText(bool value)
    {
        return value ? "true" : "false";
    }

    internal static string CanonicalText(string value)
    {
        return value.Replace("\\", "\\\\").Replace(";", "\\;").Replace("=", "\\=").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    internal static string EscapePathSegment(string value)
    {
        return value.Replace("~", "~0").Replace("/", "~1");
    }
}
