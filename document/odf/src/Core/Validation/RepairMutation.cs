namespace odf;

internal class RepairMutation
{
    internal string ProposalId = "";
    internal RepairKind Kind;
    internal string Description = "";
    internal OdfLocation Location = new OdfLocation();
    internal string BeforeValue = "";
    internal string AfterValue = "";
}
