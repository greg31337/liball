using System;
using System.Collections.Generic;

namespace fix;

internal static class DictionaryNormalizer
{
    internal static DictionaryIndex NormalizeDictionary(FixDictionary dictionary, FixDictionaryOptions options)
    {
        dictionary.Options = options;
        NormalizeDefinitionNames(dictionary, options);
        NormalizeAllMemberLists(dictionary, options);
        DictionaryIndex index = CreateDictionaryIndex(dictionary, options);
        ResolveDatatypeAliases(dictionary, index, options);
        NormalizeLengthDataPairs(dictionary, index, options);
        ValidateMemberReferences(dictionary, index, options);
        ValidateComponentRecursion(dictionary, index, options);
        ValidateScenarioRecursion(dictionary, index, options);
        for (int diagnosticIndex = 0; diagnosticIndex < index.Diagnostics.Count; diagnosticIndex++)
        {
            dictionary.Diagnostics.Add(index.Diagnostics[diagnosticIndex]);
        }
        return index;
    }

    internal static DictionaryIndex CreateDictionaryIndex(FixDictionary dictionary, FixDictionaryOptions options)
    {
        DictionaryIndex index = new DictionaryIndex(options.NormalizeNames);
        IndexDatatypes(dictionary, index, options);
        IndexCodeSets(dictionary, index, options);
        IndexFields(dictionary, index, options);
        IndexComponents(dictionary, index, options);
        IndexGroups(dictionary, index, options);
        IndexMessages(dictionary, index, options);
        IndexScenarios(dictionary, index, options);
        index.DefinitionCount = CountDefinitions(dictionary);
        if (options.MaxDefinitions < 0)
        {
            AddDiagnostic(index, ValidationSeverity.Fatal, "invalid-definition-limit", "The maximum dictionary definition count is invalid.", "", false, 0);
        }
        else if (index.DefinitionCount > options.MaxDefinitions)
        {
            AddDiagnostic(index, ValidationSeverity.Fatal, "definition-limit-exceeded", "The dictionary contains more definitions than the configured limit.", dictionary.Identity.Name, false, 0);
        }
        return index;
    }

    internal static string CreateNameKey(string value, bool normalizeNames)
    {
        if (!normalizeNames)
        {
            return value;
        }
        return value.Trim().ToUpperInvariant();
    }

    internal static string CreateMessageKey(string msgType, string scenario, bool normalizeNames)
    {
        return msgType + "\u001f" + CreateNameKey(scenario, normalizeNames);
    }

    internal static string CreateNamedMessageKey(string name, string scenario, bool normalizeNames)
    {
        return CreateNameKey(name, normalizeNames) + "\u001f" + CreateNameKey(scenario, normalizeNames);
    }

    internal static string CreateCodeKey(string codeSetName, string value, bool normalizeNames)
    {
        return CreateNameKey(codeSetName, normalizeNames) + "\u001f" + value;
    }

    internal static string CreateNamedCodeKey(string codeSetName, string codeName, bool normalizeNames)
    {
        return CreateNameKey(codeSetName, normalizeNames) + "\u001f" + CreateNameKey(codeName, normalizeNames);
    }

    internal static void NormalizeDefinitionNames(FixDictionary dictionary, FixDictionaryOptions options)
    {
        if (!options.NormalizeNames)
        {
            return;
        }
        dictionary.Identity.Name = dictionary.Identity.Name.Trim();
        dictionary.Identity.Scenario = dictionary.Identity.Scenario.Trim();
        for (int index = 0; index < dictionary.Datatypes.Count; index++)
        {
            FixDatatypeDefinition definition = dictionary.Datatypes[index];
            definition.Name = definition.Name.Trim();
            definition.BaseTypeName = definition.BaseTypeName.Trim();
        }
        for (int index = 0; index < dictionary.CodeSets.Count; index++)
        {
            FixCodeSetDefinition definition = dictionary.CodeSets[index];
            definition.Name = definition.Name.Trim();
            for (int codeIndex = 0; codeIndex < definition.Codes.Count; codeIndex++)
            {
                definition.Codes[codeIndex].Name = definition.Codes[codeIndex].Name.Trim();
            }
        }
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            FixFieldDefinition definition = dictionary.Fields[index];
            definition.Name = definition.Name.Trim();
            definition.Abbreviation = definition.Abbreviation.Trim();
            definition.DatatypeName = definition.DatatypeName.Trim();
            definition.CodeSetName = definition.CodeSetName.Trim();
        }
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            dictionary.Components[index].Name = dictionary.Components[index].Name.Trim();
            dictionary.Components[index].Abbreviation = dictionary.Components[index].Abbreviation.Trim();
        }
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            dictionary.Groups[index].Name = dictionary.Groups[index].Name.Trim();
        }
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            dictionary.Messages[index].Name = dictionary.Messages[index].Name.Trim();
            dictionary.Messages[index].Scenario = dictionary.Messages[index].Scenario.Trim();
        }
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            dictionary.Scenarios[index].Name = dictionary.Scenarios[index].Name.Trim();
            dictionary.Scenarios[index].ParentName = dictionary.Scenarios[index].ParentName.Trim();
        }
    }

    internal static void NormalizeAllMemberLists(FixDictionary dictionary, FixDictionaryOptions options)
    {
        NormalizeMemberList(dictionary.Header, options);
        NormalizeMemberList(dictionary.Trailer, options);
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            NormalizeMemberList(dictionary.Components[index].Members, options);
        }
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            NormalizeMemberList(dictionary.Groups[index].Members, options);
        }
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            NormalizeMemberList(dictionary.Messages[index].Members, options);
        }
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            NormalizeMemberList(dictionary.Scenarios[index].Members, options);
        }
    }

    internal static void NormalizeMemberList(List<FixMemberDefinition> members, FixDictionaryOptions options)
    {
        for (int index = 0; index < members.Count; index++)
        {
            members[index].Position = index;
            if (options.NormalizeNames)
            {
                members[index].Name = members[index].Name.Trim();
                members[index].Scenario = members[index].Scenario.Trim();
            }
        }
    }

    internal static void IndexDatatypes(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.Datatypes.Count; definitionIndex++)
        {
            FixDatatypeDefinition definition = dictionary.Datatypes[definitionIndex];
            string key = CreateNameKey(definition.Name, options.NormalizeNames);
            if (key.Length != 0)
            {
                if (index.DatatypesByName.ContainsKey(key))
                {
                    AddDuplicateDiagnostic(index, options, "duplicate-datatype-name", definition.Name, false, 0);
                }
                else
                {
                    index.DatatypesByName.Add(key, definition);
                }
            }
        }
    }

    internal static void IndexCodeSets(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.CodeSets.Count; definitionIndex++)
        {
            FixCodeSetDefinition definition = dictionary.CodeSets[definitionIndex];
            bool indexCodes = false;
            if (definition.Id != 0)
            {
                if (index.CodeSetsById.ContainsKey(definition.Id))
                {
                    AddDuplicateDiagnostic(index, options, "duplicate-code-set-id", definition.Name, false, 0);
                }
                else
                {
                    index.CodeSetsById.Add(definition.Id, definition);
                }
            }
            string nameKey = CreateNameKey(definition.Name, options.NormalizeNames);
            if (nameKey.Length != 0)
            {
                if (index.CodeSetsByName.ContainsKey(nameKey))
                {
                    AddDuplicateDiagnostic(index, options, "duplicate-code-set-name", definition.Name, false, 0);
                }
                else
                {
                    index.CodeSetsByName.Add(nameKey, definition);
                    indexCodes = true;
                }
            }
            if (indexCodes)
            {
                for (int codeIndex = 0; codeIndex < definition.Codes.Count; codeIndex++)
                {
                    FixCodeDefinition code = definition.Codes[codeIndex];
                    string valueKey = CreateCodeKey(definition.Name, code.Value, options.NormalizeNames);
                    if (index.CodesByValue.ContainsKey(valueKey))
                    {
                        AddDuplicateDiagnostic(index, options, "duplicate-code-value", definition.Name, false, 0);
                    }
                    else
                    {
                        index.CodesByValue.Add(valueKey, code);
                    }
                    string codeNameKey = CreateNamedCodeKey(definition.Name, code.Name, options.NormalizeNames);
                    if (code.Name.Length != 0)
                    {
                        if (index.CodesByName.ContainsKey(codeNameKey))
                        {
                            AddDuplicateDiagnostic(index, options, "duplicate-code-name", code.Name, false, 0);
                        }
                        else
                        {
                            index.CodesByName.Add(codeNameKey, code);
                        }
                    }
                }
            }
        }
    }

    internal static void IndexFields(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.Fields.Count; definitionIndex++)
        {
            FixFieldDefinition definition = dictionary.Fields[definitionIndex];
            if (index.FieldsByTag.ContainsKey(definition.Tag))
            {
                AddDuplicateDiagnostic(index, options, "duplicate-field-tag", definition.Name, true, definition.Tag);
            }
            else
            {
                index.FieldsByTag.Add(definition.Tag, definition);
            }
            IndexFieldName(index, options, definition, definition.Name);
            IndexFieldName(index, options, definition, definition.Abbreviation);
        }
    }

    internal static void IndexFieldName(DictionaryIndex index, FixDictionaryOptions options, FixFieldDefinition definition, string name)
    {
        string key = CreateNameKey(name, options.NormalizeNames);
        if (key.Length == 0)
        {
            return;
        }
        if (index.FieldsByName.ContainsKey(key))
        {
            if (index.FieldsByName[key] != definition)
            {
                AddDuplicateDiagnostic(index, options, "duplicate-field-name", name, true, definition.Tag);
            }
            return;
        }
        index.FieldsByName.Add(key, definition);
    }

    internal static void IndexComponents(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.Components.Count; definitionIndex++)
        {
            FixComponentDefinition definition = dictionary.Components[definitionIndex];
            if (index.ComponentsById.ContainsKey(definition.Id))
            {
                AddDuplicateDiagnostic(index, options, "duplicate-component-id", definition.Name, false, 0);
            }
            else
            {
                index.ComponentsById.Add(definition.Id, definition);
            }
            IndexComponentName(index, options, definition, definition.Name);
            IndexComponentName(index, options, definition, definition.Abbreviation);
        }
    }

    internal static void IndexComponentName(DictionaryIndex index, FixDictionaryOptions options, FixComponentDefinition definition, string name)
    {
        string key = CreateNameKey(name, options.NormalizeNames);
        if (key.Length == 0)
        {
            return;
        }
        if (index.ComponentsByName.ContainsKey(key))
        {
            if (index.ComponentsByName[key] != definition)
            {
                AddDuplicateDiagnostic(index, options, "duplicate-component-name", name, false, 0);
            }
            return;
        }
        index.ComponentsByName.Add(key, definition);
    }

    internal static void IndexGroups(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.Groups.Count; definitionIndex++)
        {
            FixGroupDefinition definition = dictionary.Groups[definitionIndex];
            if (index.GroupsById.ContainsKey(definition.Id))
            {
                AddDuplicateDiagnostic(index, options, "duplicate-group-id", definition.Name, false, 0);
            }
            else
            {
                index.GroupsById.Add(definition.Id, definition);
            }
            if (index.GroupsByCountTag.ContainsKey(definition.CountTag))
            {
                AddDuplicateDiagnostic(index, options, "duplicate-group-count-tag", definition.Name, true, definition.CountTag);
            }
            else
            {
                index.GroupsByCountTag.Add(definition.CountTag, definition);
            }
            string nameKey = CreateNameKey(definition.Name, options.NormalizeNames);
            if (nameKey.Length != 0)
            {
                if (index.GroupsByName.ContainsKey(nameKey))
                {
                    AddDuplicateDiagnostic(index, options, "duplicate-group-name", definition.Name, false, 0);
                }
                else
                {
                    index.GroupsByName.Add(nameKey, definition);
                }
            }
        }
    }

    internal static void IndexMessages(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.Messages.Count; definitionIndex++)
        {
            FixMessageDefinition definition = dictionary.Messages[definitionIndex];
            if (definition.Id != 0)
            {
                if (index.MessagesById.ContainsKey(definition.Id))
                {
                    AddDuplicateDiagnostic(index, options, "duplicate-message-id", definition.Name, false, 0);
                }
                else
                {
                    index.MessagesById.Add(definition.Id, definition);
                }
            }
            string typeKey = CreateMessageKey(definition.MsgType, definition.Scenario, options.NormalizeNames);
            if (index.MessagesByType.ContainsKey(typeKey))
            {
                AddDuplicateDiagnostic(index, options, "duplicate-message-type", definition.Name, false, 0);
            }
            else
            {
                index.MessagesByType.Add(typeKey, definition);
            }
            string nameKey = CreateNamedMessageKey(definition.Name, definition.Scenario, options.NormalizeNames);
            if (definition.Name.Length != 0)
            {
                if (index.MessagesByName.ContainsKey(nameKey))
                {
                    AddDuplicateDiagnostic(index, options, "duplicate-message-name", definition.Name, false, 0);
                }
                else
                {
                    index.MessagesByName.Add(nameKey, definition);
                }
            }
        }
    }

    internal static void IndexScenarios(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int definitionIndex = 0; definitionIndex < dictionary.Scenarios.Count; definitionIndex++)
        {
            FixScenarioDefinition definition = dictionary.Scenarios[definitionIndex];
            string key = CreateNameKey(definition.Name, options.NormalizeNames);
            if (index.ScenariosByName.ContainsKey(key))
            {
                AddDuplicateDiagnostic(index, options, "duplicate-scenario-name", definition.Name, false, 0);
            }
            else
            {
                index.ScenariosByName.Add(key, definition);
            }
        }
    }

    internal static void ResolveDatatypeAliases(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int datatypeIndex = 0; datatypeIndex < dictionary.Datatypes.Count; datatypeIndex++)
        {
            FixDatatypeDefinition datatype = dictionary.Datatypes[datatypeIndex];
            if (datatype.FieldType == FixFieldType.Unknown && datatype.BaseTypeName.Length != 0)
            {
                FixFieldType resolved = ResolveDatatypeFieldType(datatype.BaseTypeName, index, dictionary.Datatypes.Count);
                if (resolved == FixFieldType.Unknown)
                {
                    ValidationSeverity severity = options.AllowUnknownDatatypes ? ValidationSeverity.Warning : ValidationSeverity.Error;
                    AddDiagnostic(index, severity, "unresolved-datatype-alias", "The datatype alias cannot be resolved.", datatype.Name, false, 0);
                }
                else
                {
                    datatype.FieldType = resolved;
                }
            }
        }
        for (int fieldIndex = 0; fieldIndex < dictionary.Fields.Count; fieldIndex++)
        {
            FixFieldDefinition field = dictionary.Fields[fieldIndex];
            if (field.FieldType == FixFieldType.Unknown && field.DatatypeName.Length != 0)
            {
                FixFieldType resolved = ResolveDatatypeFieldType(field.DatatypeName, index, dictionary.Datatypes.Count);
                if (resolved == FixFieldType.Unknown)
                {
                    ValidationSeverity severity = options.AllowUnknownDatatypes ? ValidationSeverity.Warning : ValidationSeverity.Error;
                    AddDiagnostic(index, severity, "unresolved-field-datatype", "The field datatype cannot be resolved.", field.Name, true, field.Tag);
                }
                else
                {
                    field.FieldType = resolved;
                }
            }
        }
    }

    internal static FixFieldType ResolveDatatypeFieldType(string name, DictionaryIndex index, int maximumDepth)
    {
        FixFieldType builtin = ResolveBuiltinFieldType(name);
        if (builtin != FixFieldType.Unknown)
        {
            return builtin;
        }
        FixDatatypeDefinition? current = index.FindDatatypeByName(name);
        int depth = 0;
        while (current != null && depth <= maximumDepth)
        {
            if (current.FieldType != FixFieldType.Unknown)
            {
                return current.FieldType;
            }
            builtin = ResolveBuiltinFieldType(current.BaseTypeName);
            if (builtin != FixFieldType.Unknown)
            {
                return builtin;
            }
            current = index.FindDatatypeByName(current.BaseTypeName);
            depth++;
        }
        return FixFieldType.Unknown;
    }

    internal static FixFieldType ResolveBuiltinFieldType(string name)
    {
        string key = name.Trim().ToUpperInvariant();
        if (key == "INT") return FixFieldType.Int;
        if (key == "SEQNUM") return FixFieldType.SeqNum;
        if (key == "NUMINGROUP") return FixFieldType.NumInGroup;
        if (key == "LENGTH") return FixFieldType.Length;
        if (key == "TAGNUM") return FixFieldType.TagNum;
        if (key == "FLOAT") return FixFieldType.Float;
        if (key == "QTY") return FixFieldType.Qty;
        if (key == "PRICE") return FixFieldType.Price;
        if (key == "PRICEOFFSET") return FixFieldType.PriceOffset;
        if (key == "AMT") return FixFieldType.Amt;
        if (key == "PERCENTAGE") return FixFieldType.Percentage;
        if (key == "CHAR") return FixFieldType.Char;
        if (key == "BOOLEAN") return FixFieldType.Boolean;
        if (key == "STRING") return FixFieldType.String;
        if (key == "MULTIPLECHARVALUE") return FixFieldType.MultipleCharValue;
        if (key == "MULTIPLESTRINGVALUE") return FixFieldType.MultipleStringValue;
        if (key == "CURRENCY") return FixFieldType.Currency;
        if (key == "COUNTRY") return FixFieldType.Country;
        if (key == "EXCHANGE") return FixFieldType.Exchange;
        if (key == "MONTHYEAR") return FixFieldType.MonthYear;
        if (key == "DAYOFMONTH") return FixFieldType.DayOfMonth;
        if (key == "LOCALMKTDATE") return FixFieldType.LocalMktDate;
        if (key == "UTCDATEONLY") return FixFieldType.UtcDateOnly;
        if (key == "UTCTIMEONLY") return FixFieldType.UtcTimeOnly;
        if (key == "UTCTIMESTAMP") return FixFieldType.UtcTimestamp;
        if (key == "TZTIMEONLY") return FixFieldType.TzTimeOnly;
        if (key == "TZTIMESTAMP") return FixFieldType.TzTimestamp;
        if (key == "DATA") return FixFieldType.Data;
        if (key == "XMLDATA") return FixFieldType.XmlData;
        return FixFieldType.Unknown;
    }

    internal static void NormalizeLengthDataPairs(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        index.LengthFieldsByDataTag.Clear();
        index.DataFieldsByLengthTag.Clear();
        for (int fieldIndex = 0; fieldIndex < dictionary.Fields.Count; fieldIndex++)
        {
            FixFieldDefinition field = dictionary.Fields[fieldIndex];
            if (field.HasDataTag)
            {
                FixFieldDefinition? data = index.FindFieldByTag(field.DataTag);
                if (data == null || field.FieldType != FixFieldType.Length || data.FieldType != FixFieldType.Data && data.FieldType != FixFieldType.XmlData || data.HasLengthTag && data.LengthTag != field.Tag)
                {
                    AddDiagnostic(index, ValidationSeverity.Error, "invalid-length-data-pair", "The length field does not identify a compatible data field.", field.Name, true, field.Tag);
                }
                else
                {
                    data.HasLengthTag = true;
                    data.LengthTag = field.Tag;
                    IndexLengthDataPair(index, field, data, options);
                }
            }
        }
        for (int fieldIndex = 0; fieldIndex < dictionary.Fields.Count; fieldIndex++)
        {
            FixFieldDefinition data = dictionary.Fields[fieldIndex];
            if (data.HasLengthTag)
            {
                FixFieldDefinition? length = index.FindFieldByTag(data.LengthTag);
                if (length == null || length.FieldType != FixFieldType.Length || data.FieldType != FixFieldType.Data && data.FieldType != FixFieldType.XmlData || length.HasDataTag && length.DataTag != data.Tag)
                {
                    AddDiagnostic(index, ValidationSeverity.Error, "invalid-length-data-pair", "The data field does not identify a compatible length field.", data.Name, true, data.Tag);
                }
                else
                {
                    length.HasDataTag = true;
                    length.DataTag = data.Tag;
                    IndexLengthDataPair(index, length, data, options);
                }
            }
        }
    }

    internal static void IndexLengthDataPair(DictionaryIndex index, FixFieldDefinition length, FixFieldDefinition data, FixDictionaryOptions options)
    {
        if (index.LengthFieldsByDataTag.ContainsKey(data.Tag) && index.LengthFieldsByDataTag[data.Tag] != length || index.DataFieldsByLengthTag.ContainsKey(length.Tag) && index.DataFieldsByLengthTag[length.Tag] != data)
        {
            AddDuplicateDiagnostic(index, options, "duplicate-length-data-pair", length.Name, true, length.Tag);
            return;
        }
        if (!index.LengthFieldsByDataTag.ContainsKey(data.Tag))
        {
            index.LengthFieldsByDataTag.Add(data.Tag, length);
        }
        if (!index.DataFieldsByLengthTag.ContainsKey(length.Tag))
        {
            index.DataFieldsByLengthTag.Add(length.Tag, data);
        }
    }

    internal static void ValidateMemberReferences(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        ValidateMemberList(dictionary.Header, "StandardHeader", index, options);
        ValidateMemberList(dictionary.Trailer, "StandardTrailer", index, options);
        for (int definitionIndex = 0; definitionIndex < dictionary.Components.Count; definitionIndex++)
        {
            ValidateMemberList(dictionary.Components[definitionIndex].Members, dictionary.Components[definitionIndex].Name, index, options);
        }
        for (int definitionIndex = 0; definitionIndex < dictionary.Groups.Count; definitionIndex++)
        {
            FixGroupDefinition group = dictionary.Groups[definitionIndex];
            ValidateMemberList(group.Members, group.Name, index, options);
            if (index.FindFieldByTag(group.CountTag) == null)
            {
                AddReferenceDiagnostic(index, options, "unresolved-group-count-field", group.Name, true, group.CountTag);
            }
            if (group.DelimiterTag != 0 && index.FindFieldByTag(group.DelimiterTag) == null)
            {
                AddReferenceDiagnostic(index, options, "unresolved-group-delimiter-field", group.Name, true, group.DelimiterTag);
            }
        }
        for (int definitionIndex = 0; definitionIndex < dictionary.Messages.Count; definitionIndex++)
        {
            ValidateMemberList(dictionary.Messages[definitionIndex].Members, dictionary.Messages[definitionIndex].Name, index, options);
        }
        for (int definitionIndex = 0; definitionIndex < dictionary.Scenarios.Count; definitionIndex++)
        {
            FixScenarioDefinition scenario = dictionary.Scenarios[definitionIndex];
            ValidateMemberList(scenario.Members, scenario.Name, index, options);
            if (scenario.ParentName.Length != 0 && index.FindScenarioByName(scenario.ParentName) == null)
            {
                AddReferenceDiagnostic(index, options, "unresolved-parent-scenario", scenario.Name, false, 0);
            }
        }
    }

    internal static void ValidateMemberList(List<FixMemberDefinition> members, string owner, DictionaryIndex index, FixDictionaryOptions options)
    {
        for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
        {
            FixMemberDefinition member = members[memberIndex];
            bool resolved = false;
            if (member.Kind == FixMemberKind.Field)
            {
                resolved = index.FindFieldByTag(member.ReferenceId) != null;
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                resolved = index.FindComponentById(member.ReferenceId) != null;
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                resolved = index.FindGroupById(member.ReferenceId) != null;
            }
            if (!resolved)
            {
                AddReferenceDiagnostic(index, options, "unresolved-member-reference", owner, member.Kind == FixMemberKind.Field, member.ReferenceId);
            }
        }
    }

    internal static void ValidateComponentRecursion(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        List<int> reported = [];
        for (int componentIndex = 0; componentIndex < dictionary.Components.Count; componentIndex++)
        {
            List<int> path = [];
            VisitComponent(dictionary.Components[componentIndex], index, options, path, reported);
        }
    }

    internal static void VisitComponent(FixComponentDefinition component, DictionaryIndex index, FixDictionaryOptions options, List<int> path, List<int> reported)
    {
        if (path.Contains(component.Id))
        {
            if (!reported.Contains(component.Id))
            {
                AddReferenceDiagnostic(index, options, "recursive-component-reference", component.Name, false, 0);
                for (int pathIndex = 0; pathIndex < path.Count; pathIndex++)
                {
                    if (!reported.Contains(path[pathIndex]))
                    {
                        reported.Add(path[pathIndex]);
                    }
                }
                reported.Add(component.Id);
            }
            return;
        }
        path.Add(component.Id);
        for (int memberIndex = 0; memberIndex < component.Members.Count; memberIndex++)
        {
            FixMemberDefinition member = component.Members[memberIndex];
            if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? nested = index.FindComponentById(member.ReferenceId);
                if (nested != null)
                {
                    VisitComponent(nested, index, options, path, reported);
                }
            }
        }
        path.RemoveAt(path.Count - 1);
    }

    internal static void ValidateScenarioRecursion(FixDictionary dictionary, DictionaryIndex index, FixDictionaryOptions options)
    {
        List<string> reported = [];
        for (int scenarioIndex = 0; scenarioIndex < dictionary.Scenarios.Count; scenarioIndex++)
        {
            List<string> path = [];
            VisitScenario(dictionary.Scenarios[scenarioIndex], index, options, path, reported);
        }
    }

    internal static void VisitScenario(FixScenarioDefinition scenario, DictionaryIndex index, FixDictionaryOptions options, List<string> path, List<string> reported)
    {
        string key = CreateNameKey(scenario.Name, options.NormalizeNames);
        if (path.Contains(key))
        {
            if (!reported.Contains(key))
            {
                AddReferenceDiagnostic(index, options, "recursive-scenario-reference", scenario.Name, false, 0);
                for (int pathIndex = 0; pathIndex < path.Count; pathIndex++)
                {
                    if (!reported.Contains(path[pathIndex]))
                    {
                        reported.Add(path[pathIndex]);
                    }
                }
                reported.Add(key);
            }
            return;
        }
        path.Add(key);
        if (scenario.ParentName.Length != 0)
        {
            FixScenarioDefinition? parent = index.FindScenarioByName(scenario.ParentName);
            if (parent != null)
            {
                VisitScenario(parent, index, options, path, reported);
            }
        }
        path.RemoveAt(path.Count - 1);
    }

    internal static int CountDefinitions(FixDictionary dictionary)
    {
        long count = dictionary.Datatypes.Count + dictionary.CodeSets.Count + dictionary.Fields.Count + dictionary.Components.Count + dictionary.Groups.Count + dictionary.Messages.Count + dictionary.Scenarios.Count;
        for (int codeSetIndex = 0; codeSetIndex < dictionary.CodeSets.Count; codeSetIndex++)
        {
            count += dictionary.CodeSets[codeSetIndex].Codes.Count;
        }
        return count > int.MaxValue ? int.MaxValue : (int)count;
    }

    internal static void AddDuplicateDiagnostic(DictionaryIndex index, FixDictionaryOptions options, string code, string definitionName, bool hasTag, int tag)
    {
        ValidationSeverity severity = options.AllowDuplicateDefinitions ? ValidationSeverity.Warning : ValidationSeverity.Error;
        AddDiagnostic(index, severity, code, "A duplicate dictionary definition was ignored; the first definition remains indexed.", definitionName, hasTag, tag);
    }

    internal static void AddReferenceDiagnostic(DictionaryIndex index, FixDictionaryOptions options, string code, string definitionName, bool hasTag, int tag)
    {
        ValidationSeverity severity = options.RequireResolvedReferences ? ValidationSeverity.Error : ValidationSeverity.Warning;
        AddDiagnostic(index, severity, code, "A dictionary reference cannot be resolved.", definitionName, hasTag, tag);
    }

    internal static void AddDiagnostic(DictionaryIndex index, ValidationSeverity severity, string code, string message, string definitionName, bool hasTag, int tag)
    {
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.DefinitionName = definitionName;
        diagnostic.HasTag = hasTag;
        diagnostic.Tag = tag;
        index.Diagnostics.Add(diagnostic);
        if (severity == ValidationSeverity.Error || severity == ValidationSeverity.Fatal)
        {
            index.Success = false;
        }
    }
}
