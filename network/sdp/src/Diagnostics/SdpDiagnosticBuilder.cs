namespace sdp;

internal static class SdpDiagnosticBuilder
{
    internal static SdpDiagnostic CreateDiagnostic(string code, SdpValidationSeverity severity, SdpScope scope, char fieldName, string attributeName, int mediaIndex, string detail, SdpSourcePath path, SdpSourceLocation location)
    {
        SdpDiagnostic result = new SdpDiagnostic();
        result.Code = code;
        result.Severity = severity;
        result.Scope = scope;
        result.FieldName = fieldName;
        result.AttributeName = attributeName;
        result.MediaIndex = mediaIndex;
        result.Detail = detail;
        result.Path = path;
        result.Location = location;
        return result;
    }

    internal static SdpValidationIssue CreateValidationIssue(string code, SdpValidationSeverity severity, SdpScope scope, char fieldName, string attributeName, int mediaIndex, string detail, SdpSourcePath path, SdpSourceLocation location)
    {
        SdpValidationIssue result = new SdpValidationIssue();
        result.Code = code;
        result.Severity = severity;
        result.Scope = scope;
        result.FieldName = fieldName;
        result.AttributeName = attributeName;
        result.MediaIndex = mediaIndex;
        result.Detail = detail;
        result.Path = path;
        result.Location = location;
        return result;
    }

    internal static SdpDiagnostic CreateDiagnosticLimitMarker(SdpDiagnostic omitted)
    {
        SdpDiagnostic result = new SdpDiagnostic();
        result.Code = SdpDiagnosticCodes.DiagnosticLimitReached;
        result.Severity = SdpValidationSeverity.Fatal;
        result.Scope = omitted.Scope;
        result.FieldName = omitted.FieldName;
        result.AttributeName = omitted.AttributeName;
        result.MediaIndex = omitted.MediaIndex;
        result.Detail = "The configured diagnostic limit was reached; additional diagnostics were omitted.";
        result.Path = CopySourcePath(omitted.Path);
        result.Location = CopySourceLocation(omitted.Location);
        return result;
    }

    internal static SdpValidationIssue CreateValidationLimitMarker(SdpValidationIssue omitted)
    {
        SdpValidationIssue result = new SdpValidationIssue();
        result.Code = SdpDiagnosticCodes.DiagnosticLimitReached;
        result.Severity = SdpValidationSeverity.Fatal;
        result.Scope = omitted.Scope;
        result.FieldName = omitted.FieldName;
        result.AttributeName = omitted.AttributeName;
        result.MediaIndex = omitted.MediaIndex;
        result.Detail = "The configured diagnostic limit was reached; additional validation issues were omitted.";
        result.Path = CopySourcePath(omitted.Path);
        result.Location = CopySourceLocation(omitted.Location);
        return result;
    }

    internal static SdpSourcePath CopySourcePath(SdpSourcePath source)
    {
        SdpSourcePath result = new SdpSourcePath();
        if (source == null)
        {
            return result;
        }
        result.Scope = source.Scope;
        result.MediaIndex = source.MediaIndex;
        result.MediaId = source.MediaId;
        result.FieldName = source.FieldName;
        result.AttributeName = source.AttributeName;
        result.OccurrenceIndex = source.OccurrenceIndex;
        result.ValueIndex = source.ValueIndex;
        return result;
    }

    internal static SdpSourceLocation CopySourceLocation(SdpSourceLocation source)
    {
        SdpSourceLocation result = new SdpSourceLocation();
        if (source == null)
        {
            return result;
        }
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.LineNumber = source.LineNumber;
        result.ColumnNumber = source.ColumnNumber;
        return result;
    }
}
