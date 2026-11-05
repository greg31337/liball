using System.Collections.Generic;

namespace iso20022;

internal class RepositoryIntegrityResult
{
    internal bool IsValid = true;
    internal int InspectedEntityCount;
    internal int InspectedReferenceCount;
    internal List<RepositoryIntegrityDiagnostic> Diagnostics =
        new List<RepositoryIntegrityDiagnostic>();
}
