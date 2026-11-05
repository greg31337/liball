namespace fix;

internal static class WireLimit
{
    internal static LimitCheckResult CheckValue(int actual, int maximum)
    {
        LimitCheckResult result = new LimitCheckResult();
        result.Actual = actual;
        result.Maximum = maximum;
        if (maximum < 0)
        {
            result.ErrorCode = "invalid-limit";
            return result;
        }
        if (actual < 0)
        {
            result.ErrorCode = "invalid-actual";
            return result;
        }
        if (actual > maximum)
        {
            result.ErrorCode = "limit-exceeded";
            return result;
        }
        result.Allowed = true;
        return result;
    }

    internal static LimitCheckResult CheckCombinedValue(int current, int additional, int maximum)
    {
        LimitCheckResult result = new LimitCheckResult();
        result.Maximum = maximum;
        if (maximum < 0)
        {
            result.ErrorCode = "invalid-limit";
            return result;
        }
        if (current < 0 || additional < 0)
        {
            result.ErrorCode = "invalid-actual";
            return result;
        }
        if (current > maximum || additional > maximum - current)
        {
            result.Actual = maximum;
            result.ErrorCode = "limit-exceeded";
            return result;
        }
        result.Actual = current + additional;
        result.Allowed = true;
        return result;
    }
}
