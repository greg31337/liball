namespace molecule;


/// <summary>
/// Provides bond spec data and operations for molecular graphs.
/// </summary>
public class BondSpec
{
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
    /// Initializes a new instance of the <see cref="BondSpec"/> class.
    /// </summary>
    public BondSpec()
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

    /// <summary>
    /// Creates a bond specification with the supplied core attributes.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="isAromatic">The is aromatic.</param>
    /// <param name="stereo">The stereo.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>A bond specification with initialized optional fields.</returns>
    internal static BondSpec Create(BondOrder order, bool isAromatic, BondStereo stereo, BondDirection direction)
    {
        BondSpec spec = new BondSpec();
        spec.Order = order;
        spec.IsAromatic = isAromatic;
        spec.Stereo = stereo;
        spec.Direction = direction;
        return spec;
    }
}

