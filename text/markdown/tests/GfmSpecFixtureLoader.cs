using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace markdown.Tests;

public class GfmSpecExample
{
    public int ExampleNumber;
    public string Section;
    public string Markdown;
    public string Html;
    public int StartLine;
    public int EndLine;

    public GfmSpecExample(int exampleNumber, string section, string markdown, string html, int startLine, int endLine)
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

public class GfmSpecFixtureLoader
{
    public const string ExamplePrefix = "<div class=\"example\" id=\"example-";
    public const string MarkdownPrefix = "<pre><code class=\"language-markdown\">";
    public const string HtmlPrefix = "<pre><code class=\"language-html\">";
    public const string CodeSuffix = "</code></pre>";

    public static GfmSpecExample[] LoadFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        return LoadFromString(File.ReadAllText(path));
    }

    public static GfmSpecExample[] LoadFromString(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        List<GfmSpecExample> examples = new List<GfmSpecExample>();
        int searchIndex = 0;
        while (searchIndex < text.Length)
        {
            int exampleStart = text.IndexOf(ExamplePrefix, searchIndex, StringComparison.Ordinal);
            if (exampleStart < 0)
            {
                break;
            }

            int numberStart = exampleStart + ExamplePrefix.Length;
            int numberEnd = text.IndexOf('"', numberStart);
            if (numberEnd < 0 || !int.TryParse(text.Substring(numberStart, numberEnd - numberStart), out int exampleNumber))
            {
                throw new FormatException("The GFM example id must end with a numeric example number.");
            }

            int markdownStart = FindContentStart(text, MarkdownPrefix, numberEnd);
            int markdownEnd = FindContentEnd(text, markdownStart);
            int htmlStart = FindContentStart(text, HtmlPrefix, markdownEnd + CodeSuffix.Length);
            int htmlEnd = FindContentEnd(text, htmlStart);
            examples.Add(new GfmSpecExample(
                exampleNumber,
                GetSectionAt(text, exampleStart),
                DecodeCode(text.Substring(markdownStart, markdownEnd - markdownStart)),
                DecodeCode(text.Substring(htmlStart, htmlEnd - htmlStart)),
                GetLineNumberAt(text, exampleStart),
                GetLineNumberAt(text, htmlEnd + CodeSuffix.Length)));
            searchIndex = htmlEnd + CodeSuffix.Length;
        }

        if (examples.Count == 0 && text.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new FormatException("The GFM HTML fixture does not contain any examples.");
        }

        return examples.ToArray();
    }

    public static int FindContentStart(string text, string prefix, int searchIndex)
    {
        int start = text.IndexOf(prefix, searchIndex, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new FormatException("A GFM example is missing a code column.");
        }

        return start + prefix.Length;
    }

    public static int FindContentEnd(string text, int contentStart)
    {
        int end = text.IndexOf(CodeSuffix, contentStart, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new FormatException("A GFM example code column is not terminated.");
        }

        return end;
    }

    public static string DecodeCode(string encoded)
    {
        string withoutSpaceMarkup = encoded.Replace("<span class=\"space\"> </span>", " ");
        return WebUtility.HtmlDecode(withoutSpaceMarkup).Replace('→', '\t');
    }

    public static string GetSectionAt(string text, int exampleStart)
    {
        int headingStart = text.LastIndexOf("<h2 ", exampleStart, StringComparison.Ordinal);
        if (headingStart < 0)
        {
            throw new FormatException("A GFM example is not contained in a section.");
        }

        int contentStart = text.IndexOf('>', headingStart);
        int contentEnd = text.IndexOf("</h2>", contentStart, StringComparison.Ordinal);
        if (contentStart < 0 || contentEnd < 0 || contentEnd > exampleStart)
        {
            throw new FormatException("A GFM section heading is malformed.");
        }

        string content = text.Substring(contentStart + 1, contentEnd - contentStart - 1);
        content = Regex.Replace(content, "<span class=\"number\">.*?</span>", "", RegexOptions.Singleline);
        content = Regex.Replace(content, "<[^>]+>", "", RegexOptions.Singleline);
        return WebUtility.HtmlDecode(content).Trim();
    }

    public static int GetLineNumberAt(string text, int index)
    {
        int line = 1;
        int current = 0;
        while (current < index && current < text.Length)
        {
            if (text[current] == '\n')
            {
                line = line + 1;
            }

            current = current + 1;
        }

        return line;
    }

    public static bool IsExtensionSection(string section)
    {
        return section == "Tables (extension)"
            || section == "Task list items (extension)"
            || section == "Strikethrough (extension)"
            || section == "Autolinks (extension)"
            || section == "Disallowed Raw HTML (extension)";
    }
}
