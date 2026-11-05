namespace asn1;

/// <summary>
/// Contains arbitrary-size numeric arcs for an object identifier or relative object identifier.
/// </summary>
public class AsnObjectIdentifierValue
{
    /// <summary>
    /// Indicates whether the value is a relative object identifier.
    /// </summary>
    public bool Relative;

    /// <summary>
    /// Contains non-negative arbitrary-size numeric arcs.
    /// </summary>
    public System.Numerics.BigInteger[] Arcs = [];

    internal bool IsValid()
    {
        if (this.Arcs == null)
        {
            return false;
        }

        int minimumArcCount = this.Relative ? 1 : 2;
        if (this.Arcs.Length < minimumArcCount)
        {
            return false;
        }

        foreach (System.Numerics.BigInteger arc in this.Arcs)
        {
            if (arc < System.Numerics.BigInteger.Zero)
            {
                return false;
            }
        }

        if (this.Relative)
        {
            return true;
        }

        if (this.Arcs[0] > new System.Numerics.BigInteger(2))
        {
            return false;
        }

        if (this.Arcs[0] < new System.Numerics.BigInteger(2)
            && this.Arcs[1] > new System.Numerics.BigInteger(39))
        {
            return false;
        }

        return true;
    }
}
