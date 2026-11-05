using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace fix;

internal static class OrchestraDifference
{
    internal static OrchestraDiff CompareRepositories(OrchestraRepository beforeRepository, OrchestraRepository afterRepository)
    {
        OrchestraDiff result = new OrchestraDiff();
        if (beforeRepository != null) result.BeforeMetadata = CloneMetadata(beforeRepository.Metadata);
        if (afterRepository != null) result.AfterMetadata = CloneMetadata(afterRepository.Metadata);
        if (beforeRepository == null) AddDiagnostic(result.Diagnostics, "missing-before-orchestra-repository", "The earlier Orchestra repository is missing.", "/before");
        if (afterRepository == null) AddDiagnostic(result.Diagnostics, "missing-after-orchestra-repository", "The later Orchestra repository is missing.", "/after");
        if (beforeRepository == null || afterRepository == null) return result;

        List<OrchestraDiffItem> before = CreateSnapshot(beforeRepository);
        List<OrchestraDiffItem> after = CreateSnapshot(afterRepository);
        before.Sort((left, right) => CompareItems(left, right));
        after.Sort((left, right) => CompareItems(left, right));
        MergeSnapshots(before, after, result.Changes);
        return result;
    }

    internal static List<OrchestraDiffItem> CreateSnapshot(OrchestraRepository repository)
    {
        List<OrchestraDiffItem> items = [];
        AddSnapshotItem(items, "/version", IntegerText((int)repository.Version), true);
        AddSnapshotItem(items, "/metadata", CanonicalObjectText(repository.Metadata), false);
        AddAnnotationItems(items, repository.Annotations, "/annotations");
        AddCategoryItems(items, repository.Categories);
        AddSectionItems(items, repository.Sections);
        AddConceptItems(items, repository.Concepts);
        AddEncodingStandardItems(items, repository.EncodingStandards);
        AddDatatypeItems(items, repository.Datatypes);
        AddCodeSetItems(items, repository.CodeSets);
        AddEncodingItems(items, repository.CodeSetEncodings, "/encodings/code-sets");
        AddFieldItems(items, repository.Fields);
        AddComponentItems(items, repository.Components);
        AddEncodingItems(items, repository.ComponentEncodings, "/encodings/components");
        AddGroupItems(items, repository.Groups);
        AddEncodingItems(items, repository.GroupEncodings, "/encodings/groups");
        AddMessageItems(items, repository.Messages);
        AddEncodingItems(items, repository.MessageEncodings, "/encodings/messages");
        AddScenarioItems(items, repository.Scenarios);
        AddExtensionItems(items, repository.Extensions, "/extensions");
        AddVariableItems(items, repository.Variables);
        AddTimerItems(items, repository.Timers);
        AddRuleItems(items, repository.Rules);
        AddActorItems(items, repository.Actors);
        AddFlowItems(items, repository.Flows);
        AddStateMachineItems(items, repository.StateMachines);
        AddInterfaceItems(items, repository.Interfaces);
        AddRepositoryLinkItems(items, repository.RepositoryLinks);
        return items;
    }

    internal static void AddAnnotationItems(List<OrchestraDiffItem> items, List<OrchestraAnnotation> source, string ownerPath)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraAnnotation item = source[index];
            string key = item.Kind + "-" + item.Purpose + "-" + item.Language;
            AddSnapshotItem(items, IndexedPath(ownerPath, key, index), CanonicalObjectText(item), false);
        }
    }

    internal static void AddCategoryItems(List<OrchestraDiffItem> items, List<OrchestraCategory> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/categories", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddSectionItems(List<OrchestraDiffItem> items, List<OrchestraSection> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/sections", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddConceptItems(List<OrchestraDiffItem> items, List<OrchestraConcept> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/concepts", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddEncodingStandardItems(List<OrchestraDiffItem> items, List<OrchestraEncodingStandard> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/encoding-standards", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddDatatypeItems(List<OrchestraDiffItem> items, List<OrchestraDatatypeDefinition> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/datatypes", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddCodeSetItems(List<OrchestraDiffItem> items, List<OrchestraCodeSetDefinition> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraCodeSetDefinition item = source[index];
            AddSnapshotItem(items, DefinitionPath("/code-sets", item.Id, item.Name, item.Scenario, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddEncodingItems(List<OrchestraDiffItem> items, List<OrchestraEncoding> source, string ownerPath)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath(ownerPath, source[index].Standard, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddFieldItems(List<OrchestraDiffItem> items, List<OrchestraFieldDefinition> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraFieldDefinition item = source[index];
            AddSnapshotItem(items, DefinitionPath("/fields", item.Id, item.Name, item.Scenario, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddComponentItems(List<OrchestraDiffItem> items, List<OrchestraComponentDefinition> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraComponentDefinition item = source[index];
            AddSnapshotItem(items, DefinitionPath("/components", item.Id, item.Name, item.Scenario, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddGroupItems(List<OrchestraDiffItem> items, List<OrchestraGroupDefinition> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraGroupDefinition item = source[index];
            AddSnapshotItem(items, DefinitionPath("/groups", item.Id, item.Name, item.Scenario, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddMessageItems(List<OrchestraDiffItem> items, List<OrchestraMessageDefinition> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraMessageDefinition item = source[index];
            string key = item.Name.Length == 0 ? item.MessageType : item.Name;
            AddSnapshotItem(items, ScenarioPath("/messages", key, item.Scenario, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddScenarioItems(List<OrchestraDiffItem> items, List<OrchestraScenarioDefinition> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/scenarios", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddExtensionItems(List<OrchestraDiffItem> items, List<OrchestraExtension> source, string ownerPath)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraExtension item = source[index];
            AddSnapshotItem(items, IndexedPath(ownerPath, item.NamespaceUri + "-" + item.Name, index), CanonicalObjectText(item), false);
        }
    }

    internal static void AddVariableItems(List<OrchestraDiffItem> items, List<OrchestraVariable> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/variables", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddTimerItems(List<OrchestraDiffItem> items, List<OrchestraTimer> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraTimer item = source[index];
            AddSnapshotItem(items, IndexedPath("/timers", item.Actor + "-" + item.Name, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddRuleItems(List<OrchestraDiffItem> items, List<OrchestraRule> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraRule item = source[index];
            string key = item.Id == 0 ? item.Name : IntegerText(item.Id);
            AddSnapshotItem(items, IndexedPath("/rules", key, index), CanonicalObjectText(item), true);
        }
    }

    internal static void AddActorItems(List<OrchestraDiffItem> items, List<OrchestraActor> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/actors", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddFlowItems(List<OrchestraDiffItem> items, List<OrchestraFlow> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/flows", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddStateMachineItems(List<OrchestraDiffItem> items, List<OrchestraStateMachine> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraStateMachine item = source[index];
            string path = "/state-machines/" + EscapePathSegment(item.Actor) + "/" + KeySegment(item.Name, index);
            AddSnapshotItem(items, path, CanonicalObjectText(item), true);
        }
    }

    internal static void AddInterfaceItems(List<OrchestraDiffItem> items, List<OrchestraInterface> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/interfaces", source[index].Name, index), CanonicalObjectText(source[index]), true);
    }

    internal static void AddRepositoryLinkItems(List<OrchestraDiffItem> items, List<string> source)
    {
        for (int index = 0; index < source.Count; index++) AddSnapshotItem(items, IndexedPath("/repository-links", source[index], index), CanonicalText(source[index]), true);
    }

    internal static void MergeSnapshots(List<OrchestraDiffItem> before, List<OrchestraDiffItem> after, List<OrchestraChange> changes)
    {
        int beforeIndex = 0;
        int afterIndex = 0;
        while (beforeIndex < before.Count || afterIndex < after.Count)
        {
            if (beforeIndex >= before.Count)
            {
                AddChange(changes, "add", after[afterIndex].Path, "", after[afterIndex].Value, false);
                afterIndex++;
            }
            else if (afterIndex >= after.Count)
            {
                AddChange(changes, "remove", before[beforeIndex].Path, before[beforeIndex].Value, "", before[beforeIndex].Breaking);
                beforeIndex++;
            }
            else
            {
                int comparison = string.CompareOrdinal(before[beforeIndex].Path, after[afterIndex].Path);
                if (comparison < 0)
                {
                    AddChange(changes, "remove", before[beforeIndex].Path, before[beforeIndex].Value, "", before[beforeIndex].Breaking);
                    beforeIndex++;
                }
                else if (comparison > 0)
                {
                    AddChange(changes, "add", after[afterIndex].Path, "", after[afterIndex].Value, false);
                    afterIndex++;
                }
                else
                {
                    if (before[beforeIndex].Value != after[afterIndex].Value)
                    {
                        bool breaking = before[beforeIndex].Breaking || after[afterIndex].Breaking;
                        AddChange(changes, "replace", before[beforeIndex].Path, before[beforeIndex].Value, after[afterIndex].Value, breaking);
                    }
                    beforeIndex++;
                    afterIndex++;
                }
            }
        }
    }

    internal static void AddSnapshotItem(List<OrchestraDiffItem> items, string path, string value, bool breaking)
    {
        OrchestraDiffItem item = new OrchestraDiffItem();
        item.Path = path;
        item.Value = value;
        item.Breaking = breaking;
        items.Add(item);
    }

    internal static void AddChange(List<OrchestraChange> changes, string kind, string path, string before, string after, bool breaking)
    {
        OrchestraChange change = new OrchestraChange();
        change.Kind = kind;
        change.Path = path;
        change.Before = before;
        change.After = after;
        change.IsBreaking = breaking;
        changes.Add(change);
    }

    internal static int CompareItems(OrchestraDiffItem left, OrchestraDiffItem right)
    {
        int comparison = string.CompareOrdinal(left.Path, right.Path);
        if (comparison != 0) return comparison;
        return string.CompareOrdinal(left.Value, right.Value);
    }

    internal static string DefinitionPath(string ownerPath, string id, string name, string scenario, int index)
    {
        string key = id.Length == 0 ? name : id;
        return ScenarioPath(ownerPath, key, scenario, index);
    }

    internal static string ScenarioPath(string ownerPath, string key, string scenario, int index)
    {
        string path = IndexedPath(ownerPath, key, index);
        string selected = scenario.Length == 0 ? "base" : scenario;
        return path + "/scenarios/" + EscapePathSegment(selected);
    }

    internal static string IndexedPath(string ownerPath, string key, int index)
    {
        return ownerPath + "/" + KeySegment(key, index);
    }

    internal static string KeySegment(string key, int index)
    {
        if (key.Length != 0) return EscapePathSegment(key);
        return "#" + IntegerText(index);
    }

    internal static string EscapePathSegment(string value)
    {
        return value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }

    internal static string CanonicalObjectText(object value)
    {
        StringBuilder builder = new StringBuilder();
        AppendCanonicalValue(builder, value, 0);
        return builder.ToString();
    }

    internal static void AppendCanonicalValue(StringBuilder builder, object? value, int depth)
    {
        if (value == null)
        {
            builder.Append("null");
            return;
        }
        if (depth > 64)
        {
            builder.Append("depth-limit");
            return;
        }

        Type type = value.GetType();
        string typeName = type.FullName == null ? type.Name : type.FullName;
        if (typeName == "System.String")
        {
            AppendLengthText(builder, (string)value);
            return;
        }
        if (typeName == "System.Boolean")
        {
            builder.Append((bool)value ? "true" : "false");
            return;
        }
        if (typeName == "System.Int32")
        {
            builder.Append(((int)value).ToString(CultureInfo.InvariantCulture));
            return;
        }
        if (type.IsEnum)
        {
            builder.Append(Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
            return;
        }

        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            AppendDictionaryValue(builder, dictionary, depth + 1);
            return;
        }
        IList? list = value as IList;
        if (list != null)
        {
            AppendListValue(builder, list, depth + 1);
            return;
        }

        builder.Append('{');
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        Array.Sort(fields, (left, right) => string.CompareOrdinal(left.Name, right.Name));
        for (int index = 0; index < fields.Length; index++)
        {
            if (index != 0) builder.Append(';');
            builder.Append(fields[index].Name);
            builder.Append('=');
            AppendCanonicalValue(builder, fields[index].GetValue(value), depth + 1);
        }
        builder.Append('}');
    }

    internal static void AppendDictionaryValue(StringBuilder builder, IDictionary dictionary, int depth)
    {
        List<string> entries = [];
        foreach (DictionaryEntry entry in dictionary)
        {
            StringBuilder item = new StringBuilder();
            AppendCanonicalValue(item, entry.Key, depth);
            item.Append('=');
            AppendCanonicalValue(item, entry.Value, depth);
            entries.Add(item.ToString());
        }
        entries.Sort((left, right) => string.CompareOrdinal(left, right));
        builder.Append('{');
        for (int index = 0; index < entries.Count; index++)
        {
            if (index != 0) builder.Append(';');
            builder.Append(entries[index]);
        }
        builder.Append('}');
    }

    internal static void AppendListValue(StringBuilder builder, IList list, int depth)
    {
        builder.Append('[');
        for (int index = 0; index < list.Count; index++)
        {
            if (index != 0) builder.Append(';');
            AppendCanonicalValue(builder, list[index], depth);
        }
        builder.Append(']');
    }

    internal static void AppendLengthText(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
    }

    internal static string CanonicalText(string value)
    {
        StringBuilder builder = new StringBuilder();
        AppendLengthText(builder, value);
        return builder.ToString();
    }

    internal static string IntegerText(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static OrchestraMetadata CloneMetadata(OrchestraMetadata source)
    {
        OrchestraMetadata target = new OrchestraMetadata();
        target.Name = source.Name;
        target.FixVersion = source.FixVersion;
        target.Release = source.Release;
        target.HasExtensionPack = source.HasExtensionPack;
        target.ExtensionPack = source.ExtensionPack;
        target.PublicationDate = source.PublicationDate;
        target.NamespaceUri = source.NamespaceUri;
        target.RepositoryNamespace = source.RepositoryNamespace;
        target.SpecificationUri = source.SpecificationUri;
        for (int index = 0; index < source.Terms.Count; index++)
        {
            OrchestraMetadataTerm sourceTerm = source.Terms[index];
            OrchestraMetadataTerm targetTerm = new OrchestraMetadataTerm();
            targetTerm.NamespaceUri = sourceTerm.NamespaceUri;
            targetTerm.Name = sourceTerm.Name;
            targetTerm.Language = sourceTerm.Language;
            targetTerm.Value = sourceTerm.Value;
            targetTerm.Xml = sourceTerm.Xml;
            target.Terms.Add(targetTerm);
        }
        return target;
    }

    internal static void AddDiagnostic(List<OrchestraDiagnostic> diagnostics, string code, string message, string path)
    {
        OrchestraDiagnostic diagnostic = new OrchestraDiagnostic();
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ElementPath = path;
        diagnostics.Add(diagnostic);
    }
}
