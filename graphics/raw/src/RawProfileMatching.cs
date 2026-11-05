namespace raw;

/// <summary>Provides deterministic identity normalization and profile matching.</summary>
internal static class RawProfileMatcher
{
    /// <summary>Normalizes camera identity text to lowercase Unicode letters and digits.</summary>
    /// <param name="value">The source text.</param>
    /// <returns>The normalized comparison text.</returns>
    public static string NormalizeIdentityText(string? value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        string normalized = value.Normalize(System.Text.NormalizationForm.FormKC);
        System.Text.StringBuilder result = new System.Text.StringBuilder(normalized.Length);
        int index = 0;
        while (index < normalized.Length)
        {
            char character = normalized[index];
            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
            }
            index++;
        }
        return result.ToString();
    }

    internal static RawProfileMatchResult MatchRead(RawReadProfile[] profiles, RawProfileMatchInput? input)
    {
        ValidateInput(input);
        System.Collections.Generic.List<RawProfileMatchCandidate> candidates = new System.Collections.Generic.List<RawProfileMatchCandidate>();
        int index = 0;
        while (index < profiles.Length)
        {
            RawReadProfile profile = profiles[index];
            RawProfileMatchCandidate? candidate = Candidate(profile.Identity, profile.Fingerprint, profile.Experimental, profile.Id, profile, null, input);
            if (candidate != null)
            {
                candidates.Add(candidate);
            }
            index++;
        }
        return ChooseBest(candidates, (input ?? throw new System.InvalidOperationException("A required value is unavailable.")).Format, true);
    }

    internal static RawProfileMatchResult MatchWrite(RawWriteProfile[] profiles, RawProfileMatchInput input)
    {
        ValidateInput(input);
        System.Collections.Generic.List<RawProfileMatchCandidate> candidates = new System.Collections.Generic.List<RawProfileMatchCandidate>();
        int index = 0;
        while (index < profiles.Length)
        {
            RawWriteProfile profile = profiles[index];
            RawProfileMatchCandidate? candidate = Candidate(profile.Identity, profile.Fingerprint, profile.Experimental, profile.Id, null, profile, input);
            if (candidate != null)
            {
                candidates.Add(candidate);
            }
            index++;
        }
        return ChooseBest(candidates, input.Format, false);
    }

    internal static RawProfileMatchCandidate? Candidate(RawProfileIdentity identity, RawProfileFingerprint fingerprint, bool experimental, string id, RawReadProfile? readProfile, RawWriteProfile? writeProfile, RawProfileMatchInput? input)
    {
        int identityScore = IdentityScore(identity, input);
        if (identityScore < 0 || !FirmwareMatches(identity, (input ?? throw new System.InvalidOperationException("A required value is unavailable.")).Firmware))
        {
            return null;
        }
        int fingerprintScore = FingerprintScore(fingerprint, input);
        if (fingerprintScore < 0)
        {
            return null;
        }
        bool eligible = !experimental || input.AllowExperimental;
        return new RawProfileMatchCandidate(id, identityScore + fingerprintScore, identityScore, fingerprintScore, experimental, eligible, readProfile, writeProfile);
    }

    internal static int IdentityScore(RawProfileIdentity identity, RawProfileMatchInput? input)
    {
        if (identity == null || identity.Format != (input ?? throw new System.InvalidOperationException("A required value is unavailable.")).Format)
        {
            return -1;
        }
        string make = NormalizeIdentityText(input.Make);
        string model = NormalizeIdentityText(input.Model);
        string cameraId = NormalizeIdentityText(input.CameraId);
        string profileMake = NormalizeIdentityText(identity.Make);
        string profileModel = NormalizeIdentityText(identity.Model);
        string profileId = NormalizeIdentityText(identity.CameraId);
        bool idMatch = cameraId.Length > 0 && profileId.Length > 0 && cameraId == profileId;
        if (cameraId.Length > 0 && profileId.Length > 0 && !idMatch)
        {
            return -1;
        }
        bool makeMatch = make.Length > 0 && make == profileMake;
        bool modelMatch = model.Length > 0 && model == profileModel;
        bool aliasModel = AliasMatches(identity.Aliases, model);
        bool aliasCombined = AliasMatches(identity.Aliases, make + model);
        bool aliasMatch = aliasModel || aliasCombined;
        if (make.Length > 0 && !makeMatch && !aliasCombined || model.Length > 0 && !modelMatch && !aliasMatch)
        {
            return -1;
        }
        if (!idMatch && !(makeMatch && (modelMatch || aliasMatch)) && !aliasMatch)
        {
            return -1;
        }
        int score = 100;
        if (idMatch) score += 100;
        if (makeMatch) score += 30;
        if (modelMatch) score += 50;
        else if (aliasMatch) score += 40;
        return score;
    }

    internal static bool AliasMatches(string[] aliases, string value)
    {
        if (aliases == null)
        {
            return false;
        }
        int index = 0;
        while (index < aliases.Length)
        {
            string alias = NormalizeIdentityText(aliases[index]);
            if (alias.Length > 0 && alias == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool FirmwareMatches(RawProfileIdentity identity, string firmware)
    {
        bool hasMinimum = identity.FirmwareMinimum != null && identity.FirmwareMinimum.Length > 0;
        bool hasMaximum = identity.FirmwareMaximum != null && identity.FirmwareMaximum.Length > 0;
        if (!hasMinimum && !hasMaximum)
        {
            return true;
        }
        if (firmware == null || firmware.Length == 0)
        {
            return false;
        }
        if (hasMinimum && CompareFirmware(firmware, identity.FirmwareMinimum) < 0)
        {
            return false;
        }
        return !hasMaximum || CompareFirmware(firmware, identity.FirmwareMaximum) <= 0;
    }

    internal static int FingerprintScore(RawProfileFingerprint fingerprint, RawProfileMatchInput input)
    {
        if (fingerprint == null)
        {
            return -1;
        }
        int score = 0;
        if (input.HasByteOrder)
        {
            if (input.ByteOrder != fingerprint.ByteOrder) return -1;
            score += 8;
        }
        if (input.HasBigTiff)
        {
            if (input.BigTiff != fingerprint.BigTiff) return -1;
            score += 8;
        }
        score = ScoreNumber(fingerprint.Width, input.Width, 15, score);
        if (score < 0) return -1;
        score = ScoreNumber(fingerprint.Height, input.Height, 15, score);
        if (score < 0) return -1;
        score = ScoreNumber(fingerprint.BitDepth, input.BitDepth, 12, score);
        if (score < 0) return -1;
        score = ScoreNumber(fingerprint.Compression, input.Compression, 12, score);
        if (score < 0) return -1;
        score = ScoreNumber(fingerprint.SamplesPerPixel, input.SamplesPerPixel, 8, score);
        if (score < 0) return -1;
        int required = ScoreRequired(fingerprint.RequiredIfds, input.IfdPaths);
        if (required < 0) return -1;
        score += required;
        required = ScoreRequired(fingerprint.RequiredTags, input.Tags);
        if (required < 0) return -1;
        score += required;
        if ((input.MakerNoteSignature ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length > 0 && fingerprint.MakerNoteSignature.Length > 0)
        {
            if (NormalizeIdentityText(input.MakerNoteSignature) != NormalizeIdentityText(fingerprint.MakerNoteSignature)) return -1;
            score += 12;
        }
        if (input.StructuralHash.Length > 0 && fingerprint.StructuralHash.Length > 0)
        {
            if (!input.StructuralHash.Equals(fingerprint.StructuralHash, System.StringComparison.OrdinalIgnoreCase)) return -1;
            score += 40;
        }
        return score;
    }

    internal static int ScoreNumber(int required, int observed, int points, int current)
    {
        if (required > 0 && observed > 0)
        {
            return required == observed ? current + points : -1;
        }
        return current;
    }

    internal static int ScoreRequired(string[] required, string[] observed)
    {
        if (required == null || observed == null)
        {
            return -1;
        }
        if (observed.Length == 0)
        {
            return 0;
        }
        int score = 0;
        int index = 0;
        while (index < required.Length)
        {
            if (!Contains(observed, required[index]))
            {
                return -1;
            }
            score += 2;
            index++;
        }
        return score;
    }

    internal static bool Contains(string[] values, string required)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && values[index].Equals(required, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static RawProfileMatchResult ChooseBest(System.Collections.Generic.List<RawProfileMatchCandidate> candidates, RawFormat format, bool read)
    {
        Sort(candidates);
        int firstEligible = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            if (candidates[index].Eligible)
            {
                firstEligible = index;
                break;
            }
            index++;
        }
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        if (firstEligible < 0)
        {
            bool blocked = candidates.Count > 0;
            string message = blocked ? "Matching profiles are experimental and require explicit opt-in." : "No registered profile matches the normalized identity and structural evidence.";
            diagnostics.Add(Diagnostic(RawDiagnosticCode.UnsupportedProfile, RawSeverity.Error, format, message));
            return new RawProfileMatchResult(false, false, null, null, candidates.ToArray(), diagnostics.ToArray());
        }
        RawProfileMatchCandidate best = candidates[firstEligible];
        index = firstEligible + 1;
        while (index < candidates.Count)
        {
            RawProfileMatchCandidate candidate = candidates[index];
            if (candidate.Eligible && candidate.Score == best.Score)
            {
                diagnostics.Add(Diagnostic(RawDiagnosticCode.AmbiguousProfile, RawSeverity.Error, format, "Multiple eligible profiles share the highest deterministic match score."));
                return new RawProfileMatchResult(false, true, null, null, candidates.ToArray(), diagnostics.ToArray());
            }
            index++;
        }
        RawReadProfile? selectedRead = read ? best.ReadProfile : null;
        RawWriteProfile? selectedWrite = read ? null : best.WriteProfile;
        return new RawProfileMatchResult(true, false, selectedRead, selectedWrite, candidates.ToArray(), diagnostics.ToArray());
    }

    internal static void Sort(System.Collections.Generic.List<RawProfileMatchCandidate> candidates)
    {
        int index = 1;
        while (index < candidates.Count)
        {
            RawProfileMatchCandidate value = candidates[index];
            int position = index - 1;
            while (position >= 0 && Before(value, candidates[position]))
            {
                candidates[position + 1] = candidates[position];
                position--;
            }
            candidates[position + 1] = value;
            index++;
        }
    }

    internal static bool Before(RawProfileMatchCandidate first, RawProfileMatchCandidate second)
    {
        if (first.Score != second.Score)
        {
            return first.Score > second.Score;
        }
        return string.CompareOrdinal(first.ProfileId, second.ProfileId) < 0;
    }

    internal static int CompareFirmware(string first, string? second)
    {
        string[] left = FirmwareTokens(first);
        string[] right = FirmwareTokens(second);
        int count = left.Length > right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < count)
        {
            string leftToken = index < left.Length ? left[index] : "0";
            string rightToken = index < right.Length ? right[index] : "0";
            int comparison = CompareToken(leftToken, rightToken);
            if (comparison != 0)
            {
                return comparison;
            }
            index++;
        }
        return 0;
    }

    internal static string[] FirmwareTokens(string? value)
    {
        System.Collections.Generic.List<string> tokens = new System.Collections.Generic.List<string>();
        System.Text.StringBuilder token = new System.Text.StringBuilder();
        bool digits = false;
        int index = 0;
        while (index < (value ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            char character = value[index];
            if (char.IsLetterOrDigit(character))
            {
                bool nextDigits = char.IsDigit(character);
                if (token.Length > 0 && nextDigits != digits)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }
                token.Append(char.ToLowerInvariant(character));
                digits = nextDigits;
            }
            else if (token.Length > 0)
            {
                tokens.Add(token.ToString());
                token.Clear();
            }
            index++;
        }
        if (token.Length > 0)
        {
            tokens.Add(token.ToString());
        }
        return tokens.ToArray();
    }

    internal static int CompareToken(string first, string second)
    {
        bool firstDigits = Digits(first);
        bool secondDigits = Digits(second);
        if (firstDigits && secondDigits)
        {
            string left = TrimZeroes(first);
            string right = TrimZeroes(second);
            if (left.Length != right.Length)
            {
                return left.Length < right.Length ? -1 : 1;
            }
            return string.CompareOrdinal(left, right);
        }
        return string.Compare(first, second, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool Digits(string value)
    {
        if (value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (!char.IsDigit(value[index])) return false;
            index++;
        }
        return true;
    }

    internal static string TrimZeroes(string value)
    {
        int index = 0;
        while (index < value.Length - 1 && value[index] == '0')
        {
            index++;
        }
        return value.Substring(index);
    }

    internal static void ValidateInput(RawProfileMatchInput? input)
    {
        if (input == null || input.Format <= RawFormat.Unknown || input.Format > RawFormat.Nef || input.Make == null || input.Model == null || input.CameraId == null || input.Firmware == null || input.IfdPaths == null || input.Tags == null || input.MakerNoteSignature == null || input.StructuralHash == null || input.Width < 0 || input.Height < 0 || input.BitDepth < 0 || input.Compression < 0 || input.SamplesPerPixel < 0 || input.HasByteOrder && (input.ByteOrder < RawByteOrder.LittleEndian || input.ByteOrder > RawByteOrder.BigEndian) || NormalizeIdentityText(input.CameraId).Length == 0 && (NormalizeIdentityText(input.Make).Length == 0 || NormalizeIdentityText(input.Model).Length == 0))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidArgument, "A defined format, normalized camera identity, nonnegative observations, and evidence collections are required.", -1);
        }
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, RawSeverity severity, RawFormat format, string message)
    {
        return new RawDiagnostic(code, severity, format, -1, string.Empty, -1, "ProfileMatcher", message, false);
    }
}
