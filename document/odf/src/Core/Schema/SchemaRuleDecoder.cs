using System;
using System.Collections.Generic;

namespace odf;

internal static class SchemaRuleDecoder
{
    internal static SchemaRuleSet Decode(string[] chunks)
    {
        if (chunks == null || chunks.Length == 0)
        {
            throw new OdfValidationException("Encoded schema rule data is required.");
        }
        SchemaRuleSet result = new SchemaRuleSet();
        List<SchemaNamespaceRule> namespaces = new List<SchemaNamespaceRule>();
        List<SchemaRule> rules = new List<SchemaRule>();
        List<SchemaPattern> patterns = new List<SchemaPattern>();
        List<int> children = new List<int>();
        foreach (string chunk in chunks)
        {
            string[] lines = chunk.Split('\n');
            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    DecodeLine(
                        line,
                        result,
                        namespaces,
                        rules,
                        patterns,
                        children);
                }
            }
        }
        result.Namespaces = namespaces.ToArray();
        result.Rules = rules.ToArray();
        result.Patterns = patterns.ToArray();
        result.PatternChildIndexes = children.ToArray();
        BuildDerivedTables(result);
        ValidateIndexes(result);
        return result;
    }

    internal static void DecodeLine(
        string line,
        SchemaRuleSet result,
        List<SchemaNamespaceRule> namespaces,
        List<SchemaRule> rules,
        List<SchemaPattern> patterns,
        List<int> children)
    {
        string[] fields = line.Split('\t');
        if (fields.Length == 0)
        {
            return;
        }
        if (fields[0] == "H" && fields.Length == 6)
        {
            result.Version = (OdfVersion)int.Parse(fields[1]);
            result.Profile = DecodeText(fields[2]);
            result.SourceName = DecodeText(fields[3]);
            result.DatatypeLibrary = DecodeText(fields[4]);
            result.DefaultNamespace = DecodeText(fields[5]);
            return;
        }
        if (fields[0] == "N" && fields.Length == 3)
        {
            SchemaNamespaceRule namespaceRule = new SchemaNamespaceRule();
            namespaceRule.Prefix = DecodeText(fields[1]);
            namespaceRule.NamespaceName = DecodeText(fields[2]);
            namespaces.Add(namespaceRule);
            return;
        }
        if (fields[0] == "R" && fields.Length == 5)
        {
            SchemaRule rule = new SchemaRule();
            rule.Name = DecodeText(fields[1]);
            rule.Combine = DecodeText(fields[2]);
            rule.IsStart = fields[3] == "1";
            rule.PatternIndex = int.Parse(fields[4]);
            rules.Add(rule);
            return;
        }
        if (fields[0] == "P" && fields.Length == 9)
        {
            SchemaPattern pattern = new SchemaPattern();
            pattern.Kind = (SchemaPatternKind)int.Parse(fields[1]);
            pattern.Name = DecodeText(fields[2]);
            pattern.Value = DecodeText(fields[3]);
            pattern.NamespaceName = DecodeText(fields[4]);
            pattern.DatatypeLibrary = DecodeText(fields[5]);
            pattern.OwnerRuleIndex = int.Parse(fields[6]);
            pattern.FirstChildIndex = int.Parse(fields[7]);
            pattern.ChildCount = int.Parse(fields[8]);
            patterns.Add(pattern);
            return;
        }
        if (fields[0] == "C" && fields.Length == 2)
        {
            children.Add(int.Parse(fields[1]));
            return;
        }
        throw new OdfValidationException(
            "Encoded schema rule data contains an invalid row.");
    }

    internal static string DecodeText(string value)
    {
        return value.Replace("%09", "\t", StringComparison.Ordinal)
            .Replace("%0A", "\n", StringComparison.Ordinal)
            .Replace("%0D", "\r", StringComparison.Ordinal)
            .Replace("%25", "%", StringComparison.Ordinal);
    }

    internal static void BuildDerivedTables(SchemaRuleSet result)
    {
        List<SchemaElementRule> elements = new List<SchemaElementRule>();
        List<SchemaAttributeRule> attributes = new List<SchemaAttributeRule>();
        List<SchemaOccurrenceRule> occurrences = new List<SchemaOccurrenceRule>();
        List<SchemaDatatype> datatypes = new List<SchemaDatatype>();
        int index = 0;
        while (index < result.Patterns.Length)
        {
            SchemaPattern pattern = result.Patterns[index];
            if (pattern.Kind == SchemaPatternKind.Element)
            {
                elements.Add(CreateElementRule(result, pattern, index));
            }
            else if (pattern.Kind == SchemaPatternKind.Attribute)
            {
                attributes.Add(CreateAttributeRule(result, pattern, index));
            }
            else if (IsOccurrencePattern(pattern.Kind))
            {
                occurrences.Add(CreateOccurrenceRule(pattern, index));
            }
            else if (pattern.Kind == SchemaPatternKind.Data
                || pattern.Kind == SchemaPatternKind.Value)
            {
                datatypes.Add(CreateDatatype(result, pattern, index));
            }
            index = index + 1;
        }
        result.Elements = elements.ToArray();
        result.Attributes = attributes.ToArray();
        result.Occurrences = occurrences.ToArray();
        result.Datatypes = datatypes.ToArray();
    }

    internal static SchemaElementRule CreateElementRule(
        SchemaRuleSet set,
        SchemaPattern pattern,
        int patternIndex)
    {
        SchemaElementRule result = new SchemaElementRule();
        result.NamespaceName = pattern.NamespaceName;
        result.LocalName = pattern.Name;
        SchemaPattern? namePattern = DirectNamePattern(set, pattern);
        if (result.LocalName.Length == 0 && namePattern != null)
        {
            result.NamespaceName = namePattern.NamespaceName;
            result.LocalName = namePattern.Name;
        }
        result.PatternIndex = patternIndex;
        result.OwnerRuleIndex = pattern.OwnerRuleIndex;
        result.ContentPatternIndexes = ContentChildren(set, pattern);
        return result;
    }

    internal static SchemaAttributeRule CreateAttributeRule(
        SchemaRuleSet set,
        SchemaPattern pattern,
        int patternIndex)
    {
        SchemaAttributeRule result = new SchemaAttributeRule();
        result.NamespaceName = pattern.NamespaceName;
        result.LocalName = pattern.Name;
        SchemaPattern? namePattern = DirectNamePattern(set, pattern);
        if (result.LocalName.Length == 0 && namePattern != null)
        {
            result.NamespaceName = namePattern.NamespaceName;
            result.LocalName = namePattern.Name;
        }
        result.PatternIndex = patternIndex;
        result.OwnerRuleIndex = pattern.OwnerRuleIndex;
        result.ValuePatternIndexes = ContentChildren(set, pattern);
        return result;
    }

    internal static SchemaOccurrenceRule CreateOccurrenceRule(
        SchemaPattern pattern,
        int patternIndex)
    {
        SchemaOccurrenceRule result = new SchemaOccurrenceRule();
        result.PatternIndex = patternIndex;
        result.Maximum = 1;
        if (pattern.Kind == SchemaPatternKind.Optional)
        {
            result.Minimum = 0;
        }
        else if (pattern.Kind == SchemaPatternKind.ZeroOrMore)
        {
            result.Minimum = 0;
            result.Maximum = -1;
        }
        else
        {
            result.Minimum = 1;
            result.Maximum = -1;
        }
        return result;
    }

    internal static SchemaDatatype CreateDatatype(
        SchemaRuleSet set,
        SchemaPattern pattern,
        int patternIndex)
    {
        SchemaDatatype result = new SchemaDatatype();
        result.Name = pattern.Name;
        result.Library = pattern.DatatypeLibrary;
        result.Value = pattern.Value;
        result.IsValue = pattern.Kind == SchemaPatternKind.Value;
        result.PatternIndex = patternIndex;
        List<string> names = new List<string>();
        List<string> values = new List<string>();
        int childIndex = 0;
        while (childIndex < pattern.ChildCount)
        {
            int index = set.PatternChildIndexes[
                pattern.FirstChildIndex + childIndex];
            SchemaPattern child = set.Patterns[index];
            if (child.Kind == SchemaPatternKind.Param)
            {
                names.Add(child.Name);
                values.Add(child.Value);
            }
            childIndex = childIndex + 1;
        }
        result.ParameterNames = names.ToArray();
        result.ParameterValues = values.ToArray();
        return result;
    }

    internal static int[] ContentChildren(
        SchemaRuleSet set,
        SchemaPattern pattern)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < pattern.ChildCount)
        {
            int childIndex = set.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            SchemaPattern child = set.Patterns[childIndex];
            if (child.Kind != SchemaPatternKind.Name
                && child.Kind != SchemaPatternKind.AnyName
                && child.Kind != SchemaPatternKind.NamespaceName)
            {
                result.Add(childIndex);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static SchemaPattern? DirectNamePattern(
        SchemaRuleSet set,
        SchemaPattern pattern)
    {
        int index = 0;
        while (index < pattern.ChildCount)
        {
            int childIndex = set.PatternChildIndexes[
                pattern.FirstChildIndex + index];
            SchemaPattern child = set.Patterns[childIndex];
            if (child.Kind == SchemaPatternKind.Name)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    internal static bool IsOccurrencePattern(SchemaPatternKind kind)
    {
        return kind == SchemaPatternKind.Optional
            || kind == SchemaPatternKind.ZeroOrMore
            || kind == SchemaPatternKind.OneOrMore;
    }

    internal static void ValidateIndexes(SchemaRuleSet result)
    {
        foreach (SchemaRule rule in result.Rules)
        {
            if (rule.PatternIndex < 0
                || rule.PatternIndex >= result.Patterns.Length)
            {
                throw new OdfValidationException(
                    "A schema definition references an invalid pattern.");
            }
        }
        foreach (SchemaPattern pattern in result.Patterns)
        {
            if (pattern.OwnerRuleIndex < 0
                || pattern.OwnerRuleIndex >= result.Rules.Length
                || pattern.FirstChildIndex < 0
                || pattern.ChildCount < 0
                || pattern.FirstChildIndex + pattern.ChildCount
                    > result.PatternChildIndexes.Length)
            {
                throw new OdfValidationException(
                    "A schema pattern contains an invalid table index.");
            }
        }
        foreach (int child in result.PatternChildIndexes)
        {
            if (child < 0 || child >= result.Patterns.Length)
            {
                throw new OdfValidationException(
                    "A schema child-content rule references an invalid pattern.");
            }
        }
    }
}
