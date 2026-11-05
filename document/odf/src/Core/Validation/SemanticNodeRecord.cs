namespace odf;

internal class SemanticNodeRecord
{
    internal OdfNode Node = new OdfNode();
    internal string PartPath = "";
    internal string NodePath = "";
}

internal class SemanticMergeClaim
{
    internal OdfNode Origin = new OdfNode();
    internal int FirstRow;
    internal int LastRow;
    internal int FirstColumn;
    internal int LastColumn;
}

internal class SemanticFormulaStatus
{
    internal bool IsValid;
    internal bool LimitExceeded;
}
