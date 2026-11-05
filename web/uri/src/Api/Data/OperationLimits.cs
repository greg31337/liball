namespace uri;

/// <summary>
/// Defines processing limits that an operation applies to untrusted input and generated output.
/// </summary>
public class OperationLimits
{
    /// <summary>The maximum number of UTF-16 code units accepted as complete input.</summary>
    public int MaxInputCharacters;

    /// <summary>The maximum number of UTF-16 code units accepted in one component.</summary>
    public int MaxComponentCharacters;

    /// <summary>The maximum number of path segments accepted by a path operation.</summary>
    public int MaxPathSegments;

    /// <summary>The maximum number of query items accepted by a query operation.</summary>
    public int MaxQueryItems;

    /// <summary>The maximum number of expressions accepted in one URI Template.</summary>
    public int MaxTemplateExpressions;

    /// <summary>The maximum number of variable specifications accepted in one URI Template.</summary>
    public int MaxTemplateVariables;

    /// <summary>The maximum number of list or associative members consumed during one template expansion.</summary>
    public int MaxTemplateCompositeMembers;

    /// <summary>The maximum number of links accepted from combined Web Link field values.</summary>
    public int MaxWebLinks;

    /// <summary>The maximum number of parameters accepted across one Web Link field value.</summary>
    public int MaxWebLinkParameters;

    /// <summary>The maximum nesting depth accepted by an operation that processes nested input.</summary>
    public int MaxNestingDepth;

    /// <summary>The maximum number of UTF-16 code units generated as operation output.</summary>
    public int MaxOutputCharacters;

    /// <summary>
    /// Initializes limits with bounded defaults suitable for general-purpose processing.
    /// </summary>
    public OperationLimits()
    {
        MaxInputCharacters = 1048576;
        MaxComponentCharacters = 262144;
        MaxPathSegments = 16384;
        MaxQueryItems = 16384;
        MaxTemplateExpressions = 4096;
        MaxTemplateVariables = 16384;
        MaxTemplateCompositeMembers = 65536;
        MaxWebLinks = 4096;
        MaxWebLinkParameters = 16384;
        MaxNestingDepth = 64;
        MaxOutputCharacters = 4194304;
    }
}
