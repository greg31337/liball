using System.Collections.Generic;

namespace iso20022;

internal class ProfileOverlayResolution
{
    internal bool Success = true;
    internal bool OperationFailure;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
    internal DiagnosticKind Kind;
    internal List<ProfileDefinition> Profiles = new List<ProfileDefinition>();
    internal List<ProfileConstraintSelection> Constraints =
        new List<ProfileConstraintSelection>();
}
