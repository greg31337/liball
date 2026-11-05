namespace cbor;

internal class CddlExternalResolution
{
    internal string Name = "";
    internal CddlRuleKind Kind = CddlRuleKind.None;
    internal CddlRule? Rule;
    internal bool IsInvalid;
    internal string ErrorMessage = "";
    internal bool IsProcessed;
}
