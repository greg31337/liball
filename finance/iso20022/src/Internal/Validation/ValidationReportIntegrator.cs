namespace iso20022;

internal static class ValidationReportIntegrator
{
    internal static void MergeReport(
        ValidationPipelineState state,
        ValidationReport? source)
    {
        if (source == null || source.Diagnostics == null ||
            source.CompletedStages == null)
        {
            AddIntegrationFailure(
                state,
                "validation.stage.result_invalid",
                "A validation stage returned an incomplete report.");
            return;
        }
        if (source.Diagnostics.Count > state.Policy.Limits.MaxCollectionCount)
        {
            AddIntegrationFailure(
                state,
                "validation.stage.result_limit",
                "A validation stage returned more diagnostics than the collection limit permits.");
            return;
        }
        int index = 0;
        while (index < source.Diagnostics.Count && state.Continue)
        {
            ValidationDiagnostic? diagnostic = source.Diagnostics[index];
            if (diagnostic == null)
            {
                AddIntegrationFailure(
                    state,
                    "validation.stage.diagnostic_missing",
                    "A validation stage returned an empty diagnostic value.");
                return;
            }
            ValidationDiagnostic integrated = CopyDiagnostic(diagnostic);
            NormalizeDiagnostic(integrated);
            state.AddDiagnostic(integrated);
            index++;
        }
        if (source.ExternalCodeEdition != null &&
            source.ExternalCodeEdition.Length > 0)
        {
            state.Report.ExternalCodeEdition = source.ExternalCodeEdition;
        }
        if (source.DiagnosticLimitReached && state.Continue)
        {
            state.ReachDiagnosticLimit();
            return;
        }
        if (!source.Valid)
        {
            state.Report.Valid = false;
        }
        if (!source.Success)
        {
            state.Report.Success = false;
            state.Report.Valid = false;
            state.Report.ErrorCode = source.ErrorCode ?? string.Empty;
            state.Report.ErrorMessage = source.ErrorMessage ?? string.Empty;
            if (state.Report.ErrorCode.Length == 0)
            {
                state.Report.ErrorCode = "validation.stage.operation_failed";
            }
            if (state.Report.ErrorMessage.Length == 0)
            {
                state.Report.ErrorMessage =
                    "A validation stage could not complete its operation.";
            }
            state.Report.StoppedEarly = true;
            state.Continue = false;
        }
    }

    internal static void NormalizeDiagnostic(ValidationDiagnostic diagnostic)
    {
        if (diagnostic.Code == null || diagnostic.Code.Length == 0)
        {
            diagnostic.Code = "validation.stage.diagnostic";
        }
        if (diagnostic.RuleIdentifier == null ||
            diagnostic.RuleIdentifier.Length == 0)
        {
            diagnostic.RuleIdentifier = "iso20022.validation.stage";
        }
        if (diagnostic.Severity == ValidationSeverity.Unknown)
        {
            diagnostic.Severity = ValidationSeverity.Error;
        }
        if (diagnostic.Kind == DiagnosticKind.Unknown)
        {
            diagnostic.Kind = DiagnosticKind.ValidationFailure;
        }
        if (diagnostic.Message == null || diagnostic.Message.Length == 0)
        {
            diagnostic.Message = "A validation stage reported a diagnostic.";
        }
        if (diagnostic.Path == null)
        {
            diagnostic.Path = new XmlPathData();
        }
        if (diagnostic.Location == null)
        {
            diagnostic.Location = new SourceLocation();
        }
        if (diagnostic.OffendingValue == null)
        {
            diagnostic.OffendingValue = string.Empty;
        }
        if (diagnostic.ExpectedConstraint == null)
        {
            diagnostic.ExpectedConstraint = string.Empty;
        }
        if (diagnostic.SourceIdentifier == null)
        {
            diagnostic.SourceIdentifier = string.Empty;
        }
        if (diagnostic.ProfileIdentifier == null)
        {
            diagnostic.ProfileIdentifier = string.Empty;
        }
        if (diagnostic.ProfileVersion == null)
        {
            diagnostic.ProfileVersion = string.Empty;
        }
        if (diagnostic.RepairHint == null)
        {
            diagnostic.RepairHint = string.Empty;
        }
    }

    internal static ValidationDiagnostic CopyDiagnostic(
        ValidationDiagnostic source)
    {
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = source.Code;
        diagnostic.RuleIdentifier = source.RuleIdentifier;
        diagnostic.Severity = source.Severity;
        diagnostic.Stage = source.Stage;
        diagnostic.Kind = source.Kind;
        diagnostic.Message = source.Message;
        diagnostic.Path = CopyDiagnosticPath(source.Path);
        diagnostic.Location = CopyDiagnosticLocation(source.Location);
        diagnostic.OffendingValue = source.OffendingValue;
        diagnostic.OffendingValueRedacted = source.OffendingValueRedacted;
        diagnostic.ExpectedConstraint = source.ExpectedConstraint;
        diagnostic.SourceArtifact = source.SourceArtifact;
        diagnostic.SourceIdentifier = source.SourceIdentifier;
        diagnostic.ProfileIdentifier = source.ProfileIdentifier;
        diagnostic.ProfileVersion = source.ProfileVersion;
        diagnostic.RepairHint = source.RepairHint;
        return diagnostic;
    }

    internal static XmlPathData CopyDiagnosticPath(XmlPathData? source)
    {
        XmlPathData path = new XmlPathData();
        if (source == null)
        {
            return path;
        }
        path.Value = source.Value ?? string.Empty;
        if (source.Segments == null)
        {
            return path;
        }
        int index = 0;
        while (index < source.Segments.Count)
        {
            QualifiedNameData? sourceSegment = source.Segments[index];
            if (sourceSegment != null)
            {
                QualifiedNameData segment = new QualifiedNameData();
                segment.NamespaceUri = sourceSegment.NamespaceUri ?? string.Empty;
                segment.LocalName = sourceSegment.LocalName ?? string.Empty;
                segment.Prefix = sourceSegment.Prefix ?? string.Empty;
                path.Segments.Add(segment);
            }
            index++;
        }
        return path;
    }

    internal static SourceLocation CopyDiagnosticLocation(
        SourceLocation? source)
    {
        SourceLocation location = new SourceLocation();
        if (source == null)
        {
            return location;
        }
        location.SourceName = source.SourceName;
        location.HasLineInformation = source.HasLineInformation;
        location.LineNumber = source.LineNumber;
        location.ColumnNumber = source.ColumnNumber;
        location.HasCharacterOffset = source.HasCharacterOffset;
        location.CharacterOffset = source.CharacterOffset;
        return location;
    }

    internal static void AddIntegrationFailure(
        ValidationPipelineState state,
        string code,
        string message)
    {
        ValidationDiagnostic diagnostic = MessageValidator.CreateDiagnostic(
            code,
            "iso20022.validation.integration",
            DiagnosticKind.Configuration,
            message,
            string.Empty,
            "A complete bounded validation stage report.",
            state.Request);
        state.AddDiagnostic(diagnostic);
        state.Report.Success = false;
        state.Report.Valid = false;
        state.Report.ErrorCode = code;
        state.Report.ErrorMessage = message;
        state.Report.StoppedEarly = true;
        state.Continue = false;
    }
}
