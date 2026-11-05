namespace csv;

/// <summary>
/// Stores a grouping key together with its row count and decimal sum.
/// </summary>
public class CsvAggregate
{
    /// <summary>
    /// The grouping key, which may represent a null field.
    /// </summary>
    public string? Key;
    /// <summary>
    /// The number of contained or aggregated items.
    /// </summary>
    public int Count;
    /// <summary>
    /// The decimal sum for the key.
    /// </summary>
    public decimal Sum;

    /// <summary>
    /// Initializes a new CsvAggregate with library defaults.
    /// </summary>
    public CsvAggregate()
    {
        Key = "";
        Count = 0;
        Sum = 0m;
    }

    /// <summary>
    /// Creates an initialized instance from the supplied values.
    /// </summary>
    /// <param name="key">The grouping key, which may be null.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvAggregate Create(string? key)
    {
        CsvAggregate aggregate = new CsvAggregate();
        aggregate.Key = key;
        return aggregate;
    }
}
