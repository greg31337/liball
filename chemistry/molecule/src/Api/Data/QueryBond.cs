namespace molecule;


/// <summary>
/// Provides query bond data and operations for molecular graphs.
/// </summary>
public class QueryBond
{
    /// <summary>
    /// The zero-based identifier within the containing collection.
    /// </summary>
    public int Id;
    /// <summary>
    /// The begin query atom identifier.
    /// </summary>
    public int BeginQueryAtomId;
    /// <summary>
    /// The end query atom identifier.
    /// </summary>
    public int EndQueryAtomId;
    /// <summary>
    /// The raw text.
    /// </summary>
    public string RawText;
    /// <summary>
    /// The logical SMARTS expression evaluated for this bond.
    /// </summary>
    public QueryExpression Expression;
    /// <summary>
    /// Whether this query primitive accepts any value.
    /// </summary>
    public bool MatchesAny;
    /// <summary>
    /// Whether the value has order.
    /// </summary>
    public bool HasOrder;
    /// <summary>
    /// The order.
    /// </summary>
    public BondOrder Order;
    /// <summary>
    /// Whether the value has aromaticity.
    /// </summary>
    public bool HasAromaticity;
    /// <summary>
    /// Whether the value is aromatic.
    /// </summary>
    public bool IsAromatic;
    /// <summary>
    /// Whether the value has direction.
    /// </summary>
    public bool HasDirection;
    /// <summary>
    /// The direction.
    /// </summary>
    public BondDirection Direction;
    /// <summary>
    /// Whether the value has stereo.
    /// </summary>
    public bool HasStereo;
    /// <summary>
    /// The stereo.
    /// </summary>
    public BondStereo Stereo;
    /// <summary>
    /// Whether the value has dative.
    /// </summary>
    public bool HasDative;
    /// <summary>
    /// Whether the value is dative.
    /// </summary>
    public bool IsDative;
    /// <summary>
    /// Whether the value is ring bond.
    /// </summary>
    public bool IsRingBond;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBond"/> class.
    /// </summary>
    public QueryBond()
    {
        Id = -1;
        BeginQueryAtomId = -1;
        EndQueryAtomId = -1;
        RawText = "";
        Expression = new QueryExpression();
        MatchesAny = true;
        HasOrder = false;
        Order = BondOrder.Single;
        HasAromaticity = false;
        IsAromatic = false;
        HasDirection = false;
        Direction = BondDirection.None;
        HasStereo = false;
        Stereo = BondStereo.None;
        HasDative = false;
        IsDative = false;
        IsRingBond = false;
    }

    /// <summary>
    /// Sets the any.
    /// </summary>
    internal void SetAny()
    {
        MatchesAny = true;
        HasOrder = false;
        HasAromaticity = false;
        HasDirection = false;
        HasStereo = false;
        HasDative = false;
        IsRingBond = false;
    }

    /// <summary>
    /// Sets the order.
    /// </summary>
    /// <param name="order">The order.</param>
    internal void SetOrder(BondOrder order)
    {
        MatchesAny = false;
        HasOrder = true;
        Order = order;
    }

    /// <summary>
    /// Sets the aromaticity.
    /// </summary>
    /// <param name="isAromatic">The is aromatic.</param>
    internal void SetAromaticity(bool isAromatic)
    {
        MatchesAny = false;
        HasAromaticity = true;
        IsAromatic = isAromatic;
    }

    /// <summary>
    /// Sets the direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    internal void SetDirection(BondDirection direction)
    {
        MatchesAny = false;
        HasDirection = true;
        Direction = direction;
    }

    /// <summary>
    /// Sets the stereo.
    /// </summary>
    /// <param name="stereo">The stereo.</param>
    internal void SetStereo(BondStereo stereo)
    {
        MatchesAny = false;
        HasStereo = true;
        Stereo = stereo;
    }

    /// <summary>
    /// Sets the dative.
    /// </summary>
    /// <param name="isDative">The is dative.</param>
    internal void SetDative(bool isDative)
    {
        MatchesAny = false;
        HasDative = true;
        IsDative = isDative;
    }

    /// <summary>
    /// Sets the ring bond.
    /// </summary>
    internal void SetRingBond()
    {
        MatchesAny = false;
        IsRingBond = true;
    }

    /// <summary>
    /// Determines whether the supplied atom or bond satisfies this query primitive.
    /// </summary>
    /// <param name="bond">The bond.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool Matches(Bond? bond)
    {
        if (bond == null)
        {
            return false;
        }

        if (HasOrder && bond.Order != Order)
        {
            return false;
        }

        if (HasAromaticity && bond.IsAromatic != IsAromatic)
        {
            return false;
        }

        if (HasDirection && bond.Direction != Direction)
        {
            return false;
        }

        if (HasStereo && bond.Stereo != Stereo)
        {
            return false;
        }

        if (HasDative && bond.IsDative != IsDative)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the supplied atom or bond satisfies this query primitive.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool MatchesBondInMolecule(Molecule? molecule, int bondId)
    {
        if (molecule == null || bondId < 0 || bondId >= molecule.Bonds.Count)
        {
            return false;
        }

        if (Expression.Operation != QueryOperation.None)
        {
            return new SmartsExpressionMatcher().MatchesBondExpression(Expression, molecule, bondId);
        }

        if (!Matches(molecule.Bonds[bondId]))
        {
            return false;
        }

        if (IsRingBond)
        {
            RingFinder finder = new RingFinder();
            RingSet ringSet = finder.EnsureRingCache(molecule);
            if (!RingSetContainsBond(ringSet, bondId))
            {
                return false;
            }
        }

        return true;
    }

    internal bool RingSetContainsBond(RingSet ringSet, int bondId)
    {
        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            Ring ring = ringSet.Rings[ringIndex];
            int bondIndex = 0;
            while (bondIndex < ring.BondIds.Count)
            {
                if (ring.BondIds[bondIndex] == bondId)
                {
                    return true;
                }

                bondIndex = bondIndex + 1;
            }

            ringIndex = ringIndex + 1;
        }

        return false;
    }
}
