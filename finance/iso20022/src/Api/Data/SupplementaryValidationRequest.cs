using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Requests discovery, preservation, location resolution, and optional schema validation of supplementary data.</summary>
public class SupplementaryValidationRequest
{
    /// <summary>The raw XML bytes containing the message and supplementary envelopes.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generic message document when validation begins from materialized XML.</summary>
    public MessageDocument? Document;
    /// <summary>The active profile that supplies supplementary-data policy when available.</summary>
    public ProfileDefinition? Profile;
    /// <summary>The ordered caller policies for registered or anticipated payload namespaces.</summary>
    public List<SupplementaryDataPolicy> Policies = new List<SupplementaryDataPolicy>();
    /// <summary>The local schema registry used to resolve supplementary schemas.</summary>
    public ISchemaRegistry? SchemaRegistry;
    /// <summary>The local XSD validator used when payload schema validation is requested.</summary>
    public IXsdValidator? XsdValidator;
    /// <summary>The resource bounds applied during discovery and validation.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Whether available registered schemas are applied to extension payloads.</summary>
    public bool ValidateSchemas = true;
    /// <summary>Whether namespace-complete envelopes and payloads are retained in the result.</summary>
    public bool PreserveContent = true;
}
