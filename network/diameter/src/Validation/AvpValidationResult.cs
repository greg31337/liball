using System.Collections.Generic;

namespace diameter;

internal class AvpValidationFailure
{
    internal string Code = string.Empty;
    internal string ValueCode = string.Empty;
    internal DiameterAvp? Avp;
    internal DiameterAvpDefinition? Definition;
    internal uint AvpCode;
    internal uint AvpVendorId;
    internal uint ParentAvpCode;
    internal uint ParentAvpVendorId;
    internal int AvpIndex = -1;
    internal int Depth;
    internal int ExpectedLength = -1;
    internal int ActualLength = -1;
    internal int ExpectedMinimum;
    internal int ExpectedMaximum = -1;
    internal int ActualCount;
    internal int ExpectedPosition = -1;
    internal DiameterAvpFlags ExpectedFlags;
    internal DiameterAvpFlags ActualFlags;
}

internal class AvpValidationResult
{
    internal DiameterValidationStatus Status;
    internal List<AvpValidationFailure> Failures =
        new List<AvpValidationFailure>();
}
