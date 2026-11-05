namespace asn1;

internal class ResourceCounter
{
    internal AsnResourceLimits Limits;
    internal int CurrentNestingDepth;
    internal long AllocatedBytes;
    internal int DiagnosticCount;

    internal ResourceCounter(AsnResourceLimits limits)
    {
        this.Limits = limits == null ? throw new System.ArgumentNullException("limits") : limits;
    }

    internal bool TryEnterNesting()
    {
        if (!this.Limits.Unlimited && this.CurrentNestingDepth >= this.Limits.MaximumNestingDepth)
        {
            return false;
        }

        if (this.CurrentNestingDepth < int.MaxValue)
        {
            this.CurrentNestingDepth = this.CurrentNestingDepth + 1;
        }

        return true;
    }

    internal void LeaveNesting()
    {
        if (this.CurrentNestingDepth > 0)
        {
            this.CurrentNestingDepth = this.CurrentNestingDepth - 1;
        }
    }

    internal bool AllowsCollectionElements(int count)
    {
        return this.AllowsBoundedCount(count, this.Limits.MaximumCollectionElements);
    }

    internal bool AllowsTagOctets(int count)
    {
        return this.AllowsBoundedCount(count, this.Limits.MaximumTagOctets);
    }

    internal bool AllowsLengthOctets(int count)
    {
        return this.AllowsBoundedCount(count, this.Limits.MaximumLengthOctets);
    }

    internal bool AllowsIntegerBits(int count)
    {
        return this.AllowsBoundedCount(count, this.Limits.MaximumIntegerBits);
    }

    internal bool AllowsTextCharacters(int count)
    {
        return this.AllowsBoundedCount(count, this.Limits.MaximumTextCharacters);
    }

    internal bool AllowsOpenTypeBytes(int count)
    {
        return this.AllowsBoundedCount(count, this.Limits.MaximumOpenTypeBytes);
    }

    internal bool TryChargeAllocationBytes(long count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (this.Limits.Unlimited)
        {
            if (count > long.MaxValue - this.AllocatedBytes)
            {
                this.AllocatedBytes = long.MaxValue;
            }
            else
            {
                this.AllocatedBytes = this.AllocatedBytes + count;
            }

            return true;
        }

        if (this.AllocatedBytes > this.Limits.MaximumTotalAllocationBytes || count > this.Limits.MaximumTotalAllocationBytes - this.AllocatedBytes)
        {
            return false;
        }

        this.AllocatedBytes = this.AllocatedBytes + count;
        return true;
    }

    internal bool TryChargeDiagnostic()
    {
        if (!this.Limits.Unlimited && this.DiagnosticCount >= this.Limits.MaximumDiagnostics)
        {
            return false;
        }

        if (this.DiagnosticCount < int.MaxValue)
        {
            this.DiagnosticCount = this.DiagnosticCount + 1;
        }

        return true;
    }

    internal bool AllowsBoundedCount(int count, int maximum)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        return this.Limits.Unlimited || count <= maximum;
    }
}
