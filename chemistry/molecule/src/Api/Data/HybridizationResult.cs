using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by hybridization.
/// </summary>
public class HybridizationResult
{
    /// <summary>
    /// The atom identifier.
    /// </summary>
    public int AtomId;
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The hybridization.
    /// </summary>
    public Hybridization Hybridization;
    /// <summary>
    /// The sigma bond count.
    /// </summary>
    public int SigmaBondCount;
    /// <summary>
    /// The pi bond count.
    /// </summary>
    public int PiBondCount;
    /// <summary>
    /// The steric number.
    /// </summary>
    public int StericNumber;
    /// <summary>
    /// Whether the value is conjugated.
    /// </summary>
    public bool IsConjugated;
    /// <summary>
    /// The reason.
    /// </summary>
    public string Reason;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridizationResult"/> class.
    /// </summary>
    public HybridizationResult()
    {
        AtomId = -1;
        AtomicNumber = 0;
        Hybridization = Hybridization.Unknown;
        SigmaBondCount = 0;
        PiBondCount = 0;
        StericNumber = 0;
        IsConjugated = false;
        Reason = "";
    }
}

