namespace barcode;

/// <summary>
/// Holds Data Matrix shape, size, encodation, ECI, macro, and structured-append options.
/// </summary>
public class DataMatrixOptions
{
    /// <summary>
    /// The permitted symbol shape.
    /// </summary>
    public DataMatrixShape Shape;

    /// <summary>
    /// The requested high-level encodation mode.
    /// </summary>
    public DataMatrixEncodingMode EncodingMode;

    /// <summary>
    /// The requested symbol row count, or zero when automatic.
    /// </summary>
    public int Rows;

    /// <summary>
    /// The requested symbol column count, or zero when automatic.
    /// </summary>
    public int Columns;

    /// <summary>
    /// The requested ECI assignment number, or zero when no explicit assignment is requested.
    /// </summary>
    public int EciAssignmentNumber;

    /// <summary>
    /// The requested Macro type, using zero for none, five for Macro 05, or six for Macro 06.
    /// </summary>
    public int MacroType;

    /// <summary>
    /// Indicates whether reader-programming mode is requested.
    /// </summary>
    public bool ReaderProgramming;

    /// <summary>
    /// The structured-append metadata when encoding one segment of a sequence.
    /// </summary>
    public StructuredAppendInfo? StructuredAppend;
}
