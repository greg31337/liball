namespace icc;

internal class ProfileLinkerCore
{
    internal ProfileLinkerCore()
    {
    }

    internal IccProfile Link(IccProfile[] profiles, IccProfileLinkOptions options, IccWriteOptions writeOptions)
    {
        ValidateLinkOptions(options, writeOptions);
        IccOperationResult compatibility = ProbeWithFallback(profiles, options.RenderingIntent, options.IntentFallback);
        if (compatibility.Status != IccOperationStatus.Success)
        {
            throw new IccProfileValidationException(compatibility.Message);
        }

        IccTransformOptions transformOptions = new IccTransformOptions();
        transformOptions.Direction = IccTransformDirection.DeviceToDevice;
        transformOptions.RenderingIntent = options.RenderingIntent;
        transformOptions.IntentFallback = options.IntentFallback;
        transformOptions.Interpolation = options.Interpolation;
        transformOptions.PcsEncoding = IccPcsEncodingKind.Normalized;
        transformOptions.BlackPointCompensation = false;
        transformOptions.ClipOutput = true;
        transformOptions.EnableTrace = false;
        IccColorTransform transform = ProfileTransformCompiler.CompileWithOptions(profiles, transformOptions);
        int inputChannels = transform.GetSourceChannelCount();
        int outputChannels = transform.GetDestinationChannelCount();
        ValidateEndpointAndGrid(inputChannels, outputChannels, options, writeOptions.ResourceLimits);
        double[] values = SampleTransform(transform, inputChannels, outputChannels, options.GridPointCount);

        IccTag transformTag = CreateTransformTag(values, inputChannels, outputChannels, options);
        IccTag[] tags = CreateTags(profiles, transformTag, options);
        IccProfile profile = new IccProfile();
        profile.Header = CreateHeader(profiles, options);
        profile.Tags = tags;
        profile.TagTableEntries = CreateTableEntries(tags);

        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, writeOptions);
        byte[] encoded = ProfileWriterCore.EncodePlan(plan);
        IccReadOptions readOptions = new IccReadOptions();
        readOptions.ResourceLimits = writeOptions.ResourceLimits;
        readOptions.DecodeTagsLazily = false;
        return ProfileCodec.ReadEnvelope(encoded, 0, 0, readOptions);
    }

    internal IccOperationResult Probe(IccProfile[] profiles, IccRenderingIntent intent)
    {
        return ProbeWithFallback(profiles, intent, IccIntentFallback.None);
    }

    internal IccOperationResult ProbeWithFallback(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback)
    {
        if (profiles == null || profiles.Length < 2)
        {
            return CreateResult(IccOperationStatus.InvalidData, "Profile linking requires at least two profiles.");
        }
        if (!IsValidIntent(intent) || !IsValidFallback(fallback))
        {
            return CreateResult(IccOperationStatus.InvalidData, "The profile-link rendering intent or fallback policy is invalid.");
        }

        int index = 0;
        while (index < profiles.Length)
        {
            IccProfile profile = profiles[index];
            if (!ProfileTransformCompiler.HasBasicProfileData(profile))
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Profile {index} is missing its header, version, or tag collection.");
            }
            if (!ProfileTransformCompiler.HasSupportedVersion(profile))
            {
                return CreateResult(IccOperationStatus.UnsupportedFeature, $"Profile {index} is not an ICC version 2, version 4, or version 5 standard profile.");
            }
            if (!ProfileTransformCompiler.HasConnectablePcs(profile))
            {
                return CreateResult(IccOperationStatus.Conflict, $"Profile {index} does not declare a connectable PCSXYZ or PCSLAB space.");
            }
            index = index + 1;
        }

        IntentSelection source = SelectLinkTransform(profiles[0], IccTransformDirection.DeviceToPcs,
            intent, fallback);
        if (!source.Success)
        {
            return CreateResult(IccOperationStatus.NotFound, $"Profile 0 has no link input transform: {source.Message}");
        }
        index = 1;
        while (index < profiles.Length - 1)
        {
            IccProfile profile = profiles[index];
            if (profile.Header.ProfileClass == IccProfileClass.Abstract)
            {
                uint dataSpace = profile.Header.DataColorSpaceSignature;
                if (dataSpace != IccColorSpaceSignatures.Xyz && dataSpace != IccColorSpaceSignatures.Lab)
                {
                    return CreateResult(IccOperationStatus.Conflict,
                        $"Intermediate abstract profile {index} does not use PCSXYZ or PCSLAB input data.");
                }
                IntentSelection abstractDirection = SelectLinkTransform(profile,
                    IccTransformDirection.DeviceToPcs, intent, fallback);
                if (!abstractDirection.Success)
                {
                    return CreateResult(IccOperationStatus.NotFound,
                        $"Abstract profile {index} has no forward link transform: {abstractDirection.Message}");
                }
            }
            else
            {
                IntentSelection reverse = SelectLinkTransform(profile,
                    IccTransformDirection.PcsToDevice, intent, fallback);
                if (!reverse.Success)
                {
                    return CreateResult(IccOperationStatus.NotFound,
                        $"Profile {index} has no link output transform: {reverse.Message}");
                }
                IntentSelection forward = SelectLinkTransform(profile,
                    IccTransformDirection.DeviceToPcs, intent, fallback);
                if (!forward.Success)
                {
                    return CreateResult(IccOperationStatus.NotFound,
                        $"Profile {index} has no link input transform: {forward.Message}");
                }
            }
            index = index + 1;
        }

        int destinationIndex = profiles.Length - 1;
        IntentSelection destination = SelectLinkTransform(profiles[destinationIndex],
            IccTransformDirection.PcsToDevice, intent, fallback);
        if (!destination.Success)
        {
            return CreateResult(IccOperationStatus.NotFound,
                $"Profile {destinationIndex} has no link output transform: {destination.Message}");
        }

        return CreateResult(IccOperationStatus.Success, "The profile sequence can be baked into a DeviceLink transform.");
    }

    internal IntentSelection SelectLinkTransform(IccProfile profile, IccTransformDirection direction,
        IccRenderingIntent intent, IccIntentFallback fallback)
    {
        return profile.Header.Version.Kind == IccVersionKind.Version5
            ? IntentSelector.SelectVersionFiveTransform(profile, direction, intent, fallback)
            : IntentSelector.SelectIcc1Transform(profile, direction, intent, fallback, false);
    }

    internal void ValidateLinkOptions(IccProfileLinkOptions options, IccWriteOptions writeOptions)
    {
        if (options == null || options.Version == null || options.Descriptions == null)
        {
            throw new IccProfileValidationException("Profile-link options, version, and descriptions cannot be null.");
        }
        if (writeOptions == null || writeOptions.ResourceLimits == null)
        {
            throw new IccProfileValidationException("Profile-link write options and resource limits cannot be null.");
        }
        if (!IsValidIntent(options.RenderingIntent) || !IsValidFallback(options.IntentFallback))
        {
            throw new IccProfileValidationException("The profile-link rendering intent or fallback policy is invalid.");
        }
        if (options.Interpolation != IccInterpolationKind.Linear
            && options.Interpolation != IccInterpolationKind.Multilinear
            && options.Interpolation != IccInterpolationKind.Tetrahedral)
        {
            throw new IccProfileValidationException("The profile-link interpolation mode is invalid.");
        }
        if (options.Version.Kind != IccVersionKind.Version2 && options.Version.Kind != IccVersionKind.Version4)
        {
            throw new IccProfileValidationException("A linked DeviceLink must use ICC version 2 or version 4.");
        }
        int expectedMajor = options.Version.Kind == IccVersionKind.Version2
            ? IccConstants.Version2Major : IccConstants.Version4Major;
        if ((options.Version.Major != 0 && options.Version.Major != expectedMajor)
            || options.Version.Minor < 0 || options.Version.Minor > 9
            || options.Version.BugFix < 0 || options.Version.BugFix > 9
            || (options.Version.Kind == IccVersionKind.Version4 && options.Version.Minor > 4))
        {
            throw new IccProfileValidationException("The linked DeviceLink version fields are inconsistent or unsupported.");
        }
        if (options.EncodingTypeSignature != IccTypeSignatures.Lut8
            && options.EncodingTypeSignature != IccTypeSignatures.Lut16
            && options.EncodingTypeSignature != IccTypeSignatures.MultiProcessElements)
        {
            throw new IccProfileValidationException("The linked DeviceLink encoding must be LUT8, LUT16, or multi-process elements.");
        }
        if (options.EncodingTypeSignature == IccTypeSignatures.MultiProcessElements
            && options.Version.Kind != IccVersionKind.Version4)
        {
            throw new IccProfileValidationException("Multi-process-element DeviceLinks require ICC version 4.");
        }
        if (options.GridPointCount < 2 || options.GridPointCount > 255)
        {
            throw new IccProfileValidationException("The DeviceLink grid-point count must be between 2 and 255.");
        }
        CloneLocalizedStrings(options.Descriptions);
    }

    internal void ValidateEndpointAndGrid(int inputChannels, int outputChannels, IccProfileLinkOptions options,
        IccResourceLimits limits)
    {
        if (inputChannels < 1 || inputChannels > IccConstants.MaximumIcc1ChannelCount
            || outputChannels < 1 || outputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileValidationException("A version 2 or version 4 DeviceLink requires between one and fifteen endpoint channels.");
        }
        if (options.Interpolation == IccInterpolationKind.Linear && inputChannels != 1)
        {
            throw new IccProfileValidationException("Linear DeviceLink interpolation requires exactly one input channel.");
        }
        if (options.Interpolation == IccInterpolationKind.Tetrahedral && inputChannels != 3)
        {
            throw new IccProfileValidationException("Tetrahedral DeviceLink interpolation requires exactly three input channels.");
        }

        long sampleCount = 1;
        int dimension = 0;
        while (dimension < inputChannels)
        {
            if (sampleCount > int.MaxValue / options.GridPointCount)
            {
                throw new IccProfileValidationException("The selected DeviceLink grid exceeds the supported managed value count.");
            }
            sampleCount = sampleCount * options.GridPointCount;
            if (sampleCount > int.MaxValue / outputChannels)
            {
                throw new IccProfileValidationException("The selected DeviceLink grid exceeds the supported managed value count.");
            }
            dimension = dimension + 1;
        }

        int valueWidth = options.EncodingTypeSignature == IccTypeSignatures.Lut8 ? 1
            : options.EncodingTypeSignature == IccTypeSignatures.Lut16 ? 2 : 4;
        long estimatedTransformBytes = sampleCount * outputChannels * valueWidth;
        if (options.EncodingTypeSignature == IccTypeSignatures.MultiProcessElements)
        {
            estimatedTransformBytes = estimatedTransformBytes + 52;
        }
        else if (options.EncodingTypeSignature == IccTypeSignatures.Lut8)
        {
            estimatedTransformBytes = estimatedTransformBytes + 48 + ((inputChannels + outputChannels) * 256);
        }
        else
        {
            estimatedTransformBytes = estimatedTransformBytes + 52 + ((inputChannels + outputChannels) * 4);
        }
        if (estimatedTransformBytes > limits.MaximumTagBytes || estimatedTransformBytes > limits.MaximumTotalTagBytes
            || estimatedTransformBytes + IccConstants.MinimumProfileSize > limits.MaximumProfileBytes)
        {
            throw new IccProfileValidationException("The selected DeviceLink grid exceeds the configured profile-writing resource limits.");
        }
    }

    internal double[] SampleTransform(IccColorTransform transform, int inputChannels, int outputChannels, int grid)
    {
        int pointCount = 1;
        int dimension = 0;
        while (dimension < inputChannels)
        {
            pointCount = pointCount * grid;
            dimension = dimension + 1;
        }

        double[] values = new double[pointCount * outputChannels];
        float[] source = new float[inputChannels];
        int point = 0;
        while (point < pointCount)
        {
            int remainder = point;
            dimension = inputChannels - 1;
            while (dimension >= 0)
            {
                source[dimension] = (float)(remainder % grid) / (grid - 1);
                remainder = remainder / grid;
                dimension = dimension - 1;
            }

            float[] destination = transform.TransformSingleColor(source);
            int output = 0;
            while (output < outputChannels)
            {
                values[(point * outputChannels) + output] = destination[output];
                output = output + 1;
            }
            point = point + 1;
        }

        return values;
    }

    internal IccTag CreateTransformTag(double[] values, int inputChannels, int outputChannels,
        IccProfileLinkOptions options)
    {
        IccClut clut = new IccClut();
        clut.InputChannels = inputChannels;
        clut.OutputChannels = outputChannels;
        clut.GridPoints = CreateGridPoints(inputChannels, options.GridPointCount);
        clut.Values = values;
        clut.Interpolation = options.Interpolation;
        clut.InterpolationHint = options.Interpolation == IccInterpolationKind.Tetrahedral ? 1 : 0;

        IccTagData data = new IccTagData();
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = options.EncodingTypeSignature;
        uint tagSignature = IccTagSignatures.AToB0;
        if (options.EncodingTypeSignature == IccTypeSignatures.MultiProcessElements)
        {
            IccProcessingElement element = new IccProcessingElement();
            element.Signature = IccProcessingElementSignatures.Clut;
            element.Kind = IccProcessingElementKind.Clut;
            element.InputChannels = inputChannels;
            element.OutputChannels = outputChannels;
            element.InputRanges = CreateRanges(inputChannels);
            element.OutputRanges = CreateRanges(outputChannels);
            element.Clut = clut;
            IccMultiProcessElements pipeline = new IccMultiProcessElements();
            pipeline.InputChannels = inputChannels;
            pipeline.OutputChannels = outputChannels;
            pipeline.InputRanges = CreateRanges(inputChannels);
            pipeline.OutputRanges = CreateRanges(outputChannels);
            pipeline.Elements = new IccProcessingElement[] { element };
            pipeline.IsExecutable = true;
            data.Kind = IccTagDataKind.MultiProcessElements;
            data.MultiProcessElements = pipeline;
            tagSignature = IccTagSignatures.DToB0;
        }
        else
        {
            IccLut lut = new IccLut();
            lut.InputChannels = inputChannels;
            lut.OutputChannels = outputChannels;
            lut.Matrix = CreateIdentityMatrix();
            int tableSize = options.EncodingTypeSignature == IccTypeSignatures.Lut8 ? 256 : 2;
            lut.InputTables = CreateIdentityTables(inputChannels, tableSize);
            lut.OutputTables = CreateIdentityTables(outputChannels, tableSize);
            lut.Clut = clut;
            lut.PcsEncoding = options.EncodingTypeSignature == IccTypeSignatures.Lut8
                ? IccPcsEncodingKind.Normalized : IccPcsEncodingKind.Version2;
            data.Kind = IccTagDataKind.Lut;
            data.Lut = lut;
        }

        IccTag tag = new IccTag();
        tag.Signature = tagSignature;
        tag.TableEntry.Signature = tagSignature;
        tag.Data = data;
        return tag;
    }

    internal IccTag[] CreateTags(IccProfile[] profiles, IccTag transformTag, IccProfileLinkOptions options)
    {
        int metadataCount = options.IncludeProfileSequence ? (options.Version.Kind == IccVersionKind.Version4 ? 2 : 1) : 0;
        IccTag[] tags = new IccTag[3 + metadataCount];
        tags[0] = transformTag;
        tags[1] = CreateDescriptionTag(IccTagSignatures.ProfileDescription, options.Descriptions,
            "Linked ICC profile sequence", options.Version.Kind);
        tags[2] = CreateDescriptionTag(IccTagSignatures.Copyright, new IccLocalizedString[]
        {
            CreateLocalizedString("en", "US", "Generated deterministically by icc")
        }, "Generated deterministically by icc", options.Version.Kind);
        if (options.IncludeProfileSequence)
        {
            tags[3] = CreateSequenceTag(profiles, options.Version.Kind);
            if (options.Version.Kind == IccVersionKind.Version4)
            {
                tags[4] = CreateIdentifierTag(profiles);
            }
        }
        return tags;
    }

    internal IccTag CreateDescriptionTag(uint signature, IccLocalizedString[] descriptions, string fallback,
        IccVersionKind version)
    {
        IccTagData data = new IccTagData();
        if (version == IccVersionKind.Version2)
        {
            data.Kind = IccTagDataKind.Text;
            data.Text = CreateTextData(SelectDescription(descriptions, fallback));
            data.RawData.HasTypeSignature = true;
            data.RawData.TypeSignature = IccTypeSignatures.TextDescription;
        }
        else
        {
            data.Kind = IccTagDataKind.LocalizedText;
            data.LocalizedStrings = descriptions.Length == 0
                ? new IccLocalizedString[] { CreateLocalizedString("en", "US", fallback) }
                : CloneLocalizedStrings(descriptions);
            data.RawData.HasTypeSignature = true;
            data.RawData.TypeSignature = IccTypeSignatures.MultiLocalizedUnicode;
        }
        return CreateTag(signature, data);
    }

    internal IccTag CreateSequenceTag(IccProfile[] profiles, IccVersionKind version)
    {
        IccProfileSequenceEntry[] entries = new IccProfileSequenceEntry[profiles.Length];
        int index = 0;
        while (index < profiles.Length)
        {
            IccProfile profile = profiles[index];
            IccLocalizedString[] descriptions = ReadLocalizedDescriptions(profile);
            string name = ProfileDescriptionCore.FindProfileName(profile);
            IccProfileSequenceEntry entry = new IccProfileSequenceEntry();
            entry.DeviceManufacturerSignature = profile.Header.DeviceManufacturerSignature;
            entry.DeviceModelSignature = profile.Header.DeviceModelSignature;
            entry.DeviceAttributes = profile.Header.DeviceAttributes;
            entry.TechnologySignature = ReadTechnologySignature(profile);
            entry.ManufacturerDescription = CreateTextData(string.Empty);
            entry.ModelDescription = CreateTextData(name);
            if (version == IccVersionKind.Version4)
            {
                entry.ManufacturerLocalizedDescriptions = new IccLocalizedString[]
                {
                    CreateLocalizedString("en", "US", string.Empty)
                };
                entry.ModelLocalizedDescriptions = descriptions.Length == 0
                    ? new IccLocalizedString[] { CreateLocalizedString("en", "US", name) } : descriptions;
            }
            entries[index] = entry;
            index = index + 1;
        }

        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.ProfileSequence;
        data.ProfileSequence = entries;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.ProfileSequenceDescription;
        return CreateTag(IccTagSignatures.ProfileSequenceDescription, data);
    }

    internal IccTag CreateIdentifierTag(IccProfile[] profiles)
    {
        IccProfileSequenceIdentifier[] identifiers = new IccProfileSequenceIdentifier[profiles.Length];
        int index = 0;
        while (index < profiles.Length)
        {
            IccProfileSequenceIdentifier identifier = new IccProfileSequenceIdentifier();
            identifier.ProfileId = CloneProfileId(profiles[index].Header.ProfileId);
            IccLocalizedString[] descriptions = ReadLocalizedDescriptions(profiles[index]);
            identifier.Descriptions = descriptions.Length == 0
                ? new IccLocalizedString[] { CreateLocalizedString("en", "US", ProfileDescriptionCore.FindProfileName(profiles[index])) }
                : descriptions;
            identifiers[index] = identifier;
            index = index + 1;
        }

        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.ProfileSequence;
        data.ProfileSequenceIdentifiers = identifiers;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.ProfileSequenceIdentifier;
        return CreateTag(IccTagSignatures.ProfileSequenceIdentifier, data);
    }

    internal IccProfileHeader CreateHeader(IccProfile[] profiles, IccProfileLinkOptions options)
    {
        IccProfileHeader header = new IccProfileHeader();
        header.Version = CloneVersion(options.Version);
        header.ProfileClass = IccProfileClass.DeviceLink;
        header.ProfileClassSignature = IccProfileClassSignatures.DeviceLink;
        header.DataColorSpaceSignature = profiles[0].Header.DataColorSpaceSignature;
        header.DataColorSpace = HeaderCodec.DecodeColorSpace(header.DataColorSpaceSignature);
        header.ProfileConnectionSpaceSignature = profiles[profiles.Length - 1].Header.DataColorSpaceSignature;
        header.ProfileConnectionSpace = IccPcsKind.None;
        header.CreationDateTime.Year = 2000;
        header.CreationDateTime.Month = 1;
        header.CreationDateTime.Day = 1;
        header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        header.RenderingIntent = (int)options.RenderingIntent;
        header.RenderingIntentKind = options.RenderingIntent;
        header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        WriteUInt16(header.RawBytes, 24, 2000);
        WriteUInt16(header.RawBytes, 26, 1);
        WriteUInt16(header.RawBytes, 28, 1);
        return header;
    }

    internal IccVersion CloneVersion(IccVersion source)
    {
        IccVersion version = new IccVersion();
        version.Kind = source.Kind;
        version.Major = source.Major;
        version.Minor = source.Minor;
        version.BugFix = source.BugFix;
        version.SubclassMajor = source.SubclassMajor;
        version.SubclassMinor = source.SubclassMinor;
        version.IsSupported = source.IsSupported;
        version.IsFutureMinor = source.IsFutureMinor;
        version.PackedValue = source.PackedValue;
        return version;
    }

    internal IccProfileId CloneProfileId(IccProfileId source)
    {
        IccProfileId identifier = new IccProfileId();
        if (source != null && source.Bytes != null && source.Bytes.Length == IccConstants.ProfileIdSize)
        {
            System.Array.Copy(source.Bytes, identifier.Bytes, IccConstants.ProfileIdSize);
            identifier.IsPresent = source.IsPresent;
        }
        return identifier;
    }

    internal uint ReadTechnologySignature(IccProfile profile)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == IccTagSignatures.Technology && tag.Data != null
                && tag.Data.Kind == IccTagDataKind.Signature)
            {
                return tag.Data.Signature;
            }
            index = index + 1;
        }
        return 0;
    }

    internal IccLocalizedString[] ReadLocalizedDescriptions(IccProfile profile)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == IccTagSignatures.ProfileDescription && tag.Data != null
                && tag.Data.Kind == IccTagDataKind.LocalizedText && tag.Data.LocalizedStrings != null)
            {
                return CloneLocalizedStrings(tag.Data.LocalizedStrings);
            }
            index = index + 1;
        }
        return new IccLocalizedString[0];
    }

    internal IccTag CreateTag(uint signature, IccTagData data)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data = data;
        return tag;
    }

    internal IccTagTableEntry[] CreateTableEntries(IccTag[] tags)
    {
        IccTagTableEntry[] entries = new IccTagTableEntry[tags.Length];
        int index = 0;
        while (index < tags.Length)
        {
            entries[index] = tags[index].TableEntry;
            index = index + 1;
        }
        return entries;
    }

    internal IccMatrix CreateIdentityMatrix()
    {
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = 3;
        matrix.Columns = 3;
        matrix.Values = new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
        return matrix;
    }

    internal double[][] CreateIdentityTables(int channels, int size)
    {
        double[][] tables = new double[channels][];
        int channel = 0;
        while (channel < channels)
        {
            double[] table = new double[size];
            int index = 0;
            while (index < size)
            {
                table[index] = (double)index / (size - 1);
                index = index + 1;
            }
            tables[channel] = table;
            channel = channel + 1;
        }
        return tables;
    }

    internal int[] CreateGridPoints(int channels, int grid)
    {
        int[] points = new int[channels];
        int index = 0;
        while (index < channels)
        {
            points[index] = grid;
            index = index + 1;
        }
        return points;
    }

    internal IccEncodingRange[] CreateRanges(int channels)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[channels];
        int index = 0;
        while (index < channels)
        {
            ranges[index] = new IccEncodingRange();
            index = index + 1;
        }
        return ranges;
    }

    internal IccTextData CreateTextData(string value)
    {
        IccTextData text = new IccTextData();
        text.TypeSignature = IccTypeSignatures.TextDescription;
        text.Text = value;
        text.AsciiText = LegacyTagCodec.IsAsciiString(value) ? value : string.Empty;
        text.UnicodeText = value;
        return text;
    }

    internal IccLocalizedString CreateLocalizedString(string language, string country, string value)
    {
        IccLocalizedString localized = new IccLocalizedString();
        localized.Language = language;
        localized.Country = country;
        localized.Text = value;
        return localized;
    }

    internal IccLocalizedString[] CloneLocalizedStrings(IccLocalizedString[] source)
    {
        IccLocalizedString[] clone = new IccLocalizedString[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccLocalizedString item = source[index];
            if (item == null || item.Language == null || item.Country == null || item.Text == null)
            {
                throw new IccProfileValidationException($"Localized DeviceLink description {index} and its fields cannot be null.");
            }
            clone[index] = CreateLocalizedString(item.Language, item.Country, item.Text);
            index = index + 1;
        }
        return clone;
    }

    internal string SelectDescription(IccLocalizedString[] descriptions, string fallback)
    {
        if (descriptions.Length == 0)
        {
            return fallback;
        }
        return descriptions[0].Text;
    }

    internal bool IsValidIntent(IccRenderingIntent intent)
    {
        return intent == IccRenderingIntent.Perceptual
            || intent == IccRenderingIntent.MediaRelativeColorimetric
            || intent == IccRenderingIntent.Saturation
            || intent == IccRenderingIntent.IccAbsoluteColorimetric;
    }

    internal bool IsValidFallback(IccIntentFallback fallback)
    {
        return fallback == IccIntentFallback.None
            || fallback == IccIntentFallback.AllowColorimetric
            || fallback == IccIntentFallback.AllowAny;
    }

    internal IccOperationResult CreateResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        return result;
    }

    internal void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 255);
        bytes[offset + 1] = (byte)(value & 255);
    }
}
