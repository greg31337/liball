namespace qrcode;

internal class CodewordErasureMapper
{
    // Confidence is a vote margin on a 0..255 scale. Less than half confidence
    // includes a 3-to-2 grid-sampling vote (51), but not a 4-to-1 vote (153).
    internal const int MinimumReliableConfidence = 128;

    internal static bool IsUncertain(QrMatrix matrix, int index)
    {
        return matrix.Values[index] == QrModuleValue.Unknown
            || matrix.Confidence != null && matrix.Confidence[index] < MinimumReliableConfidence;
    }

    // Marker bytes are extracted in exactly the same placement order as data,
    // without unmasking. Any marked bit erases its entire symbol character once.
    internal static int[] GetPositions(byte[] markers)
    {
        global::System.Collections.Generic.List<int> positions = new global::System.Collections.Generic.List<int>();
        for (int index = 0; index < markers.Length; index++)
        {
            if (markers[index] != 0) { positions.Add(index); }
        }
        return positions.ToArray();
    }
}
