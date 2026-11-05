namespace qrcode;

/// <summary>Provides stateless exception-based QR-family encoding operations.</summary>
public class QrEncoder : IQrEncoder
{
    /// <summary>Initializes a reusable stateless QR encoder.</summary>
    public QrEncoder()
    {
    }

    /// <summary>Encodes text using the permitted modes and symbol families.</summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="options">The symbol, error-correction, control, and resource options.</param>
    /// <returns>The completed QR-family symbol.</returns>
    public QrSymbol EncodeText(string text, QrEncodeOptions options)
    {
        QrSegment[]? segments = PrepareTextSegments(text, options);
        if (segments == null)
        {
            bool unsupportedAssignment = options.EciAssignment >= 0
                && !EciTextTranscoder.IsSupportedAssignment(options.EciAssignment);
            throw new QrUnsupportedFeatureException(
                "The text cannot be represented by the requested profile and text encoding.",
                QrFailureReason.UnsupportedTextEncoding,
                unsupportedAssignment ? "ECI" : "MicroTextEncoding",
                unsupportedAssignment ? options.EciAssignment : (int)options.SymbolKind);
        }

        return SymbolEncoder.EncodeSegments(segments, options);
    }

    /// <summary>Encodes caller-supplied bytes without changing their values.</summary>
    /// <param name="data">The bytes to encode.</param>
    /// <param name="options">The symbol, error-correction, control, and resource options.</param>
    /// <returns>The completed QR-family symbol.</returns>
    public QrSymbol EncodeBytes(byte[] data, QrEncodeOptions options)
    {
        ValidateControlOptions(options);
        QrSegment payload = SegmentOptimizer.CreateSingleModeByteSegment(
            data,
            options.EciAssignment,
            options.MaximumInputBytes);
        QrSegment[] segments = AddControls(
            new QrSegment[] { payload },
            options,
            options.EciAssignment >= 0);
        return SymbolEncoder.EncodeSegments(segments, options);
    }

    /// <summary>Encodes an explicit ordered collection of payload and control segments.</summary>
    /// <param name="segments">The segments in transmission order.</param>
    /// <param name="options">The symbol, error-correction, and resource options.</param>
    /// <returns>The completed QR-family symbol.</returns>
    public QrSymbol EncodeSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        return SymbolEncoder.EncodeSegments(segments, options);
    }

    /// <summary>Splits bytes into a complete structured-append Model 2 sequence.</summary>
    /// <param name="data">The complete unsplit message bytes.</param>
    /// <param name="options">The Model 2, control, and resource options.</param>
    /// <returns>The completed structured-append sequence.</returns>
    public QrSymbolSequence EncodeStructuredAppend(byte[] data, QrEncodeOptions options)
    {
        return StructuredAppendSplitter.SplitBytes(data, options);
    }

    /// <summary>Encodes manually partitioned structured-append segments.</summary>
    /// <param name="parts">The complete ordered parts, each beginning with its manual header.</param>
    /// <param name="options">The Model 2 and resource options.</param>
    /// <returns>The validated completed structured-append sequence.</returns>
    public QrSymbolSequence EncodeManualStructuredAppend(
        QrSegment[][] parts,
        QrEncodeOptions options)
    {
        return StructuredAppendSplitter.EncodeManualParts(parts, options);
    }

    /// <summary>Attempts to encode text without throwing for expected representation or capacity failures.</summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="options">The symbol, error-correction, control, and resource options.</param>
    /// <returns>The encoded symbol or a machine-readable expected failure.</returns>
    public QrEncodeAttemptResult TryEncodeText(string text, QrEncodeOptions options)
    {
        QrSegment[]? segments = PrepareTextSegments(text, options);
        if (segments == null)
        {
            return new QrEncodeAttemptResult
            {
                FailureReason = QrFailureReason.UnsupportedTextEncoding
            };
        }

        return SymbolEncoder.TryEncodeSegments(segments, options);
    }

    internal static QrSegment[]? PrepareTextSegments(string text, QrEncodeOptions options)
    {
        ValidateControlOptions(options);
        SymbolEncoder.ValidateConfiguration(options);
        if (text == null)
        {
            throw new QrInputException("The text to encode cannot be null.", "text");
        }

        if (text.Length > options.MaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The text exceeds the configured input byte limit.",
                "MaximumInputBytes", text.Length, options.MaximumInputBytes);
        }

        SegmentOptimizer.ValidateTextCharacters(text);
        bool unsupportedAssignment = options.EciAssignment >= 0
            && !EciTextTranscoder.IsSupportedAssignment(options.EciAssignment);
        if (unsupportedAssignment && RequiresByteMode(text, options))
        {
            return null;
        }

        if (IsUnsupportedMicroText(text, options))
        {
            return null;
        }

        QrSegment[] segments;
        if (options.OptimizeSegments)
        {
            int assignment = options.EciAssignment;
            if (unsupportedAssignment)
            {
                assignment = -1;
            }

            int[] assignments = CreateAssignmentPriorities(assignment);
            QrSymbolKind kind = GetOptimizationKind(options);
            int version = GetOptimizationVersion(kind, options);
            segments = SegmentOptimizer.OptimizeTextSegmentsWithControls(
                text,
                kind,
                version,
                assignments,
                options.UseFnc1First,
                options.Fnc1SecondApplicationIndicator,
                null,
                options.MaximumSegments,
                options.MaximumInputBytes);
        }
        else
        {
            segments = CreateSingleModeTextWithControls(text, options);
        }

        return segments;
    }

    /// <summary>Attempts to encode explicit segments without throwing for capacity failure.</summary>
    /// <param name="segments">The segments in transmission order.</param>
    /// <param name="options">The symbol, error-correction, and resource options.</param>
    /// <returns>The encoded symbol or exact capacity diagnostics.</returns>
    public QrEncodeAttemptResult TryEncodeSegments(
        QrSegment[] segments,
        QrEncodeOptions options)
    {
        return SymbolEncoder.TryEncodeSegments(segments, options);
    }

    internal static void ValidateControlOptions(QrEncodeOptions options)
    {
        if (options == null)
        {
            throw new QrInputException("The encode options are required.", "options");
        }

        if (options.EciAssignment < -1)
        {
            throw new QrInputException(
                "The ECI assignment sentinel is invalid.",
                "EciAssignment");
        }

        if (options.EciAssignment >= 0)
        {
            EciEncoder.ValidateAssignment(options.EciAssignment);
        }

        if (options.Fnc1SecondApplicationIndicator < -1)
        {
            throw new QrInputException(
                "The FNC1 second-position sentinel is invalid.",
                "Fnc1SecondApplicationIndicator");
        }

        if (options.UseFnc1First && options.Fnc1SecondApplicationIndicator >= 0)
        {
            throw new QrInputException(
                "FNC1 first and second position cannot both be requested.",
                "Fnc1SecondApplicationIndicator");
        }

        if (options.Fnc1SecondApplicationIndicator >= 0)
        {
            Fnc1Encoder.ValidateSecondPositionIndicator(
                options.Fnc1SecondApplicationIndicator);
        }
    }

    internal static int[] CreateAssignmentPriorities(int assignment)
    {
        if (assignment < 0)
        {
            return new int[0];
        }

        return new int[] { assignment };
    }

    internal static QrSymbolKind GetOptimizationKind(QrEncodeOptions options)
    {
        if (options.ForceSymbolKind)
        {
            if (options.SymbolKind == QrSymbolKind.LegacyModel1)
            {
                return QrSymbolKind.Model2;
            }

            return options.SymbolKind;
        }

        return QrSymbolKind.Model2;
    }

    internal static int GetOptimizationVersion(
        QrSymbolKind kind,
        QrEncodeOptions options)
    {
        if (kind == QrSymbolKind.Micro)
        {
            int version = options.MaximumVersion;
            if (version > MicroVersionTable.MaximumVersion)
            {
                version = MicroVersionTable.MaximumVersion;
            }

            if (version < MicroVersionTable.MinimumVersion)
            {
                version = MicroVersionTable.MinimumVersion;
            }

            return version;
        }

        if (kind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.MaximumVersionIndex;
        }

        int model2Version = options.MaximumVersion;
        if (model2Version > QrLimits.Model2MaximumVersion)
        {
            model2Version = QrLimits.Model2MaximumVersion;
        }

        if (model2Version < QrLimits.Model2MinimumVersion)
        {
            model2Version = QrLimits.Model2MinimumVersion;
        }

        return model2Version;
    }

    internal static QrSegment[] CreateSingleModeTextWithControls(
        string text,
        QrEncodeOptions options)
    {
        QrSegment[] dataSegments = SegmentOptimizer.CreateSingleModeTextSegments(
            text,
            options.EciAssignment,
            options.EnableHanziCompatibility,
            options.MaximumInputBytes);
        bool addEci = dataSegments.Length > 0
            && dataSegments[0].Mode == QrEncodingMode.Byte
            && dataSegments[0].EciAssignment >= 0;
        return AddControls(dataSegments, options, addEci);
    }

    internal static QrSegment[] AddControls(
        QrSegment[] dataSegments,
        QrEncodeOptions options,
        bool addEci)
    {
        int controlCount = 0;
        if (options.UseFnc1First || options.Fnc1SecondApplicationIndicator >= 0)
        {
            controlCount++;
        }

        if (addEci)
        {
            controlCount++;
        }

        QrSegment[] segments = new QrSegment[controlCount + dataSegments.Length];
        int resultIndex = 0;
        if (options.UseFnc1First)
        {
            segments[resultIndex] = Fnc1Encoder.CreateFirstPositionControlSegment();
            resultIndex++;
        }
        else if (options.Fnc1SecondApplicationIndicator >= 0)
        {
            segments[resultIndex] = Fnc1Encoder.CreateSecondPositionControlSegment(
                options.Fnc1SecondApplicationIndicator);
            resultIndex++;
        }

        if (addEci)
        {
            int assignment = options.EciAssignment;
            if (assignment < 0 && dataSegments.Length > 0)
            {
                assignment = dataSegments[0].EciAssignment;
            }

            segments[resultIndex] = EciEncoder.CreateControlSegment(assignment);
            resultIndex++;
        }

        for (int index = 0; index < dataSegments.Length; index++)
        {
            segments[resultIndex] = dataSegments[index];
            resultIndex++;
        }

        return segments;
    }

    internal static bool IsUnsupportedMicroText(string text, QrEncodeOptions options)
    {
        if (!options.ForceSymbolKind || options.SymbolKind != QrSymbolKind.Micro)
        {
            return false;
        }

        // Without segmentation, mixed Kanji and Latin text needs a byte
        // encoding beyond Micro's default mapping. Micro cannot carry the ECI
        // control that the single-mode text factory would otherwise insert.
        bool requiresSingleByteMode = !options.OptimizeSegments
            && SegmentOptimizer.RequiresSingleByteMode(text, options.EnableHanziCompatibility);
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (requiresSingleByteMode && character > 0xFF)
            {
                return true;
            }

            bool representable = character <= 0xFF
                || QrCharacterSets.Alphanumeric.IndexOf(character) >= 0
                || KanjiEncoder.IsEncodableCharacter(character);
            if (!representable)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool RequiresByteMode(string text, QrEncodeOptions options)
    {
        if (!options.OptimizeSegments)
        {
            return SegmentOptimizer.RequiresSingleByteMode(text, options.EnableHanziCompatibility);
        }

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (QrCharacterSets.Alphanumeric.IndexOf(character) < 0
                && !KanjiEncoder.IsEncodableCharacter(character))
            {
                return true;
            }
        }

        return false;
    }
}
