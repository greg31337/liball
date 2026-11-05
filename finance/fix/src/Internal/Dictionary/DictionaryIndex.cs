using System;
using System.Collections.Generic;

namespace fix;

internal class DictionaryIndex
{
    internal bool NormalizeNames;
    internal bool Success = true;
    internal int DefinitionCount;
    internal Dictionary<string, FixDatatypeDefinition> DatatypesByName = new Dictionary<string, FixDatatypeDefinition>(StringComparer.Ordinal);
    internal Dictionary<int, FixCodeSetDefinition> CodeSetsById = new Dictionary<int, FixCodeSetDefinition>();
    internal Dictionary<string, FixCodeSetDefinition> CodeSetsByName = new Dictionary<string, FixCodeSetDefinition>(StringComparer.Ordinal);
    internal Dictionary<string, FixCodeDefinition> CodesByValue = new Dictionary<string, FixCodeDefinition>(StringComparer.Ordinal);
    internal Dictionary<string, FixCodeDefinition> CodesByName = new Dictionary<string, FixCodeDefinition>(StringComparer.Ordinal);
    internal Dictionary<int, FixFieldDefinition> FieldsByTag = new Dictionary<int, FixFieldDefinition>();
    internal Dictionary<string, FixFieldDefinition> FieldsByName = new Dictionary<string, FixFieldDefinition>(StringComparer.Ordinal);
    internal Dictionary<int, FixComponentDefinition> ComponentsById = new Dictionary<int, FixComponentDefinition>();
    internal Dictionary<string, FixComponentDefinition> ComponentsByName = new Dictionary<string, FixComponentDefinition>(StringComparer.Ordinal);
    internal Dictionary<int, FixGroupDefinition> GroupsById = new Dictionary<int, FixGroupDefinition>();
    internal Dictionary<int, FixGroupDefinition> GroupsByCountTag = new Dictionary<int, FixGroupDefinition>();
    internal Dictionary<string, FixGroupDefinition> GroupsByName = new Dictionary<string, FixGroupDefinition>(StringComparer.Ordinal);
    internal Dictionary<int, FixMessageDefinition> MessagesById = new Dictionary<int, FixMessageDefinition>();
    internal Dictionary<string, FixMessageDefinition> MessagesByType = new Dictionary<string, FixMessageDefinition>(StringComparer.Ordinal);
    internal Dictionary<string, FixMessageDefinition> MessagesByName = new Dictionary<string, FixMessageDefinition>(StringComparer.Ordinal);
    internal Dictionary<string, FixScenarioDefinition> ScenariosByName = new Dictionary<string, FixScenarioDefinition>(StringComparer.Ordinal);
    internal Dictionary<int, FixFieldDefinition> LengthFieldsByDataTag = new Dictionary<int, FixFieldDefinition>();
    internal Dictionary<int, FixFieldDefinition> DataFieldsByLengthTag = new Dictionary<int, FixFieldDefinition>();
    internal List<DictionaryDiagnostic> Diagnostics = [];

    internal DictionaryIndex(bool normalizeNames)
    {
        NormalizeNames = normalizeNames;
    }

    internal FixDatatypeDefinition? FindDatatypeByName(string name)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, NormalizeNames);
        return DatatypesByName.ContainsKey(key) ? DatatypesByName[key] : null;
    }

    internal FixCodeSetDefinition? FindCodeSetById(int id)
    {
        return CodeSetsById.ContainsKey(id) ? CodeSetsById[id] : null;
    }

    internal FixCodeSetDefinition? FindCodeSetByName(string name)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, NormalizeNames);
        return CodeSetsByName.ContainsKey(key) ? CodeSetsByName[key] : null;
    }

    internal FixCodeDefinition? FindCodeByValue(string codeSetName, string value)
    {
        string key = DictionaryNormalizer.CreateCodeKey(codeSetName, value, NormalizeNames);
        return CodesByValue.ContainsKey(key) ? CodesByValue[key] : null;
    }

    internal FixCodeDefinition? FindCodeByName(string codeSetName, string codeName)
    {
        string key = DictionaryNormalizer.CreateNamedCodeKey(codeSetName, codeName, NormalizeNames);
        return CodesByName.ContainsKey(key) ? CodesByName[key] : null;
    }

    internal FixFieldDefinition? FindFieldByTag(int tag)
    {
        return FieldsByTag.ContainsKey(tag) ? FieldsByTag[tag] : null;
    }

    internal FixFieldDefinition? FindFieldByName(string name)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, NormalizeNames);
        return FieldsByName.ContainsKey(key) ? FieldsByName[key] : null;
    }

    internal FixComponentDefinition? FindComponentById(int id)
    {
        return ComponentsById.ContainsKey(id) ? ComponentsById[id] : null;
    }

    internal FixComponentDefinition? FindComponentByName(string name)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, NormalizeNames);
        return ComponentsByName.ContainsKey(key) ? ComponentsByName[key] : null;
    }

    internal FixGroupDefinition? FindGroupById(int id)
    {
        return GroupsById.ContainsKey(id) ? GroupsById[id] : null;
    }

    internal FixGroupDefinition? FindGroupByCountTag(int countTag)
    {
        return GroupsByCountTag.ContainsKey(countTag) ? GroupsByCountTag[countTag] : null;
    }

    internal FixGroupDefinition? FindGroupByName(string name)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, NormalizeNames);
        return GroupsByName.ContainsKey(key) ? GroupsByName[key] : null;
    }

    internal FixMessageDefinition? FindMessageById(int id)
    {
        return MessagesById.ContainsKey(id) ? MessagesById[id] : null;
    }

    internal FixMessageDefinition? FindMessageByType(string msgType, string scenario)
    {
        string key = DictionaryNormalizer.CreateMessageKey(msgType, scenario, NormalizeNames);
        return MessagesByType.ContainsKey(key) ? MessagesByType[key] : null;
    }

    internal FixMessageDefinition? FindMessageByName(string name, string scenario)
    {
        string key = DictionaryNormalizer.CreateNamedMessageKey(name, scenario, NormalizeNames);
        return MessagesByName.ContainsKey(key) ? MessagesByName[key] : null;
    }

    internal FixScenarioDefinition? FindScenarioByName(string name)
    {
        string key = DictionaryNormalizer.CreateNameKey(name, NormalizeNames);
        return ScenariosByName.ContainsKey(key) ? ScenariosByName[key] : null;
    }

    internal FixFieldDefinition? FindLengthFieldByDataTag(int dataTag)
    {
        return LengthFieldsByDataTag.ContainsKey(dataTag) ? LengthFieldsByDataTag[dataTag] : null;
    }

    internal FixFieldDefinition? FindDataFieldByLengthTag(int lengthTag)
    {
        return DataFieldsByLengthTag.ContainsKey(lengthTag) ? DataFieldsByLengthTag[lengthTag] : null;
    }
}
