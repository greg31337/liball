namespace qrcode;

/// <summary>Provides stateless QR segment creation, optimization, and validation.</summary>
public class QrSegmenter : IQrSegmenter
{
    /// <summary>Initializes a reusable stateless QR segmenter.</summary>
    public QrSegmenter()
    {
    }

    /// <summary>Creates zero or one automatically selected segment for text.</summary>
    /// <param name="text">The text to represent.</param>
    /// <param name="eciAssignment">The byte-mode ECI assignment, or -1 for automatic default handling.</param>
    /// <param name="enableHanziCompatibility">Whether Hanzi subset 1 compatibility mode may be selected.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    /// <returns>The selected segment, or an empty array for empty text.</returns>
    public QrSegment[] CreateTextSegments(
        string text,
        int eciAssignment,
        bool enableHanziCompatibility,
        int maximumInputBytes)
    {
        return SegmentOptimizer.CreateSingleModeTextSegments(
            text,
            eciAssignment,
            enableHanziCompatibility,
            maximumInputBytes);
    }

    /// <summary>Creates a byte segment from caller-supplied binary data.</summary>
    /// <param name="data">The bytes to copy into the segment.</param>
    /// <param name="eciAssignment">The associated ECI assignment, or -1 for the default interpretation.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    /// <returns>A byte segment containing a defensive copy of the input.</returns>
    public QrSegment CreateByteSegment(
        byte[] data,
        int eciAssignment,
        int maximumInputBytes)
    {
        return SegmentOptimizer.CreateSingleModeByteSegment(
            data,
            eciAssignment,
            maximumInputBytes);
    }

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
    public QrSegment[] OptimizeTextSegments(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] eciAssignmentPriorities,
        bool useFnc1First,
        int fnc1SecondApplicationIndicator,
        QrStructuredAppendInfo? structuredAppend,
        int maximumSegments,
        int maximumInputBytes)
    {
        return SegmentOptimizer.OptimizeTextSegmentsWithControls(
            text,
            symbolKind,
            version,
            eciAssignmentPriorities,
            useFnc1First,
            fnc1SecondApplicationIndicator,
            structuredAppend,
            maximumSegments,
            maximumInputBytes);
    }

    /// <summary>Validates manual segments for a target profile and resource limits.</summary>
    /// <param name="segments">The segments to validate in transmission order.</param>
    /// <param name="symbolKind">The target QR profile.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="enableHanziCompatibility">Whether Hanzi subset 1 compatibility mode is enabled.</param>
    /// <param name="maximumSegments">The maximum number of payload and control segments.</param>
    /// <param name="maximumInputBytes">The maximum accepted input byte count.</param>
    public void ValidateSegments(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility,
        int maximumSegments,
        int maximumInputBytes)
    {
        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            maximumSegments,
            maximumInputBytes,
            enableHanziCompatibility);
    }
}
