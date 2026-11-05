namespace icc;

internal static class Icc1SemanticValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile == null || profile.Header == null || profile.Header.Version == null
            || profile.Tags == null)
        {
            return issues.ToArray();
        }
        IccVersionKind version = profile.Header.Version.Kind;
        if (version != IccVersionKind.Version2 && version != IccVersionKind.Version4)
        {
            return issues.ToArray();
        }

        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Data != null)
            {
                ValidateTransformChannels(profile, tag, index, issues);
                ValidateAssociatedChannels(profile, tag, index, issues);
                ValidateCicp(profile, tag, index, issues);
                ValidateMetadata(tag, index, issues);
            }
            index = index + 1;
        }

        ValidateMediaWhiteAndAdaptation(profile, issues);
        ValidateImageStateAndViewing(profile, issues);
        ValidateProfileSequences(profile, issues);
        return issues.ToArray();
    }

    internal static void ValidateTransformChannels(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        Icc1TagDefinition? definition = Icc1TagRegistry.FindDefinition(tag.Signature);
        if (definition == null || (definition.InputChannels == Icc1ChannelSource.None
            && definition.OutputChannels == Icc1ChannelSource.None))
        {
            return;
        }

        int input = 0;
        int output = 0;
        bool hasPipeline = false;
        if (tag.Data.Lut != null)
        {
            input = tag.Data.Lut.InputChannels;
            output = tag.Data.Lut.OutputChannels;
            hasPipeline = true;
        }
        else if (tag.Data.MultiStageLut != null)
        {
            input = tag.Data.MultiStageLut.InputChannels;
            output = tag.Data.MultiStageLut.OutputChannels;
            hasPipeline = true;
        }
        else if (tag.Data.MultiProcessElements != null)
        {
            input = tag.Data.MultiProcessElements.InputChannels;
            output = tag.Data.MultiProcessElements.OutputChannels;
            hasPipeline = true;
        }
        if (!hasPipeline)
        {
            return;
        }

        int expectedInput = ResolveChannelCount(definition.InputChannels, profile.Header);
        int expectedOutput = ResolveChannelCount(definition.OutputChannels, profile.Header);
        if ((expectedInput != 0 && input != expectedInput)
            || (expectedOutput != 0 && output != expectedOutput))
        {
            string intent = definition.RenderingIntent < 0
                ? definition.Role.ToString()
                : $"{definition.Role} rendering-intent index {definition.RenderingIntent}";
            issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                $"Tag '{FourCcCodec.Format(tag.Signature)}' declares a {input}-to-{output} channel transform, but its {intent} relationship to the profile header requires {expectedInput}-to-{expectedOutput} channels.",
                tag, index, "Data", "Make the transform endpoints agree with its direction, intent, and the header data and connection spaces."));
        }
    }

    internal static void ValidateAssociatedChannels(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (tag.Signature == IccTagSignatures.ColorantOrder)
        {
            ValidateColorantOrder(profile, tag, index, issues);
        }
        else if (tag.Signature == IccTagSignatures.ColorantTable
            || tag.Signature == IccTagSignatures.ColorantTableOut)
        {
            ValidateColorantTable(profile, tag, index, issues);
        }
        else if (tag.Signature == IccTagSignatures.NamedColor
            || tag.Signature == IccTagSignatures.NamedColor2)
        {
            ValidateNamedColors(profile, tag, index, issues);
        }
        else if (tag.Signature == IccTagSignatures.Chromaticity)
        {
            int expected = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
            IccChromaticity? value = tag.Data.Chromaticity;
            if (value != null && (value.XCoordinates == null || value.YCoordinates == null
                || value.XCoordinates.Length != expected || value.YCoordinates.Length != expected))
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                    $"The chromaticity tag contains a different number of coordinates than the {expected}-channel profile data colour space.",
                    tag, index, "Data.Chromaticity", "Provide one xy chromaticity for every profile data-space channel."));
            }
        }
        else if (tag.Signature == IccTagSignatures.OutputResponse)
        {
            int expected = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
            if (tag.Data.ResponseCurveChannelCount != 0
                && tag.Data.ResponseCurveChannelCount != expected)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                    $"The response-curve set declares {tag.Data.ResponseCurveChannelCount} channels, but the profile data colour space has {expected}.",
                    tag, index, "Data.ResponseCurveChannelCount", "Use one response-curve channel for every profile data-space channel."));
            }
        }
    }

    internal static void ValidateColorantOrder(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int expected = profile.Header.ProfileClass == IccProfileClass.DeviceLink
            ? ColorSpaceChannelCount.Decode(profile.Header.ProfileConnectionSpaceSignature)
            : ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        long[]? values = tag.Data.IntegerValues;
        bool invalid = values == null || values.Length != expected;
        int valueIndex = 0;
        while (!invalid && values != null && valueIndex < values.Length)
        {
            long value = values[valueIndex];
            if (value < 0 || value >= expected)
            {
                invalid = true;
            }
            int prior = 0;
            while (!invalid && prior < valueIndex)
            {
                if (values[prior] == value)
                {
                    invalid = true;
                }
                prior = prior + 1;
            }
            valueIndex = valueIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                $"The colorant-order tag is not a permutation of the {expected} associated colour-space channels.",
                tag, index, "Data.IntegerValues", "List every associated channel index exactly once."));
        }
    }

    internal static void ValidateColorantTable(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccColorantSet? set = tag.Data.Colorants;
        if (set == null)
        {
            return;
        }
        uint associated = tag.Signature == IccTagSignatures.ColorantTableOut
            ? profile.Header.ProfileConnectionSpaceSignature
            : profile.Header.DataColorSpaceSignature;
        int expected = ColorSpaceChannelCount.Decode(associated);
        uint pcsSignature = profile.Header.ProfileClass == IccProfileClass.DeviceLink
            ? IccColorSpaceSignatures.Lab : profile.Header.ProfileConnectionSpaceSignature;
        bool invalid = set.ColorSpaceSignature != associated || set.Colorants == null
            || set.Colorants.Length != expected;
        int colorantIndex = 0;
        while (!invalid && set.Colorants != null && colorantIndex < set.Colorants.Length)
        {
            IccColorant colorant = set.Colorants[colorantIndex];
            invalid = colorant == null || !IsMatchingVector(colorant.PcsColor, pcsSignature, 3);
            colorantIndex = colorantIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                $"The colorant table does not match associated colour space '{FourCcCodec.Format(associated)}', its {expected} channels, or the required PCS encoding.",
                tag, index, "Data.Colorants", "Use one colorant per associated channel and encode its three coordinates in the profile PCS."));
        }
    }

    internal static void ValidateNamedColors(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccNamedColorSet? set = tag.Data.NamedColors;
        if (set == null)
        {
            return;
        }
        int expectedDevice = ColorSpaceChannelCount.Decode(profile.Header.DataColorSpaceSignature);
        if (tag.Signature == IccTagSignatures.NamedColor)
        {
            if (set.DeviceChannelCount != expectedDevice)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                    "The legacy named-colour tint-coordinate count does not match the profile data colour space.",
                    tag, index, "Data.NamedColors.DeviceChannelCount",
                    "Use one legacy tint coordinate per profile data-space channel."));
            }
            return;
        }
        uint pcsSignature = profile.Header.ProfileConnectionSpaceSignature;
        bool invalid = set.DeviceChannelCount != 0 && set.DeviceChannelCount != expectedDevice;
        int colorIndex = 0;
        while (!invalid && set.Colors != null && colorIndex < set.Colors.Length)
        {
            IccNamedColor color = set.Colors[colorIndex];
            invalid = color == null || !IsMatchingVector(color.PcsColor, pcsSignature, 3);
            if (!invalid && color != null && set.DeviceChannelCount != 0)
            {
                invalid = !IsMatchingVector(color.DeviceColor,
                    profile.Header.DataColorSpaceSignature, set.DeviceChannelCount);
            }
            colorIndex = colorIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ChannelCount,
                "The named-colour PCS or device coordinates do not match the profile header colour spaces and channel counts.",
                tag, index, "Data.NamedColors", "Encode three coordinates in the profile PCS and either zero or one device coordinate per data-space channel."));
        }
    }

    internal static void ValidateMediaWhiteAndAdaptation(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTag? whiteTag = FindTag(profile, IccTagSignatures.MediaWhitePoint);
        IccTag? adaptationTag = FindTag(profile, IccTagSignatures.ChromaticAdaptation);
        if (adaptationTag != null && whiteTag == null)
        {
            issues.Add(CreateProfileIssue(IccValidationRuleIds.RequiredTag,
                "The chromatic-adaptation tag has no media-white-point tag to which its adopted-white relationship can apply.",
                "Tags['wtpt']", "Add the media-white-point tag or remove the unrelated chromatic-adaptation tag."));
        }
        if (profile.Header.Version.Kind == IccVersionKind.Version4
            && profile.Header.ProfileClass == IccProfileClass.Display && whiteTag != null
            && whiteTag.Data != null && whiteTag.Data.XyzValues != null
            && whiteTag.Data.XyzValues.Length == 1)
        {
            IccXyzNumber white = whiteTag.Data.XyzValues[0];
            if (white != null && (!IsClose(white.X, IccStandardColorimetry.D50X)
                || !IsClose(white.Y, IccStandardColorimetry.D50Y)
                || !IsClose(white.Z, IccStandardColorimetry.D50Z)))
            {
                int whiteIndex = FindTagIndex(profile, IccTagSignatures.MediaWhitePoint);
                issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                    "A version 4 Display profile media white point shall be the PCS D50 illuminant; the actual adopted white is represented through chromatic adaptation.",
                    whiteTag, whiteIndex, "Data.XyzValues[0]", "Encode D50 in wtpt and use chad for a non-D50 adopted white."));
            }
        }
    }

    internal static void ValidateImageStateAndViewing(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTag? imageTag = FindTag(profile, IccTagSignatures.ColorimetricIntentImageState);
        IccTag? viewingTag = FindTag(profile, IccTagSignatures.ViewingConditions);
        IccTag? viewingDescriptionTag = FindTag(profile, IccTagSignatures.ViewingConditionDescription);
        IccTag? adaptationTag = FindTag(profile, IccTagSignatures.ChromaticAdaptation);
        if (viewingDescriptionTag != null && viewingTag == null)
        {
            issues.Add(CreateProfileIssue(IccValidationRuleIds.RequiredTag,
                "A viewing-condition description is present without the viewing conditions it describes.",
                "Tags['view']", "Add the viewing-conditions tag or remove its orphaned description."));
        }
        if (imageTag != null && imageTag.Data != null)
        {
            uint state = imageTag.Data.Signature;
            bool known = IsImageState(state);
            int imageIndex = FindTagIndex(profile, IccTagSignatures.ColorimetricIntentImageState);
            if (!known)
            {
                issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                    $"Image-state signature '{FourCcCodec.Format(state)}' is not one of the registered ICC colorimetric-intent image states.",
                    imageTag, imageIndex, "Data.Signature", "Use scoe, sape, fpce, rhoc, or rpoc."));
            }
            else if (viewingTag == null)
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.RequiredTag,
                    "The selected colorimetric-intent image state requires explicit viewing conditions.",
                    "Tags['view']", "Add the viewing-conditions tag used to interpret the selected image state."));
            }
            if ((state == IccImageStateSignatures.ReflectionHardcopyOriginalColorimetry
                || state == IccImageStateSignatures.ReflectionPrintOutputColorimetry)
                && adaptationTag == null)
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.RequiredTag,
                    "A reflection-hardcopy image state requires a chromatic-adaptation matrix.",
                    "Tags['chad']", "Add the chromatic-adaptation tag for the hardcopy adopted white."));
            }
        }

        IccTag? luminanceTag = FindTag(profile, IccTagSignatures.Luminance);
        if (viewingTag != null && viewingTag.Data != null
            && viewingTag.Data.ViewingConditions != null && luminanceTag != null
            && luminanceTag.Data != null && luminanceTag.Data.XyzValues != null
            && luminanceTag.Data.XyzValues.Length == 1)
        {
            IccXyzNumber luminance = luminanceTag.Data.XyzValues[0];
            IccViewingConditions viewing = viewingTag.Data.ViewingConditions;
            if (luminance != null && (!IsClose(luminance.X, 0.0)
                || !IsClose(luminance.Z, 0.0)
                || !IsClose(luminance.Y, viewing.Illuminant.Y)))
            {
                int luminanceIndex = FindTagIndex(profile, IccTagSignatures.Luminance);
                issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                    "The luminance tag shall store zero X and Z and the same absolute Y luminance as the viewing illuminant.",
                    luminanceTag, luminanceIndex, "Data.XyzValues[0]", "Set luminance X and Z to zero and Y to the viewing-conditions illuminant Y value."));
            }
        }
    }

    internal static void ValidateCicp(IccProfile profile, IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (tag.Signature != IccTagSignatures.Cicp || tag.Data.Cicp == null)
        {
            return;
        }
        IccCicp cicp = tag.Data.Cicp;
        IccColorSpaceKind dataSpace = HeaderCodec.DecodeColorSpace(
            profile.Header.DataColorSpaceSignature);
        bool contextValid = CicpTagCodec.IsPermittedContext(profile.Header.ProfileClass,
            dataSpace, cicp.MatrixCoefficients);
        bool hasPrimaries = HasTag(profile, IccTagSignatures.RedMatrixColumn)
            && HasTag(profile, IccTagSignatures.GreenMatrixColumn)
            && HasTag(profile, IccTagSignatures.BlueMatrixColumn);
        bool expectedProfilePrimaries = cicp.ColorPrimaries == 2
            && profile.Header.Version.Kind == IccVersionKind.Version4
            && (profile.Header.ProfileClass == IccProfileClass.Input
                || profile.Header.ProfileClass == IccProfileClass.Display)
            && dataSpace == IccColorSpaceKind.Rgb && hasPrimaries;
        if (!contextValid || cicp.UsesProfilePrimaries != expectedProfilePrimaries)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "CICP matrix coefficients or profile-primary resolution are inconsistent with the profile class, data colour space, version, or RGB matrix tags.",
                tag, index, "Data.Cicp", "Use CICP only in Input or Display RGB/XYZ/YCbCr profiles and derive unspecified RGB primaries only from a complete version 4 matrix."));
        }
    }

    internal static void ValidateMetadata(IccTag tag, int index,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (tag.Signature != IccTagSignatures.Metadata || tag.Data.Metadata == null)
        {
            return;
        }
        bool invalid = false;
        int entryIndex = 0;
        while (!invalid && entryIndex < tag.Data.Metadata.Length)
        {
            IccMetadataEntry entry = tag.Data.Metadata[entryIndex];
            if (entry != null)
            {
                bool registered = DictionaryTagCodec.IsRegisteredMetadataName(entry.Name);
                invalid = !registered || entry.IsRegisteredName != registered;
            }
            entryIndex = entryIndex + 1;
        }
        if (invalid)
        {
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The metadata tag contains a name outside the ICC metadata registry or an inconsistent registration indicator.",
                tag, index, "Data.Metadata", "Use registered ICC metadata names and keep each decoded registration indicator consistent with its name."));
        }
    }

    internal static void ValidateProfileSequences(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccTag? descriptionTag = FindTag(profile, IccTagSignatures.ProfileSequenceDescription);
        IccTag? identifierTag = FindTag(profile, IccTagSignatures.ProfileSequenceIdentifier);
        int descriptionCount = descriptionTag == null || descriptionTag.Data == null
            || descriptionTag.Data.ProfileSequence == null
            ? -1 : descriptionTag.Data.ProfileSequence.Length;
        int identifierCount = identifierTag == null || identifierTag.Data == null
            || identifierTag.Data.ProfileSequenceIdentifiers == null
            ? -1 : identifierTag.Data.ProfileSequenceIdentifiers.Length;
        if (descriptionCount == 0 && descriptionTag != null)
        {
            int descriptionIndex = FindTagIndex(profile, IccTagSignatures.ProfileSequenceDescription);
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The profile sequence description is empty and therefore cannot identify the profiles used by the DeviceLink.",
                descriptionTag, descriptionIndex, "Data.ProfileSequence", "Include the ordered profiles used to construct the DeviceLink."));
        }
        if (identifierCount == 0 && identifierTag != null)
        {
            int identifierIndex = FindTagIndex(profile, IccTagSignatures.ProfileSequenceIdentifier);
            issues.Add(CreateTagIssue(IccValidationRuleIds.ProfileClassSemantics,
                "The profile sequence identifier array is empty.", identifierTag, identifierIndex,
                "Data.ProfileSequenceIdentifiers", "Include one Profile ID and description for every profile in the sequence."));
        }
        if (descriptionCount >= 0 && identifierCount >= 0
            && descriptionCount != identifierCount)
        {
            issues.Add(CreateProfileIssue(IccValidationRuleIds.ProfileClassSemantics,
                $"The profile sequence contains {descriptionCount} descriptions but {identifierCount} identifiers.",
                "Tags['pseq','psid']", "Keep profile sequence descriptions and identifiers in one-to-one ordered correspondence."));
        }
        if (identifierTag != null && identifierTag.Data != null
            && identifierTag.Data.ProfileSequenceIdentifiers != null)
        {
            int entryIndex = 0;
            while (entryIndex < identifierTag.Data.ProfileSequenceIdentifiers.Length)
            {
                IccProfileSequenceIdentifier identifier =
                    identifierTag.Data.ProfileSequenceIdentifiers[entryIndex];
                if (identifier != null && identifier.ProfileId != null
                    && identifier.ProfileId.Bytes != null
                    && identifier.ProfileId.Bytes.Length == IccConstants.ProfileIdSize)
                {
                    bool nonZero = ContainsNonZero(identifier.ProfileId.Bytes);
                    if (identifier.ProfileId.IsPresent != nonZero)
                    {
                        int identifierIndex = FindTagIndex(profile,
                            IccTagSignatures.ProfileSequenceIdentifier);
                        issues.Add(CreateTagIssue(IccValidationRuleIds.HeaderProfileId,
                            $"Profile sequence identifier {entryIndex} has inconsistent Profile ID bytes and presence state.",
                            identifierTag, identifierIndex,
                            $"Data.ProfileSequenceIdentifiers[{entryIndex}].ProfileId",
                            "Set IsPresent exactly when at least one of the 16 Profile ID bytes is non-zero."));
                    }
                }
                entryIndex = entryIndex + 1;
            }
        }
    }

    internal static int ResolveChannelCount(Icc1ChannelSource source, IccProfileHeader header)
    {
        if (source == Icc1ChannelSource.DataColorSpace)
        {
            return ColorSpaceChannelCount.Decode(header.DataColorSpaceSignature);
        }
        if (source == Icc1ChannelSource.ProfileConnectionSpace)
        {
            return ColorSpaceChannelCount.Decode(header.ProfileConnectionSpaceSignature);
        }
        if (source == Icc1ChannelSource.One)
        {
            return 1;
        }
        if (source == Icc1ChannelSource.DataOrDeviceLinkOutput)
        {
            return header.ProfileClass == IccProfileClass.DeviceLink
                ? ColorSpaceChannelCount.Decode(header.ProfileConnectionSpaceSignature)
                : ColorSpaceChannelCount.Decode(header.DataColorSpaceSignature);
        }
        return 0;
    }

    internal static bool IsMatchingVector(IccColorVector? vector, uint signature, int count)
    {
        if (vector == null || vector.Components == null
            || vector.ColorSpaceSignature != signature || vector.Components.Length != count)
        {
            return false;
        }
        return vector.ColorSpace == HeaderCodec.DecodeColorSpace(signature);
    }

    internal static bool IsImageState(uint signature)
    {
        return signature == IccImageStateSignatures.SceneColorimetryEstimates
            || signature == IccImageStateSignatures.SceneAppearanceEstimates
            || signature == IccImageStateSignatures.FocalPlaneColorimetryEstimates
            || signature == IccImageStateSignatures.ReflectionHardcopyOriginalColorimetry
            || signature == IccImageStateSignatures.ReflectionPrintOutputColorimetry;
    }

    internal static bool IsClose(double first, double second)
    {
        return System.Math.Abs(first - second) <= 0.00002;
    }

    internal static bool ContainsNonZero(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasTag(IccProfile profile, uint signature)
    {
        return FindTag(profile, signature) != null;
    }

    internal static IccTag? FindTag(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return tag;
            }
            index = index + 1;
        }
        return null;
    }

    internal static int FindTagIndex(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static IccValidationIssue CreateTagIssue(string ruleId, string message,
        IccTag tag, int index, string suffix, string remediation)
    {
        IccValidationIssue issue = CreateProfileIssue(ruleId, message,
            $"Tags[{index}].{suffix}", remediation);
        if (tag != null && tag.TableEntry != null && tag.TableEntry.Offset >= 0)
        {
            issue.Location.Offset = tag.TableEntry.Offset;
            issue.Location.Length = tag.TableEntry.Size < 0 ? 0 : tag.TableEntry.Size;
        }
        return issue;
    }

    internal static IccValidationIssue CreateProfileIssue(string ruleId, string message,
        string path, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = ruleId;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = message;
        issue.Path = path;
        issue.Remediation = remediation;
        return issue;
    }
}
