using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set5HierarchyReader
{
    private const string TableMarker = "<table class=\"Dynamic639-5OutputTables\"";

    private static readonly Regex RowPattern = new Regex(
        "<tr\\b[^>]*>(.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex CellPattern = new Regex(
        "<td\\b[^>]*>(.*?)</td>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex BreakPattern = new Regex(
        "<br\\s*/?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TagPattern = new Regex(
        "<[^>]+>",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex WhitespacePattern = new Regex(
        "\\s+",
        RegexOptions.CultureInvariant);

    private static readonly Regex NoteReferencePattern = new Regex(
        "href=\"#iso639-5-Note-([a-z])\"",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set5HierarchyData Read(string sourcePath, Iso639Set5Record[] set5Records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(set5Records);

        Dictionary<string, Iso639Set5Record> recordsByCode = CreateRecordIndex(set5Records);
        string table = ReadHierarchyTable(sourcePath);
        List<Iso639Set5HierarchyEntry> entries = new List<Iso639Set5HierarchyEntry>();
        List<Iso639Set5HierarchyRelationship> relationships =
            new List<Iso639Set5HierarchyRelationship>();
        HashSet<string> entryCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> relationshipKeys = new HashSet<string>(StringComparer.Ordinal);

        MatchCollection rowMatches = RowPattern.Matches(table);
        foreach (Match rowMatch in rowMatches)
        {
            MatchCollection cellMatches = CellPattern.Matches(rowMatch.Groups[1].Value);
            if (cellMatches.Count == 0)
            {
                continue;
            }

            if (cellMatches.Count == 1 && rowMatch.Groups[1].Value.Contains("iso639-5-Note-"))
            {
                continue;
            }

            if (cellMatches.Count != 6)
            {
                throw new InvalidDataException(
                    $"{sourcePath}: expected six cells in each ISO 639-5 hierarchy row.");
            }

            string code = ParseCode(sourcePath, ReadCellText(cellMatches[0].Groups[1].Value));
            if (!entryCodes.Add(code))
            {
                throw new InvalidDataException($"{sourcePath}: duplicate hierarchy row for code '{code}'.");
            }

            if (!recordsByCode.TryGetValue(code, out Iso639Set5Record? record))
            {
                throw new InvalidDataException(
                    $"{sourcePath}: hierarchy row code '{code}' is absent from the ISO 639-5 TSV.");
            }

            string[] englishNames = ParseNames(sourcePath, ReadCellText(cellMatches[1].Groups[1].Value), code);
            string[] frenchNames = ParseNames(sourcePath, ReadCellText(cellMatches[2].Groups[1].Value), code);
            EnsureNamesMatch(sourcePath, code, "English", englishNames, record.EnglishNames);
            EnsureNamesMatch(sourcePath, code, "French", frenchNames, record.FrenchNames);

            Iso639Set5GroupKind groupKind = ParseGroupKind(
                sourcePath,
                code,
                ReadCellLines(cellMatches[3].Groups[1].Value));
            string[] hierarchyPath = ParseHierarchyPath(
                sourcePath,
                code,
                ReadCellText(cellMatches[4].Groups[1].Value),
                recordsByCode);
            string[] noteIds = ParseNoteIds(cellMatches[5].Groups[1].Value);

            AddRelationships(hierarchyPath, relationshipKeys, relationships);
            entries.Add(new Iso639Set5HierarchyEntry(code, groupKind, hierarchyPath, noteIds));
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one ISO 639-5 hierarchy row.");
        }

        foreach (string code in recordsByCode.Keys)
        {
            if (!entryCodes.Contains(code))
            {
                throw new InvalidDataException(
                    $"{sourcePath}: ISO 639-5 TSV code '{code}' has no hierarchy row.");
            }
        }

        EnsureAcyclic(sourcePath, recordsByCode.Keys, relationships);
        return new Iso639Set5HierarchyData(entries.ToArray(), relationships.ToArray());
    }

    private static Dictionary<string, Iso639Set5Record> CreateRecordIndex(Iso639Set5Record[] records)
    {
        Dictionary<string, Iso639Set5Record> recordsByCode =
            new Dictionary<string, Iso639Set5Record>(StringComparer.Ordinal);
        foreach (Iso639Set5Record record in records)
        {
            if (!recordsByCode.TryAdd(record.Code, record))
            {
                throw new InvalidDataException($"ISO 639-5 TSV code '{record.Code}' is duplicated.");
            }
        }

        return recordsByCode;
    }

    private static string ReadHierarchyTable(string sourcePath)
    {
        byte[] bytes = File.ReadAllBytes(sourcePath);
        string content;
        try
        {
            content = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"{sourcePath}: source is not valid UTF-8.", exception);
        }

        int tableStart = content.IndexOf(TableMarker, StringComparison.Ordinal);
        if (tableStart < 0)
        {
            throw new InvalidDataException($"{sourcePath}: ISO 639-5 hierarchy table was not found.");
        }

        int tableEnd = content.IndexOf("</table>", tableStart, StringComparison.OrdinalIgnoreCase);
        if (tableEnd < 0)
        {
            throw new InvalidDataException($"{sourcePath}: ISO 639-5 hierarchy table is not terminated.");
        }

        return content.Substring(tableStart, tableEnd + "</table>".Length - tableStart);
    }

    private static string[] ReadCellLines(string cellHtml)
    {
        string withLineBreaks = BreakPattern.Replace(cellHtml, "\n");
        string withoutTags = TagPattern.Replace(withLineBreaks, "");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        string[] rawLines = decoded.Split('\n');
        List<string> lines = new List<string>();
        foreach (string rawLine in rawLines)
        {
            string line = WhitespacePattern.Replace(rawLine, " ").Trim();
            if (line.Length > 0)
            {
                lines.Add(line);
            }
        }

        return lines.ToArray();
    }

    private static string ReadCellText(string cellHtml)
    {
        return string.Join(" ", ReadCellLines(cellHtml));
    }

    private static string ParseCode(string sourcePath, string value)
    {
        if (value.Length != 3)
        {
            throw new InvalidDataException(
                $"{sourcePath}: hierarchy code '{value}' must contain exactly three letters.");
        }

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character < 'a' || character > 'z')
            {
                throw new InvalidDataException(
                    $"{sourcePath}: hierarchy code '{value}' must contain lowercase ASCII letters only.");
            }
        }

        return value;
    }

    private static string[] ParseNames(string sourcePath, string value, string code)
    {
        if (value.Length == 0)
        {
            throw new InvalidDataException($"{sourcePath}: hierarchy row '{code}' contains an empty name.");
        }

        string[] names = value.Split(';');
        for (int index = 0; index < names.Length; index++)
        {
            names[index] = names[index].Trim();
            if (names[index].Length == 0)
            {
                throw new InvalidDataException(
                    $"{sourcePath}: hierarchy row '{code}' contains an empty name.");
            }
        }

        return names;
    }

    private static void EnsureNamesMatch(
        string sourcePath,
        string code,
        string language,
        string[] hierarchyNames,
        string[] tsvNames)
    {
        if (hierarchyNames.Length != tsvNames.Length)
        {
            throw new InvalidDataException(
                $"{sourcePath}: {language} names for '{code}' do not match the ISO 639-5 TSV.");
        }

        for (int index = 0; index < hierarchyNames.Length; index++)
        {
            if (hierarchyNames[index] != tsvNames[index])
            {
                throw new InvalidDataException(
                    $"{sourcePath}: {language} names for '{code}' do not match the ISO 639-5 TSV.");
            }
        }
    }

    private static Iso639Set5GroupKind ParseGroupKind(
        string sourcePath,
        string code,
        string[] classificationLines)
    {
        if (classificationLines.Length == 0)
        {
            return Iso639Set5GroupKind.Family;
        }

        if (classificationLines.Length == 2 &&
            classificationLines[0] == "language group" &&
            classificationLines[1] == "groupe de langues")
        {
            return Iso639Set5GroupKind.LanguageGroup;
        }

        if (classificationLines.Length == 2 &&
            classificationLines[0] == "remainder group" &&
            classificationLines[1] == "groupe de langues restantes")
        {
            return Iso639Set5GroupKind.RemainderGroup;
        }

        throw new InvalidDataException(
            $"{sourcePath}: hierarchy row '{code}' has an unknown group classification.");
    }

    private static string[] ParseHierarchyPath(
        string sourcePath,
        string code,
        string value,
        Dictionary<string, Iso639Set5Record> recordsByCode)
    {
        if (value.Length == 0)
        {
            throw new InvalidDataException($"{sourcePath}: hierarchy row '{code}' has an empty path.");
        }

        string[] path = value.Split(':');
        HashSet<string> pathCodes = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < path.Length; index++)
        {
            string pathCode = ParseCode(sourcePath, path[index].Trim());
            if (!recordsByCode.ContainsKey(pathCode))
            {
                throw new InvalidDataException(
                    $"{sourcePath}: hierarchy path for '{code}' references unknown TSV code '{pathCode}'.");
            }

            if (!pathCodes.Add(pathCode))
            {
                throw new InvalidDataException(
                    $"{sourcePath}: hierarchy path for '{code}' repeats code '{pathCode}'.");
            }

            path[index] = pathCode;
        }

        if (path[path.Length - 1] != code)
        {
            throw new InvalidDataException(
                $"{sourcePath}: hierarchy path for '{code}' does not terminate at that code.");
        }

        return path;
    }

    private static string[] ParseNoteIds(string cellHtml)
    {
        MatchCollection matches = NoteReferencePattern.Matches(cellHtml);
        List<string> noteIds = new List<string>(matches.Count);
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in matches)
        {
            string noteId = match.Groups[1].Value.ToLowerInvariant();
            if (uniqueIds.Add(noteId))
            {
                noteIds.Add(noteId);
            }
        }

        return noteIds.ToArray();
    }

    private static void AddRelationships(
        string[] path,
        HashSet<string> relationshipKeys,
        List<Iso639Set5HierarchyRelationship> relationships)
    {
        for (int index = 1; index < path.Length; index++)
        {
            string parentCode = path[index - 1];
            string childCode = path[index];
            string key = parentCode + "\0" + childCode;
            if (relationshipKeys.Add(key))
            {
                relationships.Add(new Iso639Set5HierarchyRelationship(parentCode, childCode));
            }
        }
    }

    private static void EnsureAcyclic(
        string sourcePath,
        Dictionary<string, Iso639Set5Record>.KeyCollection codes,
        List<Iso639Set5HierarchyRelationship> relationships)
    {
        Dictionary<string, List<string>> childrenByParent =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (Iso639Set5HierarchyRelationship relationship in relationships)
        {
            if (!childrenByParent.TryGetValue(relationship.ParentCode, out List<string>? children))
            {
                children = new List<string>();
                childrenByParent.Add(relationship.ParentCode, children);
            }

            children.Add(relationship.ChildCode);
        }

        HashSet<string> visiting = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (string code in codes)
        {
            Visit(sourcePath, code, childrenByParent, visiting, visited);
        }
    }

    private static void Visit(
        string sourcePath,
        string code,
        Dictionary<string, List<string>> childrenByParent,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(code))
        {
            return;
        }

        if (!visiting.Add(code))
        {
            throw new InvalidDataException($"{sourcePath}: hierarchy contains a cycle at code '{code}'.");
        }

        if (childrenByParent.TryGetValue(code, out List<string>? children))
        {
            foreach (string child in children)
            {
                Visit(sourcePath, child, childrenByParent, visiting, visited);
            }
        }

        visiting.Remove(code);
        visited.Add(code);
    }
}
