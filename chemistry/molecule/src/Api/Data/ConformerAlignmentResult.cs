using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Holds an aligned conformer copy, the applied similarity transform, mapping, RMSD, and diagnostics.
/// </summary>
public class ConformerAlignmentResult
{
    /// <summary>
    /// The aligned moving conformer copy.
    /// </summary>
    public Conformer Conformer;
    /// <summary>
    /// The atom mappings used for the fit.
    /// </summary>
    public List<ConformerAtomMapping> AtomMappings;
    /// <summary>
    /// The x component of the applied translation.
    /// </summary>
    public double TranslationX;
    /// <summary>
    /// The y component of the applied translation.
    /// </summary>
    public double TranslationY;
    /// <summary>
    /// The z component of the applied translation.
    /// </summary>
    public double TranslationZ;
    /// <summary>
    /// The uniform applied scale.
    /// </summary>
    public double Scale;
    /// <summary>
    /// Rotation matrix row x, column x.
    /// </summary>
    public double RotationXX;
    /// <summary>
    /// Rotation matrix row x, column y.
    /// </summary>
    public double RotationXY;
    /// <summary>
    /// Rotation matrix row x, column z.
    /// </summary>
    public double RotationXZ;
    /// <summary>
    /// Rotation matrix row y, column x.
    /// </summary>
    public double RotationYX;
    /// <summary>
    /// Rotation matrix row y, column y.
    /// </summary>
    public double RotationYY;
    /// <summary>
    /// Rotation matrix row y, column z.
    /// </summary>
    public double RotationYZ;
    /// <summary>
    /// Rotation matrix row z, column x.
    /// </summary>
    public double RotationZX;
    /// <summary>
    /// Rotation matrix row z, column y.
    /// </summary>
    public double RotationZY;
    /// <summary>
    /// Rotation matrix row z, column z.
    /// </summary>
    public double RotationZZ;
    /// <summary>
    /// The root-mean-square distance over mapped coordinates after alignment.
    /// </summary>
    public double Rmsd;
    /// <summary>
    /// Whether alignment completed successfully.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Diagnostics produced during alignment.
    /// </summary>
    public MoleculeReport Report;

    /// <summary>
    /// Initializes an unsuccessful identity-transform result.
    /// </summary>
    public ConformerAlignmentResult()
    {
        Conformer = new Conformer(0);
        AtomMappings = new List<ConformerAtomMapping>();
        TranslationX = 0.0;
        TranslationY = 0.0;
        TranslationZ = 0.0;
        Scale = 1.0;
        RotationXX = 1.0;
        RotationXY = 0.0;
        RotationXZ = 0.0;
        RotationYX = 0.0;
        RotationYY = 1.0;
        RotationYZ = 0.0;
        RotationZX = 0.0;
        RotationZY = 0.0;
        RotationZZ = 1.0;
        Rmsd = 0.0;
        Success = false;
        Report = new MoleculeReport();
    }
}
