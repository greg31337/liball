namespace molecule;


/// <summary>
/// Stores the chemical and coordinate data for one atom in a molecule.
/// </summary>
public class Atom
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The element's atomic number; <c>0</c> represents an unspecified or query atom.
    /// </summary>
    public int AtomicNumber;
    /// <summary>
    /// The isotope mass number.
    /// </summary>
    public int IsotopeMassNumber;
    /// <summary>
    /// The formal charge.
    /// </summary>
    public int FormalCharge;
    /// <summary>
    /// The explicit hydrogen count.
    /// </summary>
    public int ExplicitHydrogenCount;
    /// <summary>
    /// The implicit hydrogen count.
    /// </summary>
    public int ImplicitHydrogenCount;
    /// <summary>
    /// The radical electron count.
    /// </summary>
    public int RadicalElectronCount;
    /// <summary>
    /// The atom map number.
    /// </summary>
    public int AtomMapNumber;
    /// <summary>
    /// Whether the value is aromatic.
    /// </summary>
    public bool IsAromatic;
    /// <summary>
    /// The query operation.
    /// </summary>
    public QueryOperation QueryOperation;
    /// <summary>
    /// The query value.
    /// </summary>
    public string QueryValue;
    /// <summary>
    /// The alias.
    /// </summary>
    public string Alias;
    /// <summary>
    /// The label.
    /// </summary>
    public string Label;
    /// <summary>
    /// The format-independent atom type, such as a MOL2 atom type.
    /// </summary>
    public string AtomType;
    /// <summary>
    /// The partial atomic charge.
    /// </summary>
    public double PartialCharge;
    /// <summary>
    /// Whether <see cref="PartialCharge"/> is assigned.
    /// </summary>
    public bool HasPartialCharge;
    /// <summary>
    /// The residue identifier, or <c>-1</c> when the atom is not assigned to a residue.
    /// </summary>
    public int ResidueId;
    /// <summary>
    /// The residue name supplied with the atom record.
    /// </summary>
    public string ResidueName;
    /// <summary>
    /// The chain identifier supplied with the atom record.
    /// </summary>
    public string ChainId;
    /// <summary>
    /// The model identifier, or <c>-1</c> when the atom is not assigned to a model.
    /// </summary>
    public int ModelId;
    /// <summary>
    /// The atom occupancy.
    /// </summary>
    public double Occupancy;
    /// <summary>
    /// Whether <see cref="Occupancy"/> is assigned.
    /// </summary>
    public bool HasOccupancy;
    /// <summary>
    /// The crystallographic temperature factor.
    /// </summary>
    public double TemperatureFactor;
    /// <summary>
    /// Whether <see cref="TemperatureFactor"/> is assigned.
    /// </summary>
    public bool HasTemperatureFactor;
    /// <summary>
    /// The alternate-location identifier.
    /// </summary>
    public string AlternateLocation;
    /// <summary>
    /// The residue insertion code.
    /// </summary>
    public string InsertionCode;
    /// <summary>
    /// Whether the value has coordinates.
    /// </summary>
    public bool HasCoordinates;
    /// <summary>
    /// The x.
    /// </summary>
    public double X;
    /// <summary>
    /// The y.
    /// </summary>
    public double Y;
    /// <summary>
    /// The z.
    /// </summary>
    public double Z;

    /// <summary>
    /// Creates an empty atom.
    /// </summary>
    /// <returns>An atom with an unset identifier and default attributes.</returns>
    internal static Atom CreateEmpty()
    {
        return new Atom(-1, null);
    }

    /// <summary>
    /// Creates an atom with the supplied identity and atomic number.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <returns>An atom with default attributes for the supplied element.</returns>
    internal static Atom CreateWithAtomicNumber(int id, int atomicNumber)
    {
        AtomSpec spec = new AtomSpec(atomicNumber, 0, 0, 0, 0, 0, 0, false, "", "");
        return new Atom(id, spec);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Atom"/> class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="spec">The spec.</param>
    public Atom(int id, AtomSpec? spec)
    {
        Id = id;

        if (spec == null)
        {
            AtomicNumber = 0;
            IsotopeMassNumber = 0;
            FormalCharge = 0;
            ExplicitHydrogenCount = 0;
            ImplicitHydrogenCount = 0;
            RadicalElectronCount = 0;
            AtomMapNumber = 0;
            IsAromatic = false;
            QueryOperation = QueryOperation.None;
            QueryValue = "";
            Alias = "";
            Label = "";
        }
        else
        {
            AtomicNumber = spec.AtomicNumber;
            IsotopeMassNumber = spec.IsotopeMassNumber;
            FormalCharge = spec.FormalCharge;
            ExplicitHydrogenCount = spec.ExplicitHydrogenCount;
            ImplicitHydrogenCount = spec.ImplicitHydrogenCount;
            RadicalElectronCount = spec.RadicalElectronCount;
            AtomMapNumber = spec.AtomMapNumber;
            IsAromatic = spec.IsAromatic;
            QueryOperation = spec.QueryOperation;
            QueryValue = spec.QueryValue;
            Alias = spec.Alias;
            Label = spec.Label;
        }

        HasCoordinates = false;
        X = 0.0;
        Y = 0.0;
        Z = 0.0;
        AtomType = "";
        PartialCharge = 0.0;
        HasPartialCharge = false;
        ResidueId = -1;
        ResidueName = "";
        ChainId = "";
        ModelId = -1;
        Occupancy = 0.0;
        HasOccupancy = false;
        TemperatureFactor = 0.0;
        HasTemperatureFactor = false;
        AlternateLocation = "";
        InsertionCode = "";
    }
}
