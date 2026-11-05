using System.Globalization;
using System.Text;

namespace UniversalHtml.Tests;

enum Html5libTreeDisposition
{
    Passed = 0,
    Failed = 1,
    Skipped = 2
}

class Html5libTreeCase
{
    public string FileName { get; set; }
    public int CaseNumber { get; set; }
    public string Input { get; set; }
    public string ContextNamespace { get; set; }
    public string ContextName { get; set; }
    public bool HasContext { get; set; }
    public bool Scripting { get; set; }
    public bool HasScripting { get; set; }
    public string Expected { get; set; }
    public bool RequiresScriptExecution { get; set; }
}

class Html5libTreeRun
{
    public string FileName { get; set; }
    public int CaseNumber { get; set; }
    public bool Scripting { get; set; }
    public Html5libTreeDisposition Disposition { get; set; }
    public string Detail { get; set; }
    public string InvariantViolation { get; set; }
    public HtmlInsertionMode[] InsertionModes { get; set; }
    public HtmlNamespace[] Namespaces { get; set; }
}

class Html5libTreeSummary
{
    public string Category { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public int GetTotal()
    {
        return this.Passed + this.Failed + this.Skipped;
    }
}

class Html5libTreeReport
{
    Html5libTreeRun[] runs;

    public Html5libTreeReport(Html5libTreeRun[] runs)
    {
        this.runs = runs;
    }

    public Html5libTreeRun[] GetRuns()
    {
        return this.runs.ToArray();
    }

    public Html5libTreeSummary GetOverallSummary()
    {
        return CreateSummary("Overall", this.runs);
    }

    public Html5libTreeSummary[] GetInsertionModeSummaries()
    {
        return Enum.GetValues<HtmlInsertionMode>()
            .Select(mode => new
            {
                Mode = mode,
                Runs = this.runs.Where(value =>
                    value.InsertionModes.Contains(mode)).ToArray()
            })
            .Where(value => value.Runs.Length > 0)
            .Select(value => CreateSummary(value.Mode.ToString(), value.Runs))
            .ToArray();
    }

    public Html5libTreeSummary[] GetNamespaceSummaries()
    {
        return Enum.GetValues<HtmlNamespace>()
            .Select(namespaceKind => new
            {
                Namespace = namespaceKind,
                Runs = this.runs.Where(value =>
                    value.Namespaces.Contains(namespaceKind)).ToArray()
            })
            .Where(value => value.Runs.Length > 0)
            .Select(value => CreateSummary(value.Namespace.ToString(),
                value.Runs))
            .ToArray();
    }

    public string FormatSummary()
    {
        StringBuilder output = new StringBuilder();
        AppendSummary(output, this.GetOverallSummary());
        output.AppendLine("Insertion modes:");
        foreach (Html5libTreeSummary summary
            in this.GetInsertionModeSummaries())
        {
            AppendSummary(output, summary);
        }

        output.AppendLine("Namespaces:");
        foreach (Html5libTreeSummary summary in this.GetNamespaceSummaries())
        {
            AppendSummary(output, summary);
        }

        return output.ToString();
    }

    static Html5libTreeSummary CreateSummary(string category,
        Html5libTreeRun[] values)
    {
        return new Html5libTreeSummary
        {
            Category = category,
            Passed = values.Count(value =>
                value.Disposition == Html5libTreeDisposition.Passed),
            Failed = values.Count(value =>
                value.Disposition == Html5libTreeDisposition.Failed),
            Skipped = values.Count(value =>
                value.Disposition == Html5libTreeDisposition.Skipped)
        };
    }

    static void AppendSummary(StringBuilder output,
        Html5libTreeSummary summary)
    {
        output.Append(summary.Category);
        output.Append(": passed=");
        output.Append(summary.Passed.ToString(CultureInfo.InvariantCulture));
        output.Append(", failed=");
        output.Append(summary.Failed.ToString(CultureInfo.InvariantCulture));
        output.Append(", skipped=");
        output.Append(summary.Skipped.ToString(CultureInfo.InvariantCulture));
        output.AppendLine();
    }
}

static class Html5libTreeConstructionAdapter
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";
    const string MathMlNamespaceUri = "http://www.w3.org/1998/Math/MathML";
    const string XLinkNamespaceUri = "http://www.w3.org/1999/xlink";
    const string XmlNamespaceUri = "http://www.w3.org/XML/1998/namespace";
    const string XmlnsNamespaceUri = "http://www.w3.org/2000/xmlns/";
    const string ScriptSkip =
        "The fixture requires script execution, which UniversalHtml intentionally does not provide.";

    public static Html5libTreeReport RunPinnedSuite()
    {
        string directory = Path.Combine(FindRepositoryRoot(), "tests", "data",
            "html5lib", "tree-construction");
        List<Html5libTreeRun> runs = new List<Html5libTreeRun>();
        foreach (string path in Directory.GetFiles(directory, "*.dat",
            SearchOption.AllDirectories).OrderBy(value => value,
                StringComparer.Ordinal))
        {
            Html5libTreeCase[] cases = ReadCases(path,
                path.Contains(Path.DirectorySeparatorChar + "scripted"
                    + Path.DirectorySeparatorChar, StringComparison.Ordinal));
            foreach (Html5libTreeCase value in cases)
            {
                AddRuns(value, runs);
            }
        }

        return new Html5libTreeReport(runs.ToArray());
    }

    static void AddRuns(Html5libTreeCase value,
        List<Html5libTreeRun> runs)
    {
        if (value.RequiresScriptExecution)
        {
            runs.Add(CreateSkippedRun(value));
            return;
        }

        if (value.HasScripting)
        {
            runs.Add(RunCase(value, value.Scripting));
            return;
        }

        runs.Add(RunCase(value, false));
        runs.Add(RunCase(value, true));
    }

    static Html5libTreeRun RunCase(Html5libTreeCase value, bool scripting)
    {
        HtmlParser parser = null;
        try
        {
            HtmlParseOptions options = HtmlParseOptions.CreateDefault();
            options.SetScriptingEnabled(scripting);
            if (value.HasContext)
            {
                HtmlDocument document = CreateDocument();
                HtmlNode context = CreateContext(document, value);
                parser = HtmlParser.CreateFragment(options, context,
                    CancellationToken.None);
            }
            else
            {
                parser = HtmlParser.Create(options, CancellationToken.None);
            }

            parser.Feed(value.Input.AsSpan(), true);
            HtmlParseResult result = parser.Complete();
            string actual = Dump(value.HasContext
                ? result.GetFragment() : result.GetDocument().GetRoot());
            string invariantViolation = value.HasContext
                ? HtmlDomInvariant.FindViolation(result.GetDocument(),
                    result.GetFragment())
                : HtmlDomInvariant.FindViolation(result.GetDocument());
            string difference = FindDifference(value.Expected, actual);
            if (!result.IsComplete())
            {
                difference = "Parser status was incomplete. "
                    + (difference ?? "");
            }

            return new Html5libTreeRun
            {
                FileName = value.FileName,
                CaseNumber = value.CaseNumber,
                Scripting = scripting,
                Disposition = difference == null
                    ? Html5libTreeDisposition.Passed
                    : Html5libTreeDisposition.Failed,
                Detail = difference ?? "",
                InvariantViolation = invariantViolation,
                InsertionModes = parser.GetVisitedInsertionModes(),
                Namespaces = parser.GetVisitedNamespaces()
            };
        }
        catch (Exception exception)
        {
            return new Html5libTreeRun
            {
                FileName = value.FileName,
                CaseNumber = value.CaseNumber,
                Scripting = scripting,
                Disposition = Html5libTreeDisposition.Failed,
                Detail = exception.GetType().Name + ": " + exception.Message,
                InvariantViolation = "Parser execution failed before invariants could be checked.",
                InsertionModes = parser == null
                    ? Array.Empty<HtmlInsertionMode>()
                    : parser.GetVisitedInsertionModes(),
                Namespaces = parser == null
                    ? Array.Empty<HtmlNamespace>()
                    : parser.GetVisitedNamespaces()
            };
        }
    }

    static Html5libTreeRun CreateSkippedRun(Html5libTreeCase value)
    {
        HtmlParser parser = null;
        try
        {
            HtmlParseOptions options = HtmlParseOptions.CreateDefault();
            options.SetScriptingEnabled(true);
            parser = HtmlParser.Create(options, CancellationToken.None);
            parser.Feed(value.Input.AsSpan(), true);
            HtmlParseResult result = parser.Complete();
            return new Html5libTreeRun
            {
                FileName = value.FileName,
                CaseNumber = value.CaseNumber,
                Scripting = true,
                Disposition = Html5libTreeDisposition.Skipped,
                Detail = ScriptSkip,
                InvariantViolation = HtmlDomInvariant.FindViolation(
                    result.GetDocument()),
                InsertionModes = parser.GetVisitedInsertionModes(),
                Namespaces = parser.GetVisitedNamespaces()
            };
        }
        catch (Exception exception)
        {
            return new Html5libTreeRun
            {
                FileName = value.FileName,
                CaseNumber = value.CaseNumber,
                Scripting = true,
                Disposition = Html5libTreeDisposition.Skipped,
                Detail = ScriptSkip,
                InvariantViolation = exception.GetType().Name + ": "
                    + exception.Message,
                InsertionModes = parser == null
                    ? Array.Empty<HtmlInsertionMode>()
                    : parser.GetVisitedInsertionModes(),
                Namespaces = parser == null
                    ? ReadExpectedNamespaces(value.Expected)
                    : parser.GetVisitedNamespaces()
            };
        }
    }

    static Html5libTreeCase[] ReadCases(string path,
        bool requiresScriptExecution)
    {
        string content = File.ReadAllText(path);
        List<Html5libTreeCase> cases = new List<Html5libTreeCase>();
        int start = content.IndexOf("#data\n", StringComparison.Ordinal);
        int caseNumber = 0;
        while (start >= 0)
        {
            int dataStart = start + 6;
            int errors = content.IndexOf("#errors\n", dataStart,
                StringComparison.Ordinal);
            int document = content.IndexOf("#document\n", errors + 8,
                StringComparison.Ordinal);
            if (errors < 0 || document < 0)
            {
                throw new InvalidDataException(
                    "Malformed tree-construction fixture " + path + ".");
            }

            int next = content.IndexOf("\n\n#data\n", document,
                StringComparison.Ordinal);
            int end = next < 0 ? content.Length : next;
            int dataEnd = errors;
            if (dataEnd > dataStart && content[dataEnd - 1] == '\n')
            {
                dataEnd--;
            }

            if (errors < dataStart || end < document + 10)
            {
                throw new InvalidDataException(
                    "Invalid section boundary in " + path + " case "
                    + caseNumber.ToString(CultureInfo.InvariantCulture) + ".");
            }

            string metadata = content.Substring(errors + 8,
                document - errors - 8).TrimEnd('\n');
            string expected = content.Substring(document + 10,
                end - document - 10).TrimEnd('\n');
            Html5libTreeCase value = new Html5libTreeCase
            {
                FileName = Path.GetRelativePath(
                    Path.Combine(FindRepositoryRoot(), "tests", "data",
                        "html5lib", "tree-construction"), path),
                CaseNumber = caseNumber,
                Input = content.Substring(dataStart, dataEnd - dataStart),
                Expected = expected,
                RequiresScriptExecution = requiresScriptExecution
            };
            ReadMetadata(metadata, value);
            cases.Add(value);
            caseNumber++;
            start = next < 0 ? -1 : next + 2;
        }

        return cases.ToArray();
    }

    static void ReadMetadata(string metadata, Html5libTreeCase value)
    {
        value.HasContext = false;
        value.ContextNamespace = "html";
        value.ContextName = "";
        value.Scripting = false;
        value.HasScripting = false;
        string marker = "#document-fragment\n";
        int fragment = metadata.IndexOf(marker, StringComparison.Ordinal);
        if (fragment >= 0)
        {
            int nameStart = fragment + marker.Length;
            int nameEnd = metadata.IndexOf('\n', nameStart);
            string context = nameEnd < 0
                ? metadata.Substring(nameStart)
                : metadata.Substring(nameStart, nameEnd - nameStart);
            value.HasContext = true;
            if (context.StartsWith("svg ", StringComparison.Ordinal))
            {
                value.ContextNamespace = "svg";
                value.ContextName = context.Substring(4);
            }
            else if (context.StartsWith("math ",
                StringComparison.Ordinal))
            {
                value.ContextNamespace = "math";
                value.ContextName = context.Substring(5);
            }
            else
            {
                value.ContextName = context;
            }
        }

        if (metadata.Contains("#script-on", StringComparison.Ordinal))
        {
            value.Scripting = true;
            value.HasScripting = true;
        }
        else if (metadata.Contains("#script-off", StringComparison.Ordinal))
        {
            value.Scripting = false;
            value.HasScripting = true;
        }
    }

    static HtmlNode CreateContext(HtmlDocument document,
        Html5libTreeCase value)
    {
        if (value.ContextNamespace == "svg")
        {
            return document.CreateElementInNamespace(SvgNamespaceUri,
                value.ContextName);
        }

        if (value.ContextNamespace == "math")
        {
            return document.CreateElementInNamespace(MathMlNamespaceUri,
                value.ContextName);
        }

        return document.CreateElement(value.ContextName);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static string Dump(HtmlNode root)
    {
        StringBuilder output = new StringBuilder();
        HtmlNode[] children = root.GetChildren();
        foreach (HtmlNode child in children)
        {
            AppendNode(output, child, 0);
        }

        if (output.Length > 0)
        {
            output.Length--;
        }

        return output.ToString();
    }

    static void AppendNode(StringBuilder output, HtmlNode node, int depth)
    {
        if (node.GetKind() == HtmlNodeKind.Element)
        {
            AppendLine(output, depth, "<" + GetElementName(node) + ">");
            foreach (HtmlAttribute attribute in node.GetAttributes()
                .OrderBy(GetAttributeName, StringComparer.Ordinal))
            {
                AppendLine(output, depth + 1, GetAttributeName(attribute)
                    + "=\"" + attribute.GetValue() + "\"");
            }

            HtmlNode content = node.GetTemplateContent();
            if (content != null)
            {
                AppendLine(output, depth + 1, "content");
                foreach (HtmlNode child in content.GetChildren())
                {
                    AppendNode(output, child, depth + 2);
                }
            }
            else
            {
                foreach (HtmlNode child in node.GetChildren())
                {
                    AppendNode(output, child, depth + 1);
                }
            }

            return;
        }

        if (node.GetKind() == HtmlNodeKind.Text)
        {
            AppendLine(output, depth, "\"" + node.GetData() + "\"");
            return;
        }

        if (node.GetKind() == HtmlNodeKind.Comment)
        {
            AppendLine(output, depth, "<!-- " + node.GetData() + " -->");
            return;
        }

        if (node.GetKind() == HtmlNodeKind.DocumentType)
        {
            AppendLine(output, depth, GetDoctype(node));
        }
    }

    static void AppendLine(StringBuilder output, int depth, string value)
    {
        output.Append("| ");
        output.Append(' ', depth * 2);
        output.Append(value);
        output.Append('\n');
    }

    static string GetElementName(HtmlNode node)
    {
        if (node.GetNamespaceUri() == SvgNamespaceUri)
        {
            return "svg " + node.GetLocalName();
        }

        if (node.GetNamespaceUri() == MathMlNamespaceUri)
        {
            return "math " + node.GetLocalName();
        }

        return node.GetLocalName();
    }

    static string GetAttributeName(HtmlAttribute attribute)
    {
        string namespaceUri = attribute.GetNamespaceUri();
        if (namespaceUri == XLinkNamespaceUri)
        {
            return "xlink " + attribute.GetLocalName();
        }

        if (namespaceUri == XmlNamespaceUri)
        {
            return "xml " + attribute.GetLocalName();
        }

        if (namespaceUri == XmlnsNamespaceUri)
        {
            return "xmlns " + attribute.GetLocalName();
        }

        return attribute.GetLocalName();
    }

    static string GetDoctype(HtmlNode node)
    {
        string result = "<!DOCTYPE " + node.GetName();
        if (node.HasPublicIdentifierStorage()
            || node.HasSystemIdentifierStorage())
        {
            result += " \"" + (node.HasPublicIdentifierStorage()
                ? node.GetPublicIdentifierStorage() : "") + "\" \""
                + (node.HasSystemIdentifierStorage()
                    ? node.GetSystemIdentifierStorage() : "") + "\"";
        }

        return result + ">";
    }

    static string FindDifference(string expected, string actual)
    {
        if (expected == actual)
        {
            return null;
        }

        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        int count = expectedLines.Length < actualLines.Length
            ? expectedLines.Length : actualLines.Length;
        int index = 0;
        while (index < count && expectedLines[index] == actualLines[index])
        {
            index++;
        }

        if (index < count)
        {
            return "Line " + (index + 1).ToString(
                CultureInfo.InvariantCulture) + " expected "
                + Quote(expectedLines[index]) + " but received "
                + Quote(actualLines[index]) + ".";
        }

        return "Expected " + expectedLines.Length.ToString(
            CultureInfo.InvariantCulture) + " lines but received "
            + actualLines.Length.ToString(CultureInfo.InvariantCulture) + ".";
    }

    static HtmlNamespace[] ReadExpectedNamespaces(string expected)
    {
        List<HtmlNamespace> result = new List<HtmlNamespace>
        {
            HtmlNamespace.Html
        };
        if (expected.Contains("<svg ", StringComparison.Ordinal))
        {
            result.Add(HtmlNamespace.Svg);
        }

        if (expected.Contains("<math ", StringComparison.Ordinal))
        {
            result.Add(HtmlNamespace.MathMl);
        }

        return result.ToArray();
    }

    static string Quote(string value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value);
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "tests", "data",
                "html5lib", "tree-construction", "tests1.dat")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the pinned html5lib tree-construction fixtures.");
    }
}
