namespace qrcode;

/// <summary>Contains the result of a low-level capacity calculation.</summary>
public class QrCapacityResult
{
    /// <summary>Whether the requested payload fits the selected symbol parameters.</summary>
    public bool Fits;

    /// <summary>The number of bits required for mode, count, control, and payload data.</summary>
    public int RequiredBitCount;

    /// <summary>The available number of data bits before terminator and padding rules are applied.</summary>
    public int DataBitCapacity;

    /// <summary>The number of unused data bits, or a negative value when the request does not fit.</summary>
    public int UnusedBitCount;

    /// <summary>The number of data codewords.</summary>
    public int DataCodewordCount;

    /// <summary>The number of error-correction codewords.</summary>
    public int ErrorCorrectionCodewordCount;

    /// <summary>The total number of data and error-correction codewords.</summary>
    public int TotalCodewordCount;

    /// <summary>The number of remainder bits after codeword placement.</summary>
    public int RemainderBitCount;
}
