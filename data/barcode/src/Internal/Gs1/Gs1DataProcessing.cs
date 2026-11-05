namespace barcode;

internal class Gs1ProcessingParseData
{
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues =
        new BarcodeValidationIssue[0];
    internal string SourceText = "";
    internal string SymbologyIdentifier = "";
    internal BarcodeFormat ScannerFormat = BarcodeFormat.Automatic;
}

internal class Gs1ProcessingBuildData
{
    internal string? Text;
    internal byte[]? Bytes;
    internal BarcodeValidationIssue[] Issues =
        new BarcodeValidationIssue[0];
}

internal static class Gs1DataProcessing
{
    internal static Gs1ProcessResult Process(Gs1ProcessRequest request)
    {
        if (request == null)
        {
            BarcodeValidationIssue issue = CreateIssue(
                "GS1_PROCESS_REQUEST_NULL",
                "The GS1 processing request must not be null.",
                "Request",
                -1,
                "a GS1 processing request",
                "null");
            return CreateFailureResult(
                Gs1Syntax.Bracketed,
                "",
                "",
                new BarcodeValidationIssue[] { issue });
        }

        BarcodeValidationIssue? syntaxIssue =
            ValidateSyntaxes(request.SourceSyntax, request.TargetSyntax);
        if (syntaxIssue != null)
        {
            return CreateFailureResult(
                request.SourceSyntax,
                ResolveSourceText(request),
                "",
                new BarcodeValidationIssue[] { syntaxIssue });
        }

        Gs1ProcessingParseData parsed = ParseSource(request);
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        AppendIssues(issues, parsed.Issues);
        Gs1Message message = CreateMessage(
            request.SourceSyntax,
            parsed.SourceText,
            parsed.SymbologyIdentifier,
            parsed.Elements,
            parsed.Issues);

        if (issues.Count == 0 && request.ValidateAssociations)
        {
            Gs1AssociationValidationResult association =
                Gs1AssociationValidator.ValidateMessage(message);
            AppendIssues(issues, association.Issues);
            message.Issues = issues.ToArray();
        }

        Gs1ProcessingBuildData built = new Gs1ProcessingBuildData();
        if (issues.Count == 0)
        {
            built = BuildTarget(
                request,
                parsed.Elements,
                parsed.ScannerFormat);
            AppendIssues(issues, built.Issues);
        }

        Gs1ProcessResult result = new Gs1ProcessResult();
        result.Success = issues.Count == 0;
        result.Message = message;
        result.Text = result.Success ? built.Text : null;
        result.Bytes = result.Success ? built.Bytes : null;
        result.Issues = issues.ToArray();
        return result;
    }

    internal static Gs1ApplicationIdentifierInfo? Lookup(
        string? applicationIdentifier)
    {
        return Gs1AiInfoFactory.Lookup(applicationIdentifier);
    }

    static Gs1ProcessingParseData ParseSource(Gs1ProcessRequest request)
    {
        switch (request.SourceSyntax)
        {
            case Gs1Syntax.Bracketed:
                return ParseBracketed(request);
            case Gs1Syntax.ElementString:
                return ParseElementString(request);
            case Gs1Syntax.BarcodeMessage:
                return ParseBarcodeMessage(request);
            case Gs1Syntax.ScannerData:
                return ParseScannerData(request);
            case Gs1Syntax.HumanReadableInterpretation:
                return ParseHri(request);
            case Gs1Syntax.DigitalLinkUri:
                return ParseDigitalLink(request);
            default:
                return new Gs1ProcessingParseData();
        }
    }

    static Gs1ProcessingParseData ParseBracketed(
        Gs1ProcessRequest request)
    {
        Gs1BracketedParseResult parsed =
            request.AllowUnknownApplicationIdentifiers
                ? Gs1ExplicitAiProcessing.ParseBracketedAllowUnknown(
                    request.SourceText)
                : Gs1BracketedSyntax.ParseBracketed(request.SourceText);
        return CreateParseData(
            parsed.Elements,
            parsed.Issues,
            request.SourceText,
            "",
            BarcodeFormat.Automatic);
    }

    static Gs1ProcessingParseData ParseElementString(
        Gs1ProcessRequest request)
    {
        Gs1UnbracketedParseResult parsed =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(
                request.SourceText);
        return CreateParseData(
            parsed.Elements,
            parsed.Issues,
            request.SourceText,
            "",
            BarcodeFormat.Automatic);
    }

    static Gs1ProcessingParseData ParseBarcodeMessage(
        Gs1ProcessRequest request)
    {
        byte[]? source = request.SourceBytes;
        if (source == null && request.SourceText != null)
        {
            source = ConvertTextToBytes(request.SourceText);
        }

        Gs1UnbracketedParseResult parsed =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(source);
        return CreateParseData(
            parsed.Elements,
            parsed.Issues,
            request.SourceText == null
                ? ConvertBytesToText(source)
                : request.SourceText,
            "",
            BarcodeFormat.Automatic);
    }

    static Gs1ProcessingParseData ParseScannerData(
        Gs1ProcessRequest request)
    {
        byte[]? source = request.SourceBytes;
        if (source == null && request.SourceText != null)
        {
            source = ConvertTextToBytes(request.SourceText);
        }

        Gs1ScannerParseResult parsed =
            Gs1ScannerData.ParseScannerData(source);
        return CreateParseData(
            parsed.Elements,
            parsed.Issues,
            request.SourceText == null
                ? ConvertBytesToText(source)
                : request.SourceText,
            parsed.SymbologyIdentifier,
            parsed.Format);
    }

    static Gs1ProcessingParseData ParseHri(Gs1ProcessRequest request)
    {
        Gs1BracketedParseResult parsed =
            Gs1ExplicitAiProcessing.ParseHri(
                request.SourceText,
                request.AllowUnknownApplicationIdentifiers);
        return CreateParseData(
            parsed.Elements,
            parsed.Issues,
            request.SourceText,
            "",
            BarcodeFormat.Automatic);
    }

    static Gs1ProcessingParseData ParseDigitalLink(
        Gs1ProcessRequest request)
    {
        Gs1DigitalLinkParseResult parsed =
            Gs1DigitalLinkSyntax.ParseDigitalLinkWithAssociationPolicy(
                request.SourceText,
                false);
        return CreateParseData(
            parsed.Message.Elements,
            parsed.Issues,
            request.SourceText,
            "",
            BarcodeFormat.Automatic);
    }

    static Gs1ProcessingBuildData BuildTarget(
        Gs1ProcessRequest request,
        Gs1Element[] elements,
        BarcodeFormat scannerFormat)
    {
        bool containsUnknown = ContainsUnknown(elements);
        switch (request.TargetSyntax)
        {
            case Gs1Syntax.Bracketed:
                return BuildBracketed(elements, containsUnknown);
            case Gs1Syntax.ElementString:
                return BuildElementString(elements, containsUnknown);
            case Gs1Syntax.BarcodeMessage:
                return BuildBarcodeMessage(elements, containsUnknown);
            case Gs1Syntax.ScannerData:
                return BuildScannerData(
                    elements,
                    containsUnknown,
                    scannerFormat);
            case Gs1Syntax.HumanReadableInterpretation:
                return BuildHri(elements);
            case Gs1Syntax.DigitalLinkUri:
                return BuildDigitalLink(request, elements);
            default:
                return new Gs1ProcessingBuildData();
        }
    }

    static Gs1ProcessingBuildData BuildBracketed(
        Gs1Element[] elements,
        bool containsUnknown)
    {
        Gs1BracketedBuildResult built = containsUnknown
            ? Gs1ExplicitAiProcessing.BuildBracketedAllowUnknown(elements)
            : Gs1BracketedSyntax.BuildBracketed(elements);
        return CreateBuildData(built.Text, null, built.Issues);
    }

    static Gs1ProcessingBuildData BuildElementString(
        Gs1Element[] elements,
        bool containsUnknown)
    {
        Gs1UnbracketedBuildResult built = containsUnknown
            ? Gs1ExplicitAiProcessing.BuildUnbracketedAllowUnknown(
                elements,
                true)
            : Gs1UnbracketedSyntax.BuildUnbracketedElementString(elements);
        return CreateBuildData(built.Text, built.Bytes, built.Issues);
    }

    static Gs1ProcessingBuildData BuildBarcodeMessage(
        Gs1Element[] elements,
        bool containsUnknown)
    {
        Gs1UnbracketedBuildResult built = containsUnknown
            ? Gs1ExplicitAiProcessing.BuildUnbracketedAllowUnknown(
                elements,
                false)
            : Gs1UnbracketedSyntax.BuildBarcodeMessage(elements);
        return CreateBuildData(built.Text, built.Bytes, built.Issues);
    }

    static Gs1ProcessingBuildData BuildScannerData(
        Gs1Element[] elements,
        bool containsUnknown,
        BarcodeFormat sourceFormat)
    {
        BarcodeFormat format = sourceFormat;
        if (format == BarcodeFormat.Automatic)
        {
            format = BarcodeFormat.Gs1128;
        }

        if (containsUnknown)
        {
            Gs1UnbracketedBuildResult payload =
                Gs1ExplicitAiProcessing.BuildUnbracketedAllowUnknown(
                    elements,
                    false);
            if (!payload.Success)
            {
                return CreateBuildData(
                    payload.Text,
                    payload.Bytes,
                    payload.Issues);
            }

            if (format != BarcodeFormat.Gs1128
                && format != BarcodeFormat.Gs1DataMatrix)
            {
                BarcodeValidationIssue issue = CreateIssue(
                    "GS1_PROCESS_UNKNOWN_AI_SCANNER_FORMAT",
                    "Unknown Application Identifiers require an AI-based GS1 scanner carrier.",
                    "TargetSyntax",
                    -1,
                    "GS1-128 or GS1 DataMatrix scanner data",
                    format.ToString());
                return CreateBuildData(
                    null,
                    null,
                    new BarcodeValidationIssue[] { issue });
            }

            string identifier = format == BarcodeFormat.Gs1128
                ? BarcodeSymbologyIdentifiers.Gs1128
                : BarcodeSymbologyIdentifiers.Gs1DataMatrix;
            string text = identifier + payload.Text;
            return CreateBuildData(
                text,
                ConvertTextToBytes(text),
                new BarcodeValidationIssue[0]);
        }

        Gs1ScannerBuildResult built =
            Gs1ScannerData.BuildScannerData(elements, format);
        return CreateBuildData(built.Text, built.Bytes, built.Issues);
    }

    static Gs1ProcessingBuildData BuildHri(Gs1Element[] elements)
    {
        Gs1HriResult built = Gs1HriFormatter.GenerateHri(
            elements,
            false,
            false);
        return CreateBuildData(
            built.CompactText,
            null,
            built.Issues);
    }

    static Gs1ProcessingBuildData BuildDigitalLink(
        Gs1ProcessRequest request,
        Gs1Element[] elements)
    {
        Gs1DigitalLinkBuildResult built =
            Gs1DigitalLinkSyntax.BuildDigitalLinkWithAssociationPolicy(
                elements,
                request.DigitalLinkBaseUri,
                false);
        return CreateBuildData(built.Text, null, built.Issues);
    }

    static BarcodeValidationIssue? ValidateSyntaxes(
        Gs1Syntax sourceSyntax,
        Gs1Syntax targetSyntax)
    {
        int sourceValue = (int)sourceSyntax;
        if (sourceValue < (int)Gs1Syntax.Bracketed
            || sourceValue > (int)Gs1Syntax.DigitalLinkUri)
        {
            return CreateIssue(
                "GS1_PROCESS_SOURCE_SYNTAX_INVALID",
                "The declared GS1 source syntax is not supported.",
                "SourceSyntax",
                -1,
                "a supported GS1 syntax",
                sourceValue.ToString());
        }

        int targetValue = (int)targetSyntax;
        if (targetValue < (int)Gs1Syntax.Bracketed
            || targetValue > (int)Gs1Syntax.DigitalLinkUri)
        {
            return CreateIssue(
                "GS1_PROCESS_TARGET_SYNTAX_INVALID",
                "The requested GS1 target syntax is not supported.",
                "TargetSyntax",
                -1,
                "a supported GS1 syntax",
                targetValue.ToString());
        }

        return null;
    }

    static bool ContainsUnknown(Gs1Element[] elements)
    {
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element != null
                && Gs1AiResolver.ResolveDefinition(
                    element.ApplicationIdentifier) == null)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static Gs1Message CreateMessage(
        Gs1Syntax syntax,
        string sourceText,
        string symbologyIdentifier,
        Gs1Element[] elements,
        BarcodeValidationIssue[] issues)
    {
        Gs1Message message = new Gs1Message();
        message.Elements = elements;
        message.Syntax = syntax;
        message.SymbologyIdentifier = symbologyIdentifier.Length == 0
            ? null
            : symbologyIdentifier;
        message.SourceText = sourceText;
        message.Issues = issues;
        return message;
    }

    static Gs1ProcessingParseData CreateParseData(
        Gs1Element[]? elements,
        BarcodeValidationIssue[]? issues,
        string? sourceText,
        string symbologyIdentifier,
        BarcodeFormat scannerFormat)
    {
        Gs1ProcessingParseData result = new Gs1ProcessingParseData();
        result.Elements = elements == null
            ? new Gs1Element[0]
            : elements;
        result.Issues = issues == null
            ? new BarcodeValidationIssue[0]
            : issues;
        result.SourceText = sourceText == null ? "" : sourceText;
        result.SymbologyIdentifier = symbologyIdentifier;
        result.ScannerFormat = scannerFormat;
        return result;
    }

    static Gs1ProcessingBuildData CreateBuildData(
        string? text,
        byte[]? bytes,
        BarcodeValidationIssue[] issues)
    {
        Gs1ProcessingBuildData result = new Gs1ProcessingBuildData();
        result.Text = text;
        result.Bytes = bytes;
        result.Issues = issues;
        return result;
    }

    static Gs1ProcessResult CreateFailureResult(
        Gs1Syntax syntax,
        string sourceText,
        string symbologyIdentifier,
        BarcodeValidationIssue[] issues)
    {
        Gs1ProcessResult result = new Gs1ProcessResult();
        result.Success = false;
        result.Message = CreateMessage(
            syntax,
            sourceText,
            symbologyIdentifier,
            new Gs1Element[0],
            issues);
        result.Text = null;
        result.Bytes = null;
        result.Issues = issues;
        return result;
    }

    static string ResolveSourceText(Gs1ProcessRequest request)
    {
        if (request.SourceText != null)
        {
            return request.SourceText;
        }

        return ConvertBytesToText(request.SourceBytes);
    }

    static byte[] ConvertTextToBytes(string source)
    {
        byte[] bytes = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            bytes[index] = (byte)source[index];
            index = index + 1;
        }

        return bytes;
    }

    static string ConvertBytesToText(byte[]? source)
    {
        if (source == null)
        {
            return "";
        }

        char[] characters = new char[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            characters[index] = (char)source[index];
            index = index + 1;
        }

        return new string(characters);
    }

    static void AppendIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> target,
        BarcodeValidationIssue[]? source)
    {
        if (source == null)
        {
            return;
        }

        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    static BarcodeValidationIssue CreateIssue(
        string code,
        string message,
        string field,
        int offset,
        string expected,
        string observed)
    {
        return ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Error,
            code,
            message,
            Gs1ElementStringParser.StandardName,
            field,
            offset,
            expected,
            observed);
    }
}
