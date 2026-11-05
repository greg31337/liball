using System.Text;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by deterministic parse.
/// </summary>
public class DeterministicParseResult
{
    /// <summary>
    /// The molecule.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage;
    /// <summary>
    /// The line number.
    /// </summary>
    public int LineNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicParseResult"/> class.
    /// </summary>
    public DeterministicParseResult()
    {
        Molecule = new Molecule();
        Success = false;
        ErrorMessage = "";
        LineNumber = 0;
    }
}

