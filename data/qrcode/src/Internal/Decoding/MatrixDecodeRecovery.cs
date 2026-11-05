namespace qrcode;

internal class MatrixDecodeRecovery
{
    // Orientation normalization is ordinary decoding. EnableRecovery controls
    // retries of competing BCH-valid metadata and uncertain module values;
    // TryMirrored/TryInverted control
    // their respective orientation hypotheses. A fixed polarity is authoritative.
    internal static MatrixRecoveryResult RecoverCodewords(QrMatrix matrix, QrDecodeOptions options)
    {
        if (options == null) { throw new QrInputException("Decode options are required.", "options"); }
        return RecoverWithinBudget(matrix, options, new RecoveryBudget(options.MaximumRecoveryAttempts));
    }

    internal static MatrixRecoveryResult RecoverWithinBudget(QrMatrix matrix, QrDecodeOptions options, RecoveryBudget budget)
    {
        MatrixInformationSupport.ValidateInput(matrix, options);
        if (options.MaximumRecoveryAttempts <= 0 || options.MaximumSegments <= 0
            || options.Polarity != QrPolarity.Automatic && options.Polarity != QrPolarity.DarkOnLight && options.Polarity != QrPolarity.LightOnDark)
        {
            throw new QrInputException("Decode limits and polarity must be valid.", "options");
        }

        int area = CheckedDimensions.CalculateArea(matrix.Width, matrix.Height, int.MaxValue);
        int work = CheckedDimensions.CalculateAllocationBytes(area, 320, options.MaximumAllocationBytes);
        CheckedDimensions.CalculateSum(work, 4096, options.MaximumAllocationBytes, "MaximumAllocationBytes");
        MatrixRecoveryResult result = new MatrixRecoveryResult();
        int polarities = options.Polarity == QrPolarity.Automatic && options.TryInverted ? 2 : 1;
        // Exhaust ordinary orientation/metadata choices before spending any
        // budget on speculative values, so flipping cannot starve those retries.
        int phases = options.EnableRecovery ? 2 : 1;
        for (int phase = 0; phase < phases; phase++)
        {
            for (int polarity = 0; polarity < polarities; polarity++)
            {
                bool inverted = options.Polarity == QrPolarity.LightOnDark || polarity == 1;
                QrMatrix normalized = inverted ? MatrixTransformer.InvertPolarity(matrix) : MatrixTransformer.Rotate(matrix, 0);
                int mirrors = options.TryMirrored ? 2 : 1;
                for (int mirror = 0; mirror < mirrors; mirror++)
                {
                    QrMatrix reflected = mirror == 0 ? normalized : MatrixTransformer.Mirror(normalized);
                    for (int rotation = 0; rotation < 4; rotation++)
                    {
                        if (budget.IsExhausted())
                        {
                            result.FailureReason = QrFailureReason.ResourceLimit;
                            return result;
                        }
                        QrMatrix oriented = MatrixTransformer.Rotate(reflected, rotation);
                        SymbolFamilyResult families = SymbolFamilyDiscriminator.Discriminate(oriented, options);
                        foreach (SymbolFamilyCandidate family in families.Candidates)
                        {
                            if (family.Rejected || !family.HasSufficientEvidence
                                || family.SymbolKind == QrSymbolKind.LegacyModel1 && (inverted || mirror != 0))
                            {
                                continue;
                            }

                            if (phase == 1)
                            {
                                if (UncertainModuleRecovery.RecoverWithinBudget(oriented, family.SymbolKind, family.Version, options, result, budget))
                                {
                                    QrMatrix repaired = result.Matrix == null ? oriented : result.Matrix;
                                    SetOrientation(result, repaired, rotation, mirror != 0, inverted, result.UsedAlternateInformation);
                                    return result;
                                }
                                if (result.HasAmbiguousRepair || result.FailureReason == QrFailureReason.ResourceLimit) { return result; }
                                continue;
                            }

                            if (Attempt(oriented, family.SymbolKind, options, -1, -1, result, budget))
                            {
                                SetOrientation(result, oriented, rotation, mirror != 0, inverted, false);
                                return result;
                            }

                            if (result.FailureReason == QrFailureReason.ResourceLimit) { return result; }
                            if (!options.EnableRecovery) { continue; }
                            MatrixInformationChoice[] choices = GetAlternateChoices(oriented, family.SymbolKind, family.Version, options);
                            foreach (MatrixInformationChoice choice in choices)
                            {
                                if (Attempt(oriented, family.SymbolKind, options, choice.FormatCopy, choice.VersionCopy, result, budget))
                                {
                                    SetOrientation(result, oriented, rotation, mirror != 0, inverted, true);
                                    return result;
                                }

                                if (result.FailureReason == QrFailureReason.ResourceLimit) { return result; }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    internal static QrDecodeResult Decode(QrMatrix matrix, QrDecodeOptions options)
    {
        MatrixRecoveryResult recovered = RecoverCodewords(matrix, options);
        return CreateDecodedResult(recovered, options);
    }

    internal static QrDecodeResult CreateDecodedResult(MatrixRecoveryResult recovered, QrDecodeOptions options)
    {
        MatrixCodewordResult? words = recovered.Codewords;
        if (!recovered.Success || words == null)
        {
            throw new QrDecodeException("No matrix hypothesis yielded verified codewords.", recovered.FailureReason, -1, recovered.AttemptCount);
        }

        // Once BCH, geometry, and RS agree, parse the verified payload using the
        // caller's policy. Strict failure never silently falls back to lenient padding.
        QrDecodeResult decoded = DecodedSymbolBuilder.Create(words, options);
        return AddDiagnostics(decoded, recovered, options);
    }

    internal static QrDecodeResult AddDiagnostics(QrDecodeResult decoded, MatrixRecoveryResult recovered, QrDecodeOptions options)
    {
        if (decoded.Symbol != null) { decoded.Symbol.Matrix = recovered.Matrix; }
        global::System.Collections.Generic.List<string> warnings = new global::System.Collections.Generic.List<string>(decoded.Warnings);
        if (recovered.QuarterTurnsClockwise != 0) { warnings.Add($"Normalized matrix by {recovered.QuarterTurnsClockwise * 90} clockwise degrees."); }
        if (recovered.IsMirrored) { warnings.Add("Decoded mirrored matrix geometry."); }
        if (recovered.IsInverted) { warnings.Add("Decoded reversed-reflectance matrix."); }
        if (recovered.UsedAlternateInformation) { warnings.Add("Decoded using an alternate BCH-verified metadata copy."); }
        if (recovered.FlippedModuleIndices.Length > 0) { warnings.Add($"Resolved {recovered.FlippedModuleIndices.Length} uncertain modules using BCH/RS-verified search."); }
        if (!options.Strict) { warnings.Add("Payload padding validation was disabled."); }
        decoded.Warnings = warnings.ToArray();
        return decoded;
    }

    static bool Attempt(QrMatrix matrix, QrSymbolKind kind, QrDecodeOptions options, int formatCopy, int versionCopy, MatrixRecoveryResult result, RecoveryBudget budget)
    {
        if (!budget.TryBeginMatrixAttempt(result)) { return false; }
        MatrixCodewordResult words = MatrixCodewordReader.ReadWithCopyChoices(matrix, kind, options, formatCopy, versionCopy);
        if (words.Success)
        {
            result.Codewords = words;
            result.Success = true;
            result.FailureReason = QrFailureReason.None;
            return true;
        }

        result.FailureReason = RecoveryFailure.Choose(result.FailureReason, words.FailureReason);
        return false;
    }

    static void SetOrientation(MatrixRecoveryResult result, QrMatrix matrix, int rotation, bool mirrored, bool inverted, bool alternate)
    {
        result.Matrix = matrix;
        result.QuarterTurnsClockwise = rotation;
        result.IsMirrored = mirrored;
        result.IsInverted = inverted;
        result.UsedAlternateInformation = alternate;
    }

    internal static MatrixInformationChoice[] GetAlternateChoices(QrMatrix matrix, QrSymbolKind kind, int dimensionVersion, QrDecodeOptions options)
    {
        if (kind == QrSymbolKind.Micro) { return new MatrixInformationChoice[0]; }
        MatrixInformationCopy[] formats;
        int selectedFormat;
        int versionCopy = -1;
        bool originalVersionValid = true;
        if (kind == QrSymbolKind.Model2)
        {
            Model2InformationResult information = Model2InformationReader.Read(matrix, options);
            formats = information.FormatCopies;
            selectedFormat = information.SelectedFormatCopy;
            originalVersionValid = information.Version == dimensionVersion;
            if (dimensionVersion >= 7)
            {
                for (int index = 0; index < information.VersionCopies.Length; index++)
                {
                    if (information.VersionCopies[index].Success && information.VersionCopies[index].Data == dimensionVersion)
                    {
                        versionCopy = index;
                        break;
                    }
                }

                // Dimensions cannot replace missing or contradicting BCH evidence.
                if (versionCopy < 0) { return new MatrixInformationChoice[0]; }
            }
        }
        else if (kind == QrSymbolKind.rMQR)
        {
            RectangularMicroInformationResult information = RectangularMicroInformationReader.Read(matrix, options);
            formats = information.FormatCopies;
            selectedFormat = information.SelectedFormatCopy;
        }
        else
        {
            formats = new MatrixInformationCopy[] { LegacyModel1Decoder.ReadFormat(matrix, dimensionVersion, false),
                LegacyModel1Decoder.ReadFormat(matrix, dimensionVersion, true) };
            selectedFormat = MatrixInformationSupport.ChooseCopy(formats);
        }

        global::System.Collections.Generic.List<MatrixInformationChoice> choices = new global::System.Collections.Generic.List<MatrixInformationChoice>();
        int previousData = -1;
        for (int index = 0; index < formats.Length; index++)
        {
            MatrixInformationCopy copy = formats[index];
            if (!copy.Success || copy.Data == previousData
                || kind == QrSymbolKind.rMQR && copy.Data % 32 != dimensionVersion
                || originalVersionValid && selectedFormat >= 0 && copy.Data == formats[selectedFormat].Data)
            {
                continue;
            }

            choices.Add(new MatrixInformationChoice { FormatCopy = index, VersionCopy = versionCopy });
            previousData = copy.Data;
        }

        return choices.ToArray();
    }
}
