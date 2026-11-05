namespace png;

/// <summary>
/// Describes one explicit change performed by the PNG repair workflow.
/// </summary>
public class PngRepairAction
{
    /// <summary>
    /// Initializes a new instance of the PngRepairAction class with its documented default values.
    /// </summary>
    public PngRepairAction()
    {
    }

    /// <summary>
    /// Stores a stable machine-readable repair code.
    /// </summary>
    public string Code = "";
    /// <summary>
    /// Stores a developer-readable description of the change.
    /// </summary>
    public string Message = "";
    /// <summary>
    /// Stores the affected chunk type, or an empty string for trailing datastream bytes.
    /// </summary>
    public string ChunkType = "";
    /// <summary>
    /// Stores the original datastream offset associated with the change.
    /// </summary>
    public long Offset;
}

/// <summary>
/// Reports every performed repair and the validation state before and after the workflow.
/// </summary>
public class PngRepairReport
{
    /// <summary>
    /// Initializes a new instance of the PngRepairReport class with its documented default values.
    /// </summary>
    public PngRepairReport()
    {
    }

    /// <summary>
    /// Indicates whether at least one requested repair changed the datastream.
    /// </summary>
    public bool Changed;
    /// <summary>
    /// Indicates whether critical or structural corruption remains after all requested safe repairs.
    /// </summary>
    public bool HasUnrepairedCriticalErrors;
    /// <summary>
    /// Stores strict validation findings for the original datastream.
    /// </summary>
    public PngValidationResult BeforeValidation = new PngValidationResult();
    /// <summary>
    /// Stores strict validation findings for the repaired datastream.
    /// </summary>
    public PngValidationResult AfterValidation = new PngValidationResult();
    /// <summary>
    /// Stores repair actions in the order they were performed.
    /// </summary>
    public PngRepairAction[] Actions = new PngRepairAction[0];
}

/// <summary>
/// Contains repaired PNG bytes and chunks together with the complete repair report.
/// </summary>
public class PngRepairResult
{
    /// <summary>
    /// Initializes a new instance of the PngRepairResult class with its documented default values.
    /// </summary>
    public PngRepairResult()
    {
    }

    /// <summary>
    /// Stores the repaired PNG datastream beginning with its signature.
    /// </summary>
    public byte[] Data = new byte[0];
    /// <summary>
    /// Stores the repaired chunks in datastream order.
    /// </summary>
    public PngChunk[] Chunks = new PngChunk[0];
    /// <summary>
    /// Stores the actions and validation findings produced by the repair workflow.
    /// </summary>
    public PngRepairReport Report = new PngRepairReport();
}
