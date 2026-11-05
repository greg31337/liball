namespace uuid;

/// <summary>Defines the fixed KSUID timestamp limits.</summary>
public static class KsuidLimits
{
    /// <summary>The KSUID epoch expressed as Unix seconds.</summary>
    public const long EpochUnixSeconds = 1400000000;
    /// <summary>The largest unsigned 32-bit seconds offset.</summary>
    public const long MaxAdjustedSeconds = 4294967295;
    /// <summary>The latest representable Unix second.</summary>
    public const long MaxUnixSeconds = 5694967295;
}
