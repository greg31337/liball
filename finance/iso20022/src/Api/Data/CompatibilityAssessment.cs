using System.Collections.Generic;

namespace iso20022;

/// <summary>Classifies reader, writer, schema, data-loss, and semantic compatibility.</summary>
public class CompatibilityAssessment
{
    /// <summary>The overall compatibility classification.</summary>
    public CompatibilityKind Kind;
    /// <summary>Whether an existing source reader can consume target messages.</summary>
    public bool ReaderCompatible;
    /// <summary>Whether an existing source writer can produce acceptable target messages.</summary>
    public bool WriterCompatible;
    /// <summary>Whether the compared schemas accept the relevant opposite-version instances.</summary>
    public bool SchemaCompatible;
    /// <summary>Whether migration can retain all source information.</summary>
    public bool Lossless;
    /// <summary>Whether retained content has equivalent business meaning.</summary>
    public bool SemanticallyCompatible;
    /// <summary>The assessment confidence from zero through one.</summary>
    public decimal Confidence;
    /// <summary>The message paths that determine this assessment.</summary>
    public List<XmlPathData> AffectedPaths = new List<XmlPathData>();
    /// <summary>The differences that determine this assessment.</summary>
    public List<DifferenceItem> Differences = new List<DifferenceItem>();
    /// <summary>The ordered human-readable compatibility rationale.</summary>
    public List<string> Rationale = new List<string>();
}
