namespace pptx;

/// <summary>
/// Controls safety limits applied while reading a presentation ZIP package.
/// </summary>
public class PptxZipReadOptions
{
    /// <summary>
    /// Sets the maximum number of entries accepted from an archive. A value of zero rejects every entry.
    /// </summary>
    public int MaxEntryCount;
    /// <summary>
    /// Sets the maximum inflated byte length accepted for one entry. A value of zero accepts only empty entries.
    /// </summary>
    public long MaxEntryBytes;
    /// <summary>
    /// Sets the maximum combined inflated byte length accepted across all entries. A value of zero accepts only empty entries.
    /// </summary>
    public long MaxTotalBytes;
    /// <summary>
    /// Sets the maximum inflated-to-compressed size ratio. A value of zero disables this check.
    /// </summary>
    public double MaxCompressionRatio;

    /// <summary>
    /// Initializes a new zip read options with safe defaults or the supplied values.
    /// </summary>
    public PptxZipReadOptions()
    {
        MaxEntryCount = 10000;
        MaxEntryBytes = 536870912;
        MaxTotalBytes = 2147483648;
        MaxCompressionRatio = 100.0;
    }
    internal static PptxZipReadOptions CreateDefault()
    {
        return new PptxZipReadOptions();
    }
    internal void Validate()
    {
        if (MaxEntryCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("MaxEntryCount");
        }

        if (MaxEntryBytes < 0L)
        {
            throw new System.ArgumentOutOfRangeException("MaxEntryBytes");
        }

        if (MaxTotalBytes < 0L)
        {
            throw new System.ArgumentOutOfRangeException("MaxTotalBytes");
        }

        if (MaxCompressionRatio < 0.0 || double.IsNaN(MaxCompressionRatio) || double.IsInfinity(MaxCompressionRatio))
        {
            throw new System.ArgumentOutOfRangeException("MaxCompressionRatio");
        }
    }
}
