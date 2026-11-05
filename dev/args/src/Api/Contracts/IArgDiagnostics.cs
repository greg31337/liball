namespace args;

/// <summary>
/// Defines diagnostic formatting operations for application and command models.
/// </summary>
public interface IArgDiagnostics
{
    /// <summary>Describes an application model.</summary>
    string Describe(ArgApp app);
    /// <summary>Describes a command model.</summary>
    string DescribeCommand(ArgCommand command);
    /// <summary>Returns deterministic JSON metadata for an application model.</summary>
    string MetadataJson(ArgApp app);
    /// <summary>Returns deterministic JSON metadata for a command model.</summary>
    string CommandMetadataJson(ArgCommand command);
}
