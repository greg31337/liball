namespace odf;

internal class SchemaValidationResult
{
    internal bool IsValid;
    internal bool LimitExceeded;
    internal bool InvalidDatatype;
    internal int Operations;
    internal int MaximumDepth;
}
