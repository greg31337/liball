namespace avif;

/// <summary>Reports the outcome of one exact random-access source request.</summary>
public class AvifRandomAccessReadResult
{
    /// <summary>The exact-read outcome.</summary>
    public AvifResultCode Code;

    /// <summary>The byte count written when the operation succeeds.</summary>
    public int BytesRead;

    /// <summary>The first unavailable absolute source offset.</summary>
    public long MissingOffset;

    /// <summary>The unavailable byte count beginning at <see cref="MissingOffset"/>.</summary>
    public long MissingLength;

    /// <summary>Creates an exact random-access read result.</summary>
    /// <param name="code">The read outcome.</param>
    /// <param name="bytesRead">The byte count written on success.</param>
    /// <param name="missingOffset">The first unavailable source offset.</param>
    /// <param name="missingLength">The unavailable byte count.</param>
    public AvifRandomAccessReadResult(
        AvifResultCode code,
        int bytesRead,
        long missingOffset,
        long missingLength)
    {
        Code = code;
        BytesRead = bytesRead;
        MissingOffset = missingOffset;
        MissingLength = missingLength;
    }
}
