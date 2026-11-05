namespace qrcode;

/// <summary>Provides stateless QR Reed-Solomon parity generation and block repair.</summary>
public class QrReedSolomonCodec : IQrReedSolomonCodec
{
    /// <summary>Initializes a reusable stateless Reed-Solomon codec.</summary>
    public QrReedSolomonCodec()
    {
    }

    /// <summary>Creates the requested number of parity codewords for one data block.</summary>
    /// <param name="dataCodewords">The data codewords in transmission order.</param>
    /// <param name="errorCorrectionCodewordCount">The number of parity codewords to create.</param>
    /// <returns>Newly allocated parity codewords in transmission order.</returns>
    public byte[] CreateErrorCorrection(
        byte[] dataCodewords,
        int errorCorrectionCodewordCount)
    {
        return ReedSolomonEncoder.CreateParity(
            dataCodewords,
            errorCorrectionCodewordCount);
    }

    /// <summary>Repairs one data-and-parity block using optional known erasure locations.</summary>
    /// <param name="codewords">The received data and parity codewords in transmission order.</param>
    /// <param name="errorCorrectionCodewordCount">The number of trailing parity codewords.</param>
    /// <param name="erasurePositions">Known unreliable positions, or an empty array when none are known.</param>
    /// <returns>A correction result containing a defensive codeword copy and verification diagnostics.</returns>
    public QrCorrectionResult CorrectCodewords(
        byte[] codewords,
        int errorCorrectionCodewordCount,
        int[] erasurePositions)
    {
        return ReedSolomonDecoder.CorrectCodewordsWithErasures(
            codewords,
            errorCorrectionCodewordCount,
            erasurePositions);
    }
}
