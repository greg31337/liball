namespace qrcode;

/// <summary>Contains decoded symbols, structured-append assembly outputs, and scan diagnostics.</summary>
public class QrDecodeSet
{
    /// <summary>All successfully decoded individual symbols.</summary>
    public QrDecodeResult[] Results = new QrDecodeResult[0];

    /// <summary>Structured-append parts that could not be assembled.</summary>
    public QrDecodeResult[] UnassembledResults = new QrDecodeResult[0];

    /// <summary>Combined structured-append payloads.</summary>
    public QrDecodeResult[] AssembledResults = new QrDecodeResult[0];

    /// <summary>Failed decode attempts retained for diagnostics.</summary>
    public QrAttemptResult[] FailedAttempts = new QrAttemptResult[0];

    /// <summary>The number of detection candidates examined.</summary>
    public int CandidateCount;

    /// <summary>Non-fatal scan and assembly diagnostics in processing order.</summary>
    public string[] Diagnostics = new string[0];
}
