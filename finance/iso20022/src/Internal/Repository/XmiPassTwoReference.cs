namespace iso20022;

internal class XmiPassTwoReference
{
    internal string SourceIdentifier = string.Empty;
    internal RepositoryEntityKind SourceKind;
    internal int SourceDocumentOrder;
    internal string PropertyName = string.Empty;
    internal int Position;
    internal int AttributeOrder = -1;
    internal string TargetIdentifier = string.Empty;
    internal string RawToken = string.Empty;
    internal RepositoryEntityKind TargetKind;
    internal int TargetDocumentOrder = -1;
    internal string RawValue = string.Empty;
    internal bool IsContainment;
    internal bool IsResolved;
    internal bool IsForwardReference;
    internal bool IsBackwardReference;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
    internal bool HasLineInformation;
    internal int LineNumber;
    internal int ColumnNumber;
}
