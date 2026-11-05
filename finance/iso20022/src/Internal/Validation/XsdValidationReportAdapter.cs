namespace iso20022;

internal static class XsdValidationReportAdapter
{
    internal static ValidationReport CreateReport(
        SchemaCompilationResult? source,
        SchemaArtifact? selector)
    {
        ValidationReport report = new ValidationReport();
        report.Success = true;
        report.Valid = true;
        if (source == null || source.DiagnosticCodes == null ||
            source.DiagnosticMessages == null ||
            source.DiagnosticLocations == null ||
            source.DiagnosticPaths == null ||
            source.DiagnosticSchemaUris == null || source.Schemas == null)
        {
            return CreateAdapterFailure(
                "validation.xsd.result_invalid",
                "The XSD validator returned an incomplete result.",
                selector);
        }
        int index = 0;
        while (index < source.DiagnosticCodes.Count)
        {
            ValidationDiagnostic diagnostic = CreateSchemaDiagnostic(
                source,
                selector,
                index);
            report.Diagnostics.Add(diagnostic);
            if (diagnostic.Severity == ValidationSeverity.Error)
            {
                report.Valid = false;
            }
            index++;
        }
        if (source.Success && !source.Found)
        {
            return CreateAdapterFailure(
                "validation.xsd.schema_not_found",
                "The exact schema selected for XSD validation was not found.",
                selector);
        }
        if (source.Success)
        {
            return report;
        }
        string sourceErrorCode = source.ErrorCode ?? string.Empty;
        string sourceErrorMessage = source.ErrorMessage ?? string.Empty;
        if (sourceErrorCode == "schema.validation.invalid")
        {
            report.Valid = false;
            if (report.Diagnostics.Count == 0)
            {
                AddOperationDiagnostic(
                    report,
                    "validation.xsd.error",
                    sourceErrorMessage,
                    DiagnosticKind.InvalidValue,
                    selector);
            }
            return report;
        }
        report.Success = false;
        report.Valid = false;
        report.ErrorCode = MapOperationCode(sourceErrorCode);
        report.ErrorMessage = sourceErrorMessage;
        report.StoppedEarly = true;
        if (sourceErrorCode == "schema.validation.diagnostic_limit")
        {
            report.DiagnosticLimitReached = true;
        }
        if (report.ErrorMessage.Length == 0)
        {
            report.ErrorMessage = "The XSD validation operation could not complete.";
        }
        if (report.Diagnostics.Count == 0)
        {
            DiagnosticKind kind = DiagnosticKind.Configuration;
            if (sourceErrorCode == "schema.validation.resource_limit" ||
                sourceErrorCode == "schema.validation.input_limit" ||
                sourceErrorCode == "schema.validation.diagnostic_limit")
            {
                kind = DiagnosticKind.ResourceLimit;
            }
            AddOperationDiagnostic(
                report,
                report.ErrorCode,
                report.ErrorMessage,
                kind,
                selector);
        }
        return report;
    }

    internal static ValidationDiagnostic CreateSchemaDiagnostic(
        SchemaCompilationResult source,
        SchemaArtifact? selector,
        int index)
    {
        string sourceCode = source.DiagnosticCodes[index] ?? string.Empty;
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = "validation.xsd.error";
        diagnostic.RuleIdentifier = "iso20022.xsd.validation";
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Kind = DiagnosticKind.Configuration;
        if (sourceCode == "schema.validation.warning")
        {
            diagnostic.Code = "validation.xsd.warning";
            diagnostic.Severity = ValidationSeverity.Warning;
            diagnostic.Kind = DiagnosticKind.ValidationFailure;
        }
        else if (sourceCode == "schema.validation.error")
        {
            diagnostic.Kind = DiagnosticKind.InvalidValue;
        }
        if (index < source.DiagnosticMessages.Count)
        {
            diagnostic.Message = source.DiagnosticMessages[index];
        }
        if (diagnostic.Message.Length == 0)
        {
            diagnostic.Message = "The selected XSD reported a validation diagnostic.";
        }
        if (index < source.DiagnosticLocations.Count)
        {
            diagnostic.Location = source.DiagnosticLocations[index];
        }
        if (index < source.DiagnosticPaths.Count)
        {
            diagnostic.Path = source.DiagnosticPaths[index];
        }
        string schemaUri = string.Empty;
        if (index < source.DiagnosticSchemaUris.Count)
        {
            schemaUri = source.DiagnosticSchemaUris[index];
        }
        diagnostic.SourceIdentifier = schemaUri;
        diagnostic.SourceArtifact = FindSchemaArtifact(source, selector, schemaUri);
        diagnostic.ExpectedConstraint = "Content accepted by the exact selected XSD closure.";
        return diagnostic;
    }

    internal static ArtifactManifest? FindSchemaArtifact(
        SchemaCompilationResult source,
        SchemaArtifact? selector,
        string schemaUri)
    {
        int index = 0;
        while (index < source.Schemas.Count)
        {
            if (schemaUri.Length == 0 ||
                source.Schemas[index].BaseUri == schemaUri)
            {
                return source.Schemas[index].Artifact;
            }
            index++;
        }
        if (source.RootSchema != null)
        {
            return source.RootSchema.Artifact;
        }
        if (selector != null)
        {
            return selector.Artifact;
        }
        return null;
    }

    internal static string MapOperationCode(string sourceCode)
    {
        if (sourceCode == "schema.validation.resource_limit" ||
            sourceCode == "schema.validation.input_limit")
        {
            return "validation.xsd.resource_limit";
        }
        if (sourceCode == "schema.validation.diagnostic_limit")
        {
            return "validation.xsd.diagnostic_limit";
        }
        if (sourceCode == "schema.validation.schema_missing" ||
            sourceCode == "schema.registry.not_found")
        {
            return "validation.xsd.schema_not_found";
        }
        if (sourceCode.StartsWith("schema.compile", System.StringComparison.Ordinal) ||
            sourceCode.StartsWith("schema.import", System.StringComparison.Ordinal))
        {
            return "validation.xsd.schema_invalid";
        }
        return "validation.xsd.operation_failed";
    }

    internal static ValidationReport CreateAdapterFailure(
        string code,
        string message,
        SchemaArtifact? selector)
    {
        ValidationReport report = new ValidationReport();
        report.Success = false;
        report.Valid = false;
        report.StoppedEarly = true;
        report.ErrorCode = code;
        report.ErrorMessage = message;
        AddOperationDiagnostic(
            report,
            code,
            message,
            DiagnosticKind.Configuration,
            selector);
        return report;
    }

    internal static void AddOperationDiagnostic(
        ValidationReport report,
        string code,
        string message,
        DiagnosticKind kind,
        SchemaArtifact? selector)
    {
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = "iso20022.xsd.validation";
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Kind = kind;
        diagnostic.Message = message;
        diagnostic.ExpectedConstraint =
            "A completed validation against the exact selected XSD closure.";
        if (selector != null)
        {
            diagnostic.SourceArtifact = selector.Artifact;
            diagnostic.SourceIdentifier = selector.BaseUri;
        }
        report.Diagnostics.Add(diagnostic);
    }
}
