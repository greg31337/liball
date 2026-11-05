using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides molecule error data and operations for molecular graphs.
/// </summary>
public class MoleculeError
{
    /// <summary>
    /// The code.
    /// </summary>
    public string Code;
    /// <summary>
    /// The message.
    /// </summary>
    public string Message;
    /// <summary>
    /// The location.
    /// </summary>
    public DiagnosticLocation Location;
    /// <summary>
    /// The atom identifier.
    /// </summary>
    public int AtomId;
    /// <summary>
    /// The bond identifier.
    /// </summary>
    public int BondId;
    /// <summary>
    /// The severity.
    /// </summary>
    public ErrorSeverity Severity;

    /// <summary>
    /// Creates an empty error.
    /// </summary>
    /// <returns>An error with empty text, no graph references, and error severity.</returns>
    internal static MoleculeError CreateEmpty()
    {
        return new MoleculeError("", "", DiagnosticLocation.CreateEmpty(), -1, -1, ErrorSeverity.Error);
    }

    /// <summary>
    /// Creates an error without source or graph location information.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="severity">The severity.</param>
    /// <returns>An error containing the supplied code, message, and severity.</returns>
    internal static MoleculeError Create(string? code, string? message, ErrorSeverity severity)
    {
        return new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), -1, -1, severity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeError"/> class.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="location">The location.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <param name="severity">The severity.</param>
    public MoleculeError(string? code, string? message, DiagnosticLocation? location, int atomId, int bondId, ErrorSeverity severity)
    {
        if (code == null)
        {
            Code = "";
        }
        else
        {
            Code = code;
        }

        if (message == null)
        {
            Message = "";
        }
        else
        {
            Message = message;
        }

        if (location == null)
        {
            Location = DiagnosticLocation.CreateEmpty();
        }
        else
        {
            Location = location;
        }

        AtomId = atomId;
        BondId = bondId;
        Severity = severity;
    }

    /// <summary>
    /// Determines whether the value has atom.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasAtom()
    {
        return AtomId >= 0;
    }

    /// <summary>
    /// Determines whether the value has bond.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasBond()
    {
        return BondId >= 0;
    }

    /// <summary>
    /// Determines whether the value is error.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsError()
    {
        return Severity == ErrorSeverity.Error || Severity == ErrorSeverity.Fatal;
    }

    /// <summary>
    /// Determines whether the value is warning.
    /// </summary>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsWarning()
    {
        return Severity == ErrorSeverity.Warning;
    }
}

