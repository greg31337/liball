using System;
using System.Threading;

namespace iso20022;

/// <summary>Requests deterministic migration planning or execution between exact message versions.</summary>
public class MigrationRequest
{
    /// <summary>The source repository snapshot supplying message structure when available.</summary>
    public RepositorySnapshot? SourceRepository;
    /// <summary>The target repository snapshot supplying message structure when available.</summary>
    public RepositorySnapshot? TargetRepository;
    /// <summary>The exact source message identifier.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact target message identifier.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The raw source XML bytes, or an empty array for a materialized input.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic source message when supplied.</summary>
    public MessageDocument? Document;
    /// <summary>The generated strongly typed source message when supplied.</summary>
    public object? Model;
    /// <summary>The exact source-to-target difference used for planning when already available.</summary>
    public RepositoryDiff? Difference;
    /// <summary>The caller-supplied or previously generated plan used for execution.</summary>
    public MigrationPlan? Plan;
    /// <summary>An explicit mapping definition augmenting migration planning or execution.</summary>
    public MappingDefinition? Mapping;
    /// <summary>Whether unmapped source content may be retained as supplementary data.</summary>
    public bool AllowSupplementaryFallback;
    /// <summary>Whether migrated output must pass target validation.</summary>
    public bool ValidateTarget = true;
    /// <summary>The validator used for optional target validation.</summary>
    public IMessageValidator? Validator;
    /// <summary>The target validation stage and policy selection.</summary>
    public ValidationPolicy TargetValidationPolicy = new ValidationPolicy();
    /// <summary>The caller context supplied to target validation.</summary>
    public ValidationContext ValidationContext = new ValidationContext();
    /// <summary>The resource bounds applied during planning, execution, and validation.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The cancellation request observed during migration.</summary>
    public CancellationToken CancellationToken;
}
