namespace xml;

/// <summary>
/// Carries an XSLT extension instruction or function binding.
/// </summary>
public class XsltExtensionBinding
{
    /// <summary>
    /// The expanded extension name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The immutable implementation binding key.
    /// </summary>
    public string? BindingKey;
    /// <summary>
    /// The minimum supported extension version.
    /// </summary>
    public string? MinimumVersion;
    /// <summary>
    /// Whether the binding represents an extension instruction.
    /// </summary>
    public bool IsInstruction;
    /// <summary>
    /// Whether the binding represents an extension function.
    /// </summary>
    public bool IsFunction;
    /// <summary>
    /// The extension function implementation.
    /// </summary>
    public IXPathFunction? FunctionImplementation;
    /// <summary>
    /// The extension instruction implementation.
    /// </summary>
    public IXsltExtensionInstruction? InstructionImplementation;
}
