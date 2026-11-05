namespace template;

/// <summary>
/// Selects the precedence used for unqualified template variable names.
/// </summary>
public enum TemplateNameLookupOrder
{
    /// <summary>
    /// Searches local variables, then the current context, then the root context.
    /// </summary>
    LocalCurrentRoot = 0,
    /// <summary>
    /// Searches the current context, then local variables, then the root context.
    /// </summary>
    CurrentLocalRoot = 1,
    /// <summary>
    /// Searches the root context, then local variables, then the current context.
    /// </summary>
    RootLocalCurrent = 2
}
