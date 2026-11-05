using System.Collections.Generic;

namespace diameter;

internal class CommandValidationFailure
{
    internal string Code = string.Empty;
    internal uint AvpCode;
    internal uint AvpVendorId;
    internal int AvpIndex = -1;
    internal int ExpectedMinimum;
    internal int ExpectedMaximum = -1;
    internal int ActualCount;
    internal int ExpectedPosition = -1;
    internal DiameterMessageFlags ExpectedFlags;
    internal DiameterMessageFlags ActualFlags;
}

internal class CommandValidationResult
{
    internal DiameterValidationStatus Status;
    internal DiameterCommandDefinition? Definition;
    internal List<CommandValidationFailure> Failures =
        new List<CommandValidationFailure>();
}
