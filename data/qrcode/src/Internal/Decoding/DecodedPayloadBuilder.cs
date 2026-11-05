namespace qrcode;

internal class DecodedPayloadBuilder
{
    // Consumes structurally parsed segments. RawBytes concatenates payload Data
    // without control designators; per-segment Data keeps the exact representation
    // (including escaped FNC1 percent characters). Text carries FNC1 interpretation.
    // The identifier describes the parsed profile and explicit controls only;
    // symbol conformance is assessed separately.
    internal static QrDecodeResult Create(PayloadParseResult parsed, QrDecodeOptions options)
    {
        DecodeStatus status = new DecodeStatus();
        QrDecodeResult? result = TryCreate(parsed, options, status);
        if (result == null) { throw status.Error ?? new QrDecodeException("The payload is invalid.", status.FailureReason, -1, 0); }
        return result;
    }

    internal static QrDecodeResult? TryCreate(PayloadParseResult parsed, QrDecodeOptions options, DecodeStatus status)
    {
        if (!ValidateInput(parsed, options, status)) { return null; }
        QrDecodeResult result = new QrDecodeResult { Segments = new QrSegment[parsed.Segments.Length] };
        global::System.Collections.Generic.List<int> assignments = new global::System.Collections.Generic.List<int>();
        global::System.Collections.Generic.List<string> warnings = new global::System.Collections.Generic.List<string>();
        global::System.Text.StringBuilder text = new global::System.Text.StringBuilder();
        int activeEci = -1;
        int rawCount = 0;
        bool completeText = true;
        for (int index = 0; index < parsed.Segments.Length; index++)
        {
            QrSegment source = parsed.Segments[index];
            QrSegment segment = CopySegment(source);
            result.Segments[index] = segment;
            if (segment.Mode == QrEncodingMode.Eci)
            {
                activeEci = segment.EciAssignment;
                assignments.Add(activeEci);
            }
            else if (segment.Mode == QrEncodingMode.Fnc1First)
            {
                result.UsesFnc1First = true;
            }
            else if (segment.Mode == QrEncodingMode.Fnc1Second)
            {
                result.Fnc1SecondApplicationIndicator = segment.Fnc1ApplicationIndicator;
            }
            else if (segment.Mode == QrEncodingMode.StructuredAppend)
            {
                result.StructuredAppend = segment.StructuredAppend;
            }
            else
            {
                rawCount += segment.Data.Length;
                if (segment.Mode == QrEncodingMode.Byte)
                {
                    int assignment = ByteEncodingInference.ResolveAssignment(segment.Data, activeEci,
                        options.EnableByteEncodingInference, false);
                    if (activeEci < 0 && options.RequireExplicitEci && assignment != ByteEncodingInference.DefaultAssignment)
                    {
                        status.Fail(QrFailureReason.InvalidData, "A non-default byte encoding requires an explicit ECI designator.");
                        return null;
                    }
                    segment.EciAssignment = assignment;
                    if (activeEci < 0 && assignment != ByteEncodingInference.DefaultAssignment)
                    {
                        warnings.Add($"Segment {index}: inferred byte encoding ECI {assignment} without an explicit designator.");
                    }

                    if (!EciTextTranscoder.IsSupportedAssignment(assignment))
                    {
                        if (!options.PreserveUnsupportedEciBytes)
                        {
                            status.FailureReason = QrFailureReason.UnsupportedTextEncoding;
                            status.Error = new QrUnsupportedFeatureException("The byte segment ECI has no supported text mapping.",
                                QrFailureReason.UnsupportedTextEncoding, "EciTextMapping", assignment);
                            return null;
                        }

                        segment.Text = null;
                        warnings.Add($"Segment {index}: unsupported ECI {assignment}; raw bytes retained.");
                    }
                    else
                    {
                        segment.Text = EciTextTranscoder.TryDecodeBytes(segment.Data, assignment);
                        if (segment.Text == null)
                        {
                            warnings.Add($"Segment {index}: malformed or unmapped bytes for ECI {assignment}; raw bytes retained.");
                        }
                    }
                }
                else if (segment.Mode == QrEncodingMode.Kanji || segment.Mode == QrEncodingMode.Hanzi)
                {
                    int assignment = segment.Mode == QrEncodingMode.Kanji ? QrEciAssignments.ShiftJis : QrEciAssignments.Gb2312;
                    segment.Text = EciTextTranscoder.TryDecodeBytes(segment.Data, assignment);
                    if (segment.Text == null)
                    {
                        warnings.Add($"Segment {index}: malformed or unmapped {segment.Mode} character mapping; raw bytes retained.");
                    }

                    if (segment.Mode == QrEncodingMode.Hanzi)
                    {
                        result.UsesHanziCompatibility = true;
                    }
                }

                if (segment.Text == null)
                {
                    completeText = false;
                }
                else
                {
                    text.Append(segment.Text);
                }
            }
        }

        result.RawBytes = new byte[rawCount];
        int offset = 0;
        foreach (QrSegment segment in result.Segments)
        {
            if (IsPayload(segment.Mode))
            {
                global::System.Array.Copy(segment.Data, 0, result.RawBytes, offset, segment.Data.Length);
                offset += segment.Data.Length;
            }
        }

        result.Text = completeText ? text.ToString() : null;
        result.EciAssignments = assignments.ToArray();
        result.Warnings = warnings.ToArray();
        result.SymbologyIdentifier = SymbologyIdentifierSelector.GetIdentifier(parsed.SymbolKind, assignments.Count > 0,
            result.UsesFnc1First, result.Fnc1SecondApplicationIndicator);
        return result;
    }

    internal static QrSegment CopySegment(QrSegment source)
    {
        QrSegment result = new QrSegment
        {
            Mode = source.Mode, CharacterCount = source.CharacterCount,
            Data = (byte[])source.Data.Clone(), Text = source.Text, EciAssignment = source.EciAssignment,
            Fnc1ApplicationIndicator = source.Fnc1ApplicationIndicator,
            IsHanziCompatibilityMode = source.IsHanziCompatibilityMode
        };
        QrStructuredAppendInfo? info = source.StructuredAppend;
        if (info != null)
        {
            result.StructuredAppend = new QrStructuredAppendInfo
            {
                Index = info.Index, Total = info.Total, Parity = info.Parity,
                ApplicationIdentifier = info.ApplicationIdentifier, IsComplete = info.IsComplete
            };
        }

        return result;
    }

    internal static bool IsPayload(QrEncodingMode mode)
    {
        return mode == QrEncodingMode.Numeric || mode == QrEncodingMode.Alphanumeric || mode == QrEncodingMode.Byte
            || mode == QrEncodingMode.Kanji || mode == QrEncodingMode.Hanzi;
    }

    static bool ValidateInput(PayloadParseResult parsed, QrDecodeOptions options, DecodeStatus status)
    {
        if (parsed == null || parsed.Segments == null || options == null)
        {
            throw new QrInputException("Parsed segments and decode options are required.", "parsed");
        }

        if (options.MaximumSegments <= 0 || options.MaximumAllocationBytes <= 0)
        {
            throw new QrInputException("Segment and allocation limits must be positive.", "options");
        }

        if (parsed.Segments.Length > options.MaximumSegments)
        {
            return status.FailLimit("MaximumSegments", parsed.Segments.Length, options.MaximumSegments);
        }

        long budget = (long)parsed.Segments.Length * 2048 + 4096;
        if (!status.CheckAllocation(budget, options.MaximumAllocationBytes)) { return false; }
        foreach (QrSegment segment in parsed.Segments)
        {
            if (segment == null || segment.Data == null || segment.CharacterCount < 0)
            {
                throw new QrInputException("Every parsed segment requires valid data and a nonnegative count.", "Segments");
            }

            if (!IsPayload(segment.Mode) && segment.Mode != QrEncodingMode.Eci && segment.Mode != QrEncodingMode.Fnc1First
                && segment.Mode != QrEncodingMode.Fnc1Second && segment.Mode != QrEncodingMode.StructuredAppend)
            {
                throw new QrInputException("The segment mode is not a parsed payload or control mode.", "Mode");
            }

            if (segment.Mode == QrEncodingMode.Eci)
            {
                EciEncoder.ValidateAssignment(segment.EciAssignment);
            }

            budget += (long)segment.Data.Length * 16;
            int characters = segment.Text == null ? 0 : segment.Text.Length;
            budget += (long)characters * 4;
            if (!status.CheckAllocation(budget, options.MaximumAllocationBytes)) { return false; }
        }
        return true;
    }
}
