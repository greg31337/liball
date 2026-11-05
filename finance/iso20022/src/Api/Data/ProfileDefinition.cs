using System.Collections.Generic;

namespace iso20022;

/// <summary>Defines a versioned market-practice or caller overlay without mutating the base ISO definition.</summary>
public class ProfileDefinition
{
    /// <summary>The stable profile identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The human-readable profile name.</summary>
    public string Name = string.Empty;
    /// <summary>The exact profile version.</summary>
    public string Version = string.Empty;
    /// <summary>The human-readable profile definition.</summary>
    public string Definition = string.Empty;
    /// <summary>The inherited profile identifier, or an empty value for a direct ISO overlay.</summary>
    public string BaseProfileIdentifier = string.Empty;
    /// <summary>The compatible base message version policy.</summary>
    public VersionSelector BaseVersion = new VersionSelector();
    /// <summary>The exact message identifiers selected by this profile, or an empty list when the base version policy is sufficient.</summary>
    public List<MessageIdentifier> MessageIdentifiers = new List<MessageIdentifier>();
    /// <summary>The exact namespace URIs selected by this profile.</summary>
    public List<string> NamespaceUris = new List<string>();
    /// <summary>The profile cardinality, value, path, and inherited-constraint changes.</summary>
    public List<ProfileConstraint> Constraints = new List<ProfileConstraint>();
    /// <summary>The profile semantic rule additions, removals, and overrides.</summary>
    public List<ProfileRuleReference> Rules = new List<ProfileRuleReference>();
    /// <summary>The supplementary namespace, schema, ignore, and understanding policies supplied by this profile.</summary>
    public List<SupplementaryDataPolicy> SupplementaryDataPolicies =
        new List<SupplementaryDataPolicy>();
    /// <summary>The exact profile artifact provenance.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
}
