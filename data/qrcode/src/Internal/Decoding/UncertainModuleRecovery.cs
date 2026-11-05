namespace qrcode;

internal class UncertainModuleRecovery
{
    internal const int MaximumCandidateModules = 4;

    // Search singles before pairs, ordered by confidence then row-major position.
    // At most four modules and two changed/resolved values are considered. Every
    // candidate goes through BCH, dimension/remainder checks, and verified RS.
    internal static bool Recover(QrMatrix matrix, QrSymbolKind kind, int version, QrDecodeOptions options, MatrixRecoveryResult result)
    {
        RecoveryBudget budget = new RecoveryBudget(options.MaximumRecoveryAttempts);
        budget.AttemptCount = result.AttemptCount;
        return RecoverWithinBudget(matrix, kind, version, options, result, budget);
    }

    internal static bool RecoverWithinBudget(QrMatrix matrix, QrSymbolKind kind, int version, QrDecodeOptions options,
        MatrixRecoveryResult result, RecoveryBudget budget)
    {
        if (!options.EnableRecovery || kind == QrSymbolKind.Micro && version == 1) { return false; }
        int[] candidates = GetCandidates(matrix, kind, version);
        MatrixRecoveryResult pending = new MatrixRecoveryResult();
        for (int size = 1; size <= 2; size++)
        {
            for (int first = 0; first < candidates.Length; first++)
            {
                int end = size == 1 ? first + 1 : candidates.Length;
                int start = size == 1 ? first : first + 1;
                for (int second = start; second < end; second++)
                {
                    int[] indices = size == 1 ? new int[] { candidates[first] } : new int[] { candidates[first], candidates[second] };
                    int firstValues = CodewordErasureMapper.IsUncertain(matrix, indices[0]) ? 2 : 1;
                    int secondValues = size == 2 && CodewordErasureMapper.IsUncertain(matrix, indices[1]) ? 2 : 1;
                    for (int a = 0; a < firstValues; a++)
                    {
                        for (int b = 0; b < secondValues; b++)
                        {
                            if (budget.IsExhausted())
                            {
                                result.FailureReason = QrFailureReason.ResourceLimit;
                                return false;
                            }
                            QrMatrix trial = MatrixTransformer.Rotate(matrix, 0);
                            if (trial.Confidence == null)
                            {
                                trial.Confidence = new byte[trial.Values.Length];
                                global::System.Array.Fill(trial.Confidence, (byte)255);
                            }
                            SetValue(trial, matrix, indices[0], a);
                            if (size == 2) { SetValue(trial, matrix, indices[1], b); }
                            if (!CheckChoice(trial, kind, options, -1, -1, indices, pending, result, budget)) { return false; }
                            MatrixInformationChoice[] choices = MatrixDecodeRecovery.GetAlternateChoices(trial, kind, version, options);
                            foreach (MatrixInformationChoice choice in choices)
                            {
                                if (!CheckChoice(trial, kind, options, choice.FormatCopy, choice.VersionCopy, indices, pending, result, budget)) { return false; }
                            }
                        }
                    }
                }
            }
        }

        // Never publish a tentative repair from an incomplete or ambiguous search.
        if (!pending.Success) { return false; }
        result.Success = true;
        result.FailureReason = QrFailureReason.None;
        result.Codewords = pending.Codewords;
        result.Matrix = pending.Matrix;
        result.FlippedModuleIndices = pending.FlippedModuleIndices;
        result.UsedAlternateInformation = pending.UsedAlternateInformation;
        return true;
    }

    internal static int[] GetCandidates(QrMatrix matrix, QrSymbolKind kind, int version)
    {
        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        bool[] eligible = new bool[width * height];
        if (kind == QrSymbolKind.LegacyModel1)
        {
            int[] offsets = LegacyModel1Placement.CreateOffsets(version);
            for (int bit = 4; bit < offsets.Length; bit++) { eligible[offsets[bit]] = true; }
            // Legacy remainder characters depend on ECC. Exclude any character
            // that can be unprotected for this version, even before reading format.
            int remainder = 0;
            for (int level = 0; level < 4; level++)
            {
                remainder = global::System.Math.Max(remainder, LegacyModel1Table.GetRemainderCodewordCount(version, level));
            }
            for (int bit = offsets.Length - remainder * 8; bit < offsets.Length; bit++) { eligible[offsets[bit]] = false; }
            for (int bit = 0; bit < 15; bit++)
            {
                eligible[LegacyModel1Table.GetFormatPrimaryRow(bit) * width + LegacyModel1Table.GetFormatPrimaryColumn(bit)] = true;
                eligible[LegacyModel1Table.GetFormatSecondaryRow(version, bit) * width + LegacyModel1Table.GetFormatSecondaryColumn(version, bit)] = true;
            }
        }
        else
        {
            QrModuleRole[] roles = ModuleClassifier.CreateEncodingRoleMap(kind, version, kind == QrSymbolKind.Micro && version == 1 ? -1 : 0);
            for (int index = 0; index < roles.Length; index++)
            {
                QrModuleRole role = roles[index];
                eligible[index] = role == QrModuleRole.Data || role == QrModuleRole.ErrorCorrection
                    || role == QrModuleRole.Format || role == QrModuleRole.Version;
            }
        }

        int[] best = new int[MaximumCandidateModules];
        int count = 0;
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                if (!eligible[row * width + column]) { continue; }
                int index = (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
                int confidence = Confidence(matrix, index);
                if (confidence == 255) { continue; }
                int insertion = 0;
                while (insertion < count && Confidence(matrix, best[insertion]) <= confidence) { insertion++; }
                if (insertion >= best.Length) { continue; }
                if (count < best.Length) { count++; }
                for (int move = count - 1; move > insertion; move--) { best[move] = best[move - 1]; }
                best[insertion] = index;
            }
        }
        int[] result = new int[count];
        global::System.Array.Copy(best, result, count);
        return result;
    }

    static int Confidence(QrMatrix matrix, int index)
    {
        if (matrix.Values[index] == QrModuleValue.Unknown) { return 0; }
        return matrix.Confidence == null ? 255 : matrix.Confidence[index];
    }

    static void SetValue(QrMatrix trial, QrMatrix source, int index, int variant)
    {
        if (CodewordErasureMapper.IsUncertain(source, index))
        {
            trial.Values[index] = variant == 0 ? QrModuleValue.Light : QrModuleValue.Dark;
        }
        else
        {
            trial.Values[index] = source.Values[index] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        }
        if (trial.Confidence != null) { trial.Confidence[index] = 255; }
    }

    static bool CheckChoice(QrMatrix trial, QrSymbolKind kind, QrDecodeOptions options, int format, int version,
        int[] indices, MatrixRecoveryResult pending, MatrixRecoveryResult result, RecoveryBudget budget)
    {
        if (!budget.TryBeginMatrixAttempt(result)) { return false; }
        MatrixCodewordResult words = MatrixCodewordReader.ReadWithCopyChoices(trial, kind, options, format, version);
        if (!words.Success)
        {
            result.FailureReason = RecoveryFailure.Choose(result.FailureReason, words.FailureReason);
            return true;
        }
        if (pending.Codewords != null && !Equivalent(pending.Codewords, words))
        {
            result.HasAmbiguousRepair = true;
            result.FailureReason = QrFailureReason.InvalidData;
            return false;
        }
        if (!pending.Success)
        {
            pending.Success = true;
            pending.Codewords = words;
            pending.Matrix = trial;
            pending.FlippedModuleIndices = indices;
            pending.UsedAlternateInformation = format >= 0 || version >= 0;
        }
        return true;
    }

    static bool Equivalent(MatrixCodewordResult first, MatrixCodewordResult second)
    {
        BlockCorrectionResult? a = first.Correction;
        BlockCorrectionResult? b = second.Correction;
        if (a == null || b == null || first.SymbolKind != second.SymbolKind || first.Version != second.Version
            || first.ErrorCorrectionLevel != second.ErrorCorrectionLevel || first.MaskPattern != second.MaskPattern
            || a.Codewords.Length != b.Codewords.Length) { return false; }
        for (int index = 0; index < a.Codewords.Length; index++)
        {
            if (a.Codewords[index] != b.Codewords[index]) { return false; }
        }
        return true;
    }
}
