using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace CountryCodes.DataGenerator.Parsers;

internal static class HtmlTableExtractor
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(15);

    private static readonly Regex CommentPattern = new Regex(
        "<!--.*?-->",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex TablePattern = new Regex(
        "<table\\b(?<attributes>[^>]*)>(?<body>.*?)</table\\s*>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex RowPattern = new Regex(
        "<tr\\b[^>]*>(?<body>.*?)</tr\\s*>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex CellPattern = new Regex(
        "<t[dh]\\b[^>]*>(?<body>.*?)</t[dh]\\s*>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex TagPattern = new Regex(
        "<[^>]+>",
        RegexOptions.Singleline | RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex WhitespacePattern = new Regex(
        "\\s+",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    private static readonly Regex IdPattern = new Regex(
        "(?:^|\\s)id\\s*=\\s*(?:\"(?<double>[^\"]*)\"|'(?<single>[^']*)'|(?<bare>[^\\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    internal static HtmlTables Parse(TextReader input, string sourceName)
    {
        string html = input.ReadToEnd();
        try
        {
            string cleanedHtml = CommentPattern.Replace(html, " ");
            List<HtmlTable> tables = new List<HtmlTable>();
            foreach (Match tableMatch in TablePattern.Matches(cleanedHtml))
            {
                string attributes = tableMatch.Groups["attributes"].Value;
                string? id = ReadId(attributes);
                List<IReadOnlyList<string>> rows = new List<IReadOnlyList<string>>();
                string tableBody = tableMatch.Groups["body"].Value;
                foreach (Match rowMatch in RowPattern.Matches(tableBody))
                {
                    List<string> cells = new List<string>();
                    string rowBody = rowMatch.Groups["body"].Value;
                    foreach (Match cellMatch in CellPattern.Matches(rowBody))
                    {
                        cells.Add(ReadCellText(cellMatch.Groups["body"].Value));
                    }

                    if (cells.Count != 0)
                    {
                        rows.Add(cells);
                    }
                }

                tables.Add(new HtmlTable(tableMatch.Index, id, rows));
            }

            if (tables.Count == 0)
            {
                throw new SourceParseException(sourceName, "no HTML tables found");
            }

            return new HtmlTables(cleanedHtml, tables);
        }
        catch (RegexMatchTimeoutException exception)
        {
            throw new SourceParseException(sourceName, "HTML table scan timed out", exception);
        }
    }

    private static string? ReadId(string attributes)
    {
        Match match = IdPattern.Match(attributes);
        if (!match.Success)
        {
            return null;
        }

        if (match.Groups["double"].Success)
        {
            return match.Groups["double"].Value;
        }

        if (match.Groups["single"].Success)
        {
            return match.Groups["single"].Value;
        }

        return match.Groups["bare"].Value;
    }

    private static string ReadCellText(string cellHtml)
    {
        string withoutTags = TagPattern.Replace(cellHtml, " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern.Replace(decoded, " ").Trim();
    }
}

internal sealed class HtmlTables
{
    internal HtmlTables(string html, IReadOnlyList<HtmlTable> tables)
    {
        Html = html;
        Tables = tables;
    }

    internal string Html { get; }

    internal IReadOnlyList<HtmlTable> Tables { get; }
}

internal sealed class HtmlTable
{
    internal HtmlTable(int position, string? id, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        Position = position;
        Id = id;
        Rows = rows;
    }

    internal int Position { get; }

    internal string? Id { get; }

    internal IReadOnlyList<IReadOnlyList<string>> Rows { get; }
}
