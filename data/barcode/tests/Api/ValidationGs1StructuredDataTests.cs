namespace barcode.Tests;

public class ValidationGs1StructuredDataTests
{
    [Test]
    public void ValidationTypesHoldTargetsIssuesMetricsAndDiagnostics()
    {
        BarcodeEncodeRequest data = new BarcodeEncodeRequest();
        BarcodeSymbol symbol = new BarcodeSymbol();
        BarcodeRenderRequest render = new BarcodeRenderRequest();
        BarcodeImage image = new BarcodeImage();
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = BarcodeFormat.Itf14;
        request.Data = data;
        request.Symbol = symbol;
        request.Render = render;
        request.Image = image;
        request.Mode = BarcodeValidationMode.Tolerant;
        request.ValidateGs1Profile = true;
        BarcodeValidationIssue issue = new BarcodeValidationIssue();
        issue.Severity = BarcodeValidationSeverity.Error;
        issue.Code = "INVALID_CHECK_DIGIT";
        issue.Message = "The check digit does not match.";
        issue.Standard = "GS1 General Specifications 26.0";
        issue.Field = "01";
        issue.Offset = 15;
        issue.Expected = "3";
        issue.Observed = "4";
        BarcodeQualityMetric metric = new BarcodeQualityMetric();
        metric.Name = "contrast";
        metric.Value = 0.65;
        metric.Threshold = 0.5;
        metric.MeetsThreshold = true;
        metric.IsHeuristic = true;
        metric.Interpretation = "Adequate contrast diagnostic.";
        BarcodeValidationIssue[] issues = new BarcodeValidationIssue[] { issue };
        BarcodeQualityMetric[] metrics = new BarcodeQualityMetric[] { metric };
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        BarcodeDiagnostic[] diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeValidationReport report = new BarcodeValidationReport();
        report.IsConformant = false;
        report.Mode = BarcodeValidationMode.Tolerant;
        report.Standard = "ISO/IEC 16390:2007";
        report.Profile = "ITF-14";
        report.Issues = issues;
        report.Metrics = metrics;
        report.Diagnostics = diagnostics;

        Assert.Multiple(() =>
        {
            Assert.That(request.Format, Is.EqualTo(BarcodeFormat.Itf14));
            Assert.That(request.Data, Is.SameAs(data));
            Assert.That(request.Symbol, Is.SameAs(symbol));
            Assert.That(request.Render, Is.SameAs(render));
            Assert.That(request.Image, Is.SameAs(image));
            Assert.That(request.Mode, Is.EqualTo(BarcodeValidationMode.Tolerant));
            Assert.That(request.ValidateGs1Profile, Is.True);
            Assert.That(issue.Severity, Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(issue.Code, Is.EqualTo("INVALID_CHECK_DIGIT"));
            Assert.That(issue.Message, Is.EqualTo("The check digit does not match."));
            Assert.That(issue.Standard, Is.EqualTo("GS1 General Specifications 26.0"));
            Assert.That(issue.Field, Is.EqualTo("01"));
            Assert.That(issue.Offset, Is.EqualTo(15));
            Assert.That(issue.Expected, Is.EqualTo("3"));
            Assert.That(issue.Observed, Is.EqualTo("4"));
            Assert.That(metric.Name, Is.EqualTo("contrast"));
            Assert.That(metric.Value, Is.EqualTo(0.65));
            Assert.That(metric.Threshold, Is.EqualTo(0.5));
            Assert.That(metric.MeetsThreshold, Is.True);
            Assert.That(metric.IsHeuristic, Is.True);
            Assert.That(metric.Interpretation, Is.EqualTo("Adequate contrast diagnostic."));
            Assert.That(report.IsConformant, Is.False);
            Assert.That(report.Mode, Is.EqualTo(BarcodeValidationMode.Tolerant));
            Assert.That(report.Standard, Is.EqualTo("ISO/IEC 16390:2007"));
            Assert.That(report.Profile, Is.EqualTo("ITF-14"));
            Assert.That(report.Issues, Is.SameAs(issues));
            Assert.That(report.Metrics, Is.SameAs(metrics));
            Assert.That(report.Diagnostics, Is.SameAs(diagnostics));
        });
    }

    [Test]
    public void ChecksumTypesHoldEverySourceAndOutcome()
    {
        byte[] bytes = new byte[] { 1, 2, 3 };
        int[] codewords = new int[] { 104, 33, 34 };
        BarcodeChecksumRequest request = new BarcodeChecksumRequest();
        request.Format = BarcodeFormat.Code128;
        request.Algorithm = "Modulo103";
        request.Text = "AB";
        request.Bytes = bytes;
        request.Codewords = codewords;
        request.HasObservedValue = true;
        request.ObservedValue = 35;
        BarcodeChecksumResult result = new BarcodeChecksumResult();
        result.Format = BarcodeFormat.Code128;
        result.Algorithm = "Modulo103";
        result.CalculatedValue = 35;
        result.HasObservedValue = true;
        result.ObservedValue = 35;
        result.IsValid = true;

        bytes[0] = 9;
        codewords[0] = 105;

        Assert.Multiple(() =>
        {
            Assert.That(request.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(request.Algorithm, Is.EqualTo("Modulo103"));
            Assert.That(request.Text, Is.EqualTo("AB"));
            Assert.That(request.Bytes, Is.SameAs(bytes));
            Assert.That(request.Bytes?[0], Is.EqualTo(9));
            Assert.That(request.Codewords, Is.SameAs(codewords));
            Assert.That(request.Codewords?[0], Is.EqualTo(105));
            Assert.That(request.HasObservedValue, Is.True);
            Assert.That(request.ObservedValue, Is.EqualTo(35));
            Assert.That(result.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(result.Algorithm, Is.EqualTo("Modulo103"));
            Assert.That(result.CalculatedValue, Is.EqualTo(35));
            Assert.That(result.HasObservedValue, Is.True);
            Assert.That(result.ObservedValue, Is.EqualTo(35));
            Assert.That(result.IsValid, Is.True);
        });
    }

    [Test]
    public void Gs1DefinitionElementAndMessageHoldDictionaryAndSourceContext()
    {
        string[] required = new string[] { "21" };
        string[] excluded = new string[] { "02" };
        string[] qualifiers = new string[] { "10", "21" };
        Gs1ApplicationIdentifierInfo definition = new Gs1ApplicationIdentifierInfo();
        definition.Identifier = "01";
        definition.Title = "Global Trade Item Number";
        definition.MinimumLength = 14;
        definition.MaximumLength = 14;
        definition.CharacterSet = "numeric";
        definition.FixedLength = true;
        definition.HasCheckCharacter = true;
        definition.IsPrimaryKey = true;
        definition.RequiredIdentifiers = required;
        definition.ExcludedIdentifiers = excluded;
        definition.Qualifiers = qualifiers;
        string[] components = new string[] { "09501101530003" };
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = "01";
        element.Value = "09501101530003";
        element.Components = components;
        element.Definition = definition;
        element.SourceOffset = 0;
        element.SourceLength = 18;
        element.VariableLength = false;
        Gs1Element[] elements = new Gs1Element[] { element };
        BarcodeValidationIssue issue = new BarcodeValidationIssue();
        BarcodeValidationIssue[] issues = new BarcodeValidationIssue[] { issue };
        Gs1Message message = new Gs1Message();
        message.Elements = elements;
        message.Syntax = Gs1Syntax.Bracketed;
        message.SymbologyIdentifier = "]C1";
        message.SourceText = "(01)09501101530003";
        message.Issues = issues;

        required[0] = "10";
        components[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(definition.Identifier, Is.EqualTo("01"));
            Assert.That(definition.Title, Is.EqualTo("Global Trade Item Number"));
            Assert.That(definition.MinimumLength, Is.EqualTo(14));
            Assert.That(definition.MaximumLength, Is.EqualTo(14));
            Assert.That(definition.CharacterSet, Is.EqualTo("numeric"));
            Assert.That(definition.FixedLength, Is.True);
            Assert.That(definition.HasCheckCharacter, Is.True);
            Assert.That(definition.IsPrimaryKey, Is.True);
            Assert.That(definition.RequiredIdentifiers, Is.SameAs(required));
            Assert.That(definition.RequiredIdentifiers?[0], Is.EqualTo("10"));
            Assert.That(definition.ExcludedIdentifiers, Is.SameAs(excluded));
            Assert.That(definition.Qualifiers, Is.SameAs(qualifiers));
            Assert.That(element.ApplicationIdentifier, Is.EqualTo("01"));
            Assert.That(element.Value, Is.EqualTo("09501101530003"));
            Assert.That(element.Components, Is.SameAs(components));
            Assert.That(element.Components?[0], Is.EqualTo("changed"));
            Assert.That(element.Definition, Is.SameAs(definition));
            Assert.That(element.SourceOffset, Is.EqualTo(0));
            Assert.That(element.SourceLength, Is.EqualTo(18));
            Assert.That(element.VariableLength, Is.False);
            Assert.That(message.Elements, Is.SameAs(elements));
            Assert.That(message.Syntax, Is.EqualTo(Gs1Syntax.Bracketed));
            Assert.That(message.SymbologyIdentifier, Is.EqualTo("]C1"));
            Assert.That(message.SourceText, Is.EqualTo("(01)09501101530003"));
            Assert.That(message.Issues, Is.SameAs(issues));
        });
    }

    [Test]
    public void Gs1ProcessTypesHoldInputPolicyAndSerializedOutput()
    {
        byte[] sourceBytes = new byte[] { 93, 67, 49 };
        Gs1ProcessRequest request = new Gs1ProcessRequest();
        request.SourceText = "(01)09501101530003";
        request.SourceBytes = sourceBytes;
        request.SourceSyntax = Gs1Syntax.Bracketed;
        request.TargetSyntax = Gs1Syntax.DigitalLinkUri;
        request.AllowUnknownApplicationIdentifiers = true;
        request.ValidateAssociations = true;
        request.DigitalLinkBaseUri = "https://id.gs1.org";
        Gs1Message message = new Gs1Message();
        byte[] outputBytes = new byte[] { 1, 2 };
        BarcodeValidationIssue issue = new BarcodeValidationIssue();
        BarcodeValidationIssue[] issues = new BarcodeValidationIssue[] { issue };
        Gs1ProcessResult result = new Gs1ProcessResult();
        result.Success = true;
        result.Message = message;
        result.Text = "https://id.gs1.org/01/09501101530003";
        result.Bytes = outputBytes;
        result.Issues = issues;

        sourceBytes[0] = 1;
        outputBytes[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(request.SourceText, Is.EqualTo("(01)09501101530003"));
            Assert.That(request.SourceBytes, Is.SameAs(sourceBytes));
            Assert.That(request.SourceBytes?[0], Is.EqualTo(1));
            Assert.That(request.SourceSyntax, Is.EqualTo(Gs1Syntax.Bracketed));
            Assert.That(request.TargetSyntax, Is.EqualTo(Gs1Syntax.DigitalLinkUri));
            Assert.That(request.AllowUnknownApplicationIdentifiers, Is.True);
            Assert.That(request.ValidateAssociations, Is.True);
            Assert.That(request.DigitalLinkBaseUri, Is.EqualTo("https://id.gs1.org"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.SameAs(message));
            Assert.That(result.Text, Is.EqualTo("https://id.gs1.org/01/09501101530003"));
            Assert.That(result.Bytes, Is.SameAs(outputBytes));
            Assert.That(result.Bytes?[0], Is.EqualTo(9));
            Assert.That(result.Issues, Is.SameAs(issues));
        });
    }

    [Test]
    public void StructuredAppendTypesHoldSegmentsCompletenessAndConflicts()
    {
        BarcodeDecodeResult segment = new BarcodeDecodeResult();
        BarcodeDecodeResult[] segments = new BarcodeDecodeResult[] { segment };
        StructuredAppendRequest request = new StructuredAppendRequest();
        request.Format = BarcodeFormat.MacroPdf417;
        request.Segments = segments;
        request.RequireComplete = true;
        request.AllowIdenticalDuplicates = true;
        BarcodePayload payload = new BarcodePayload();
        int[] missingIndices = new int[] { 2, 3 };
        string[] conflicts = new string[] { "segment 1 file ID mismatch" };
        BarcodeValidationIssue issue = new BarcodeValidationIssue();
        BarcodeValidationIssue[] issues = new BarcodeValidationIssue[] { issue };
        StructuredAppendResult result = new StructuredAppendResult();
        result.Complete = false;
        result.Payload = payload;
        result.Segments = segments;
        result.MissingIndices = missingIndices;
        result.Conflicts = conflicts;
        result.FileIdentifier = "FILE-42";
        result.Issues = issues;

        missingIndices[0] = 4;
        conflicts[0] = "changed conflict";

        Assert.Multiple(() =>
        {
            Assert.That(request.Format, Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(request.Segments, Is.SameAs(segments));
            Assert.That(request.RequireComplete, Is.True);
            Assert.That(request.AllowIdenticalDuplicates, Is.True);
            Assert.That(result.Complete, Is.False);
            Assert.That(result.Payload, Is.SameAs(payload));
            Assert.That(result.Segments, Is.SameAs(segments));
            Assert.That(result.MissingIndices, Is.SameAs(missingIndices));
            Assert.That(result.MissingIndices?[0], Is.EqualTo(4));
            Assert.That(result.Conflicts, Is.SameAs(conflicts));
            Assert.That(result.Conflicts?[0], Is.EqualTo("changed conflict"));
            Assert.That(result.FileIdentifier, Is.EqualTo("FILE-42"));
            Assert.That(result.Issues, Is.SameAs(issues));
        });
    }

    [Test]
    public void ConfigurationHoldsChecksumValidationGs1AndAssemblyObjects()
    {
        BarcodeChecksumRequest checksumRequest = new BarcodeChecksumRequest();
        BarcodeChecksumResult checksumResult = new BarcodeChecksumResult();
        BarcodeValidationRequest validationRequest = new BarcodeValidationRequest();
        BarcodeValidationReport validationReport = new BarcodeValidationReport();
        Gs1ProcessRequest gs1Request = new Gs1ProcessRequest();
        Gs1ProcessResult gs1Result = new Gs1ProcessResult();
        StructuredAppendRequest appendRequest = new StructuredAppendRequest();
        StructuredAppendResult appendResult = new StructuredAppendResult();
        BarcodeConfiguration configuration = new BarcodeConfiguration();
        configuration.ChecksumRequest = checksumRequest;
        configuration.ChecksumResult = checksumResult;
        configuration.ValidationRequest = validationRequest;
        configuration.ValidationReport = validationReport;
        configuration.Gs1Request = gs1Request;
        configuration.Gs1Result = gs1Result;
        configuration.StructuredAppendRequest = appendRequest;
        configuration.StructuredAppendResult = appendResult;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.ChecksumRequest, Is.SameAs(checksumRequest));
            Assert.That(configuration.ChecksumResult, Is.SameAs(checksumResult));
            Assert.That(configuration.ValidationRequest, Is.SameAs(validationRequest));
            Assert.That(configuration.ValidationReport, Is.SameAs(validationReport));
            Assert.That(configuration.Gs1Request, Is.SameAs(gs1Request));
            Assert.That(configuration.Gs1Result, Is.SameAs(gs1Result));
            Assert.That(configuration.StructuredAppendRequest, Is.SameAs(appendRequest));
            Assert.That(configuration.StructuredAppendResult, Is.SameAs(appendResult));
        });
    }
}
