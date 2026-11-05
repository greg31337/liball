namespace uri;

internal class TemplateExpansionState
{
    internal OperationLimits Limits;
    internal int CompositeMembers;

    internal TemplateExpansionState(OperationLimits limits)
    {
        Limits = limits;
        CompositeMembers = 0;
    }
}
