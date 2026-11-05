using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class SchemaRuleDeltaBuilder
{
    internal static SchemaVersionDelta CompareVersionRuleSets(
        SchemaRuleSet[] previousSets,
        SchemaRuleSet[] currentSets,
        OdfVersion previousVersion,
        OdfVersion currentVersion)
    {
        if (previousSets == null || currentSets == null)
        {
            throw new OdfValidationException(
                "Previous and current schema rule sets are required.");
        }
        List<SchemaRuleDelta> changes = new List<SchemaRuleDelta>();
        foreach (SchemaRuleSet current in currentSets)
        {
            SchemaRuleSet? previous = FindProfile(previousSets, current.Profile);
            if (previous != null)
            {
                AddDefinitionDeltas(changes, previous, current);
                AddElementDeltas(changes, previous, current);
                AddAttributeDeltas(changes, previous, current);
                AddDatatypeDeltas(changes, previous, current);
            }
        }
        SchemaVersionDelta result = new SchemaVersionDelta();
        result.PreviousVersion = previousVersion;
        result.CurrentVersion = currentVersion;
        result.Changes = changes.ToArray();
        return result;
    }

    internal static SchemaRuleSet? FindProfile(
        SchemaRuleSet[] sets,
        string profile)
    {
        foreach (SchemaRuleSet set in sets)
        {
            if (set.Profile == profile)
            {
                return set;
            }
        }
        return null;
    }

    internal static void AddDefinitionDeltas(
        List<SchemaRuleDelta> changes,
        SchemaRuleSet previous,
        SchemaRuleSet current)
    {
        string[] previousNames = UniqueDefinitionNames(previous);
        string[] currentNames = UniqueDefinitionNames(current);
        foreach (string name in currentNames)
        {
            int[] oldIndexes = DefinitionIndexes(previous, name);
            int[] newIndexes = DefinitionIndexes(current, name);
            if (oldIndexes.Length == 0)
            {
                changes.Add(CreateDelta(
                    current.Profile,
                    SchemaRuleDeltaKind.Added,
                    SchemaRuleDeltaCategory.Definition,
                    "",
                    name,
                    oldIndexes,
                    newIndexes));
            }
            else if (DefinitionSignature(previous, oldIndexes)
                != DefinitionSignature(current, newIndexes))
            {
                changes.Add(CreateDelta(
                    current.Profile,
                    SchemaRuleDeltaKind.Changed,
                    SchemaRuleDeltaCategory.Definition,
                    "",
                    name,
                    oldIndexes,
                    newIndexes));
            }
        }
        foreach (string name in previousNames)
        {
            int[] newIndexes = DefinitionIndexes(current, name);
            if (newIndexes.Length == 0)
            {
                changes.Add(CreateDelta(
                    current.Profile,
                    SchemaRuleDeltaKind.Removed,
                    SchemaRuleDeltaCategory.Definition,
                    "",
                    name,
                    DefinitionIndexes(previous, name),
                    newIndexes));
            }
        }
    }

    internal static void AddElementDeltas(
        List<SchemaRuleDelta> changes,
        SchemaRuleSet previous,
        SchemaRuleSet current)
    {
        AddNameDeltas(
            changes,
            previous,
            current,
            SchemaRuleDeltaCategory.Element,
            ElementNames(previous),
            ElementNames(current));
    }

    internal static void AddAttributeDeltas(
        List<SchemaRuleDelta> changes,
        SchemaRuleSet previous,
        SchemaRuleSet current)
    {
        AddNameDeltas(
            changes,
            previous,
            current,
            SchemaRuleDeltaCategory.Attribute,
            AttributeNames(previous),
            AttributeNames(current));
    }

    internal static void AddDatatypeDeltas(
        List<SchemaRuleDelta> changes,
        SchemaRuleSet previous,
        SchemaRuleSet current)
    {
        AddNameDeltas(
            changes,
            previous,
            current,
            SchemaRuleDeltaCategory.Datatype,
            DatatypeNames(previous),
            DatatypeNames(current));
    }

    internal static void AddNameDeltas(
        List<SchemaRuleDelta> changes,
        SchemaRuleSet previous,
        SchemaRuleSet current,
        SchemaRuleDeltaCategory category,
        string[] previousNames,
        string[] currentNames)
    {
        foreach (string key in currentNames)
        {
            if (!Contains(previousNames, key))
            {
                string[] parts = SplitNameKey(key);
                changes.Add(CreateDelta(
                    current.Profile,
                    SchemaRuleDeltaKind.Added,
                    category,
                    parts[0],
                    parts[1],
                    NameIndexes(previous, category, parts[0], parts[1]),
                    NameIndexes(current, category, parts[0], parts[1])));
            }
        }
        foreach (string key in previousNames)
        {
            if (!Contains(currentNames, key))
            {
                string[] parts = SplitNameKey(key);
                changes.Add(CreateDelta(
                    current.Profile,
                    SchemaRuleDeltaKind.Removed,
                    category,
                    parts[0],
                    parts[1],
                    NameIndexes(previous, category, parts[0], parts[1]),
                    NameIndexes(current, category, parts[0], parts[1])));
            }
        }
    }

    internal static SchemaRuleDelta CreateDelta(
        string profile,
        SchemaRuleDeltaKind kind,
        SchemaRuleDeltaCategory category,
        string namespaceName,
        string name,
        int[] previousIndexes,
        int[] currentIndexes)
    {
        SchemaRuleDelta result = new SchemaRuleDelta();
        result.Profile = profile;
        result.Kind = kind;
        result.Category = category;
        result.NamespaceName = namespaceName;
        result.Name = name;
        result.PreviousIndexes = previousIndexes;
        result.CurrentIndexes = currentIndexes;
        return result;
    }

    internal static string[] UniqueDefinitionNames(SchemaRuleSet set)
    {
        List<string> result = new List<string>();
        foreach (SchemaRule rule in set.Rules)
        {
            if (!Contains(result.ToArray(), rule.Name))
            {
                result.Add(rule.Name);
            }
        }
        return result.ToArray();
    }

    internal static int[] DefinitionIndexes(
        SchemaRuleSet set,
        string name)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < set.Rules.Length)
        {
            if (set.Rules[index].Name == name)
            {
                result.Add(index);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static string DefinitionSignature(
        SchemaRuleSet set,
        int[] indexes)
    {
        StringBuilder result = new StringBuilder();
        foreach (int index in indexes)
        {
            SchemaRule rule = set.Rules[index];
            result.Append(rule.Combine);
            result.Append('|');
            AppendPatternSignature(result, set, rule.PatternIndex);
            result.Append(';');
        }
        return result.ToString();
    }

    internal static void AppendPatternSignature(
        StringBuilder result,
        SchemaRuleSet set,
        int patternIndex)
    {
        SchemaPattern pattern = set.Patterns[patternIndex];
        result.Append((int)pattern.Kind);
        result.Append(':');
        AppendSignatureText(result, pattern.Name);
        AppendSignatureText(result, pattern.Value);
        AppendSignatureText(result, pattern.NamespaceName);
        AppendSignatureText(result, pattern.DatatypeLibrary);
        result.Append('[');
        int index = 0;
        while (index < pattern.ChildCount)
        {
            AppendPatternSignature(
                result,
                set,
                set.PatternChildIndexes[pattern.FirstChildIndex + index]);
            index = index + 1;
        }
        result.Append(']');
    }

    internal static void AppendSignatureText(
        StringBuilder result,
        string value)
    {
        result.Append(value.Length);
        result.Append('#');
        result.Append(value);
    }

    internal static string[] ElementNames(SchemaRuleSet set)
    {
        List<string> result = new List<string>();
        foreach (SchemaElementRule rule in set.Elements)
        {
            AddUniqueName(result, rule.NamespaceName, rule.LocalName);
        }
        return result.ToArray();
    }

    internal static string[] AttributeNames(SchemaRuleSet set)
    {
        List<string> result = new List<string>();
        foreach (SchemaAttributeRule rule in set.Attributes)
        {
            AddUniqueName(result, rule.NamespaceName, rule.LocalName);
        }
        return result.ToArray();
    }

    internal static string[] DatatypeNames(SchemaRuleSet set)
    {
        List<string> result = new List<string>();
        foreach (SchemaDatatype rule in set.Datatypes)
        {
            AddUniqueName(result, rule.Library, rule.Name);
        }
        return result.ToArray();
    }

    internal static void AddUniqueName(
        List<string> names,
        string namespaceName,
        string name)
    {
        if (name.Length == 0)
        {
            return;
        }
        string key = NameKey(namespaceName, name);
        if (!Contains(names.ToArray(), key))
        {
            names.Add(key);
        }
    }

    internal static string NameKey(string namespaceName, string name)
    {
        return namespaceName + "\u001f" + name;
    }

    internal static string[] SplitNameKey(string key)
    {
        int separator = key.IndexOf('\u001f');
        if (separator < 0)
        {
            return ["", key];
        }
        return
        [
            key.Substring(0, separator),
            key.Substring(separator + 1)
        ];
    }

    internal static bool Contains(string[] values, string value)
    {
        foreach (string item in values)
        {
            if (item == value)
            {
                return true;
            }
        }
        return false;
    }

    internal static int[] NameIndexes(
        SchemaRuleSet set,
        SchemaRuleDeltaCategory category,
        string namespaceName,
        string name)
    {
        if (category == SchemaRuleDeltaCategory.Element)
        {
            return ElementIndexes(set, namespaceName, name);
        }
        if (category == SchemaRuleDeltaCategory.Attribute)
        {
            return AttributeIndexes(set, namespaceName, name);
        }
        return DatatypeIndexes(set, namespaceName, name);
    }

    internal static int[] ElementIndexes(
        SchemaRuleSet set,
        string namespaceName,
        string name)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < set.Elements.Length)
        {
            SchemaElementRule rule = set.Elements[index];
            if (rule.NamespaceName == namespaceName
                && rule.LocalName == name)
            {
                result.Add(index);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static int[] AttributeIndexes(
        SchemaRuleSet set,
        string namespaceName,
        string name)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < set.Attributes.Length)
        {
            SchemaAttributeRule rule = set.Attributes[index];
            if (rule.NamespaceName == namespaceName
                && rule.LocalName == name)
            {
                result.Add(index);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static int[] DatatypeIndexes(
        SchemaRuleSet set,
        string library,
        string name)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < set.Datatypes.Length)
        {
            SchemaDatatype rule = set.Datatypes[index];
            if (rule.Library == library && rule.Name == name)
            {
                result.Add(index);
            }
            index = index + 1;
        }
        return result.ToArray();
    }
}
