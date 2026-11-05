using System;

namespace diameter;

internal class CreditControlEventReplay
{
    internal DiameterCreditControlRequest Request =
        new DiameterCreditControlRequest();
    internal DiameterCreditControlResult Result =
        new DiameterCreditControlResult();
    internal DateTimeOffset ExpiresAt;
}
