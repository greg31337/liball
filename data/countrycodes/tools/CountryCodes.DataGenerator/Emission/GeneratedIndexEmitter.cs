using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CountryCodes.DataGenerator.Emission;

internal sealed class GeneratedIndexCollector
{
    private readonly List<ExactIndexEmissionRow> exactRows = new List<ExactIndexEmissionRow>();
    private readonly List<SearchIndexEmissionRow> searchRows = new List<SearchIndexEmissionRow>();

    internal IReadOnlyList<ExactIndexEmissionRow> ExactRows => exactRows;

    internal IReadOnlyList<SearchIndexEmissionRow> SearchRows => searchRows;

    internal void AddExact(string kind, string key, int rowIndex)
    {
        exactRows.Add(new ExactIndexEmissionRow(kind, key, rowIndex));
    }

    internal void AddSearch(string kind, string key, int rowIndex)
    {
        searchRows.Add(new SearchIndexEmissionRow(kind, key, rowIndex));
    }
}

internal static class GeneratedIndexEmitter
{
    private const string FileName = "GeneratedIndexTable.g.cs";

    internal static GeneratedIndexEmissionResult Emit(
        GeneratedIndexCollector collector,
        string outputDirectory)
    {
        List<ExactIndexEmissionRow> exactRows =
            new List<ExactIndexEmissionRow>(collector.ExactRows);
        List<SearchIndexEmissionRow> searchRows =
            new List<SearchIndexEmissionRow>(collector.SearchRows);
        exactRows.Sort(CompareExactRows);
        searchRows.Sort(CompareSearchRows);
        ValidateExactRows(exactRows);
        ValidateSearchRows(searchRows);

        string path = Path.Combine(outputDirectory, FileName);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(path, Render(exactRows, searchRows), new UTF8Encoding(false));
        return new GeneratedIndexEmissionResult(exactRows.Count, searchRows.Count, path);
    }

    private static void ValidateExactRows(IReadOnlyList<ExactIndexEmissionRow> rows)
    {
        HashSet<string> kinds = new HashSet<string>(StringComparer.Ordinal);
        ExactIndexEmissionRow? previous = null;
        foreach (ExactIndexEmissionRow row in rows)
        {
            if (row.Key.Length == 0 || row.RowIndex < 0)
            {
                throw new EmissionException("Generated exact index contains an invalid entry.");
            }

            if (previous != null && CompareExactRows(previous, row) >= 0)
            {
                throw new EmissionException(
                    "Generated exact-index rows are not unique stable order.");
            }

            kinds.Add(row.Kind);
            previous = row;
        }

        foreach (string kind in ExactKinds)
        {
            if (!kinds.Contains(kind))
            {
                throw new EmissionException($"Generated exact index has no '{kind}' entries.");
            }
        }
    }

    private static void ValidateSearchRows(IReadOnlyList<SearchIndexEmissionRow> rows)
    {
        HashSet<string> kinds = new HashSet<string>(StringComparer.Ordinal);
        SearchIndexEmissionRow? previous = null;
        foreach (SearchIndexEmissionRow row in rows)
        {
            if (row.Key.Length == 0 || row.RowIndex < 0)
            {
                throw new EmissionException("Generated search index contains an invalid entry.");
            }

            if (previous != null && CompareSearchRows(previous, row) >= 0)
            {
                throw new EmissionException(
                    "Generated search-index rows are not unique stable order.");
            }

            kinds.Add(row.Kind);
            previous = row;
        }

        foreach (string kind in SearchKinds)
        {
            if (!kinds.Contains(kind))
            {
                throw new EmissionException($"Generated search index has no '{kind}' entries.");
            }
        }
    }

    private static int CompareExactRows(ExactIndexEmissionRow left, ExactIndexEmissionRow right)
    {
        int comparison = ExactKindOrder(left.Kind).CompareTo(ExactKindOrder(right.Kind));
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Key, right.Key);
        }

        return comparison == 0 ? left.RowIndex.CompareTo(right.RowIndex) : comparison;
    }

    private static int CompareSearchRows(SearchIndexEmissionRow left, SearchIndexEmissionRow right)
    {
        int comparison = string.CompareOrdinal(left.Key, right.Key);
        if (comparison == 0)
        {
            comparison = SearchKindOrder(left.Kind).CompareTo(SearchKindOrder(right.Kind));
        }

        return comparison == 0 ? left.RowIndex.CompareTo(right.RowIndex) : comparison;
    }

    private static int ExactKindOrder(string kind)
    {
        for (int index = 0; index < ExactKinds.Length; index += 1)
        {
            if (string.Equals(ExactKinds[index], kind, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new EmissionException($"Unknown exact-index kind '{kind}'.");
    }

    private static int SearchKindOrder(string kind)
    {
        for (int index = 0; index < SearchKinds.Length; index += 1)
        {
            if (string.Equals(SearchKinds[index], kind, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new EmissionException($"Unknown search-index kind '{kind}'.");
    }

    private static string Render(
        IReadOnlyList<ExactIndexEmissionRow> exactRows,
        IReadOnlyList<SearchIndexEmissionRow> searchRows)
    {
        StringBuilder builder = new StringBuilder(1_500_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        RenderEnums(builder);
        Line(builder, string.Empty);
        RenderTypes(builder);
        Line(builder, string.Empty);
        Line(builder, "internal static class GeneratedIndexTable");
        Line(builder, "{");
        RenderExactRows(builder, exactRows);
        RenderSearchRows(builder, searchRows);
        Line(builder, "}");
        return builder.ToString();
    }

    private static void RenderEnums(StringBuilder builder)
    {
        Line(builder, "internal enum GeneratedExactIndexKind");
        Line(builder, "{");
        for (int index = 0; index < ExactKinds.Length; index += 1)
        {
            string suffix = index + 1 == ExactKinds.Length ? string.Empty : ",";
            Line(
                builder,
                $"    {ExactKinds[index]} = {index.ToString(CultureInfo.InvariantCulture)}{suffix}");
        }

        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedSearchIndexKind");
        Line(builder, "{");
        for (int index = 0; index < SearchKinds.Length; index += 1)
        {
            string suffix = index + 1 == SearchKinds.Length ? string.Empty : ",";
            Line(
                builder,
                $"    {SearchKinds[index]} = {index.ToString(CultureInfo.InvariantCulture)}{suffix}");
        }

        Line(builder, "}");
    }

    private static void RenderTypes(StringBuilder builder)
    {
        Line(builder, "internal class GeneratedExactIndexRow");
        Line(builder, "{");
        Line(builder, "    internal GeneratedExactIndexKind Kind;");
        Line(builder, "    internal string Key;");
        Line(builder, "    internal int RowIndex;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedExactIndexRow(");
        Line(builder, "        GeneratedExactIndexKind kind,");
        Line(builder, "        string key,");
        Line(builder, "        int rowIndex)");
        Line(builder, "    {");
        Line(builder, "        Kind = kind;");
        Line(builder, "        Key = key;");
        Line(builder, "        RowIndex = rowIndex;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedSearchIndexRow");
        Line(builder, "{");
        Line(builder, "    internal string SearchKey;");
        Line(builder, "    internal GeneratedSearchIndexKind Kind;");
        Line(builder, "    internal int RowIndex;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedSearchIndexRow(");
        Line(builder, "        string searchKey,");
        Line(builder, "        GeneratedSearchIndexKind kind,");
        Line(builder, "        int rowIndex)");
        Line(builder, "    {");
        Line(builder, "        SearchKey = searchKey;");
        Line(builder, "        Kind = kind;");
        Line(builder, "        RowIndex = rowIndex;");
        Line(builder, "    }");
        Line(builder, "}");
    }

    private static void RenderExactRows(
        StringBuilder builder,
        IReadOnlyList<ExactIndexEmissionRow> rows)
    {
        Line(builder, "    internal static readonly GeneratedExactIndexRow[] ExactRows =");
        Line(builder, "    {");
        foreach (ExactIndexEmissionRow row in rows)
        {
            Line(builder, "        new GeneratedExactIndexRow(");
            Line(builder, $"            GeneratedExactIndexKind.{row.Kind},");
            Line(builder, $"            {Literal(row.Key)},");
            Line(
                builder,
                $"            {row.RowIndex.ToString(CultureInfo.InvariantCulture)}),");
        }

        Line(builder, "    };");
    }

    private static void RenderSearchRows(
        StringBuilder builder,
        IReadOnlyList<SearchIndexEmissionRow> rows)
    {
        Line(builder, string.Empty);
        Line(builder, "    internal static readonly GeneratedSearchIndexRow[] SearchRows =");
        Line(builder, "    {");
        foreach (SearchIndexEmissionRow row in rows)
        {
            Line(builder, "        new GeneratedSearchIndexRow(");
            Line(builder, $"            {Literal(row.Key)},");
            Line(builder, $"            GeneratedSearchIndexKind.{row.Kind},");
            Line(
                builder,
                $"            {row.RowIndex.ToString(CultureInfo.InvariantCulture)}),");
        }

        Line(builder, "    };");
    }

    private static string Literal(string value)
    {
        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal) + "\"";
    }

    private static void Line(StringBuilder builder, string value)
    {
        builder.Append(value);
        builder.Append('\n');
    }

    private static readonly string[] ExactKinds =
    {
        "CountryAlpha2",
        "CountryAlpha3",
        "CountryNumeric",
        "ReservedAlpha2",
        "ReservedAlpha3",
        "ReservedNumeric",
        "ReservedAlpha4",
        "SubdivisionCode",
        "SubdivisionAlias",
        "HistoricalAlpha2",
        "HistoricalAlpha3",
        "HistoricalAlpha4",
        "HistoricalNumeric",
        "CountryChangeAlpha4",
        "GeographicRegionM49",
        "CountryRegionAlpha2"
    };

    private static readonly string[] SearchKinds =
    {
        "CountryName",
        "HistoricalCountryName",
        "SubdivisionName",
        "GeographicRegionName"
    };
}

internal sealed class ExactIndexEmissionRow
{
    internal ExactIndexEmissionRow(string kind, string key, int rowIndex)
    {
        Kind = kind;
        Key = key;
        RowIndex = rowIndex;
    }

    internal string Kind { get; }

    internal string Key { get; }

    internal int RowIndex { get; }
}

internal sealed class SearchIndexEmissionRow
{
    internal SearchIndexEmissionRow(string kind, string key, int rowIndex)
    {
        Kind = kind;
        Key = key;
        RowIndex = rowIndex;
    }

    internal string Kind { get; }

    internal string Key { get; }

    internal int RowIndex { get; }
}
