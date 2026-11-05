namespace qrcode;

internal class SymbolFamilyDiscriminator
{
    // Operates on one canonical orientation with normalized polarity. Recovery
    // callers perform rotations/reflections/inversion before invoking this stage.
    // Only declared quiet-zone extents are removed; light borders are never guessed.
    // Caller role metadata is not evidence. Unknown or zero-confidence cells abstain.
    internal static SymbolFamilyResult Discriminate(QrMatrix matrix, QrDecodeOptions options)
    {
        ValidateInput(matrix, options);
        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        global::System.Collections.Generic.List<SymbolFamilyCandidate> candidates =
            new global::System.Collections.Generic.List<SymbolFamilyCandidate>();
        if (width == height)
        {
            if (width >= 21 && width <= 177 && (width - 17) % 4 == 0)
            {
                int version = (width - 17) / 4;
                if (options.AllowModel2)
                {
                    candidates.Add(CreateCandidate(QrSymbolKind.Model2, version, width, height));
                }

                if (options.AllowLegacyModel1 && version <= LegacyModel1Table.MaximumVersion)
                {
                    candidates.Add(CreateCandidate(QrSymbolKind.LegacyModel1, version, width, height));
                }
            }
            else if (options.AllowMicro && width >= 11 && width <= 17 && width % 2 == 1)
            {
                candidates.Add(CreateCandidate(QrSymbolKind.Micro, (width - 9) / 2, width, height));
            }
        }
        else if (options.AllowRectangularMicro)
        {
            for (int version = RectangularMicroVersionTable.MinimumVersionIndex;
                version <= RectangularMicroVersionTable.MaximumVersionIndex; version++)
            {
                if (RectangularMicroVersionTable.GetWidth(version) == width
                    && RectangularMicroVersionTable.GetHeight(version) == height)
                {
                    candidates.Add(CreateCandidate(QrSymbolKind.rMQR, version, width, height));
                    break;
                }
            }
        }

        if (candidates.Count > 0)
        {
            // Conservative bound for pattern/role builders, their temporary maps,
            // and the small result objects, covering both overlapping square profiles.
            int workBytes = CheckedDimensions.CalculateAllocationBytes(width * height, 64, options.MaximumAllocationBytes);
            CheckedDimensions.CalculateSum(workBytes, 4096, options.MaximumAllocationBytes, "MaximumAllocationBytes");
        }

        int viable = 0;
        int supported = 0;
        foreach (SymbolFamilyCandidate candidate in candidates)
        {
            Evaluate(matrix, candidate);
            if (!candidate.Rejected)
            {
                viable++;
                if (candidate.HasSufficientEvidence)
                {
                    supported++;
                }
            }
        }

        SymbolFamilyStatus status = SymbolFamilyStatus.NoMatch;
        if (viable > 0)
        {
            status = supported == 0 ? SymbolFamilyStatus.InsufficientEvidence
                : viable > 1 ? SymbolFamilyStatus.Ambiguous : SymbolFamilyStatus.Identified;
        }

        return new SymbolFamilyResult { Status = status, Candidates = candidates.ToArray() };
    }

    static SymbolFamilyCandidate CreateCandidate(QrSymbolKind kind, int version, int width, int height)
    {
        return new SymbolFamilyCandidate { SymbolKind = kind, Version = version, Width = width, Height = height };
    }

    static void Evaluate(QrMatrix matrix, SymbolFamilyCandidate candidate)
    {
        int version = candidate.Version;
        QrSymbolKind kind = candidate.SymbolKind;
        BitMatrixBuffer patterns;
        QrModuleRole[] roles;
        if (kind == QrSymbolKind.Micro)
        {
            int errorCorrectionIndex = version == 1 ? MicroVersionTable.DetectionOnly : EccBlockTable.Low;
            patterns = FunctionPatternBuilder.CreateMicroFunctionPatterns(version, errorCorrectionIndex, 0);
            roles = ModuleClassifier.CreateMicroRoleMap(version, errorCorrectionIndex);
        }
        else if (kind == QrSymbolKind.rMQR)
        {
            patterns = FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(version, 0);
            roles = ModuleClassifier.CreateRectangularMicroRoleMap(version, 0);
        }
        else
        {
            patterns = FunctionPatternBuilder.CreateModel2FunctionPatterns(version, 0, 0);
            roles = ModuleClassifier.CreateModel2RoleMap(version, 0);
        }

        int earlyExtensionMismatches = 0;
        for (int row = 0; row < candidate.Height; row++)
        {
            for (int column = 0; column < candidate.Width; column++)
            {
                QrModuleRole role = roles[row * candidate.Width + column];
                bool fixedModule = role == QrModuleRole.Finder || role == QrModuleRole.Separator
                    || role == QrModuleRole.Timing || role == QrModuleRole.DarkModule;
                bool distinctive = role == QrModuleRole.Alignment || role == QrModuleRole.Version;
                bool dark = patterns.GetModule(row, column);
                bool earlyDark = dark;
                if (kind == QrSymbolKind.LegacyModel1)
                {
                    distinctive = LegacyModel1Table.IsExtensionModule(version, row, column);
                    if (distinctive)
                    {
                        fixedModule = false;
                        dark = LegacyModel1Table.IsStandardExtensionDarkModule(version, row, column);
                        earlyDark = dark || LegacyModel1Table.IsEarlyExtensionAdditionalDarkModule(version, row, column);
                    }
                    else if (row == 6 && column >= 8 && column < candidate.Width - 8
                        || column == 6 && row >= 8 && row < candidate.Height - 8)
                    {
                        // Model 1 timing continues through positions occupied by
                        // Model 2 alignment patterns on larger versions.
                        fixedModule = true;
                        dark = (row + column) % 2 == 0;
                    }
                }

                if (!fixedModule && !distinctive)
                {
                    continue;
                }

                candidate.FixedModuleCount++;
                if (distinctive)
                {
                    candidate.DistinctiveModuleCount++;
                }

                int index = (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
                QrModuleValue value = matrix.Values[index];
                if (value == QrModuleValue.Unknown || matrix.Confidence != null && matrix.Confidence[index] == 0)
                {
                    continue;
                }

                candidate.ObservedModuleCount++;
                bool observedDark = value == QrModuleValue.Dark;
                if (observedDark != dark)
                {
                    candidate.MismatchedModuleCount++;
                }

                if (distinctive)
                {
                    candidate.ObservedDistinctiveModuleCount++;
                    if (observedDark != dark)
                    {
                        candidate.MismatchedDistinctiveModuleCount++;
                    }

                    if (observedDark != earlyDark)
                    {
                        earlyExtensionMismatches++;
                    }
                }
            }
        }

        if (kind == QrSymbolKind.LegacyModel1 && earlyExtensionMismatches < candidate.MismatchedDistinctiveModuleCount)
        {
            candidate.MismatchedModuleCount -= candidate.MismatchedDistinctiveModuleCount - earlyExtensionMismatches;
            candidate.MismatchedDistinctiveModuleCount = earlyExtensionMismatches;
        }

        int commonCount = candidate.FixedModuleCount - candidate.DistinctiveModuleCount;
        int observedCommon = candidate.ObservedModuleCount - candidate.ObservedDistinctiveModuleCount;
        int mismatchedCommon = candidate.MismatchedModuleCount - candidate.MismatchedDistinctiveModuleCount;
        bool commonObserved = observedCommon * 2 >= commonCount;
        bool distinctiveObserved = candidate.ObservedDistinctiveModuleCount * 2 >= candidate.DistinctiveModuleCount;
        // Half the cells in each group must be available, with at most 20% of
        // observed cells damaged. Separate groups prevent shared finder evidence
        // from hiding contradictory alignment/version or legacy extension evidence.
        candidate.HasSufficientEvidence = commonObserved && distinctiveObserved;
        candidate.Rejected = commonObserved && mismatchedCommon * 5 > observedCommon
            || distinctiveObserved && candidate.MismatchedDistinctiveModuleCount * 5 > candidate.ObservedDistinctiveModuleCount;
    }

    static void ValidateInput(QrMatrix matrix, QrDecodeOptions options)
    {
        if (matrix == null || options == null)
        {
            throw new QrInputException("Matrix and decode options are required.", matrix == null ? "matrix" : "options");
        }

        CheckedDimensions.ValidateDimensionBounds(matrix.Width, matrix.Height,
            options.MaximumMatrixDimension, options.MaximumMatrixDimension, int.MaxValue);
        if (options.MaximumAllocationBytes <= 0)
        {
            throw new QrInputException("The allocation limit must be positive.", "MaximumAllocationBytes");
        }

        int area = CheckedDimensions.CalculateArea(matrix.Width, matrix.Height, int.MaxValue);
        if (matrix.Values == null || matrix.Values.Length != area
            || matrix.Confidence != null && matrix.Confidence.Length != area
            || matrix.Roles != null && matrix.Roles.Length != area)
        {
            throw new QrInputException("Matrix buffers must match the declared dimensions.", "matrix");
        }

        if (matrix.QuietZoneLeft < 0 || matrix.QuietZoneRight < 0 || matrix.QuietZoneTop < 0 || matrix.QuietZoneBottom < 0
            || (long)matrix.QuietZoneLeft + matrix.QuietZoneRight >= matrix.Width
            || (long)matrix.QuietZoneTop + matrix.QuietZoneBottom >= matrix.Height)
        {
            throw new QrInputException("Quiet-zone extents must leave a positive encoding region.", "matrix");
        }

        foreach (QrModuleValue value in matrix.Values)
        {
            if (value != QrModuleValue.Unknown && value != QrModuleValue.Light && value != QrModuleValue.Dark)
            {
                throw new QrInputException("The matrix contains an undefined module value.", "Values");
            }
        }
    }
}
