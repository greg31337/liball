using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Provides descriptor value data and operations for molecular graphs.
/// </summary>
public class DescriptorValue
{
    /// <summary>
    /// The name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The text.
    /// </summary>
    public string Text;
    /// <summary>
    /// The number.
    /// </summary>
    public double Number;
    /// <summary>
    /// Whether the value is text.
    /// </summary>
    public bool IsText;
    /// <summary>
    /// Whether the value is number.
    /// </summary>
    public bool IsNumber;
    /// <summary>
    /// Whether the requested value was found.
    /// </summary>
    public bool Found;
    /// <summary>
    /// Whether the registered descriptor produced a value.
    /// </summary>
    public bool Available;
    /// <summary>
    /// The stable diagnostic code when a registered descriptor is unavailable.
    /// </summary>
    public string DiagnosticCode;
    /// <summary>
    /// The human-readable diagnostic when a registered descriptor is unavailable.
    /// </summary>
    public string DiagnosticMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptorValue"/> class.
    /// </summary>
    public DescriptorValue()
    {
        Name = "";
        Text = "";
        Number = 0.0;
        IsText = false;
        IsNumber = false;
        Found = false;
        Available = false;
        DiagnosticCode = "";
        DiagnosticMessage = "";
    }
}
