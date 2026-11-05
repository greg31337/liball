namespace asn1;

internal class IntegerConstraintSet
{
    internal IntegerInterval[] Intervals = [];
    internal System.Numerics.BigInteger FiniteCardinality;
    internal bool IsEmpty;
    internal bool HasFiniteCardinality;

    internal bool Contains(System.Numerics.BigInteger value)
    {
        int index = 0;
        while (index < this.Intervals.Length)
        {
            IntegerInterval interval = this.Intervals[index];
            bool aboveLower = !interval.HasLowerBound || value >= interval.LowerBound;
            bool belowUpper = !interval.HasUpperBound || value <= interval.UpperBound;
            if (aboveLower && belowUpper)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
