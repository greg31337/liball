namespace qrcode;

/// <summary>Encodes text, bytes, explicit segments, and structured-append sequences.</summary>
public interface IQrEncoder
{
    /// <summary>Encodes text using the permitted modes and symbol families.</summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="options">The symbol, error-correction, control, and resource options.</param>
    /// <returns>The completed QR-family symbol.</returns>
    QrSymbol EncodeText(string text, QrEncodeOptions options);

    /// <summary>Encodes caller-supplied bytes without changing their values.</summary>
    /// <param name="data">The bytes to encode.</param>
    /// <param name="options">The symbol, error-correction, control, and resource options.</param>
    /// <returns>The completed QR-family symbol.</returns>
    QrSymbol EncodeBytes(byte[] data, QrEncodeOptions options);

    /// <summary>Encodes an explicit ordered collection of payload and control segments.</summary>
    /// <param name="segments">The segments in transmission order.</param>
    /// <param name="options">The symbol, error-correction, and resource options.</param>
    /// <returns>The completed QR-family symbol.</returns>
    QrSymbol EncodeSegments(QrSegment[] segments, QrEncodeOptions options);

    /// <summary>Splits bytes into a complete structured-append Model 2 sequence.</summary>
    /// <param name="data">The complete unsplit message bytes.</param>
    /// <param name="options">The Model 2, control, and resource options.</param>
    /// <returns>The completed structured-append sequence.</returns>
    QrSymbolSequence EncodeStructuredAppend(byte[] data, QrEncodeOptions options);

    /// <summary>Encodes manually partitioned structured-append segments.</summary>
    /// <param name="parts">The complete ordered parts, each beginning with its manual header.</param>
    /// <param name="options">The Model 2 and resource options.</param>
    /// <returns>The validated completed structured-append sequence.</returns>
    QrSymbolSequence EncodeManualStructuredAppend(
        QrSegment[][] parts,
        QrEncodeOptions options);

    /// <summary>Attempts to encode text without throwing for expected representation or capacity failures.</summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="options">The symbol, error-correction, control, and resource options.</param>
    /// <returns>The encoded symbol or a machine-readable expected failure.</returns>
    QrEncodeAttemptResult TryEncodeText(string text, QrEncodeOptions options);

    /// <summary>Attempts to encode explicit segments without throwing for capacity failure.</summary>
    /// <param name="segments">The segments in transmission order.</param>
    /// <param name="options">The symbol, error-correction, and resource options.</param>
    /// <returns>The encoded symbol or exact capacity diagnostics.</returns>
    QrEncodeAttemptResult TryEncodeSegments(
        QrSegment[] segments,
        QrEncodeOptions options);
}
