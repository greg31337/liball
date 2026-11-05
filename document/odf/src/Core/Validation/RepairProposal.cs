namespace odf;

internal class RepairProposal
{
    internal string Id = "";
    internal RepairKind Kind;
    internal string Description = "";
    internal OdfLocation Location = new OdfLocation();
    internal bool IsSafe;
    internal bool IsApplied;
    internal string OldValue = "";
    internal string NewValue = "";
    internal string Prefix = "";
    internal OdfDocument? Document;
    internal OdfPackage? Package;
    internal OdfPart? Part;
    internal OdfNode? Node;
    internal OdfAttribute? Attribute;
    internal OdfAttribute[] RelatedAttributes = [];
}
