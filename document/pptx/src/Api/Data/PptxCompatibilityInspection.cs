using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Contains compatibility feature occurrences grouped by handling level.
/// </summary>
public class PptxCompatibilityInspection
{
    /// <summary>Contains every classified feature in deterministic package order.</summary>
    public List<PptxCompatibilityFeature> Features;
    /// <summary>Contains features with public mutation operations.</summary>
    public List<PptxCompatibilityFeature> Editable;
    /// <summary>Contains modeled features with limited mutation support.</summary>
    public List<PptxCompatibilityFeature> Readable;
    /// <summary>Contains unmodeled features retained during round-trip.</summary>
    public List<PptxCompatibilityFeature> PreservedOnly;
    /// <summary>Contains recognized features outside supported scope.</summary>
    public List<PptxCompatibilityFeature> Unsupported;
    /// <summary>Contains features requiring explicit trust.</summary>
    public List<PptxCompatibilityFeature> Unsafe;
    /// <summary>Contains detailed security scanner diagnostics for the inspected package.</summary>
    public PptxResult SecurityDiagnostics;

    /// <summary>
    /// Initializes an empty compatibility inspection.
    /// </summary>
    public PptxCompatibilityInspection()
    {
        Features = new List<PptxCompatibilityFeature>();
        Editable = new List<PptxCompatibilityFeature>();
        Readable = new List<PptxCompatibilityFeature>();
        PreservedOnly = new List<PptxCompatibilityFeature>();
        Unsupported = new List<PptxCompatibilityFeature>();
        Unsafe = new List<PptxCompatibilityFeature>();
        SecurityDiagnostics = new PptxResult();
    }

    internal void Add(PptxCompatibilityFeature feature)
    {
        Features.Add(feature);
        if (feature.Level == PptxCompatibilityLevel.Editable)
        {
            Editable.Add(feature);
        }
        else if (feature.Level == PptxCompatibilityLevel.Readable)
        {
            Readable.Add(feature);
        }
        else if (feature.Level == PptxCompatibilityLevel.PreservedOnly)
        {
            PreservedOnly.Add(feature);
        }
        else if (feature.Level == PptxCompatibilityLevel.Unsupported)
        {
            Unsupported.Add(feature);
        }
        else
        {
            Unsafe.Add(feature);
        }
    }
}
