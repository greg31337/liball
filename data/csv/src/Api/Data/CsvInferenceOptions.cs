namespace csv;

/// <summary>
/// Controls the size and behavior of schema inference samples.
/// </summary>
public class CsvInferenceOptions
{
    /// <summary>
    /// Maximum sampled rows; zero means no row limit.
    /// </summary>
    public int MaxSampleRows;
    /// <summary>
    /// Whether inference attempts built-in types.
    /// </summary>
    public bool InferTypes;
    /// <summary>
    /// Whether sampled nulls mark columns nullable.
    /// </summary>
    public bool InferNullability;
    /// <summary>
    /// Whether empty text counts as null during inference.
    /// </summary>
    public bool TreatEmptyAsNull;
    /// <summary>
    /// Conversion rules used to recognize values.
    /// </summary>
    public CsvConversionOptions ConversionOptions;

    /// <summary>
    /// Initializes a new CsvInferenceOptions with library defaults.
    /// </summary>
    public CsvInferenceOptions()
    {
        MaxSampleRows = 100;
        InferTypes = true;
        InferNullability = true;
        TreatEmptyAsNull = false;
        ConversionOptions = CsvConversionOptions.CreateDefault();
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvInferenceOptions CreateDefault()
    {
        CsvInferenceOptions options = new CsvInferenceOptions();
        return options;
    }
}
