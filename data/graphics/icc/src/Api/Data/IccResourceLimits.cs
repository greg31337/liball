namespace icc;

/// <summary>
/// Defines hard limits used to bound work and allocation for untrusted profile data.
/// </summary>
public class IccResourceLimits
{
    /// <summary>
    /// Gets or sets the largest accepted profile size in bytes.
    /// </summary>
    public int MaximumProfileBytes;

    /// <summary>
    /// Gets or sets the largest carrier buffered by a stream embedding operation.
    /// </summary>
    public int MaximumCarrierBytes;

    /// <summary>
    /// Gets or sets the largest accepted number of tags in one profile.
    /// </summary>
    public int MaximumTagCount;

    /// <summary>
    /// Gets or sets the largest accepted encoded size of one tag in bytes.
    /// </summary>
    public int MaximumTagBytes;

    /// <summary>
    /// Gets or sets the largest total encoded tag payload size in bytes.
    /// </summary>
    public int MaximumTotalTagBytes;

    /// <summary>
    /// Gets or sets the largest accepted nesting depth for elements, arrays, and structures.
    /// </summary>
    public int MaximumNestingDepth;

    /// <summary>
    /// Gets or sets the largest expanded size of compressed profile content in bytes.
    /// </summary>
    public int MaximumDecompressedBytes;

    /// <summary>
    /// Gets or sets the largest calculator instruction count for one execution.
    /// </summary>
    public int MaximumCalculatorInstructions;

    /// <summary>
    /// Gets or sets the largest calculator operand stack depth.
    /// </summary>
    public int MaximumCalculatorStackDepth;

    /// <summary>
    /// Gets or sets the largest calculator temporary and persistent storage size in values.
    /// </summary>
    public int MaximumCalculatorStorageValues;

    /// <summary>
    /// Gets or sets the largest number of diagnostics retained for one operation.
    /// </summary>
    public int MaximumDiagnosticCount;

    /// <summary>
    /// Initializes conservative limits suitable for processing untrusted profiles.
    /// </summary>
    public IccResourceLimits()
    {
        MaximumProfileBytes = 67108864;
        MaximumCarrierBytes = 67108864;
        MaximumTagCount = 4096;
        MaximumTagBytes = 33554432;
        MaximumTotalTagBytes = 67108864;
        MaximumNestingDepth = 32;
        MaximumDecompressedBytes = 67108864;
        MaximumCalculatorInstructions = 100000;
        MaximumCalculatorStackDepth = 1024;
        MaximumCalculatorStorageValues = 65536;
        MaximumDiagnosticCount = 1024;
    }
}
