using System;

namespace fake;

/// <summary>Selects the lifecycle state of a synthetic merchandise return.</summary>
public enum CommerceReturnStatus
{
    /// <summary>The return-shaped fixture has been requested but not resolved.</summary>
    Requested = 0,

    /// <summary>The return-shaped fixture has been approved.</summary>
    Approved = 1,

    /// <summary>The return-shaped fixture has been rejected.</summary>
    Rejected = 2,

    /// <summary>The return-shaped fixture has a synthetic refund calculation.</summary>
    Refunded = 3
}
