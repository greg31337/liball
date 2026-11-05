namespace molecule;


/// <summary>
/// Holds a normalized molecule copy, its applied two-dimensional transform, and diagnostics.
/// </summary>
public class CoordinateNormalizationResult
{
    /// <summary>
    /// The normalized molecule copy, or an empty molecule when no input was supplied.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// The x translation applied before rotation and scale.
    /// </summary>
    public double TranslationX;
    /// <summary>
    /// The y translation applied before rotation and scale.
    /// </summary>
    public double TranslationY;
    /// <summary>
    /// The counterclockwise rotation in degrees.
    /// </summary>
    public double RotationDegrees;
    /// <summary>
    /// The uniform scale applied to x and y coordinates.
    /// </summary>
    public double Scale;
    /// <summary>
    /// The number of assigned atom coordinates transformed.
    /// </summary>
    public int CoordinateCount;
    /// <summary>
    /// Whether the operation transformed the coordinates successfully.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Diagnostics produced by normalization.
    /// </summary>
    public MoleculeReport Report;

    /// <summary>
    /// Initializes an unsuccessful empty normalization result.
    /// </summary>
    public CoordinateNormalizationResult()
    {
        Molecule = new Molecule();
        TranslationX = 0.0;
        TranslationY = 0.0;
        RotationDegrees = 0.0;
        Scale = 1.0;
        CoordinateCount = 0;
        Success = false;
        Report = new MoleculeReport();
    }
}
