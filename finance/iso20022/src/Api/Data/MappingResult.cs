using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains mapped output, rule application, unmapped content, and target validation.</summary>
public class MappingResult
{
    /// <summary>Whether mapping and requested target validation completed successfully.</summary>
    public bool Success;
    /// <summary>The exact mapping definition that was applied.</summary>
    public MappingDefinition? Mapping;
    /// <summary>The exact mapped target message identifier when applicable.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The mapped raw XML bytes when materialized.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The mapped generic message when materialized.</summary>
    public MessageDocument? Document;
    /// <summary>The mapped generated message or canonical model when materialized.</summary>
    public object? Model;
    /// <summary>The mapping rules applied successfully in deterministic order.</summary>
    public List<MappingRuleDefinition> AppliedRules = new List<MappingRuleDefinition>();
    /// <summary>The mapping rules that could not complete.</summary>
    public List<MappingRuleDefinition> FailedRules = new List<MappingRuleDefinition>();
    /// <summary>The source paths not consumed by mapping rules.</summary>
    public List<XmlPathData> UnmappedSourcePaths = new List<XmlPathData>();
    /// <summary>The required target paths not produced by mapping rules.</summary>
    public List<XmlPathData> MissingTargetPaths = new List<XmlPathData>();
    /// <summary>Whether applied rules intentionally lost source information.</summary>
    public bool DataLoss;
    /// <summary>The target validation report when validation was requested and reached.</summary>
    public ValidationReport? TargetValidation;
    /// <summary>The ordered located mapping diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when mapping could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when mapping could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
