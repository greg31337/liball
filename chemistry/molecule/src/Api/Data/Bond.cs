namespace molecule;


/// <summary>
/// Stores the endpoints and chemical attributes of one bond in a molecule.
/// </summary>
public class Bond
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The begin atom identifier.
    /// </summary>
    public int BeginAtomId;
    /// <summary>
    /// The end atom identifier.
    /// </summary>
    public int EndAtomId;
    /// <summary>
    /// The order.
    /// </summary>
    public BondOrder Order;
    /// <summary>
    /// Whether the value is aromatic.
    /// </summary>
    public bool IsAromatic;
    /// <summary>
    /// The stereo.
    /// </summary>
    public BondStereo Stereo;
    /// <summary>
    /// The direction.
    /// </summary>
    public BondDirection Direction;
    /// <summary>
    /// Whether the value is dative.
    /// </summary>
    public bool IsDative;
    /// <summary>
    /// The query operation.
    /// </summary>
    public QueryOperation QueryOperation;
    /// <summary>
    /// The query value.
    /// </summary>
    public string QueryValue;
    /// <summary>
    /// The source flags.
    /// </summary>
    public string SourceFlags;

    /// <summary>
    /// Creates an empty bond.
    /// </summary>
    /// <returns>A bond with unset identifiers and default attributes.</returns>
    internal static Bond CreateEmpty()
    {
        return new Bond(-1, -1, -1, null);
    }

    /// <summary>
    /// Creates a bond with the supplied identity, endpoints, and order.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="beginAtomId">The begin atom identifier.</param>
    /// <param name="endAtomId">The end atom identifier.</param>
    /// <param name="order">The order.</param>
    /// <returns>A bond with default attributes for the supplied order.</returns>
    internal static Bond CreateWithOrder(int id, int beginAtomId, int endAtomId, BondOrder order)
    {
        BondSpec spec = BondSpec.Create(order, false, BondStereo.None, BondDirection.None);
        return new Bond(id, beginAtomId, endAtomId, spec);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bond"/> class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="beginAtomId">The begin atom identifier.</param>
    /// <param name="endAtomId">The end atom identifier.</param>
    /// <param name="spec">The spec.</param>
    public Bond(int id, int beginAtomId, int endAtomId, BondSpec? spec)
    {
        Id = id;
        BeginAtomId = beginAtomId;
        EndAtomId = endAtomId;

        if (spec == null)
        {
            Order = BondOrder.Single;
            IsAromatic = false;
            Stereo = BondStereo.None;
            Direction = BondDirection.None;
            IsDative = false;
            QueryOperation = QueryOperation.None;
            QueryValue = "";
            SourceFlags = "";
        }
        else
        {
            Order = spec.Order;
            IsAromatic = spec.IsAromatic;
            Stereo = spec.Stereo;
            Direction = spec.Direction;
            IsDative = spec.IsDative;
            QueryOperation = spec.QueryOperation;
            QueryValue = spec.QueryValue;
            SourceFlags = spec.SourceFlags;
        }
    }
}

