using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by tautomer canonicalization.
/// </summary>
public class TautomerCanonicalizationResult
{
    /// <summary>
    /// The molecule.
    /// </summary>
    public Molecule Molecule;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// The scores.
    /// </summary>
    public List<TautomerScore> Scores;
    /// <summary>
    /// The best score.
    /// </summary>
    public TautomerScore BestScore;
    /// <summary>
    /// The selected index.
    /// </summary>
    public int SelectedIndex;
    /// <summary>
    /// The tautomer count.
    /// </summary>
    public int TautomerCount;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="TautomerCanonicalizationResult"/> class.
    /// </summary>
    public TautomerCanonicalizationResult()
    {
        Molecule = new Molecule();
        Report = new MoleculeReport();
        Scores = new List<TautomerScore>();
        BestScore = new TautomerScore();
        SelectedIndex = -1;
        TautomerCount = 0;
        Success = true;
    }
}

