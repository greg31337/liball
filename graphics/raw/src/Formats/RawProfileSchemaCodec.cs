namespace raw;

/// <summary>Reads and writes the strict, versioned read-profile and write-profile JSON schema.</summary>
internal static class RawProfileSchemaCodec
{
    /// <summary>The profile schema version written and understood by this release.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Reads a complete UTF-8 profile document.</summary>
    /// <param name="json">The exact JSON bytes.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The validated read/write profile document.</returns>
    /// <exception cref="RawFormatException">The document contains duplicate members or invalid profile values.</exception>
    /// <exception cref="RawLimitException">The document exceeds configured limits.</exception>
    /// <exception cref="RawUnsupportedException">The schema version is unsupported.</exception>
    /// <exception cref="System.Text.Json.JsonException">The JSON shape, member names, or value types do not match the schema.</exception>
    public static RawProfileSchemaDocument Read(byte[] json, RawLimits limits)
    {
        RequireLimits(limits);
        if (json == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Profile JSON bytes are required.", -1);
        }
        RequireSize(json.LongLength, limits);
        if (!System.Text.Unicode.Utf8.IsValid(json))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidProfile, "Profile JSON must contain valid UTF-8.", -1);
        }
        System.Text.Json.JsonDocumentOptions documentOptions = new System.Text.Json.JsonDocumentOptions();
        documentOptions.AllowTrailingCommas = false;
        documentOptions.CommentHandling = System.Text.Json.JsonCommentHandling.Disallow;
        documentOptions.MaxDepth = limits.MaxRecursionDepth;
        using (System.Text.Json.JsonDocument parsed = System.Text.Json.JsonDocument.Parse(json, documentOptions))
        {
            int[] memberCount = new int[] { 0 };
            RejectDuplicates(parsed.RootElement, memberCount, limits);
        }
        RawProfileSchemaDocument? document = System.Text.Json.JsonSerializer.Deserialize<RawProfileSchemaDocument>(json, Options(limits, false));
        if (document == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidProfile, "The profile document cannot be null.", -1);
        }
        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The profile schema version is not supported.", -1);
        }
        RawValidationReport report = document.Validate(limits);
        if (!report.IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidProfile, "The profile document contains invalid read or write profile data.", -1);
        }
        return document;
    }

    /// <summary>Writes a deterministic UTF-8 profile document.</summary>
    /// <param name="document">The read/write profile document.</param>
    /// <param name="limits">Resource limits.</param>
    /// <returns>The complete JSON bytes.</returns>
    /// <exception cref="RawWriteException">The schema version or profile data is invalid.</exception>
    /// <exception cref="RawLimitException">The document exceeds configured limits.</exception>
    public static byte[] Write(RawProfileSchemaDocument document, RawLimits limits)
    {
        RequireLimits(limits);
        if (document == null || document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "A current-version profile document is required.", -1);
        }
        RawValidationReport report = document.Validate(limits);
        if (!report.IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The profile document contains invalid read or write profile data.", -1);
        }
        byte[] json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(document, Options(limits, true));
        RequireSize(json.LongLength, limits);
        return json;
    }

    internal static System.Text.Json.JsonSerializerOptions Options(RawLimits limits, bool indented)
    {
        System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions();
        options.IncludeFields = true;
        options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = false;
        options.WriteIndented = indented;
        options.MaxDepth = limits.MaxRecursionDepth;
        options.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
        options.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict;
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }

    internal static void RejectDuplicates(System.Text.Json.JsonElement element, int[] memberCount, RawLimits limits)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
            {
                memberCount[0]++;
                if (memberCount[0] > limits.MaxTagCount)
                {
                    throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The profile JSON member count exceeds the configured tag-count limit.", -1);
                }
                if (!names.Add(property.Name))
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidProfile, "The profile JSON contains a duplicate object member.", -1);
                }
                RejectDuplicates(property.Value, memberCount, limits);
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (System.Text.Json.JsonElement item in element.EnumerateArray())
            {
                RejectDuplicates(item, memberCount, limits);
            }
        }
    }

    internal static void RequireLimits(RawLimits limits)
    {
        if (limits == null || !limits.Validate().IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Valid resource limits are required.", -1);
        }
    }

    internal static void RequireSize(long length, RawLimits limits)
    {
        long maximum = limits.MaxMetadataBytes < limits.MaxAllocationBytes ? limits.MaxMetadataBytes : limits.MaxAllocationBytes;
        if (limits.MaxFileSizeBytes < maximum)
        {
            maximum = limits.MaxFileSizeBytes;
        }
        if (length <= 0 || length > maximum)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The profile JSON exceeds configured metadata, file, or allocation limits.", -1);
        }
    }
}

internal static class RawProfileSchemaValidation
{
    internal static RawValidationReport Validate(RawProfileSchemaDocument document, RawLimits limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (document == null || limits == null || !limits.Validate().IsValid())
        {
            Add(messages, "Profiles", "A profile document and valid limits are required.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (document.SchemaVersion != RawProfileSchemaCodec.CurrentSchemaVersion)
        {
            Add(messages, "Profiles.SchemaVersion", "The profile schema version is not supported.");
        }
        if (document.ReadProfiles == null || document.WriteProfiles == null)
        {
            Add(messages, "Profiles.Collections", "Read and write profile collections are required.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if ((long)document.ReadProfiles.Length + document.WriteProfiles.Length > limits.MaxTagCount)
        {
            Add(messages, "Profiles.Collections", "The profile count exceeds the configured tag-count limit.");
        }
        System.Collections.Generic.HashSet<string> readIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        System.Collections.Generic.HashSet<string> writeIds = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < document.ReadProfiles.Length)
        {
            RawReadProfile profile = document.ReadProfiles[index];
            string context = "Profiles.ReadProfiles[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
            if (profile == null)
            {
                Add(messages, context, "Every read profile is required.");
            }
            else
            {
                if (!Required(profile.Id) || !readIds.Add(profile.Id)) Add(messages, context + ".Id", "Every read profile requires a unique nonempty identifier.");
                ValidateCommon(profile.Identity, profile.Fingerprint, profile.Geometry, profile.Codec, profile.Calibration, profile.Provenance, context, limits, messages);
            }
            index++;
        }
        index = 0;
        while (index < document.WriteProfiles.Length)
        {
            RawWriteProfile profile = document.WriteProfiles[index];
            string context = "Profiles.WriteProfiles[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
            if (profile == null)
            {
                Add(messages, context, "Every write profile is required.");
            }
            else
            {
                if (!Required(profile.Id) || !writeIds.Add(profile.Id)) Add(messages, context + ".Id", "Every write profile requires a unique nonempty identifier.");
                if (!Required(profile.ReadProfileId) || !readIds.Contains(profile.ReadProfileId)) Add(messages, context + ".ReadProfileId", "A write profile must reference an existing read profile.");
                ValidateCommon(profile.Identity, profile.Fingerprint, profile.Geometry, profile.Codec, profile.Calibration, profile.Provenance, context, limits, messages);
                ValidateEvidence(profile.Evidence, profile.Experimental, context, messages);
            }
            index++;
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static void ValidateCommon(RawProfileIdentity identity, RawProfileFingerprint fingerprint, RawProfileGeometry geometry, RawProfileCodec codec, RawProfileCalibration calibration, RawProfileProvenance[] provenance, string context, RawLimits limits, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (identity == null || fingerprint == null || geometry == null || codec == null || calibration == null || provenance == null)
        {
            Add(messages, context, "Identity, fingerprint, geometry, codec, calibration, and provenance are required.");
            return;
        }
        if (identity.Format <= RawFormat.Unknown || identity.Format > RawFormat.Nef || !Required(identity.Make) || !Required(identity.Model) || identity.CameraId == null || identity.Aliases == null || identity.FirmwareMinimum == null || identity.FirmwareMaximum == null)
        {
            Add(messages, context + ".Identity", "A defined format, canonical make/model, aliases, identifiers, and firmware bounds are required.");
        }
        ValidateStrings(identity.Aliases, false, context + ".Identity.Aliases", messages);
        if (fingerprint.ByteOrder < RawByteOrder.LittleEndian || fingerprint.ByteOrder > RawByteOrder.BigEndian || fingerprint.Width < 0 || fingerprint.Height < 0 || fingerprint.BitDepth < 0 || fingerprint.BitDepth > 64 || fingerprint.Compression < 0 || fingerprint.SamplesPerPixel < 0 || fingerprint.RequiredIfds == null || fingerprint.RequiredTags == null || fingerprint.MakerNoteSignature == null || fingerprint.StructuralHash == null)
        {
            Add(messages, context + ".Fingerprint", "Fingerprint values and collections must be defined and nonnegative.");
        }
        ValidateStrings(fingerprint.RequiredIfds, true, context + ".Fingerprint.RequiredIfds", messages);
        ValidateStrings(fingerprint.RequiredTags, true, context + ".Fingerprint.RequiredTags", messages);
        ValidateGeometry(geometry, context, limits, messages);
        ValidateCodec(codec, context, messages);
        ValidateCalibration(calibration, context, messages);
        ValidateProvenance(provenance, context, messages);
    }

    internal static void ValidateGeometry(RawProfileGeometry geometry, string context, RawLimits limits, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (!RawModelValidation.ValidImageGeometry(geometry.StoredWidth, geometry.StoredHeight, limits) || geometry.ActiveArea == null || geometry.MaskedAreas == null)
        {
            Add(messages, context + ".Geometry", "Stored dimensions, an active area, and masked-area collection are required and bounded.");
            return;
        }
        if (!geometry.ActiveArea.Validate(geometry.StoredWidth, geometry.StoredHeight).IsValid() || geometry.ActiveArea.Kind != RawAreaKind.Active)
        {
            Add(messages, context + ".Geometry.ActiveArea", "The active area must be bounded and have Active semantics.");
        }
        int index = 0;
        while (index < geometry.MaskedAreas.Length)
        {
            RawArea area = geometry.MaskedAreas[index];
            if (area == null || !area.Validate(geometry.StoredWidth, geometry.StoredHeight).IsValid() || area.Kind != RawAreaKind.Masked && area.Kind != RawAreaKind.OpticalBlack)
            {
                Add(messages, context + ".Geometry.MaskedAreas", "Every masked area must be bounded and marked Masked or OpticalBlack.");
                break;
            }
            index++;
        }
        if (geometry.Cfa != null && !geometry.Cfa.Validate().IsValid())
        {
            Add(messages, context + ".Geometry.Cfa", "The CFA pattern is invalid.");
        }
    }

    internal static void ValidateCodec(RawProfileCodec codec, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (!Required(codec.Name) || codec.Compression < 0 || codec.Packing < RawPacking.ByteAligned || codec.Packing > RawPacking.Raw14 || codec.StoredWordBits < 0 || codec.StoredWordBits > 64 || codec.Predictor < 0 || codec.PlanarConfiguration < 1 || codec.PlanarConfiguration > 2 || codec.RowAlignment <= 0 || codec.SplitRow < -1 || codec.SliceWidths == null || codec.Quirks == null)
        {
            Add(messages, context + ".Codec", "Codec identity, packing, widths, predictor, planar layout, alignment, split row, slices, and quirks are invalid.");
        }
        int index = 0;
        while (codec.SliceWidths != null && index < codec.SliceWidths.Length)
        {
            if (codec.SliceWidths[index] <= 0)
            {
                Add(messages, context + ".Codec.SliceWidths", "Every slice width must be positive.");
                break;
            }
            index++;
        }
        ValidateStrings(codec.Quirks, true, context + ".Codec.Quirks", messages);
    }

    internal static void ValidateCalibration(RawProfileCalibration calibration, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (calibration.BlackLevels == null || calibration.WhiteLevels == null || calibration.WhiteBalance == null || calibration.ColorMatrix == null || calibration.ForwardMatrix == null || calibration.Illuminant == null || !RawModelValidation.IsFinite(calibration.BaselineExposure))
        {
            Add(messages, context + ".Calibration", "Calibration arrays, illuminant text, and finite exposure are required.");
            return;
        }
        if (calibration.ColorMatrix.Length != 0 && calibration.ColorMatrix.Length != 9 || calibration.ForwardMatrix.Length != 0 && calibration.ForwardMatrix.Length != 9)
        {
            Add(messages, context + ".Calibration.Matrices", "Color and forward matrices must be empty or three by three.");
        }
        if (!Finite(calibration.BlackLevels) || !Finite(calibration.WhiteLevels) || !Finite(calibration.WhiteBalance) || !Finite(calibration.ColorMatrix) || !Finite(calibration.ForwardMatrix))
        {
            Add(messages, context + ".Calibration", "Every calibration number must be finite.");
        }
    }

    internal static void ValidateProvenance(RawProfileProvenance[] provenance, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (provenance.Length == 0)
        {
            Add(messages, context + ".Provenance", "At least one provenance record is required.");
        }
        int index = 0;
        while (index < provenance.Length)
        {
            RawProfileProvenance item = provenance[index];
            if (item == null || !Required(item.Field) || !Required(item.Source) || item.Kind <= RawProfileProvenanceKind.Unknown || item.Kind > RawProfileProvenanceKind.Research || item.License == null || item.Retrieved == null || item.Notes == null)
            {
                Add(messages, context + ".Provenance", "Every provenance record requires a field, source, defined category, and non-null detail strings.");
                break;
            }
            index++;
        }
    }

    internal static void ValidateEvidence(RawProfileEvidence[] evidence, bool experimental, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (evidence == null)
        {
            Add(messages, context + ".Evidence", "A write profile requires an evidence collection.");
            return;
        }
        bool applicationPassed = false;
        int index = 0;
        while (index < evidence.Length)
        {
            RawProfileEvidence item = evidence[index];
            if (item == null || item.Kind < RawProfileEvidenceKind.Unknown || item.Kind > RawProfileEvidenceKind.ErrorBound || !Required(item.Tool) || item.Version == null || item.Kind == RawProfileEvidenceKind.Application && !Required(item.Version) || !Required(item.SampleId) || !Required(item.Check) || item.Result <= RawProfileEvidenceResult.Unknown || item.Result > RawProfileEvidenceResult.Failed || item.Details == null)
            {
                Add(messages, context + ".Evidence", "Every evidence record requires a tool, sample, check, defined result, and non-null details.");
                break;
            }
            if (item.Kind == RawProfileEvidenceKind.Application && item.Result == RawProfileEvidenceResult.Passed)
            {
                applicationPassed = true;
            }
            index++;
        }
        if (!experimental && !applicationPassed)
        {
            Add(messages, context + ".Evidence", "A non-experimental write profile requires at least one passed application compatibility check with an exact version.");
        }
    }

    internal static void ValidateStrings(string[]? values, bool requireValues, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (values == null)
        {
            Add(messages, context, "The string collection is required.");
            return;
        }
        System.Collections.Generic.HashSet<string> unique = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || requireValues && !Required(values[index]) || !unique.Add(values[index]))
            {
                Add(messages, context, "String values must be non-null, unique, and nonempty when required.");
                break;
            }
            index++;
        }
    }

    internal static bool Finite(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!RawModelValidation.IsFinite(values[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool Required(string? value)
    {
        return value != null && value.Length > 0;
    }

    internal static void Add(System.Collections.Generic.List<RawValidationMessage> messages, string context, string message)
    {
        messages.Add(new RawValidationMessage(RawValidationCategory.Profile, RawDiagnosticCode.InvalidProfile, RawSeverity.Error, RawFormat.Unknown, -1, context, message, false));
    }
}
