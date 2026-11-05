namespace iso20022;

internal class SchemaCandidateSelection
{
    internal bool Success;
    internal bool Found;
    internal SchemaArtifact? Schema;
    internal string NormalizedUri = string.Empty;
    internal int CandidateCount;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
