namespace diameter;

/// <summary>
/// Defines application identifiers used by the built-in Diameter applications and extensions.
/// </summary>
public static class DiameterApplicationIds
{
    /// <summary>The Diameter common-message application identifier.</summary>
    public const uint Common = 0;

    /// <summary>The Diameter base accounting application identifier.</summary>
    public const uint BaseAccounting = 3;

    /// <summary>The Diameter credit-control application identifier.</summary>
    public const uint CreditControl = 4;

    /// <summary>The Diameter capabilities-update application identifier.</summary>
    public const uint CapabilitiesUpdate = 10;

    /// <summary>The common application identifier used by realm-based redirection.</summary>
    public const uint RealmBasedRedirect = 0;

    /// <summary>The common application identifier used by overload control.</summary>
    public const uint OverloadControl = 0;

    /// <summary>The common application identifier used by routing message priority.</summary>
    public const uint RoutingMessagePriority = 0;

    /// <summary>The common application identifier used by load information conveyance.</summary>
    public const uint Load = 0;

    /// <summary>The application identifier advertised by relay agents.</summary>
    public const uint Relay = 4294967295;
}
