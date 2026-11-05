namespace xml;

/// <summary>
/// Identifies an XSLT trace event.
/// </summary>
public enum XsltTraceEventKind
{
    /// <summary>
    /// Enters a template.
    /// </summary>
    EnterTemplate = 0,
    /// <summary>
    /// Exits a template.
    /// </summary>
    ExitTemplate = 1,
    /// <summary>
    /// Enters a stylesheet function.
    /// </summary>
    EnterFunction = 2,
    /// <summary>
    /// Exits a stylesheet function.
    /// </summary>
    ExitFunction = 3,
    /// <summary>
    /// Enters an instruction.
    /// </summary>
    EnterInstruction = 4,
    /// <summary>
    /// Exits an instruction.
    /// </summary>
    ExitInstruction = 5,
    /// <summary>
    /// Enters a mode.
    /// </summary>
    EnterMode = 6,
    /// <summary>
    /// Exits a mode.
    /// </summary>
    ExitMode = 7,
    /// <summary>
    /// Enters a component.
    /// </summary>
    EnterComponent = 8,
    /// <summary>
    /// Exits a component.
    /// </summary>
    ExitComponent = 9
}
