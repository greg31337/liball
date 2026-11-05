using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="SvgDepictionWriter"/>.
/// </summary>
public interface ISvgDepictionWriter
{
    /// <summary>
    /// Writes the string.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteString(Molecule? molecule);

    /// <summary>
    /// Writes the string using the specified depiction options.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string WriteStringWithOptions(Molecule? molecule, SvgDepictionOptions? options);
}

