namespace icc;

internal class ProfileCloneContext
{
    internal System.Collections.Generic.Dictionary<byte[], byte[]> ByteArrays;
    internal System.Collections.Generic.Dictionary<IccByteLocation, IccByteLocation> Locations;
    internal System.Collections.Generic.Dictionary<IccOperationResult, IccOperationResult> OperationResults;
    internal System.Collections.Generic.Dictionary<IccValidationIssue, IccValidationIssue> ValidationIssues;
    internal System.Collections.Generic.Dictionary<IccTagTableEntry, IccTagTableEntry> TableEntries;
    internal System.Collections.Generic.Dictionary<IccRawData, IccRawData> RawDataValues;
    internal System.Collections.Generic.Dictionary<IccTagData, IccTagData> TagDataValues;
    internal System.Collections.Generic.Dictionary<IccTag, IccTag> Tags;
    internal System.Collections.Generic.Dictionary<IccTagArray, IccTagArray> TagArrays;
    internal System.Collections.Generic.Dictionary<IccTagStructure, IccTagStructure> TagStructures;
    internal System.Collections.Generic.Dictionary<IccProcessingElement, IccProcessingElement> ProcessingElements;
    internal System.Collections.Generic.Dictionary<IccAdaptiveGainCurveNode[], IccAdaptiveGainCurveNode[]> AdaptiveGainCurveNodeArrays;
    internal System.Collections.Generic.Dictionary<IccCalculatorProgram, IccCalculatorProgram> CalculatorPrograms;
    internal System.Collections.Generic.Dictionary<IccProfileSequenceIdentifier, IccProfileSequenceIdentifier> ProfileSequenceIdentifiers;
    internal System.Collections.Generic.Dictionary<IccProfile, IccProfile> Profiles;
    internal System.Collections.Generic.Dictionary<TagDecodeSource, TagDecodeSource> TagDecodeSources;
    internal System.Collections.Generic.Dictionary<TagDecodeState, TagDecodeState> TagDecodeStates;

    internal ProfileCloneContext()
    {
        ByteArrays = new System.Collections.Generic.Dictionary<byte[], byte[]>();
        Locations = new System.Collections.Generic.Dictionary<IccByteLocation, IccByteLocation>();
        OperationResults = new System.Collections.Generic.Dictionary<IccOperationResult, IccOperationResult>();
        ValidationIssues = new System.Collections.Generic.Dictionary<IccValidationIssue, IccValidationIssue>();
        TableEntries = new System.Collections.Generic.Dictionary<IccTagTableEntry, IccTagTableEntry>();
        RawDataValues = new System.Collections.Generic.Dictionary<IccRawData, IccRawData>();
        TagDataValues = new System.Collections.Generic.Dictionary<IccTagData, IccTagData>();
        Tags = new System.Collections.Generic.Dictionary<IccTag, IccTag>();
        TagArrays = new System.Collections.Generic.Dictionary<IccTagArray, IccTagArray>();
        TagStructures = new System.Collections.Generic.Dictionary<IccTagStructure, IccTagStructure>();
        ProcessingElements = new System.Collections.Generic.Dictionary<IccProcessingElement, IccProcessingElement>();
        AdaptiveGainCurveNodeArrays = new System.Collections.Generic.Dictionary<IccAdaptiveGainCurveNode[], IccAdaptiveGainCurveNode[]>();
        CalculatorPrograms = new System.Collections.Generic.Dictionary<IccCalculatorProgram, IccCalculatorProgram>();
        ProfileSequenceIdentifiers = new System.Collections.Generic.Dictionary<IccProfileSequenceIdentifier, IccProfileSequenceIdentifier>();
        Profiles = new System.Collections.Generic.Dictionary<IccProfile, IccProfile>();
        TagDecodeSources = new System.Collections.Generic.Dictionary<TagDecodeSource, TagDecodeSource>();
        TagDecodeStates = new System.Collections.Generic.Dictionary<TagDecodeState, TagDecodeState>();
    }
}

internal static class ProfileClone
{
    internal static IccProfile CloneProfile(IccProfile source)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile to clone cannot be null.");
        }

        ProfileCloneContext context = new ProfileCloneContext();
        return CloneProfileWithContext(source, context);
    }

    internal static IccProfile CloneProfileWithContext(IccProfile source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile to clone cannot be null.");
        }

        if (context.Profiles.ContainsKey(source))
        {
            return context.Profiles[source];
        }

        IccProfile clone = new IccProfile();
        context.Profiles.Add(source, clone);
        clone.Header = CloneHeader(source.Header, context);
        clone.TagTableEntries = CloneTableEntryArray(source.TagTableEntries, context);
        clone.Tags = CloneTagArray(source.Tags, context);
        clone.ValidationIssues = CloneValidationIssueArray(source.ValidationIssues, context);
        clone.OriginalBytes = CloneBytes(source.OriginalBytes, "profile original bytes", context);
        clone.TrailingBytes = CloneBytes(source.TrailingBytes, "profile trailing bytes", context);
        clone.Location = CloneLocation(source.Location, "profile location", context);
        clone.ReadResult = CloneOperationResult(source.ReadResult, "profile read result", context);
        return clone;
    }

    internal static IccProfileHeader CloneHeaderStandalone(IccProfileHeader source)
    {
        ProfileCloneContext context = new ProfileCloneContext();
        return CloneHeader(source, context);
    }

    internal static IccTag CloneTagStandalone(IccTag source)
    {
        ProfileCloneContext context = new ProfileCloneContext();
        return CloneTag(source, "tag", context);
    }

    internal static IccTagData CloneTagDataStandalone(IccTagData source)
    {
        ProfileCloneContext context = new ProfileCloneContext();
        return CloneTagData(source, "tag data", context);
    }

    internal static IccProfileHeader CloneHeader(IccProfileHeader source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile header to clone cannot be null.");
        }

        IccProfileHeader clone = new IccProfileHeader();
        clone.DeclaredSize = source.DeclaredSize;
        clone.PreferredCmmSignature = source.PreferredCmmSignature;
        clone.Version = CloneVersion(source.Version);
        clone.ProfileClass = source.ProfileClass;
        clone.ProfileClassSignature = source.ProfileClassSignature;
        clone.DataColorSpaceSignature = source.DataColorSpaceSignature;
        clone.DataColorSpace = source.DataColorSpace;
        clone.ProfileConnectionSpaceSignature = source.ProfileConnectionSpaceSignature;
        clone.ProfileConnectionSpace = source.ProfileConnectionSpace;
        clone.CreationDateTime = CloneDateTime(source.CreationDateTime);
        clone.ProfileFileSignature = source.ProfileFileSignature;
        clone.PrimaryPlatformSignature = source.PrimaryPlatformSignature;
        clone.ProfileFlags = source.ProfileFlags;
        clone.IsEmbedded = source.IsEmbedded;
        clone.RequiresEmbeddedData = source.RequiresEmbeddedData;
        clone.RequiresMcsSubset = source.RequiresMcsSubset;
        clone.UsesExtendedPcsRange = source.UsesExtendedPcsRange;
        clone.DeviceManufacturerSignature = source.DeviceManufacturerSignature;
        clone.DeviceModelSignature = source.DeviceModelSignature;
        clone.DeviceAttributes = source.DeviceAttributes;
        clone.RenderingIntent = source.RenderingIntent;
        clone.RenderingIntentKind = source.RenderingIntentKind;
        clone.PcsIlluminantX = source.PcsIlluminantX;
        clone.PcsIlluminantY = source.PcsIlluminantY;
        clone.PcsIlluminantZ = source.PcsIlluminantZ;
        clone.ProfileCreatorSignature = source.ProfileCreatorSignature;
        clone.ProfileId = CloneProfileId(source.ProfileId, context);
        clone.SpectralPcsSignature = source.SpectralPcsSignature;
        clone.SpectralPcs = source.SpectralPcs;
        clone.SpectralPcsRangeStart = source.SpectralPcsRangeStart;
        clone.SpectralPcsRangeEnd = source.SpectralPcsRangeEnd;
        clone.SpectralPcsRangeSteps = source.SpectralPcsRangeSteps;
        clone.BiSpectralPcsRangeStart = source.BiSpectralPcsRangeStart;
        clone.BiSpectralPcsRangeEnd = source.BiSpectralPcsRangeEnd;
        clone.BiSpectralPcsRangeSteps = source.BiSpectralPcsRangeSteps;
        clone.MaterialConnectionSpaceSignature = source.MaterialConnectionSpaceSignature;
        clone.MaterialConnectionSpace = source.MaterialConnectionSpace;
        clone.ProfileSubclassSignature = source.ProfileSubclassSignature;
        clone.IccMaxReservedValue = source.IccMaxReservedValue;
        clone.ContextualBytes = CloneBytes(source.ContextualBytes, "profile header contextual bytes", context);
        clone.RawBytes = CloneBytes(source.RawBytes, "profile header raw bytes", context);
        clone.ValidationIssues = CloneValidationIssueArray(source.ValidationIssues, context);
        return clone;
    }

    internal static IccValidationIssue[] CloneValidationIssueArray(IccValidationIssue[] source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile header validation-issue array to clone cannot be null.");
        }

        IccValidationIssue[] clone = new IccValidationIssue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneValidationIssue(source[index], index, context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccValidationIssue CloneValidationIssue(IccValidationIssue source, int issueIndex, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"Profile validation issue {issueIndex} cannot be null.");
        }

        if (context.ValidationIssues.ContainsKey(source))
        {
            return context.ValidationIssues[source];
        }

        IccValidationIssue clone = new IccValidationIssue();
        context.ValidationIssues.Add(source, clone);
        clone.RuleId = source.RuleId;
        clone.Severity = source.Severity;
        clone.Message = source.Message;
        clone.Path = source.Path;
        clone.Location = CloneLocation(source.Location, $"profile validation issue {issueIndex} location", context);
        clone.Remediation = source.Remediation;
        return clone;
    }

    internal static IccVersion CloneVersion(IccVersion source)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile version to clone cannot be null.");
        }

        IccVersion clone = new IccVersion();
        clone.Kind = source.Kind;
        clone.Major = source.Major;
        clone.Minor = source.Minor;
        clone.BugFix = source.BugFix;
        clone.SubclassMajor = source.SubclassMajor;
        clone.SubclassMinor = source.SubclassMinor;
        clone.IsSupported = source.IsSupported;
        clone.IsFutureMinor = source.IsFutureMinor;
        clone.PackedValue = source.PackedValue;
        return clone;
    }

    internal static IccDateTime CloneDateTime(IccDateTime source)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile date and time to clone cannot be null.");
        }

        IccDateTime clone = new IccDateTime();
        clone.Year = source.Year;
        clone.Month = source.Month;
        clone.Day = source.Day;
        clone.Hour = source.Hour;
        clone.Minute = source.Minute;
        clone.Second = source.Second;
        return clone;
    }

    internal static IccProfileId CloneProfileId(IccProfileId source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile ID to clone cannot be null.");
        }

        IccProfileId clone = new IccProfileId();
        clone.Bytes = CloneBytes(source.Bytes, "profile ID bytes", context);
        clone.IsPresent = source.IsPresent;
        return clone;
    }

    internal static IccTagTableEntry[] CloneTableEntryArray(IccTagTableEntry[] source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile tag-table array to clone cannot be null.");
        }

        IccTagTableEntry[] clone = new IccTagTableEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneTableEntry(source[index], $"tag-table entry {index}", context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccTagTableEntry CloneTableEntry(IccTagTableEntry source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.TableEntries.ContainsKey(source))
        {
            return context.TableEntries[source];
        }

        IccTagTableEntry clone = new IccTagTableEntry();
        context.TableEntries.Add(source, clone);
        clone.Signature = source.Signature;
        clone.Offset = source.Offset;
        clone.Size = source.Size;
        clone.OriginalIndex = source.OriginalIndex;
        clone.SharedDataId = source.SharedDataId;
        return clone;
    }

    internal static IccTag[] CloneTagArray(IccTag[] source, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException("The profile tag array to clone cannot be null.");
        }

        IccTag[] clone = new IccTag[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = CloneTag(source[index], $"tag {index}", context);
            index = index + 1;
        }

        return clone;
    }

    internal static IccTag CloneTag(IccTag source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.Tags.ContainsKey(source))
        {
            return context.Tags[source];
        }

        IccTag clone = new IccTag();
        context.Tags.Add(source, clone);
        clone.Signature = source.Signature;
        clone.TableEntry = CloneTableEntry(source.TableEntry, $"{path} table entry", context);
        clone.Data = CloneTagData(source.Data, $"{path} data", context);
        clone.PaddingBytes = CloneBytes(source.PaddingBytes, $"{path} padding bytes", context);
        clone.IsDecodeAttempted = source.IsDecodeAttempted;
        clone.DecodeResult = CloneOperationResult(source.DecodeResult, $"{path} decode result", context);
        if (source.DecodeState != null)
        {
            clone.DecodeState = CloneTagDecodeState(source.DecodeState, context);
        }

        return clone;
    }

    internal static TagDecodeState CloneTagDecodeState(TagDecodeState source, ProfileCloneContext context)
    {
        if (context.TagDecodeStates.ContainsKey(source))
        {
            return context.TagDecodeStates[source];
        }

        TagDecodeSource decodeSource = CloneTagDecodeSource(source.Source, context);
        TagDecodeState clone = new TagDecodeState(decodeSource, source.PayloadOffset, source.PayloadSize, source.AbsolutePayloadOffset, source.ProfileVersion, source.PreserveOriginalBytes);
        context.TagDecodeStates.Add(source, clone);
        clone.MaximumDecompressedBytes = source.MaximumDecompressedBytes;
        clone.MaximumProfileBytes = source.MaximumProfileBytes;
        clone.MaximumTagCount = source.MaximumTagCount;
        clone.MaximumTagBytes = source.MaximumTagBytes;
        clone.MaximumTotalTagBytes = source.MaximumTotalTagBytes;
        clone.MaximumDiagnosticCount = source.MaximumDiagnosticCount;
        clone.MaximumNestingDepth = source.MaximumNestingDepth;
        clone.CurrentNestingDepth = source.CurrentNestingDepth;
        clone.MaximumCalculatorInstructions = source.MaximumCalculatorInstructions;
        clone.MaximumCalculatorStackDepth = source.MaximumCalculatorStackDepth;
        clone.MaximumCalculatorStorageValues = source.MaximumCalculatorStorageValues;
        clone.HasProfileContext = source.HasProfileContext;
        clone.TagSignature = source.TagSignature;
        clone.ProfileClass = source.ProfileClass;
        clone.ProfileClassSignature = source.ProfileClassSignature;
        clone.DataColorSpace = source.DataColorSpace;
        clone.DataColorSpaceSignature = source.DataColorSpaceSignature;
        clone.ProfileConnectionSpace = source.ProfileConnectionSpace;
        clone.ProfileConnectionSpaceSignature = source.ProfileConnectionSpaceSignature;
        clone.SpectralPcsSignature = source.SpectralPcsSignature;
        clone.MaterialConnectionSpace = source.MaterialConnectionSpace;
        clone.MaterialConnectionSpaceSignature = source.MaterialConnectionSpaceSignature;
        clone.HasMatrixPrimaries = source.HasMatrixPrimaries;
        clone.IsEmbedded = source.IsEmbedded;
        clone.RequiresEmbeddedData = source.RequiresEmbeddedData;
        if (source.IsAttempted)
        {
            clone.Data = CloneTagData(source.Data, "lazy tag data", context);
            clone.Result = CloneOperationResult(source.Result, "lazy tag decode result", context);
            clone.IsAttempted = true;
        }

        return clone;
    }

    internal static TagDecodeSource CloneTagDecodeSource(TagDecodeSource source, ProfileCloneContext context)
    {
        if (context.TagDecodeSources.ContainsKey(source))
        {
            return context.TagDecodeSources[source];
        }

        TagDecodeSource clone = new TagDecodeSource(source.ProfileBytes.ToArray());
        context.TagDecodeSources.Add(source, clone);
        return clone;
    }

    internal static IccTagData CloneTagData(IccTagData source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.TagDataValues.ContainsKey(source))
        {
            return context.TagDataValues[source];
        }

        IccTagData clone = new IccTagData();
        context.TagDataValues.Add(source, clone);
        clone.Kind = source.Kind;
        clone.RawData = CloneRawData(source.RawData, $"{path} raw data", context);
        ExtendedTagDataClone.CopyFields(source, clone, path, context);
        return clone;
    }

    internal static IccRawData CloneRawData(IccRawData source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.RawDataValues.ContainsKey(source))
        {
            return context.RawDataValues[source];
        }

        IccRawData clone = new IccRawData();
        context.RawDataValues.Add(source, clone);
        clone.TypeSignature = source.TypeSignature;
        clone.HasTypeSignature = source.HasTypeSignature;
        clone.Bytes = CloneBytes(source.Bytes, $"{path} bytes", context);
        return clone;
    }

    internal static IccOperationResult CloneOperationResult(IccOperationResult source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.OperationResults.ContainsKey(source))
        {
            return context.OperationResults[source];
        }

        IccOperationResult clone = new IccOperationResult();
        context.OperationResults.Add(source, clone);
        clone.Status = source.Status;
        clone.Message = source.Message;
        clone.Location = CloneLocation(source.Location, $"{path} location", context);
        return clone;
    }

    internal static IccByteLocation CloneLocation(IccByteLocation source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.Locations.ContainsKey(source))
        {
            return context.Locations[source];
        }

        IccByteLocation clone = new IccByteLocation();
        context.Locations.Add(source, clone);
        clone.Offset = source.Offset;
        clone.Length = source.Length;
        return clone;
    }

    internal static byte[] CloneBytes(byte[] source, string path, ProfileCloneContext context)
    {
        if (source == null)
        {
            throw new IccProfileValidationException($"The {path} to clone cannot be null.");
        }

        if (context.ByteArrays.ContainsKey(source))
        {
            return context.ByteArrays[source];
        }

        byte[] clone = new byte[source.Length];
        System.ReadOnlySpan<byte> sourceSpan = new System.ReadOnlySpan<byte>(source);
        sourceSpan.CopyTo(clone);
        context.ByteArrays.Add(source, clone);
        return clone;
    }
}
