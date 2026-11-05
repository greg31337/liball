using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace odf;

internal class QueryServiceHit
{
    internal OdfSearchMatch Match = new OdfSearchMatch();
    internal string FieldName = "";
}

internal static class QueryServiceModel
{
    internal static OdfSearchMatch[] QueryLiteral(
        OdfDocument document,
        string value,
        bool matchCase)
    {
        RequireDocument(document);
        RequireSearchValue(value, "A literal query cannot be empty.");
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        CrossPartQueryRecord[] records = index.GetAllRecords();
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        List<QueryServiceHit> hits = new List<QueryServiceHit>();
        int indexValue = 0;
        while (indexValue < records.Length)
        {
            CrossPartQueryRecord record = records[indexValue];
            if (record.Category != "qualified-name")
            {
                AddLiteralHits(
                    hits,
                    record,
                    value,
                    comparison);
            }
            indexValue = indexValue + 1;
        }
        return MatchesFromHits(hits);
    }

    internal static OdfSearchMatch[] QueryRegularExpression(
        OdfDocument document,
        string pattern,
        bool matchCase)
    {
        RequireDocument(document);
        RequireSearchValue(
            pattern,
            "A regular-expression query cannot be empty.");
        RegexOptions options = RegexOptions.CultureInvariant;
        if (!matchCase)
        {
            options = options | RegexOptions.IgnoreCase;
        }
        Regex expression = new Regex(
            pattern,
            options,
            TimeSpan.FromSeconds(1));
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        CrossPartQueryRecord[] records = index.GetAllRecords();
        List<QueryServiceHit> hits = new List<QueryServiceHit>();
        int indexValue = 0;
        while (indexValue < records.Length)
        {
            CrossPartQueryRecord record = records[indexValue];
            if (record.Category != "qualified-name")
            {
                AddRegularExpressionHits(
                    hits,
                    record,
                    expression);
            }
            indexValue = indexValue + 1;
        }
        return MatchesFromHits(hits);
    }

    internal static OdfSearchMatch[] QueryNodes(
        OdfDocument document,
        OdfQualifiedName name)
    {
        RequireDocument(document);
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        CrossPartQueryRecord[] records =
            index.FindQualifiedNames(name);
        List<QueryServiceHit> hits = new List<QueryServiceHit>();
        int indexValue = 0;
        while (indexValue < records.Length)
        {
            CrossPartQueryRecord record = records[indexValue];
            string matchedValue = "{" + name.NamespaceName + "}"
                + name.LocalName;
            AddUniqueHit(
                hits,
                CreateHit(
                    record,
                    matchedValue,
                    matchedValue,
                    0,
                    matchedValue.Length,
                    true));
            indexValue = indexValue + 1;
        }
        return MatchesFromHits(hits);
    }

    internal static OdfSearchMatch[] QueryField(
        OdfDocument document,
        OdfQualifiedName fieldName,
        string value,
        bool matchCase)
    {
        RequireDocument(document);
        if (value == null)
        {
            throw new OdfDocumentException(
                "A field query value is required.");
        }
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        CrossPartQueryRecord[] records = index.FindFields(fieldName);
        List<QueryServiceHit> hits = new List<QueryServiceHit>();
        if (value.Length == 0)
        {
            int indexValue = 0;
            while (indexValue < records.Length)
            {
                CrossPartQueryRecord record = records[indexValue];
                AddUniqueHit(
                    hits,
                    CreateHit(
                        record,
                        record.Value,
                        record.Value,
                        0,
                        record.Value.Length,
                        true));
                indexValue = indexValue + 1;
            }
        }
        else
        {
            StringComparison comparison = matchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            int indexValue = 0;
            while (indexValue < records.Length)
            {
                AddLiteralHits(
                    hits,
                    records[indexValue],
                    value,
                    comparison);
                indexValue = indexValue + 1;
            }
        }
        return MatchesFromHits(hits);
    }

    internal static OdfSearchMatch[] QueryReferences(
        OdfDocument document,
        string target)
    {
        RequireDocument(document);
        RequireSearchValue(target, "A reference target cannot be empty.");
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        CrossPartQueryRecord[] records = index.FindReferences(target);
        List<QueryServiceHit> hits = new List<QueryServiceHit>();
        int indexValue = 0;
        while (indexValue < records.Length)
        {
            CrossPartQueryRecord record = records[indexValue];
            int offset = record.Value.IndexOf(
                target,
                StringComparison.Ordinal);
            if (offset < 0)
            {
                offset = 0;
            }
            AddUniqueHit(
                hits,
                CreateHit(
                    record,
                    target,
                    record.Value,
                    offset,
                    target.Length,
                    record.Value == target));
            indexValue = indexValue + 1;
        }
        return MatchesFromHits(hits);
    }

    static void AddLiteralHits(
        List<QueryServiceHit> hits,
        CrossPartQueryRecord record,
        string value,
        StringComparison comparison)
    {
        int offset = 0;
        while (offset <= record.Value.Length - value.Length)
        {
            int found = record.Value.IndexOf(value, offset, comparison);
            if (found < 0)
            {
                return;
            }
            string matched = record.Value.Substring(found, value.Length);
            AddUniqueHit(
                hits,
                CreateHit(
                    record,
                    matched,
                    record.Value,
                    found,
                    value.Length,
                    record.Value.Length == value.Length
                        && matched == value));
            offset = found + value.Length;
        }
    }

    static void AddRegularExpressionHits(
        List<QueryServiceHit> hits,
        CrossPartQueryRecord record,
        Regex expression)
    {
        Match match = expression.Match(record.Value);
        while (match.Success)
        {
            AddUniqueHit(
                hits,
                CreateHit(
                    record,
                    match.Value,
                    record.Value,
                    match.Index,
                    match.Length,
                    match.Index == 0
                        && match.Length == record.Value.Length));
            match = match.NextMatch();
        }
    }

    static QueryServiceHit CreateHit(
        CrossPartQueryRecord record,
        string matchedValue,
        string context,
        int offset,
        int length,
        bool exact)
    {
        QueryServiceHit result = new QueryServiceHit();
        result.FieldName = LocalFieldName(record.FieldName);
        result.Match.MatchedValue = matchedValue;
        result.Match.Context = context;
        result.Match.Node = record.Node;
        result.Match.IsExact = exact;
        result.Match.Location = CopyLocation(record, offset, length);
        return result;
    }

    static OdfLocation CopyLocation(
        CrossPartQueryRecord record,
        int offset,
        int length)
    {
        OdfLocation result = new OdfLocation();
        if (record.Node != null && record.Node.Location != null)
        {
            OdfLocation source = record.Node.Location;
            result.PartPath = source.PartPath;
            result.NodePath = source.NodePath;
            result.XmlId = source.XmlId;
            result.Line = source.Line;
            result.Column = source.Column;
            result.ByteOffset = source.ByteOffset;
        }
        if (record.PartPath.Length > 0)
        {
            result.PartPath = record.PartPath;
        }
        if (result.XmlId.Length == 0 && record.Node != null)
        {
            result.XmlId = record.Node.XmlId;
        }
        result.TextOffset = offset;
        result.TextLength = length;
        return result;
    }

    static void AddUniqueHit(
        List<QueryServiceHit> hits,
        QueryServiceHit candidate)
    {
        if (candidate.Match.Node != null)
        {
            int index = 0;
            while (index < hits.Count)
            {
                QueryServiceHit existing = hits[index];
                if (existing.Match.Node == candidate.Match.Node
                    && existing.FieldName == candidate.FieldName
                    && existing.Match.MatchedValue
                        == candidate.Match.MatchedValue
                    && existing.Match.Location.TextOffset
                        == candidate.Match.Location.TextOffset)
                {
                    return;
                }
                index = index + 1;
            }
        }
        hits.Add(candidate);
    }

    static OdfSearchMatch[] MatchesFromHits(
        List<QueryServiceHit> hits)
    {
        OdfSearchMatch[] result = new OdfSearchMatch[hits.Count];
        int index = 0;
        while (index < hits.Count)
        {
            result[index] = hits[index].Match;
            index = index + 1;
        }
        return result;
    }

    static string LocalFieldName(string fieldName)
    {
        int separator = fieldName.LastIndexOf('\u001F');
        if (separator >= 0 && separator + 1 < fieldName.Length)
        {
            return fieldName.Substring(separator + 1);
        }
        return fieldName;
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for querying.");
        }
    }

    static void RequireSearchValue(string value, string message)
    {
        if (value == null || value.Length == 0)
        {
            throw new OdfDocumentException(message);
        }
    }
}
