using System;
using System.Collections.Generic;
using System.IO;

namespace CountryCodes.DataGenerator.Parsers;

internal static class UnM49HtmlParser
{
    private static readonly LocaleTableSpec[] CountryTables =
    {
        new LocaleTableSpec("ENG_COUNTRIES", "en"),
        new LocaleTableSpec("CHN_COUNTRIES", "zh"),
        new LocaleTableSpec("RUS_COUNTRIES", "ru"),
        new LocaleTableSpec("FRA_COUNTRIES", "fr"),
        new LocaleTableSpec("ESP_COUNTRIES", "es"),
        new LocaleTableSpec("ARB_COUNTRIES", "ar")
    };

    private static readonly LocaleTableSpec[] HierarchyTables =
    {
        new LocaleTableSpec("downloadTableEN", "en"),
        new LocaleTableSpec("downloadTableZH", "zh"),
        new LocaleTableSpec("downloadTableRU", "ru"),
        new LocaleTableSpec("downloadTableFR", "fr"),
        new LocaleTableSpec("downloadTableES", "es"),
        new LocaleTableSpec("downloadTableAR", "ar")
    };

    private static readonly UnM49HistoryKind[] HistoryKinds =
    {
        UnM49HistoryKind.AddedOrChanged,
        UnM49HistoryKind.NameChanged,
        UnM49HistoryKind.NotInCurrentUse,
        UnM49HistoryKind.Reintroduced,
        UnM49HistoryKind.Removed
    };

    internal static UnM49MethodologyResult ParseMethodology(TextReader input, string sourceName)
    {
        HtmlTables document = HtmlTableExtractor.Parse(input, sourceName);
        List<UnM49CountryRecord> countries = ParseCountryTables(document, sourceName);
        List<UnM49HistoryRecord> history = ParseHistoryTables(document, sourceName);
        return new UnM49MethodologyResult(countries, history);
    }

    internal static IReadOnlyList<UnM49HierarchyRecord> ParseOverview(
        TextReader input,
        string sourceName)
    {
        HtmlTables document = HtmlTableExtractor.Parse(input, sourceName);
        List<UnM49HierarchyRecord> records = new List<UnM49HierarchyRecord>();
        foreach (LocaleTableSpec specification in HierarchyTables)
        {
            HtmlTable table = FindTableById(document.Tables, specification.Marker, sourceName);
            if (table.Rows.Count < 2)
            {
                throw new SourceParseException(
                    sourceName,
                    $"hierarchy table '{specification.Marker}' has no data rows");
            }

            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex += 1)
            {
                IReadOnlyList<string> cells = table.Rows[rowIndex];
                if (cells.Count < 12)
                {
                    throw new SourceParseException(
                        sourceName,
                        $"hierarchy table '{specification.Marker}' row {rowIndex} " +
                        $"has {cells.Count} cells; expected at least 12");
                }

                records.Add(new UnM49HierarchyRecord(
                    specification.Locale,
                    RequireCell(cells, 0, sourceName, specification.Marker, rowIndex),
                    RequireCell(cells, 1, sourceName, specification.Marker, rowIndex),
                    NullIfEmpty(cells[2]),
                    NullIfEmpty(cells[3]),
                    NullIfEmpty(cells[4]),
                    NullIfEmpty(cells[5]),
                    NullIfEmpty(cells[6]),
                    NullIfEmpty(cells[7]),
                    RequireCell(cells, 8, sourceName, specification.Marker, rowIndex),
                    RequireCell(cells, 9, sourceName, specification.Marker, rowIndex),
                    RequireCell(cells, 10, sourceName, specification.Marker, rowIndex),
                    RequireCell(cells, 11, sourceName, specification.Marker, rowIndex)));
            }
        }

        if (records.Count == 0)
        {
            throw new SourceParseException(sourceName, "no M49 hierarchy records found");
        }

        return records;
    }

    private static List<UnM49CountryRecord> ParseCountryTables(
        HtmlTables document,
        string sourceName)
    {
        List<UnM49CountryRecord> records = new List<UnM49CountryRecord>();
        foreach (LocaleTableSpec specification in CountryTables)
        {
            int markerPosition = FindMarkerPosition(document.Html, specification.Marker, sourceName);
            HtmlTable table = FindFirstTableAfter(document.Tables, markerPosition, sourceName);
            if (table.Rows.Count < 2)
            {
                throw new SourceParseException(
                    sourceName,
                    $"country table '{specification.Marker}' has no data rows");
            }

            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex += 1)
            {
                IReadOnlyList<string> cells = table.Rows[rowIndex];
                if (cells.Count != 3)
                {
                    throw new SourceParseException(
                        sourceName,
                        $"country table '{specification.Marker}' row {rowIndex} " +
                        $"has {cells.Count} cells; expected 3");
                }

                records.Add(new UnM49CountryRecord(
                    specification.Locale,
                    RequireCell(cells, 0, sourceName, specification.Marker, rowIndex),
                    RequireCell(cells, 1, sourceName, specification.Marker, rowIndex),
                    RequireCell(cells, 2, sourceName, specification.Marker, rowIndex)));
            }
        }

        return records;
    }

    private static List<UnM49HistoryRecord> ParseHistoryTables(
        HtmlTables document,
        string sourceName)
    {
        int changesStart = FindMarkerPosition(document.Html, "changes", sourceName);
        int changesEnd = FindMarkerPosition(document.Html, "qa", sourceName);
        if (changesEnd <= changesStart)
        {
            throw new SourceParseException(sourceName, "M49 changes section has invalid boundaries");
        }

        List<HtmlTable> historyTables = new List<HtmlTable>();
        foreach (HtmlTable table in document.Tables)
        {
            if (table.Position > changesStart && table.Position < changesEnd)
            {
                historyTables.Add(table);
            }
        }

        if (historyTables.Count != HistoryKinds.Length)
        {
            throw new SourceParseException(
                sourceName,
                $"changes section contains {historyTables.Count} tables; expected {HistoryKinds.Length}");
        }

        List<UnM49HistoryRecord> records = new List<UnM49HistoryRecord>();
        for (int tableIndex = 0; tableIndex < historyTables.Count; tableIndex += 1)
        {
            HtmlTable table = historyTables[tableIndex];
            UnM49HistoryKind kind = HistoryKinds[tableIndex];
            if (table.Rows.Count < 2)
            {
                throw new SourceParseException(
                    sourceName,
                    $"history table '{kind}' has no data rows");
            }

            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex += 1)
            {
                IReadOnlyList<string> cells = table.Rows[rowIndex];
                int expectedCells = kind == UnM49HistoryKind.AddedOrChanged ? 4 : 3;
                if (cells.Count != expectedCells)
                {
                    throw new SourceParseException(
                        sourceName,
                        $"history table '{kind}' row {rowIndex} has {cells.Count} cells; " +
                        $"expected {expectedCells}");
                }

                string? changeDate = kind == UnM49HistoryKind.AddedOrChanged
                    ? NullIfEmpty(cells[2])
                    : null;
                string note = kind == UnM49HistoryKind.AddedOrChanged ? cells[3] : cells[2];
                records.Add(new UnM49HistoryRecord(
                    kind,
                    RequireCell(cells, 0, sourceName, kind.ToString(), rowIndex),
                    RequireCell(cells, 1, sourceName, kind.ToString(), rowIndex),
                    changeDate,
                    note));
            }
        }

        return records;
    }

    private static int FindMarkerPosition(string html, string marker, string sourceName)
    {
        string doubleQuoted = $"id=\"{marker}\"";
        int position = html.IndexOf(doubleQuoted, StringComparison.OrdinalIgnoreCase);
        if (position < 0)
        {
            string singleQuoted = $"id='{marker}'";
            position = html.IndexOf(singleQuoted, StringComparison.OrdinalIgnoreCase);
        }

        if (position < 0)
        {
            throw new SourceParseException(sourceName, $"missing HTML marker id '{marker}'");
        }

        return position;
    }

    private static HtmlTable FindFirstTableAfter(
        IReadOnlyList<HtmlTable> tables,
        int position,
        string sourceName)
    {
        foreach (HtmlTable table in tables)
        {
            if (table.Position > position)
            {
                return table;
            }
        }

        throw new SourceParseException(sourceName, "HTML marker is not followed by a table");
    }

    private static HtmlTable FindTableById(
        IReadOnlyList<HtmlTable> tables,
        string id,
        string sourceName)
    {
        HtmlTable? result = null;
        foreach (HtmlTable table in tables)
        {
            if (!string.Equals(table.Id, id, StringComparison.Ordinal))
            {
                continue;
            }

            if (result != null)
            {
                throw new SourceParseException(sourceName, $"duplicate HTML table id '{id}'");
            }

            result = table;
        }

        if (result == null)
        {
            throw new SourceParseException(sourceName, $"missing HTML table id '{id}'");
        }

        return result;
    }

    private static string RequireCell(
        IReadOnlyList<string> cells,
        int cellIndex,
        string sourceName,
        string tableName,
        int rowIndex)
    {
        string value = cells[cellIndex];
        if (value.Length == 0)
        {
            throw new SourceParseException(
                sourceName,
                $"table '{tableName}' row {rowIndex} cell {cellIndex} must not be empty");
        }

        return value;
    }

    private static string? NullIfEmpty(string value)
    {
        return value.Length == 0 ? null : value;
    }

    private sealed class LocaleTableSpec
    {
        internal LocaleTableSpec(string marker, string locale)
        {
            Marker = marker;
            Locale = locale;
        }

        internal string Marker { get; }

        internal string Locale { get; }
    }
}

internal sealed class UnM49MethodologyResult
{
    internal UnM49MethodologyResult(
        IReadOnlyList<UnM49CountryRecord> countries,
        IReadOnlyList<UnM49HistoryRecord> history)
    {
        Countries = countries;
        History = history;
    }

    internal IReadOnlyList<UnM49CountryRecord> Countries { get; }

    internal IReadOnlyList<UnM49HistoryRecord> History { get; }
}
