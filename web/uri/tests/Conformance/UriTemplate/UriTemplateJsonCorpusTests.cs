using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace uri.Tests;

public class UriTemplateJsonCorpusTests
{
    private static readonly string[] CorpusFiles = new string[]
    {
        "spec-examples.json",
        "spec-examples-by-section.json",
        "extended-tests.json",
        "negative-tests.json"
    };

    public static IEnumerable<TestCaseData> CorpusCases()
    {
        int fileIndex = 0;
        while (fileIndex < CorpusFiles.Length)
        {
            string fileName = CorpusFiles[fileIndex];
            string path = GetCorpusPath(fileName);
            using (JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(path)))
            {
                foreach (JsonProperty groupProperty in
                    document.RootElement.EnumerateObject())
                {
                    JsonElement group = groupProperty.Value;
                    UriTemplateBinding[] bindings = ReadBindings(
                        group.GetProperty("variables"));
                    JsonElement testCases = group.GetProperty("testcases");
                    int caseIndex = 0;
                    foreach (JsonElement testCase in testCases.EnumerateArray())
                    {
                        JsonElement.ArrayEnumerator values =
                            testCase.EnumerateArray();
                        values.MoveNext();
                        string templateText = values.Current.GetString() ?? "";
                        values.MoveNext();
                        JsonElement expectedElement = values.Current;
                        bool failureExpected =
                            expectedElement.ValueKind == JsonValueKind.False;
                        string[] expected = ReadExpected(expectedElement);
                        TestCaseData data = new TestCaseData(
                            fileName,
                            groupProperty.Name,
                            caseIndex,
                            templateText,
                            bindings,
                            expected,
                            failureExpected);
                        data.SetName(CreateCaseName(
                            fileName,
                            groupProperty.Name,
                            caseIndex,
                            templateText));
                        yield return data;
                        caseIndex = caseIndex + 1;
                    }
                }
            }

            fileIndex = fileIndex + 1;
        }
    }

    [TestCaseSource(nameof(CorpusCases))]
    public void PublicApiExecutesCorpusVector(
        string fileName,
        string groupName,
        int caseIndex,
        string templateText,
        UriTemplateBinding[] bindings,
        string[] expected,
        bool failureExpected)
    {
        IUriTemplateParser parser = new UriTemplateParser();
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplateParseResult parsed = parser.TryParseTemplate(
            templateText,
            new UriTemplateParseOptions());

        if (failureExpected)
        {
            bool failed = !parsed.Success;
            UriTemplateExpansionResult expansion =
                new UriTemplateExpansionResult(true, "", 0, null);
            if (parsed.Success)
            {
                expansion = expander.TryExpandTemplate(
                    parsed.Template,
                    bindings,
                    new UriTemplateExpansionOptions());
                failed = !expansion.Success;
            }

            Assert.That(
                failed,
                Is.True,
                FailureMessage(
                    fileName,
                    groupName,
                    caseIndex,
                    templateText,
                    expansion.Text));
            return;
        }

        Assert.That(
            parsed.Success,
            Is.True,
            FailureMessage(
                fileName,
                groupName,
                caseIndex,
                templateText,
                DiagnosticText(parsed.Diagnostic)));
        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            parsed.Template,
            bindings,
            new UriTemplateExpansionOptions());
        Assert.That(
            result.Success,
            Is.True,
            FailureMessage(
                fileName,
                groupName,
                caseIndex,
                templateText,
                DiagnosticText(result.Diagnostic)));
        Assert.That(
            result.Text,
            Is.AnyOf(expected),
            FailureMessage(
                fileName,
                groupName,
                caseIndex,
                templateText,
                result.Text));
    }

    private static UriTemplateBinding[] ReadBindings(JsonElement variables)
    {
        List<UriTemplateBinding> bindings = new List<UriTemplateBinding>();
        foreach (JsonProperty property in variables.EnumerateObject())
        {
            UriTemplateValue value = ReadValue(property.Value);
            bindings.Add(new UriTemplateBinding(property.Name, value));
        }

        return bindings.ToArray();
    }

    private static UriTemplateValue ReadValue(JsonElement element)
    {
        UriTemplateValue value = new UriTemplateValue();
        if (element.ValueKind == JsonValueKind.Null)
        {
            return value;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            List<string> members = new List<string>();
            foreach (JsonElement member in element.EnumerateArray())
            {
                members.Add(ReadScalar(member));
            }

            value.Kind = UriTemplateValueKind.List;
            value.List = members.ToArray();
            return value;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            List<UriTemplateMapEntry> members =
                new List<UriTemplateMapEntry>();
            foreach (JsonProperty member in element.EnumerateObject())
            {
                bool defined = member.Value.ValueKind != JsonValueKind.Null;
                string memberValue = "";
                if (defined)
                {
                    memberValue = ReadScalar(member.Value);
                }

                members.Add(new UriTemplateMapEntry(
                    member.Name,
                    defined,
                    memberValue));
            }

            value.Kind = UriTemplateValueKind.Map;
            value.Map = members.ToArray();
            return value;
        }

        value.Kind = UriTemplateValueKind.Scalar;
        value.Scalar = ReadScalar(element);
        return value;
    }

    private static string ReadScalar(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? "";
        }

        return element.GetRawText();
    }

    private static string[] ReadExpected(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return new string[] { element.GetString() ?? "" };
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            List<string> values = new List<string>();
            foreach (JsonElement value in element.EnumerateArray())
            {
                values.Add(value.GetString() ?? "");
            }

            return values.ToArray();
        }

        return new string[0];
    }

    private static string GetCorpusPath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/test-data/uri-template",
            fileName));
    }

    private static string CreateCaseName(
        string fileName,
        string groupName,
        int caseIndex,
        string templateText)
    {
        return "RFC6570 " + fileName + " " + groupName + " " +
            caseIndex + " " + templateText;
    }

    private static string FailureMessage(
        string fileName,
        string groupName,
        int caseIndex,
        string templateText,
        string detail)
    {
        return fileName + " / " + groupName + " / " + caseIndex +
            " / " + templateText + " / " + detail;
    }

    private static string DiagnosticText(UriDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return "no diagnostic";
        }

        return diagnostic.Code + " at " + diagnostic.Range.Offset;
    }
}
