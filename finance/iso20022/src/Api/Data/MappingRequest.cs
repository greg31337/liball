using System;
using System.Threading;

namespace iso20022;

/// <summary>Requests execution of one explicit message or canonical-model mapping.</summary>
public class MappingRequest
{
    /// <summary>The raw source XML bytes, or an empty array for a materialized input.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic source message when supplied.</summary>
    public MessageDocument? Document;
    /// <summary>The generated source message or caller canonical model when supplied.</summary>
    public object? Model;
    /// <summary>The exact inspectable mapping definition.</summary>
    public MappingDefinition? Mapping;
    /// <summary>An explicit target message identifier overriding an absent mapping target.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>Whether the mapped target must be validated before success is returned.</summary>
    public bool ValidateTarget = true;
    /// <summary>The validator used for optional target validation.</summary>
    public IMessageValidator? Validator;
    /// <summary>The target validation stage and policy selection.</summary>
    public ValidationPolicy TargetValidationPolicy = new ValidationPolicy();
    /// <summary>The caller context supplied to target validation.</summary>
    public ValidationContext ValidationContext = new ValidationContext();
    /// <summary>The resource bounds applied during mapping and target validation.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The cancellation request observed during mapping.</summary>
    public CancellationToken CancellationToken;
}
