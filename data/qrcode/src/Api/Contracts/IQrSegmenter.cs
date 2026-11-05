namespace qrcode;

/// <summary>Creates, optimizes, and validates QR payload and control segments.</summary>
public interface IQrSegmenter
{
    /// <summary>Creates zero or one automatically selected segment for text.</summary>
    /// <param name="text">The text to represent.</param>
    /// <param name="eciAssignment">The byte-mode ECI assignment, or -1 for automatic default handling.</param>
    /// <param name="enableHanziCompatibility">Whether Hanzi subset 1 compatibility mode may be selected.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    /// <returns>The selected segment, or an empty array for empty text.</returns>
    QrSegment[] CreateTextSegments(
        string text,
        int eciAssignment,
        bool enableHanziCompatibility,
        int maximumInputBytes);

    /// <summary>Creates a byte segment from caller-supplied binary data.</summary>
    /// <param name="data">The bytes to copy into the segment.</param>
    /// <param name="eciAssignment">The associated ECI assignment, or -1 for the default interpretation.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    /// <returns>A byte segment containing a defensive copy of the input.</returns>
    QrSegment CreateByteSegment(
        byte[] data,
        int eciAssignment,
        int maximumInputBytes);

    /// <summary>Finds a minimum-bit segmentation and prepends requested control segments.</summary>
    /// <param name="text">The text to optimize.</param>
    /// <param name="symbolKind">The target QR profile.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="eciAssignmentPriorities">Preferred built-in ECI assignments used to break equal-cost choices.</param>
    /// <param name="useFnc1First">Whether to prepend an FNC1 first-position control.</param>
    /// <param name="fnc1SecondApplicationIndicator">The FNC1 second-position indicator, or -1 when disabled.</param>
    /// <param name="structuredAppend">Structured-append metadata, or <c>null</c> when disabled.</param>
    /// <param name="maximumSegments">The maximum number of payload and control segments.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    /// <returns>The optimized control and payload segments in transmission order.</returns>
    QrSegment[] OptimizeTextSegments(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] eciAssignmentPriorities,
        bool useFnc1First,
        int fnc1SecondApplicationIndicator,
        QrStructuredAppendInfo? structuredAppend,
        int maximumSegments,
        int maximumInputBytes);

    /// <summary>Validates manual segments for a target profile and resource limits.</summary>
    /// <param name="segments">The segments to validate in transmission order.</param>
    /// <param name="symbolKind">The target QR profile.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="enableHanziCompatibility">Whether Hanzi subset 1 compatibility mode is enabled.</param>
    /// <param name="maximumSegments">The maximum number of payload and control segments.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    void ValidateSegments(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility,
        int maximumSegments,
        int maximumInputBytes);
}
