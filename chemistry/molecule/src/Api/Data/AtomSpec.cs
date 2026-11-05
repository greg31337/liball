namespace molecule;


/// <summary>
/// Provides atom spec data and operations for molecular graphs.
/// </summary>
public class AtomSpec
{
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
    /// Creates an empty atom specification.
    /// </summary>
    /// <returns>An atom specification with neutral, unset defaults.</returns>
    internal static AtomSpec CreateEmpty()
    {
        return new AtomSpec(0, 0, 0, 0, 0, 0, 0, false, "", "");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomSpec"/> class.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <param name="isotopeMassNumber">The isotope mass number.</param>
    /// <param name="formalCharge">The formal charge.</param>
    /// <param name="explicitHydrogenCount">The explicit hydrogen count.</param>
    /// <param name="implicitHydrogenCount">The implicit hydrogen count.</param>
    /// <param name="radicalElectronCount">The radical electron count.</param>
    /// <param name="atomMapNumber">The atom map number.</param>
    /// <param name="isAromatic">The is aromatic.</param>
    /// <param name="alias">The alias.</param>
    /// <param name="label">The label.</param>
    public AtomSpec(int atomicNumber, int isotopeMassNumber, int formalCharge, int explicitHydrogenCount, int implicitHydrogenCount, int radicalElectronCount, int atomMapNumber, bool isAromatic, string? alias, string? label)
    {
        AtomicNumber = atomicNumber;
        IsotopeMassNumber = isotopeMassNumber;
        FormalCharge = formalCharge;
        ExplicitHydrogenCount = explicitHydrogenCount;
        ImplicitHydrogenCount = implicitHydrogenCount;
        RadicalElectronCount = radicalElectronCount;
        AtomMapNumber = atomMapNumber;
        IsAromatic = isAromatic;
        QueryOperation = QueryOperation.None;
        QueryValue = "";

        if (alias == null)
        {
            Alias = "";
        }
        else
        {
            Alias = alias;
        }

        if (label == null)
        {
            Label = "";
        }
        else
        {
            Label = label;
        }
    }
}

