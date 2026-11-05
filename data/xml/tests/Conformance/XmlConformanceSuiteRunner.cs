using xml;

namespace xml.Tests;

internal enum XmlConformanceRunDisposition
{
    Passed,
    Failed,
    NotApplicable
}

internal sealed class XmlConformanceRunResult
{
    internal XmlConformanceTestCase Test = new XmlConformanceTestCase();
    internal bool Validating;
    internal XmlConformanceRunDisposition Disposition;
    internal string Reason = "";
    internal XmlDiagnostic[] Diagnostics = [];
    internal Exception? Exception;
}

internal sealed class XmlConformanceSuiteRunner
{
    internal const string HistoricalEditionReason = "The test is explicitly limited to XML 1.0 editions 1-4; this parser implements XML 1.0 Fifth Edition.";
    internal XmlConformanceSuiteAdapter Suite;
    internal XmlParser Parser;
    internal IXmlResourceResolver Resolver;

    internal XmlConformanceSuiteRunner(XmlConformanceSuiteAdapter suite)
    {
        Suite = suite;
        Parser = new XmlParser();
        Resolver = suite.CreateResourceResolver();
    }

    internal XmlConformanceRunResult[] RunAll()
    {
        List<XmlConformanceRunResult> results = new List<XmlConformanceRunResult>(Suite.TestCases.Length * 2);
        int index = 0;
        while (index < Suite.TestCases.Length)
        {
            results.Add(Run(Suite.TestCases[index], false));
            results.Add(Run(Suite.TestCases[index], true));
            index = index + 1;
        }

        return results.ToArray();
    }

    internal XmlConformanceRunResult Run(XmlConformanceTestCase test, bool validating)
    {
        XmlConformanceRunResult result = new XmlConformanceRunResult();
        result.Test = test;
        result.Validating = validating;
        if (test.Version == XmlVersion.Xml10 && test.Editions.Length != 0 && !test.Editions.Contains(5))
        {
            result.Disposition = XmlConformanceRunDisposition.NotApplicable;
            result.Reason = HistoricalEditionReason;
            return result;
        }

        try
        {
            XmlDocument document = Parser.ParseXmlDocument(Suite.CreateInput(test), Suite.CreateParserOptions(test, validating), Resolver, null, null);
            result.Diagnostics = document.Diagnostics == null ? [] : document.Diagnostics;
        }
        catch (Exception failure)
        {
            result.Exception = failure;
            XmlDiagnostic? diagnostic = ReadDiagnostic(failure);
            result.Diagnostics = diagnostic == null ? [] : [diagnostic];
        }

        Classify(result);
        return result;
    }

    internal void Classify(XmlConformanceRunResult result)
    {
        XmlConformanceExpectation expected = result.Test.ExpectedOutcome(result.Validating);
        if (expected == XmlConformanceExpectation.Accept)
        {
            if (result.Exception == null && !HasErrorDiagnostic(result.Diagnostics))
            {
                result.Disposition = XmlConformanceRunDisposition.Passed;
                return;
            }

            if (!result.Validating && result.Test.Classification == XmlConformanceClassification.Invalid && HasExpectedDiagnostic(result, [XmlErrorCategory.Validation]))
            {
                result.Disposition = XmlConformanceRunDisposition.Passed;
                return;
            }

            Fail(result, "Expected the document to be accepted.");
            return;
        }

        if (expected == XmlConformanceExpectation.RejectValidity)
        {
            if (HasExpectedDiagnostic(result, [XmlErrorCategory.Validation]))
            {
                result.Disposition = XmlConformanceRunDisposition.Passed;
                return;
            }

            Fail(result, "Expected a DTD validity diagnostic.");
            return;
        }

        if (expected == XmlConformanceExpectation.RejectWellFormedness)
        {
            if (HasExpectedDiagnostic(result, result.Test.ExpectedDiagnosticCategories(result.Validating)))
            {
                result.Disposition = XmlConformanceRunDisposition.Passed;
                return;
            }

            Fail(result, "Expected a well-formedness or namespace diagnostic.");
            return;
        }

        if (result.Exception == null || IsXmlProcessingException(result.Exception))
        {
            result.Disposition = XmlConformanceRunDisposition.Passed;
            return;
        }

        Fail(result, "The optional-error case raised a non-XML processing exception.");
    }

    internal bool HasExpectedDiagnostic(XmlConformanceRunResult result, XmlErrorCategory[] categories)
    {
        if (result.Exception == null && !HasErrorDiagnostic(result.Diagnostics))
        {
            return false;
        }

        return result.Diagnostics.Any(diagnostic => categories.Contains(diagnostic.Category));
    }

    internal bool HasErrorDiagnostic(XmlDiagnostic[] diagnostics)
    {
        return diagnostics.Any(diagnostic => diagnostic.Severity == XmlDiagnosticSeverity.Error || diagnostic.Severity == XmlDiagnosticSeverity.Fatal);
    }

    internal bool IsXmlProcessingException(Exception failure)
    {
        return failure is XmlException
            || failure is XmlWellFormednessException
            || failure is XmlNamespaceException
            || failure is XmlValidationException
            || failure is XmlResourceException
            || failure is XmlLimitException;
    }

    internal XmlDiagnostic? ReadDiagnostic(Exception failure)
    {
        if (failure is XmlException general)
        {
            return general.Diagnostic;
        }

        if (failure is XmlWellFormednessException wellFormedness)
        {
            return wellFormedness.Diagnostic;
        }

        if (failure is XmlNamespaceException namespaces)
        {
            return namespaces.Diagnostic;
        }

        if (failure is XmlValidationException validation)
        {
            return validation.Diagnostic;
        }

        if (failure is XmlResourceException resource)
        {
            return resource.Diagnostic;
        }

        return failure is XmlLimitException limit ? limit.Diagnostic : null;
    }

    internal void Fail(XmlConformanceRunResult result, string reason)
    {
        result.Disposition = XmlConformanceRunDisposition.Failed;
        string observed;
        if (result.Exception != null)
        {
            observed = result.Exception.GetType().Name + ": " + result.Exception.Message;
        }
        else if (result.Diagnostics.Length != 0)
        {
            observed = string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Category + "/" + diagnostic.Code + ": " + diagnostic.Message));
        }
        else
        {
            observed = "accepted without an error diagnostic";
        }

        result.Reason = reason + " Observed " + observed;
    }
}
