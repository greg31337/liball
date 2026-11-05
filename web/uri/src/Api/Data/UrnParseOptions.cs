namespace uri;

/// <summary>
/// Defines options shared by URN parsing operations.
/// </summary>
public class UrnParseOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The generic or registered-namespace validation policy.</summary>
    public UrnValidationMode ValidationMode;

    /// <summary>The optional application-supplied namespace-specific handler.</summary>
    public IUrnNamespaceHandler? NamespaceHandler;

    /// <summary>Initializes generic URN parsing without a namespace-specific handler.</summary>
    public UrnParseOptions()
    {
        Limits = new OperationLimits();
        ValidationMode = UrnValidationMode.GenericSyntax;
        NamespaceHandler = null;
    }
}
