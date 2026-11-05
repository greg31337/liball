using System.Collections.Generic;

namespace iso20022;

/// <summary>Controls metadata scanning without requiring full body materialization.</summary>
public class MetadataExtractionOptions
{
    /// <summary>Whether business application header fields are inspected.</summary>
    public bool IncludeHeader = true;
    /// <summary>Whether payload fields are inspected.</summary>
    public bool IncludePayload = true;
    /// <summary>Whether sensitive values are replaced by redacted markers.</summary>
    public bool RedactSensitiveValues;
    /// <summary>The resource bounds applied while extracting metadata.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Additional qualified paths treated as business references.</summary>
    public List<XmlPathData> AdditionalReferencePaths = new List<XmlPathData>();
}
