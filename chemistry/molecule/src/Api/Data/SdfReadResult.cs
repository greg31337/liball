using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Contains SDF records, aggregate diagnostics, version state, and record counts.
/// </summary>
public class SdfReadResult
{
    /// <summary>
    /// Retained records in input order.
    /// </summary>
    public List<SdfRecord> Records;
    /// <summary>
    /// Aggregate diagnostics from processed records.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether every processed record succeeded.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Whether both V2000 and V3000 records were observed.
    /// </summary>
    public bool MixedMolfileVersions;
    /// <summary>
    /// The number of processed records.
    /// </summary>
    public int ReadCount;
    /// <summary>
    /// The number of failed records.
    /// </summary>
    public int FailedCount;

    /// <summary>
    /// Initializes an empty SDF read result.
    /// </summary>
    public SdfReadResult()
    {
        Records = new List<SdfRecord>();
        Report = new MoleculeReport();
        Success = true;
        MixedMolfileVersions = false;
        ReadCount = 0;
        FailedCount = 0;
    }
}
