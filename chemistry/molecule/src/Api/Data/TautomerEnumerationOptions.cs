using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Controls tautomer enumeration options behavior.
/// </summary>
public class TautomerEnumerationOptions
{
    /// <summary>
    /// The maximum tautomer count.
    /// </summary>
    public int MaximumTautomerCount;
    /// <summary>
    /// The maximum transform count.
    /// </summary>
    public int MaximumTransformCount;
    /// <summary>
    /// Whether input molecule contributes to the operation.
    /// </summary>
    public bool IncludeInputMolecule;

    /// <summary>
    /// Initializes a new instance of the <see cref="TautomerEnumerationOptions"/> class.
    /// </summary>
    public TautomerEnumerationOptions()
    {
        MaximumTautomerCount = 100;
        MaximumTransformCount = 1000;
        IncludeInputMolecule = true;
    }
}

