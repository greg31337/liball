namespace molecule;


/// <summary>
/// Provides isotope data and operations for molecular graphs.
/// </summary>
public class Isotope
{
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The mass number.
    /// </summary>
    public int MassNumber;
    /// <summary>
    /// The exact mass.
    /// </summary>
    public double ExactMass;
    /// <summary>
    /// The natural abundance.
    /// </summary>
    public double NaturalAbundance;
    /// <summary>
    /// Whether this is the most abundant naturally occurring isotope or, for an element without naturally occurring isotopes, its designated representative isotope.
    /// </summary>
    public bool IsMajor;

    /// <summary>
    /// Creates an empty isotope sentinel.
    /// </summary>
    /// <returns>An isotope with zero-valued fields.</returns>
    internal static Isotope CreateEmpty()
    {
        return new Isotope(0, 0, 0.0, 0.0, false);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Isotope"/> class.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <param name="massNumber">The mass number.</param>
    /// <param name="exactMass">The exact mass.</param>
    /// <param name="naturalAbundance">The natural abundance.</param>
    /// <param name="isMajor">The is major.</param>
    public Isotope(int atomicNumber, int massNumber, double exactMass, double naturalAbundance, bool isMajor)
    {
        AtomicNumber = atomicNumber;
        MassNumber = massNumber;
        ExactMass = exactMass;
        NaturalAbundance = naturalAbundance;
        IsMajor = isMajor;
    }
}
