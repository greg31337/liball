namespace icc;

internal class ProfileConstructionCore
{
    internal ProfileConstructionCore()
    {
    }

    internal IccProfile CreateProfileSkeleton(IccProfileBuildOptions options)
    {
        ValidateBuildOptions(options);
        IccProfile profile = new IccProfile();
        profile.Header = CreateSkeletonHeader(options);
        profile.Tags = CreateSkeletonTags(options);
        profile.TagTableEntries = BuildTableEntries(profile.Tags);
        return SerializeProfile(profile, false);
    }

    internal IccProfile CreateRgbProfile(IccProfileBuildOptions options)
    {
        MatrixProfileFactory factory = new MatrixProfileFactory(this);
        return factory.CreateRgbProfile(options);
    }

    internal IccProfile CreateGrayProfile(IccProfileBuildOptions options)
    {
        GrayIdentityProfileFactory factory = new GrayIdentityProfileFactory(this);
        return factory.CreateGrayProfile(options);
    }

    internal IccProfile CreateXyzIdentityProfile(IccProfileBuildOptions options)
    {
        GrayIdentityProfileFactory factory = new GrayIdentityProfileFactory(this);
        return factory.CreateXyzIdentityProfile(options);
    }

    internal IccProfile CreateAbstractProfile(IccProfileBuildOptions options)
    {
        LutProfileFactory factory = new LutProfileFactory(this);
        return factory.CreateAbstractProfile(options);
    }

    internal IccProfile CreateNamedColorProfile(IccProfileBuildOptions options)
    {
        NamedColorProfileFactory factory = new NamedColorProfileFactory(this);
        return factory.CreateNamedColorProfile(options);
    }

    internal IccProfile CreateOutputProfile(IccProfileBuildOptions options)
    {
        OutputProfileFactory factory = new OutputProfileFactory(this);
        return factory.CreateOutputProfile(options);
    }

    internal IccProfile CreateColorEncodingProfile(IccProfileBuildOptions options)
    {
        ColorEncodingProfileFactory factory = new ColorEncodingProfileFactory(this);
        return factory.CreateColorEncodingProfile(options);
    }

    internal IccProfile PreflightProfile(IccProfile profile)
    {
        return SerializeProfile(profile, true);
    }

    internal IccProfile SerializeProfile(IccProfile profile,
        bool requireConforming)
    {
        IccWriteOptions writeOptions = new IccWriteOptions();
        writeOptions.Mode = IccWriteMode.Canonical;
        writeOptions.InternIdenticalTagPayloads = true;
        writeOptions.ComputeProfileId = true;
        writeOptions.PreserveOriginalCreationDateTime = false;
        writeOptions.PreserveUnknownData = true;
        writeOptions.RejectInvalidProfile = false;
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, writeOptions);
        byte[] bytes = ProfileWriterCore.EncodePlan(plan);
        IccReadOptions readOptions = new IccReadOptions();
        readOptions.ResourceLimits = writeOptions.ResourceLimits;
        readOptions.DecodeTagsLazily = false;
        IccProfile result = ProfileCodec.ReadEnvelope(bytes, 0, 0, readOptions);
        if (requireConforming)
        {
            return ValidateConformingProfile(result);
        }
        return result;
    }

    internal IccProfile ValidateConformingProfile(IccProfile profile)
    {
        IccValidationReport report = new ValidationReportCore().Build(profile,
            new IccValidationPolicy());
        if (!report.IsValid)
        {
            throw new IccProfileValidationException(
                CreateConstructionValidationMessage(report));
        }
        return profile;
    }

    internal IccProfile CreateProfile(IccProfileHeader header, IccTag[] tags)
    {
        if (header == null)
        {
            throw new IccProfileValidationException("The profile header cannot be null.");
        }

        if (tags == null)
        {
            throw new IccProfileValidationException("The profile tag array cannot be null.");
        }

        int tagIndex = 0;
        while (tagIndex < tags.Length)
        {
            IccTag tag = tags[tagIndex];
            if (tag == null)
            {
                throw new IccProfileValidationException(
                    $"Profile tag {tagIndex} cannot be null.");
            }

            int previousIndex = 0;
            while (previousIndex < tagIndex)
            {
                if (tags[previousIndex].Signature == tag.Signature)
                {
                    throw new IccProfileValidationException(
                        $"Profile tag {tagIndex} duplicates signature 0x{tag.Signature:X8}.");
                }
                previousIndex = previousIndex + 1;
            }
            tagIndex = tagIndex + 1;
        }

        ProfileCloneContext context = new ProfileCloneContext();
        IccProfile profile = new IccProfile();
        profile.Header = ProfileClone.CloneHeader(header, context);
        profile.Tags = ProfileClone.CloneTagArray(tags, context);
        profile.TagTableEntries = BuildTableEntries(profile.Tags);
        return PreflightProfile(profile);
    }

    internal IccProfile CloneProfile(IccProfile profile)
    {
        return ProfileClone.CloneProfile(profile);
    }

    internal IccProfile SetProfileHeader(IccProfile profile,
        IccProfileHeader header)
    {
        if (profile == null)
        {
            throw new IccProfileValidationException("The profile cannot be null.");
        }
        if (header == null)
        {
            throw new IccProfileValidationException("The replacement profile header cannot be null.");
        }

        IccProfile clone = ProfileClone.CloneProfile(profile);
        clone.Header = ProfileClone.CloneHeaderStandalone(header);
        return ValidateMutation(clone, false);
    }

    internal IccProfile SetProfileTag(IccProfile profile, IccTag tag)
    {
        if (profile == null)
        {
            throw new IccProfileValidationException("The profile cannot be null.");
        }

        if (tag == null)
        {
            throw new IccProfileValidationException("The profile tag cannot be null.");
        }

        IccProfile clone = ProfileClone.CloneProfile(profile);
        IccTag tagClone = ProfileClone.CloneTagStandalone(tag);
        tagClone.TableEntry.Signature = tagClone.Signature;
        int matchIndex = FindTagIndex(clone.Tags, tag.Signature);
        if (matchIndex >= 0)
        {
            tagClone.TableEntry.OriginalIndex =
                clone.Tags[matchIndex].TableEntry.OriginalIndex;
            tagClone.TableEntry.SharedDataId =
                clone.Tags[matchIndex].TableEntry.SharedDataId;
            clone.Tags[matchIndex] = tagClone;
        }
        else
        {
            IccTag[] expanded = new IccTag[clone.Tags.Length + 1];
            int index = 0;
            while (index < clone.Tags.Length)
            {
                expanded[index] = clone.Tags[index];
                index = index + 1;
            }

            expanded[clone.Tags.Length] = tagClone;
            clone.Tags = expanded;
        }

        clone.TagTableEntries = BuildTableEntries(clone.Tags);
        return ValidateMutation(clone, true);
    }

    internal IccProfile RemoveProfileTag(IccProfile profile, uint tagSignature)
    {
        if (profile == null)
        {
            throw new IccProfileValidationException("The profile cannot be null.");
        }

        IccProfile clone = ProfileClone.CloneProfile(profile);
        int retainedCount = 0;
        int index = 0;
        while (index < clone.Tags.Length)
        {
            if (clone.Tags[index].Signature != tagSignature)
            {
                retainedCount = retainedCount + 1;
            }

            index = index + 1;
        }

        IccTag[] retained = new IccTag[retainedCount];
        int destinationIndex = 0;
        index = 0;
        while (index < clone.Tags.Length)
        {
            if (clone.Tags[index].Signature != tagSignature)
            {
                retained[destinationIndex] = clone.Tags[index];
                destinationIndex = destinationIndex + 1;
            }

            index = index + 1;
        }

        clone.Tags = retained;
        clone.TagTableEntries = BuildTableEntries(clone.Tags);
        return ValidateMutation(clone, true);
    }

    internal IccProfile ValidateMutation(IccProfile profile,
        bool preserveCreationDateTime)
    {
        IccWriteOptions writeOptions = new IccWriteOptions();
        writeOptions.Mode = IccWriteMode.Preservation;
        writeOptions.PreserveOriginalCreationDateTime = preserveCreationDateTime
            && profile.Header.RawBytes != null
            && profile.Header.RawBytes.Length == IccConstants.HeaderSize;
        writeOptions.PreserveUnknownData = true;
        writeOptions.RejectInvalidProfile = false;
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile,
            writeOptions);
        byte[] bytes = ProfileWriterCore.EncodePlan(plan);
        IccReadOptions readOptions = new IccReadOptions();
        readOptions.ResourceLimits = writeOptions.ResourceLimits;
        readOptions.DecodeTagsLazily = false;
        IccProfile result = ProfileCodec.ReadEnvelope(bytes, 0, 0,
            readOptions);
        IccValidationReport report = new ValidationReportCore().Build(result,
            new IccValidationPolicy());
        if (!report.IsValid)
        {
            throw new IccProfileValidationException(
                CreateMutationValidationMessage(report));
        }
        return result;
    }

    internal string CreateMutationValidationMessage(IccValidationReport report)
    {
        int index = 0;
        while (index < report.Issues.Length)
        {
            IccValidationIssue issue = report.Issues[index];
            if (issue.Severity == IccValidationSeverity.Error
                || issue.Severity == IccValidationSeverity.Fatal)
            {
                return $"The profile mutation is invalid: {issue.RuleId}: {issue.Message}";
            }
            index = index + 1;
        }
        return "The profile mutation is invalid under the conforming validation policy.";
    }

    internal string CreateConstructionValidationMessage(IccValidationReport report)
    {
        int index = 0;
        while (index < report.Issues.Length)
        {
            IccValidationIssue issue = report.Issues[index];
            if (issue.Severity == IccValidationSeverity.Error
                || issue.Severity == IccValidationSeverity.Fatal)
            {
                return $"The constructed profile is invalid: {issue.RuleId}: {issue.Message}";
            }
            index = index + 1;
        }
        return "The constructed profile is invalid under the conforming validation policy.";
    }

    internal int FindTagIndex(IccTag[] tags, uint signature)
    {
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index].Signature == signature)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void ValidateBuildOptions(IccProfileBuildOptions options)
    {
        if (options == null || options.Version == null || options.Descriptions == null
            || options.Copyright == null || options.MediaWhitePoint == null
            || options.CreationDateTime == null)
        {
            throw new IccProfileValidationException("Profile build options and all required metadata fields cannot be null.");
        }

        ValidateVersion(options.Version);
        if (GetProfileClassSignature(options.ProfileClass) == 0)
        {
            throw new IccProfileValidationException("A profile skeleton requires an ICC.1 input, display, output, DeviceLink, ColorSpace, abstract, or NamedColor class.");
        }
        if (!IsValidRenderingIntent(options.RenderingIntent))
        {
            throw new IccProfileValidationException("The profile rendering intent is invalid.");
        }

        int dataChannels = ColorSpaceChannelCount.Decode(options.DataColorSpaceSignature);
        if (dataChannels < 1 || dataChannels > IccConstants.MaximumIcc1ChannelCount
            || HeaderCodec.DecodeColorSpace(options.DataColorSpaceSignature) == IccColorSpaceKind.Unknown)
        {
            throw new IccProfileValidationException("The profile data colour space must be a supported one-through-fifteen-channel ICC.1 space.");
        }

        if (options.ProfileClass == IccProfileClass.DeviceLink)
        {
            int outputChannels = ColorSpaceChannelCount.Decode(options.ProfileConnectionSpaceSignature);
            if (outputChannels < 1 || outputChannels > IccConstants.MaximumIcc1ChannelCount
                || HeaderCodec.DecodeColorSpace(options.ProfileConnectionSpaceSignature) == IccColorSpaceKind.Unknown)
            {
                throw new IccProfileValidationException("A DeviceLink skeleton requires a supported one-through-fifteen-channel output space.");
            }
            if (options.ChromaticAdaptationMatrix != null)
            {
                throw new IccProfileValidationException("A DeviceLink skeleton cannot contain media-white chromatic adaptation metadata.");
            }
        }
        else if (options.ProfileConnectionSpaceSignature != IccColorSpaceSignatures.Xyz
            && options.ProfileConnectionSpaceSignature != IccColorSpaceSignatures.Lab)
        {
            throw new IccProfileValidationException("A non-DeviceLink ICC.1 skeleton requires PCSXYZ or PCSLAB.");
        }

        ValidateDateTime(options.CreationDateTime);
        ValidateMediaWhite(options.MediaWhitePoint);
        ValidateDescriptions(options.Descriptions, options.Version.Kind);
        if (options.Copyright.Text == null)
        {
            throw new IccProfileValidationException("Profile copyright text cannot be null.");
        }
        if (options.Version.Kind == IccVersionKind.Version2
            && !LegacyTagCodec.IsAsciiString(options.Copyright.Text))
        {
            throw new IccProfileValidationException("Version 2 copyright text must contain only non-NULL 7-bit ASCII characters.");
        }

        if (options.ProfileClass == IccProfileClass.Display
            && options.Version.Kind == IccVersionKind.Version4
            && !IsD50(options.MediaWhitePoint))
        {
            throw new IccProfileValidationException("A version 4 Display profile media white point must be D50; use chromatic adaptation to describe another adopted white.");
        }
        if (options.ChromaticAdaptationMatrix != null)
        {
            ValidateChromaticAdaptationMatrix(options.ChromaticAdaptationMatrix);
        }
    }

    internal void ValidateVersion(IccVersion version)
    {
        int expectedMajor = version.Kind == IccVersionKind.Version2
            ? IccConstants.Version2Major : IccConstants.Version4Major;
        if ((version.Kind != IccVersionKind.Version2 && version.Kind != IccVersionKind.Version4)
            || (version.Major != 0 && version.Major != expectedMajor)
            || version.Minor < 0 || version.Minor > 9 || version.BugFix < 0 || version.BugFix > 9
            || (version.Kind == IccVersionKind.Version4 && version.Minor > 4))
        {
            throw new IccProfileValidationException("A profile skeleton requires a consistent supported ICC version 2 or version 4 definition.");
        }
    }

    internal void ValidateDateTime(IccDateTime value)
    {
        int maximumDay = 31;
        if (value.Month == 4 || value.Month == 6 || value.Month == 9 || value.Month == 11)
        {
            maximumDay = 30;
        }
        else if (value.Month == 2)
        {
            bool leap = (value.Year % 4 == 0 && value.Year % 100 != 0) || value.Year % 400 == 0;
            maximumDay = leap ? 29 : 28;
        }

        if (value.Year < 1 || value.Year > 65535 || value.Month < 1 || value.Month > 12
            || value.Day < 1 || value.Day > maximumDay || value.Hour < 0 || value.Hour > 23
            || value.Minute < 0 || value.Minute > 59 || value.Second < 0 || value.Second > 59)
        {
            throw new IccProfileValidationException("The profile creation time must be a valid Gregorian ICC dateTimeNumber.");
        }
    }

    internal void ValidateMediaWhite(IccXyzColor value)
    {
        if (!IsRepresentable(value.X) || !IsRepresentable(value.Y) || !IsRepresentable(value.Z)
            || value.X < 0.0 || value.Y <= 0.0 || value.Z < 0.0)
        {
            throw new IccProfileValidationException("The media white point must contain finite non-negative, representable XYZ values with positive Y.");
        }
    }

    internal void ValidateDescriptions(IccLocalizedString[] values, IccVersionKind version)
    {
        int index = 0;
        while (index < values.Length)
        {
            IccLocalizedString value = values[index];
            if (value == null || value.Language == null || value.Country == null
                || value.Text == null || value.RawBytes == null)
            {
                throw new IccProfileValidationException($"Localized profile description {index} and its fields cannot be null.");
            }
            if ((version == IccVersionKind.Version4 || version == IccVersionKind.Version5)
                && (!IsLowercaseLocale(value.Language) || !IsUppercaseLocale(value.Country)))
            {
                throw new IccProfileValidationException($"Localized profile description {index} requires a two-letter lowercase language and uppercase country code.");
            }
            index = index + 1;
        }
    }

    internal void ValidateChromaticAdaptationMatrix(IccMatrix matrix)
    {
        if (matrix.Rows != 3 || matrix.Columns != 3 || matrix.Values == null
            || matrix.Values.Length != 9)
        {
            throw new IccProfileValidationException("The chromatic-adaptation matrix must contain exactly three rows and three columns.");
        }

        int index = 0;
        while (index < matrix.Values.Length)
        {
            if (!IsRepresentable(matrix.Values[index]))
            {
                throw new IccProfileValidationException("Every chromatic-adaptation matrix value must be finite and representable as s15Fixed16.");
            }
            index = index + 1;
        }

        double determinant = MatrixMath.CalculateDeterminant3x3(matrix.Values);
        if (!System.Double.IsFinite(determinant) || System.Math.Abs(determinant) <= 1.0e-12)
        {
            throw new IccProfileValidationException("The chromatic-adaptation matrix must be invertible.");
        }
    }

    internal IccProfileHeader CreateSkeletonHeader(IccProfileBuildOptions options)
    {
        IccProfileHeader header = new IccProfileHeader();
        header.Version.Kind = options.Version.Kind;
        header.Version.Major = options.Version.Kind == IccVersionKind.Version2
            ? IccConstants.Version2Major : options.Version.Kind == IccVersionKind.Version4
                ? IccConstants.Version4Major : IccConstants.Version5Major;
        header.Version.Minor = options.Version.Minor;
        header.Version.BugFix = options.Version.BugFix;
        header.Version.IsSupported = true;
        header.Version.PackedValue = ((uint)header.Version.Major << 24)
            | ((uint)header.Version.Minor << 20) | ((uint)header.Version.BugFix << 16);
        header.ProfileClass = options.ProfileClass;
        header.ProfileClassSignature = GetProfileClassSignature(options.ProfileClass);
        header.DataColorSpaceSignature = options.DataColorSpaceSignature;
        header.DataColorSpace = HeaderCodec.DecodeColorSpace(options.DataColorSpaceSignature);
        header.ProfileConnectionSpaceSignature = options.ProfileConnectionSpaceSignature;
        header.ProfileConnectionSpace = options.ProfileClass == IccProfileClass.DeviceLink
            ? IccPcsKind.None : HeaderCodec.DecodePcs(options.ProfileConnectionSpaceSignature);
        header.CreationDateTime = CloneDateTime(options.CreationDateTime);
        header.ProfileFileSignature = IccConstants.ProfileFileSignature;
        header.RenderingIntent = (int)options.RenderingIntent;
        header.RenderingIntentKind = options.RenderingIntent;
        header.PcsIlluminantX = IccStandardColorimetry.D50EncodedX;
        header.PcsIlluminantY = IccStandardColorimetry.D50EncodedY;
        header.PcsIlluminantZ = IccStandardColorimetry.D50EncodedZ;
        return header;
    }

    internal IccTag[] CreateSkeletonTags(IccProfileBuildOptions options)
    {
        bool includeWhite = options.ProfileClass != IccProfileClass.DeviceLink;
        int tagCount = includeWhite ? 3 : 2;
        if (options.ChromaticAdaptationMatrix != null)
        {
            tagCount = tagCount + 1;
        }

        IccTag[] tags = new IccTag[tagCount];
        tags[0] = CreateProfileDescriptionTag(options);
        tags[1] = CreateCopyrightTag(options);
        int index = 2;
        if (includeWhite)
        {
            tags[index] = CreateMediaWhiteTag(options.MediaWhitePoint);
            index = index + 1;
        }
        if (options.ChromaticAdaptationMatrix != null)
        {
            tags[index] = CreateChromaticAdaptationTag(options.ChromaticAdaptationMatrix);
        }
        return tags;
    }

    internal IccTag CreateProfileDescriptionTag(IccProfileBuildOptions options)
    {
        if (options.Version.Kind == IccVersionKind.Version2)
        {
            string description = options.Descriptions.Length == 0
                ? "ICC profile" : options.Descriptions[0].Text;
            return CreateTextTag(IccTagSignatures.ProfileDescription,
                description, IccTypeSignatures.TextDescription);
        }

        IccLocalizedString[] descriptions = options.Descriptions.Length == 0
            ? new IccLocalizedString[] { CreateLocalizedString("en", "US", "ICC profile") }
            : CloneLocalizedStrings(options.Descriptions);
        return CreateLocalizedTag(IccTagSignatures.ProfileDescription, descriptions);
    }

    internal IccTag CreateCopyrightTag(IccProfileBuildOptions options)
    {
        if (options.Version.Kind == IccVersionKind.Version2)
        {
            return CreateTextTag(IccTagSignatures.Copyright,
                options.Copyright.Text, IccTypeSignatures.Text);
        }

        IccLocalizedString localized = CreateLocalizedString("en", "US", options.Copyright.Text);
        return CreateLocalizedTag(IccTagSignatures.Copyright,
            new IccLocalizedString[] { localized });
    }

    internal IccTag CreateMediaWhiteTag(IccXyzColor white)
    {
        IccXyzNumber value = new IccXyzNumber();
        value.X = white.X;
        value.Y = white.Y;
        value.Z = white.Z;
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.XyzArray;
        data.XyzValues = new IccXyzNumber[] { value };
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.Xyz;
        return CreateTag(IccTagSignatures.MediaWhitePoint, data);
    }

    internal IccTag CreateChromaticAdaptationTag(IccMatrix matrix)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.FixedPointArray;
        data.FixedPointValues = CloneDoubles(matrix.Values);
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.S15Fixed16Array;
        return CreateTag(IccTagSignatures.ChromaticAdaptation, data);
    }

    internal IccTag CreateTextTag(uint signature, string value, uint typeSignature)
    {
        IccTextData text = new IccTextData();
        text.TypeSignature = typeSignature;
        text.Text = value;
        text.AsciiText = LegacyTagCodec.IsAsciiString(value) ? value : string.Empty;
        text.UnicodeText = value;
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.Text;
        data.Text = text;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = typeSignature;
        return CreateTag(signature, data);
    }

    internal IccTag CreateLocalizedTag(uint signature, IccLocalizedString[] values)
    {
        IccTagData data = new IccTagData();
        data.Kind = IccTagDataKind.LocalizedText;
        data.LocalizedStrings = values;
        data.RawData.HasTypeSignature = true;
        data.RawData.TypeSignature = IccTypeSignatures.MultiLocalizedUnicode;
        return CreateTag(signature, data);
    }

    internal IccTag CreateTag(uint signature, IccTagData data)
    {
        IccTag tag = new IccTag();
        tag.Signature = signature;
        tag.TableEntry.Signature = signature;
        tag.Data = data;
        return tag;
    }

    internal IccLocalizedString[] CloneLocalizedStrings(IccLocalizedString[] values)
    {
        IccLocalizedString[] clone = new IccLocalizedString[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            clone[index] = CreateLocalizedString(values[index].Language,
                values[index].Country, values[index].Text);
            index = index + 1;
        }
        return clone;
    }

    internal IccLocalizedString CreateLocalizedString(string language, string country, string text)
    {
        IccLocalizedString value = new IccLocalizedString();
        value.Language = language;
        value.Country = country;
        value.Text = text;
        return value;
    }

    internal IccDateTime CloneDateTime(IccDateTime value)
    {
        IccDateTime clone = new IccDateTime();
        clone.Year = value.Year;
        clone.Month = value.Month;
        clone.Day = value.Day;
        clone.Hour = value.Hour;
        clone.Minute = value.Minute;
        clone.Second = value.Second;
        return clone;
    }

    internal double[] CloneDoubles(double[] values)
    {
        double[] clone = new double[values.Length];
        System.Array.Copy(values, clone, values.Length);
        return clone;
    }

    internal uint GetProfileClassSignature(IccProfileClass profileClass)
    {
        switch (profileClass)
        {
            case IccProfileClass.Input:
                return IccProfileClassSignatures.Input;
            case IccProfileClass.Display:
                return IccProfileClassSignatures.Display;
            case IccProfileClass.Output:
                return IccProfileClassSignatures.Output;
            case IccProfileClass.DeviceLink:
                return IccProfileClassSignatures.DeviceLink;
            case IccProfileClass.ColorSpace:
                return IccProfileClassSignatures.ColorSpace;
            case IccProfileClass.Abstract:
                return IccProfileClassSignatures.Abstract;
            case IccProfileClass.NamedColor:
                return IccProfileClassSignatures.NamedColor;
            default:
                return 0;
        }
    }

    internal bool IsValidRenderingIntent(IccRenderingIntent intent)
    {
        return intent == IccRenderingIntent.Perceptual
            || intent == IccRenderingIntent.MediaRelativeColorimetric
            || intent == IccRenderingIntent.Saturation
            || intent == IccRenderingIntent.IccAbsoluteColorimetric;
    }

    internal bool IsLowercaseLocale(string value)
    {
        return value.Length == 2 && value[0] >= 'a' && value[0] <= 'z'
            && value[1] >= 'a' && value[1] <= 'z';
    }

    internal bool IsUppercaseLocale(string value)
    {
        return value.Length == 2 && value[0] >= 'A' && value[0] <= 'Z'
            && value[1] >= 'A' && value[1] <= 'Z';
    }

    internal bool IsRepresentable(double value)
    {
        return System.Double.IsFinite(value) && value >= IccNumberCodec.S15Fixed16Minimum
            && value <= IccNumberCodec.S15Fixed16Maximum;
    }

    internal bool IsD50(IccXyzColor value)
    {
        return System.Math.Abs(value.X - IccStandardColorimetry.D50X) <= 0.0001
            && System.Math.Abs(value.Y - IccStandardColorimetry.D50Y) <= 0.0001
            && System.Math.Abs(value.Z - IccStandardColorimetry.D50Z) <= 0.0001;
    }

    internal IccTagTableEntry[] BuildTableEntries(IccTag[] tags)
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
}
