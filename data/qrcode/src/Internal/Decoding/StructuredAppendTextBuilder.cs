namespace qrcode;

internal class StructuredAppendTextBuilder
{
    internal static string? Build(QrDecodeResult[] parts)
    {
        global::System.Text.StringBuilder text = new global::System.Text.StringBuilder();
        global::System.Collections.Generic.List<byte> run = new global::System.Collections.Generic.List<byte>();
        int assignment = -1;
        foreach (QrDecodeResult part in parts)
        {
            if (part.Segments.Length == 0)
            {
                if (!Flush(run, assignment, text) || part.Text == null) { return null; }
                assignment = -1;
                text.Append(part.Text);
                continue;
            }
            foreach (QrSegment segment in part.Segments)
            {
                if (segment.Mode == QrEncodingMode.Byte)
                {
                    // Decoding records the effective assignment on each byte
                    // segment. A missing assignment resets to the default; ECI
                    // must never leak from the preceding symbol.
                    int next = segment.EciAssignment < 0 ? ByteEncodingInference.DefaultAssignment : segment.EciAssignment;
                    if (next != assignment && !Flush(run, assignment, text)) { return null; }
                    assignment = next;
                    run.AddRange(segment.Data);
                }
                else if (DecodedPayloadBuilder.IsPayload(segment.Mode))
                {
                    if (!Flush(run, assignment, text) || segment.Text == null) { return null; }
                    assignment = -1;
                    // Numeric/alphanumeric text already includes per-part FNC1
                    // interpretation. Joining escaped raw text could change it.
                    text.Append(segment.Text);
                }
                else if (segment.Mode == QrEncodingMode.Eci && segment.EciAssignment != assignment)
                {
                    if (!Flush(run, assignment, text)) { return null; }
                    assignment = -1;
                }
            }
        }
        return Flush(run, assignment, text) ? text.ToString() : null;
    }

    static bool Flush(global::System.Collections.Generic.List<byte> run, int assignment, global::System.Text.StringBuilder text)
    {
        if (run.Count == 0) { return true; }
        if (!EciTextTranscoder.IsSupportedAssignment(assignment)) { return false; }
        string? decoded = EciTextTranscoder.TryDecodeBytes(run.ToArray(), assignment);
        if (decoded == null) { return false; }
        text.Append(decoded);
        run.Clear();
        return true;
    }
}
