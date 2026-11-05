namespace fake;

/// <summary>Represents a correlated synthetic science fixture.</summary>
/// <remarks>The experiment identifier and description are synthetic and do not represent conducted research.</remarks>
public class ScienceRecord
{
    /// <summary>The broad scientific field.</summary>
    public string Field = string.Empty;

    /// <summary>The element name correlated with its symbol and atomic number.</summary>
    public string Element = string.Empty;

    /// <summary>The chemical symbol correlated with <c>Element</c>.</summary>
    public string ElementSymbol = string.Empty;

    /// <summary>The atomic number correlated with <c>Element</c>.</summary>
    public int AtomicNumber;

    /// <summary>The synthetic correlated measurement fixture.</summary>
    public MeasurementRecord Measurement = new MeasurementRecord();

    /// <summary>A planet name.</summary>
    public string Planet = string.Empty;

    /// <summary>A generic space term.</summary>
    public string Space = string.Empty;

    /// <summary>The synthetic experiment label correlated with <c>Field</c>.</summary>
    public string Experiment = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the experiment represents conducted research, always <c>false</c>.</summary>
    public bool RepresentsExperiment;

    internal ScienceRecord()
    {
    }
}
