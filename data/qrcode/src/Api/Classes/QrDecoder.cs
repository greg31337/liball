namespace qrcode;

/// <summary>Provides reusable stateless matrix and image-detection decoding.</summary>
public class QrDecoder : IQrDecoder
{
    /// <summary>Initializes a stateless QR-family decoder.</summary>
    public QrDecoder()
    {
    }

    /// <summary>Decodes a matrix with verified error correction and the requested payload policy.</summary>
    /// <param name="matrix">The module values and optional confidence.</param>
    /// <param name="options">The decode policy and resource limits.</param>
    /// <returns>The complete decoded result.</returns>
    /// <exception cref="QrDecodeException">The symbol or payload could not be decoded.</exception>
    public QrDecodeResult DecodeMatrix(QrMatrix matrix, QrDecodeOptions options)
    {
        QrAttemptResult attempt = TryDecodeMatrix(matrix, options);
        if (attempt.Result == null)
        {
            throw new QrDecodeException("The matrix could not be decoded.", attempt.FailureReason, -1, attempt.AttemptCount);
        }
        return attempt.Result;
    }

    /// <summary>Samples the source image and decodes one supplied detection.</summary>
    /// <param name="image">The raw or supplied-binary source image.</param>
    /// <param name="detection">The candidate boundary and dimensions.</param>
    /// <param name="sampling">The preprocessing and sampling options.</param>
    /// <param name="options">The decode policy and resource limits.</param>
    /// <returns>The complete result with fresh detection diagnostics.</returns>
    /// <exception cref="QrDecodeException">The detection or its payload could not be decoded.</exception>
    public QrDecodeResult DecodeDetection(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrDecodeOptions options)
    {
        QrAttemptResult attempt = TryDecodeDetection(image, detection, sampling, options);
        if (attempt.Result == null)
        {
            throw new QrDecodeException("The detection could not be decoded.", attempt.FailureReason, detection.CandidateIndex, attempt.AttemptCount);
        }
        return attempt.Result;
    }

    /// <summary>Returns expected matrix or payload failures without throwing.</summary>
    /// <param name="matrix">The matrix to decode without modifying caller buffers.</param>
    /// <param name="options">The decode policy and resource limits.</param>
    /// <returns>The complete result or failure diagnostics, without partial payloads.</returns>
    public QrAttemptResult TryDecodeMatrix(QrMatrix matrix, QrDecodeOptions options)
    {
        MatrixRecoveryResult recovered = MatrixDecodeRecovery.RecoverCodewords(matrix, options);
        return BuildAttempt(recovered, options, recovered.AttemptCount);
    }

    /// <summary>Returns expected image-detection or payload failures without throwing.</summary>
    /// <param name="image">The raw or supplied-binary source image.</param>
    /// <param name="detection">The candidate boundary and dimensions; cached samples are ignored.</param>
    /// <param name="sampling">The preprocessing and sampling options.</param>
    /// <param name="options">The decode policy and shared recovery limits.</param>
    /// <returns>The complete result or failure diagnostics for one candidate.</returns>
    public QrAttemptResult TryDecodeDetection(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrDecodeOptions options)
    {
        ImageRecoveryResult recovered = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, options);
        MatrixRecoveryResult? matrix = recovered.Recovery;
        // Image recovery publishes a matrix only after codeword recovery succeeds.
        if (matrix == null)
        {
            return new QrAttemptResult { FailureReason = recovered.FailureReason, CandidateCount = 1, AttemptCount = recovered.AttemptCount };
        }
        QrAttemptResult attempt = BuildAttempt(matrix, options, recovered.AttemptCount);
        if (attempt.Result != null) { ImageDecodeRecovery.AddDiagnostics(attempt.Result, recovered); }
        return attempt;
    }

    internal static QrAttemptResult BuildAttempt(MatrixRecoveryResult recovered, QrDecodeOptions options, int attempts)
    {
        QrAttemptResult attempt = new QrAttemptResult { CandidateCount = 1, AttemptCount = attempts, FailureReason = recovered.FailureReason };
        MatrixCodewordResult? words = recovered.Codewords;
        if (!recovered.Success || words == null) { return attempt; }
        DecodeStatus status = new DecodeStatus();
        QrDecodeResult? result = DecodedSymbolBuilder.TryCreate(words, options, status);
        if (result == null)
        {
            attempt.FailureReason = status.FailureReason;
            return attempt;
        }
        MatrixDecodeRecovery.AddDiagnostics(result, recovered, options);
        attempt.Success = true;
        attempt.FailureReason = QrFailureReason.None;
        attempt.Result = result;
        attempt.Text = result.Text;
        attempt.RawBytes = result.RawBytes;
        attempt.Segments = result.Segments;
        attempt.CorrectedErrorCount = result.CorrectedErrorCount;
        attempt.CorrectedErasureCount = result.CorrectedErasureCount;
        return attempt;
    }
}
