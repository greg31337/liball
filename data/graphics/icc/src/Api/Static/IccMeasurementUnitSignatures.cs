namespace icc;

/// <summary>Defines registered response-curve measurement-unit signatures.</summary>
public static class IccMeasurementUnitSignatures
{
    /// <summary>The ISO 5-3 Status A density response <c>StaA</c>.</summary>
    public const uint StatusA = 0x53746141;
    /// <summary>The ISO 5-3 Status E density response <c>StaE</c>.</summary>
    public const uint StatusE = 0x53746145;
    /// <summary>The ISO 5-3 Status I density response <c>StaI</c>.</summary>
    public const uint StatusI = 0x53746149;
    /// <summary>The ISO 5-3 Status T density response <c>StaT</c>.</summary>
    public const uint StatusT = 0x53746154;
    /// <summary>The ISO 5-3 Status M density response <c>StaM</c>.</summary>
    public const uint StatusM = 0x5374614d;
    /// <summary>The DIN E response without a polarizing filter <c>DN  </c>.</summary>
    public const uint DinE = 0x444e2020;
    /// <summary>The DIN E response with a polarizing filter <c>DN P</c>.</summary>
    public const uint DinEPolarized = 0x444e2050;
    /// <summary>The DIN I response without a polarizing filter <c>DNN </c>.</summary>
    public const uint DinI = 0x444e4e20;
    /// <summary>The DIN I response with a polarizing filter <c>DNNP</c>.</summary>
    public const uint DinIPolarized = 0x444e4e50;
}
