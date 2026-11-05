using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace markdown.Tests;

public class CommonMarkSpecExample
{
    public int ExampleNumber;
    public string Section;
    public string Markdown;
    public string Html;
    public int StartLine;
    public int EndLine;

    public CommonMarkSpecExample(int exampleNumber, string section, string markdown, string html, int startLine, int endLine)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (markdown == null)
        {
            throw new ArgumentNullException("markdown");
        }

        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        if (exampleNumber < 1)
        {
            throw new ArgumentOutOfRangeException("exampleNumber");
        }

        if (startLine < 1)
        {
            throw new ArgumentOutOfRangeException("startLine");
        }

        if (endLine < startLine)
        {
            throw new ArgumentOutOfRangeException("endLine");
        }

        ExampleNumber = exampleNumber;
        Section = section;
        Markdown = markdown;
        Html = html;
        StartLine = startLine;
        EndLine = endLine;
    }

    public override string ToString()
    {
        return "Example " + ExampleNumber.ToString() + " (" + Section + ")";
    }
}

public class CommonMarkSpecFixtureLoader
{
    public static CommonMarkSpecExample[] LoadFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        return LoadFromString(File.ReadAllText(path));
    }

    public static CommonMarkSpecExample[] LoadFromString(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string trimmed = text.TrimStart();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal)
            && !trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return LoadFromSpecText(text);
        }

        using JsonDocument document = JsonDocument.Parse(text);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("The CommonMark fixture root must be a JSON array.");
        }

        int count = document.RootElement.GetArrayLength();
        CommonMarkSpecExample[] examples = new CommonMarkSpecExample[count];
        int index = 0;
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("Each CommonMark fixture entry must be a JSON object.");
            }

            examples[index] = new CommonMarkSpecExample(
                GetRequiredInteger(element, "example"),
                GetRequiredString(element, "section"),
                GetRequiredString(element, "markdown"),
                GetRequiredString(element, "html"),
                GetRequiredInteger(element, "start_line"),
                GetRequiredInteger(element, "end_line"));
            index = index + 1;
        }

        return examples;
    }

    public static CommonMarkSpecExample[] LoadFromSpecText(string text)
    {
        string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalized.Split(new char[] { '\n' });
        List<CommonMarkSpecExample> examples = new List<CommonMarkSpecExample>();
        string section = "";
        int index = 0;

        while (index < lines.Length)
        {
            string line = lines[index];
            if (IsSectionHeadingLine(line))
            {
                section = GetSectionHeadingText(line);
            }

            if (IsExampleOpeningFenceLine(line))
            {
                CommonMarkSpecExample example = ParseExample(lines, index, examples.Count + 1, section);
                examples.Add(example);
                index = example.EndLine;
            }
            else
            {
                index = index + 1;
            }
        }

        return examples.ToArray();
    }

    public static int GetRequiredInteger(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value)
            || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out int result))
        {
            throw new FormatException("CommonMark fixture field '" + name + "' must be an Int32.");
        }

        return result;
    }

    public static string GetRequiredString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value)
            || value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("CommonMark fixture field '" + name + "' must be a string.");
        }

        string? result = value.GetString();
        if (result == null)
        {
            throw new FormatException("CommonMark fixture field '" + name + "' must not be null.");
        }

        return result;
    }

    public static CommonMarkSpecExample ParseExample(string[] lines, int openingIndex, int exampleNumber, string section)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (openingIndex < 0 || openingIndex >= lines.Length)
        {
            throw new ArgumentOutOfRangeException("openingIndex");
        }

        if (exampleNumber < 1)
        {
            throw new ArgumentOutOfRangeException("exampleNumber");
        }

        string fence = GetOpeningFence(lines[openingIndex]);
        if (fence.Length == 0)
        {
            throw new FormatException("Opening line is not a CommonMark example fence.");
        }

        List<string> markdownLines = new List<string>();
        List<string> htmlLines = new List<string>();
        bool inHtml = false;
        int index = openingIndex + 1;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (IsExampleClosingFenceLine(line, fence))
            {
                if (!inHtml)
                {
                    throw new FormatException("CommonMark example is missing the Markdown/HTML separator.");
                }

                return new CommonMarkSpecExample(
                    exampleNumber,
                    section,
                    JoinFixtureLines(markdownLines),
                    JoinFixtureLines(htmlLines),
                    openingIndex + 1,
                    index + 1);
            }

            if (!inHtml && line == ".")
            {
                inHtml = true;
            }
            else if (inHtml)
            {
                htmlLines.Add(line);
            }
            else
            {
                markdownLines.Add(line);
            }

            index = index + 1;
        }

        throw new FormatException("CommonMark example is missing the closing fence.");
    }

    public static bool IsSectionHeadingLine(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int index = 0;
        while (index < line.Length && line[index] == '#')
        {
            index = index + 1;
        }

        return index > 0 && index < line.Length && line[index] == ' ';
    }

    public static string GetSectionHeadingText(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int index = 0;
        while (index < line.Length && line[index] == '#')
        {
            index = index + 1;
        }

        if (index < line.Length && line[index] == ' ')
        {
            index = index + 1;
        }

        return line.Substring(index).Trim();
    }

    public static bool IsExampleOpeningFenceLine(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        return GetOpeningFence(line).Length > 0;
    }

    public static string GetOpeningFence(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int count = CountOpeningBackticks(line);
        if (count < 3)
        {
            return "";
        }

        string rest = line.Substring(count).Trim();
        if (rest != "example")
        {
            return "";
        }

        return new string('`', count);
    }

    public static int CountOpeningBackticks(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int count = 0;
        while (count < line.Length && line[count] == '`')
        {
            count = count + 1;
        }

        return count;
    }

    public static bool IsExampleClosingFenceLine(string line, string fence)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (fence == null)
        {
            throw new ArgumentNullException("fence");
        }

        return line.Trim() == fence;
    }

    public static string JoinFixtureLines(List<string> lines)
    {
        if (lines == null)
        {
            throw new ArgumentNullException("lines");
        }

        if (lines.Count == 0)
        {
            return "";
        }

        StringWriter writer = new StringWriter();
        int index = 0;
        while (index < lines.Count)
        {
            writer.Write(lines[index]);
            writer.Write("\n");
            index = index + 1;
        }

        return writer.ToString();
    }
}
