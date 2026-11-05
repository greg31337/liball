using System.Text.RegularExpressions;
using asn1;

namespace asn1.Tests;

public class RfcModuleCorpusTests
{
    [Test]
    public void Rfc5911Rfc5912AndRfc6268CompileAsOneModuleCorpus()
    {
        string[] selectedModules = [
            "AlgorithmInformation-2009",
            "PKIX-CommonTypes-2009",
            "BinarySigningTimeModule-2010"
        ];
        AsnSourceUnit[] rfcSources = ExtractCorpus(
            "RFC-5911.txt",
            "RFC-5912.txt",
            "RFC-6268.txt")
            .Where(item => selectedModules.Contains(ReadModuleName(item.Text)))
            .ToArray();
        AsnSourceUnit[] sources = [.. rfcSources, CreatePkixSupportModule()];

        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput { SourceUnits = sources });

        Assert.That(rfcSources, Has.Length.EqualTo(3));
        Assert.That(rfcSources.Select(item => item.Name),
            Has.Some.StartsWith("RFC-5911.txt:"));
        Assert.That(rfcSources.Select(item => item.Name),
            Has.Some.StartsWith("RFC-5912.txt:"));
        Assert.That(rfcSources.Select(item => item.Name),
            Has.Some.StartsWith("RFC-6268.txt:"));
        Assert.That(result.Success, Is.True,
            Format(result.Diagnostics, sources));
        Assert.That(result.Modules.Select(item => item.Name),
            Does.Contain("AlgorithmInformation-2009"));
        Assert.That(result.Modules.Select(item => item.Name),
            Does.Contain("PKIX-CommonTypes-2009"));
        Assert.That(result.Modules.Select(item => item.Name),
            Does.Contain("BinarySigningTimeModule-2010"));

        AsnAssignment algorithmClass = result.Assignments.Single(item =>
            item.Name == "ALGORITHM" && item.Kind == AsnAssignmentKind.ObjectClass);
        Assert.That(algorithmClass.ObjectClass?.SyntaxTokens,
            Does.Contain("IDENTIFIER"));
        Assert.That(algorithmClass.ObjectClass?.Fields.Select(item => item.Name),
            Does.Contain("&id"));

        AsnAssignment attributeSet = result.Assignments.Single(item =>
            item.Name == "AttributeSet" && item.Kind == AsnAssignmentKind.Type);
        Assert.That(attributeSet.Parameters.Select(item => item.Kind),
            Does.Contain(AsnParameterKind.ObjectSet));

        AsnAssignment securityCategory = result.Assignments.Single(item =>
            item.Name == "SecurityCategory" && item.Kind == AsnAssignmentKind.Type);
        Assert.That(securityCategory.Type?.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "type", "value" }));

        AsnAssignment binarySigningTime = result.Assignments.Single(item =>
            item.Name == "aa-binarySigningTime" && item.Kind == AsnAssignmentKind.Object);
        Assert.That(binarySigningTime.Object?.FieldSettings.Select(item => item.FieldName),
            Does.Contain("&id"));
    }

    private static AsnSourceUnit CreatePkixSupportModule()
    {
        return new AsnSourceUnit
        {
            Name = "PKIX1Implicit-2009-support.asn",
            Text = "PKIX1Implicit-2009 "
                + "{iso(1) identified-organization(3) dod(6) internet(1) "
                + "security(5) mechanisms(5) pkix(7) id-mod(0) "
                + "id-mod-pkix1-implicit-02(59)} "
                + "DEFINITIONS IMPLICIT TAGS ::= BEGIN "
                + "KeyUsage ::= BIT STRING END"
        };
    }

    private static AsnSourceUnit[] ExtractCorpus(params string[] fileNames)
    {
        Dictionary<string, AsnSourceUnit> modules = new(StringComparer.Ordinal);
        foreach (string fileName in fileNames)
        {
            string path = FindSpecification(fileName);
            foreach (AsnSourceUnit module in ExtractModules(path, fileName))
            {
                string moduleName = ReadModuleName(module.Text);
                if (moduleName != "ModuleNumbers" && !modules.ContainsKey(moduleName))
                {
                    modules.Add(moduleName, module);
                }
            }
        }

        return modules.Values.ToArray();
    }

    private static AsnSourceUnit[] ExtractModules(string path, string sourceName)
    {
        string[] lines = File.ReadAllLines(path);
        List<AsnSourceUnit> modules = [];
        for (int definitionsIndex = 0; definitionsIndex < lines.Length; definitionsIndex++)
        {
            if (!lines[definitionsIndex].Contains("DEFINITIONS", StringComparison.Ordinal)
                || !lines[definitionsIndex].Contains("::=", StringComparison.Ordinal))
            {
                continue;
            }

            int moduleStart = FindModuleStart(lines, definitionsIndex);
            int moduleEnd = FindModuleEnd(lines, definitionsIndex);
            if (moduleStart < 0 || moduleEnd < 0)
            {
                continue;
            }

            string text = CleanModule(lines[moduleStart..(moduleEnd + 1)]);
            string moduleName = ReadModuleName(text);
            modules.Add(new AsnSourceUnit
            {
                Name = sourceName + ":" + moduleName,
                Text = text
            });
        }

        return modules.ToArray();
    }

    private static int FindModuleStart(string[] lines, int definitionsIndex)
    {
        int index = definitionsIndex - 1;
        int lowerBound = Math.Max(0, definitionsIndex - 80);
        while (index >= lowerBound)
        {
            string candidate = lines[index].Trim().TrimStart('\f');
            Match match = Regex.Match(candidate,
                "^([A-Za-z][A-Za-z0-9-]*)(?:\\s*\\{|$)");
            if (match.Success)
            {
                return index;
            }

            index--;
        }

        return -1;
    }

    private static int FindModuleEnd(string[] lines, int definitionsIndex)
    {
        int index = definitionsIndex + 1;
        while (index < lines.Length)
        {
            if (lines[index].Trim().TrimStart('\f') == "END")
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private static string CleanModule(string[] lines)
    {
        List<string> cleaned = [];
        foreach (string sourceLine in lines)
        {
            string line = sourceLine.TrimStart('\f');
            if (line.Contains("[Page ", StringComparison.Ordinal)
                || Regex.IsMatch(line.TrimStart(), "^RFC [0-9]+\\s"))
            {
                continue;
            }

            cleaned.Add(line);
        }

        return string.Join("\n", cleaned);
    }

    private static string ReadModuleName(string text)
    {
        Match match = Regex.Match(text,
            "^\\s*([A-Za-z][A-Za-z0-9-]*)",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static string FindSpecification(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "specs", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Cannot locate vendored specification.", fileName);
    }

    private static string Format(AsnDiagnostic[] diagnostics, AsnSourceUnit[] sources)
    {
        return string.Join(" | ", diagnostics.Select(item =>
            item.Code + ":" + item.Span?.Start.SourceName + ":"
                + item.Span?.Start.Line + ":" + item.Span?.Start.Column
                + ":" + ReadDiagnosticLine(item, sources) + ":" + item.Message));
    }

    private static string ReadDiagnosticLine(
        AsnDiagnostic diagnostic,
        AsnSourceUnit[] sources)
    {
        AsnSourceUnit? source = sources.SingleOrDefault(item =>
            item.Name == diagnostic.Span?.Start.SourceName);
        if (source == null || diagnostic.Span == null)
        {
            return "";
        }

        string[] lines = source.Text.Split('\n');
        int index = diagnostic.Span.Start.Line - 1;
        return index < 0 || index >= lines.Length ? "" : lines[index].Trim();
    }

}
