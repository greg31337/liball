namespace icc;

internal static class ProfileDescriptionCore
{
    internal static string SummarizeProfile(IccProfile profile)
    {
        ValidateProfile(profile);
        IccProfileHeader header = profile.Header;
        string name = FindProfileName(profile);
        string version = DescriptionFormatter.DescribeProfileVersion(header.Version);
        string profileClass = DescriptionFormatter.DescribeProfileClassKind(header.ProfileClass);
        string dataSpace = DescriptionFormatter.DescribeColorSpaceWithChannels(header.DataColorSpaceSignature, header.DataColorSpace);
        string pcs = DescriptionFormatter.DescribePcsKind(header.ProfileConnectionSpace);
        if (header.ProfileConnectionSpaceSignature != 0)
        {
            pcs = $"{pcs}; {SignatureDatabase.FormatDescription("FourCC", header.ProfileConnectionSpaceSignature)}";
        }

        string intent = DescriptionFormatter.DescribeRenderingIntentKind(header.RenderingIntentKind);
        string tagWord = profile.Tags.Length == 1 ? "tag" : "tags";
        return $"\"{DescriptionFormatter.EscapeText(name)}\" — {version}; {profileClass}; {dataSpace}; {pcs}; {intent}; {profile.Tags.Length} {tagWord}";
    }

    internal static string InspectProfileText(IccProfile profile, IccInspectionDetail detail, int maximumValues)
    {
        return InspectionTextWriter.WriteProfile(profile, detail, maximumValues);
    }

    internal static string InspectTagText(IccTag tag, IccInspectionDetail detail, int maximumValues)
    {
        if (tag == null)
        {
            throw new IccProfileValidationException("A profile tag cannot be null when it is inspected.");
        }

        ValidateInspectionArguments(detail, maximumValues);
        if (tag.Data == null)
        {
            return $"{SignatureDatabase.DescribeTag(tag.Signature)}: data unavailable";
        }

        IccTagData data = tag.Data;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(SignatureDatabase.DescribeTag(tag.Signature));
        builder.Append(": ");
        if (data.Kind == IccTagDataKind.LocalizedText)
        {
            builder.Append("localized text [");
            builder.Append(DescriptionFormatter.DescribeLocalizedRecords(data.LocalizedStrings, maximumValues));
            builder.Append("]; preferred=\"");
            builder.Append(DescriptionFormatter.EscapeText(DescriptionFormatter.SelectLocalizedText(data.LocalizedStrings)));
            builder.Append('"');
        }
        else if (data.Kind == IccTagDataKind.Text && data.Text != null)
        {
            builder.Append("text \"");
            builder.Append(DescriptionFormatter.EscapeText(SelectTextValue(data.Text)));
            builder.Append('"');
        }
        else if (data.Kind == IccTagDataKind.Cicp && data.Cicp != null)
        {
            builder.Append("CICP ");
            builder.Append(DescriptionFormatter.DescribeCicp(data.Cicp));
        }
        else if (data.Kind == IccTagDataKind.NamedColors && data.NamedColors != null)
        {
            AppendNamedColors(builder, data.NamedColors, maximumValues);
        }
        else if (data.Kind == IccTagDataKind.Colorants && data.Colorants != null)
        {
            AppendColorants(builder, data.Colorants, maximumValues);
        }
        else if (data.Kind == IccTagDataKind.Signature)
        {
            builder.Append("signature ");
            builder.Append(SignatureDatabase.FormatDescription("FourCC", data.Signature));
        }
        else if (data.Kind == IccTagDataKind.Curve && data.Curve != null)
        {
            builder.Append("curve ");
            builder.Append(data.Curve.Kind.ToString());
            builder.Append($"; {data.Curve.Samples.Length} samples; {data.Curve.Segments.Length} segments");
        }
        else if (data.Lut != null)
        {
            builder.Append($"LUT {data.Lut.InputChannels}-to-{data.Lut.OutputChannels}; grid ");
            builder.Append(InspectionTextWriter.FormatIntegers(data.Lut.Clut.GridPoints, maximumValues));
        }
        else if (data.MultiStageLut != null)
        {
            builder.Append($"multi-stage LUT {data.MultiStageLut.InputChannels}-to-{data.MultiStageLut.OutputChannels}; A={data.MultiStageLut.ACurves.Length}; B={data.MultiStageLut.BCurves.Length}; M={data.MultiStageLut.MCurves.Length}");
        }
        else if (data.MultiProcessElements != null)
        {
            builder.Append($"MPE {data.MultiProcessElements.InputChannels}-to-{data.MultiProcessElements.OutputChannels}; {data.MultiProcessElements.Elements.Length} elements; executable={data.MultiProcessElements.IsExecutable}");
        }
        else if (data.TagArray != null)
        {
            builder.Append($"ICC.2 array {SignatureDatabase.DescribeArray(data.TagArray.ArraySignature)}; {data.TagArray.Values.Length} values");
        }
        else if (data.TagStructure != null)
        {
            builder.Append($"ICC.2 structure {SignatureDatabase.DescribeStructure(data.TagStructure.StructureSignature)}; {data.TagStructure.Members.Length} members");
        }
        else if (data.Kind == IccTagDataKind.Raw)
        {
            builder.Append("raw/unknown payload");
        }
        else
        {
            builder.Append("data kind ");
            builder.Append(data.Kind.ToString());
        }

        if (detail == IccInspectionDetail.Raw && data.RawData != null && data.RawData.Bytes != null)
        {
            builder.Append($"; raw length={data.RawData.Bytes.Length} bytes");
            AppendRawPreview(builder, data.RawData.Bytes, maximumValues);
        }

        return builder.ToString();
    }

    internal static string DescribeTransformSelection(IccProfile profile,
        IccTransformDirection direction, IccRenderingIntent intent)
    {
        ValidateProfile(profile);
        IntentSelection selection = SelectTransform(profile, direction, intent);
        string selected = selection.UsesMatrixTrc ? "matrix/TRC"
            : selection.TagSignature == 0 ? "none"
                : SignatureDatabase.DescribeTag(selection.TagSignature);
        return $"{direction}; requested {DescriptionFormatter.DescribeRenderingIntentKind(intent)}; success={selection.Success}; selected {selected}; selected intent {DescriptionFormatter.DescribeRenderingIntentKind(selection.SelectedIntent)}; fallback={selection.UsedFallback}; {selection.Message}";
    }

    internal static string DescribeProfileIdStatus(IccProfile profile)
    {
        IccProfileHeader header = profile.Header;
        if (header.Version == null)
        {
            return "not applicable without a profile version";
        }
        if (header.Version.Kind != IccVersionKind.Version4
            && header.Version.Kind != IccVersionKind.Version5)
        {
            return "not applicable to this profile version";
        }
        if (header.ProfileId == null || header.ProfileId.Bytes == null
            || header.ProfileId.Bytes.Length != IccConstants.ProfileIdSize)
        {
            return "invalid Profile ID field";
        }
        bool hasBytes = HeaderCodec.ContainsNonZeroBytes(header.ProfileId.Bytes, 0,
            header.ProfileId.Bytes.Length);
        if (!hasBytes)
        {
            return header.ProfileId.IsPresent ? "invalid presence indicator" : "absent";
        }
        if (!header.ProfileId.IsPresent)
        {
            return "invalid presence indicator";
        }
        if (!CanVerifyOriginalProfileId(profile.OriginalBytes))
        {
            return $"present ({System.Convert.ToHexString(header.ProfileId.Bytes)}); verification unavailable";
        }
        IccProfileId computed = ProfileIdCore.Compute(profile.OriginalBytes);
        return ProfileIdCore.BytesEqual(header.ProfileId.Bytes, computed.Bytes)
            ? $"valid ({System.Convert.ToHexString(header.ProfileId.Bytes)})"
            : $"mismatch ({System.Convert.ToHexString(header.ProfileId.Bytes)})";
    }

    internal static bool CanVerifyOriginalProfileId(byte[] bytes)
    {
        if (bytes == null || bytes.Length < IccConstants.MinimumProfileSize)
        {
            return false;
        }
        if (ProfileIdCore.ReadUInt32(bytes, 0) != (uint)bytes.Length
            || ProfileIdCore.ReadUInt32(bytes, 36) != IccConstants.ProfileFileSignature)
        {
            return false;
        }
        IccVersion version = HeaderCodec.DecodeVersion(ProfileIdCore.ReadUInt32(bytes, 8));
        return version.IsSupported && (version.Kind == IccVersionKind.Version4
            || version.Kind == IccVersionKind.Version5);
    }

    internal static string FindProfileName(IccProfile profile)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == IccTagSignatures.ProfileDescription && tag.Data != null)
            {
                string description = SelectTagDescription(tag.Data);
                if (description.Length != 0)
                {
                    return description;
                }
            }

            index = index + 1;
        }

        return "Unnamed ICC profile";
    }

    internal static string SelectTagDescription(IccTagData data)
    {
        if (data.Kind == IccTagDataKind.LocalizedText && data.LocalizedStrings != null)
        {
            return DescriptionFormatter.SelectLocalizedText(data.LocalizedStrings);
        }

        if (data.Kind == IccTagDataKind.Text && data.Text != null)
        {
            return SelectTextValue(data.Text);
        }

        return string.Empty;
    }

    internal static string SelectTextValue(IccTextData data)
    {
        if (data.Text != null && data.Text.Length != 0)
        {
            return data.Text;
        }
        if (data.UnicodeText != null && data.UnicodeText.Length != 0)
        {
            return data.UnicodeText;
        }
        if (data.AsciiText != null && data.AsciiText.Length != 0)
        {
            return data.AsciiText;
        }
        if (data.ScriptCodeText != null)
        {
            return data.ScriptCodeText;
        }

        return string.Empty;
    }

    internal static void AppendNamedColors(System.Text.StringBuilder builder, IccNamedColorSet set, int maximumValues)
    {
        if (set.Colors == null)
        {
            throw new IccProfileValidationException("A named-colour collection cannot be null when it is inspected.");
        }

        builder.Append($"{set.Colors.Length} named colours");
        int limit = set.Colors.Length < maximumValues ? set.Colors.Length : maximumValues;
        int index = 0;
        while (index < limit)
        {
            IccNamedColor color = set.Colors[index];
            if (color == null || color.Description == null)
            {
                throw new IccProfileValidationException($"Named colour {index} and its description cannot be null.");
            }

            builder.Append(index == 0 ? ": " : ", ");
            builder.Append(DescriptionFormatter.DescribeColor(color.Description));
            index = index + 1;
        }

        if (limit < set.Colors.Length)
        {
            builder.Append($", … {set.Colors.Length - limit} more");
        }
    }

    internal static void AppendColorants(System.Text.StringBuilder builder, IccColorantSet set, int maximumValues)
    {
        if (set.Colorants == null)
        {
            throw new IccProfileValidationException("A colorant collection cannot be null when it is inspected.");
        }

        string space = DescriptionFormatter.DescribeColorSpaceWithChannels(set.ColorSpaceSignature, HeaderCodec.DecodeColorSpace(set.ColorSpaceSignature));
        builder.Append($"{set.Colorants.Length} colorants for {space}");
        int limit = set.Colorants.Length < maximumValues ? set.Colorants.Length : maximumValues;
        int index = 0;
        while (index < limit)
        {
            IccColorant colorant = set.Colorants[index];
            if (colorant == null || colorant.Name == null)
            {
                throw new IccProfileValidationException($"Colorant {index} and its name cannot be null.");
            }

            builder.Append(index == 0 ? ": " : ", ");
            builder.Append('"');
            builder.Append(DescriptionFormatter.EscapeText(colorant.Name));
            builder.Append('"');
            index = index + 1;
        }

        if (limit < set.Colorants.Length)
        {
            builder.Append($", … {set.Colorants.Length - limit} more");
        }
    }

    internal static void AppendRawPreview(System.Text.StringBuilder builder, byte[] bytes,
        int maximumValues)
    {
        int limit = bytes.Length < maximumValues ? bytes.Length : maximumValues;
        if (limit > 0)
        {
            builder.Append("; raw=");
            int index = 0;
            while (index < limit)
            {
                builder.Append(bytes[index].ToString("X2",
                    System.Globalization.CultureInfo.InvariantCulture));
                index = index + 1;
            }
        }
        if (limit < bytes.Length)
        {
            builder.Append($"; … {bytes.Length - limit} bytes omitted");
        }
    }

    internal static IntentSelection SelectTransform(IccProfile profile,
        IccTransformDirection direction, IccRenderingIntent intent)
    {
        if (profile.Header.Version == null
            || (profile.Header.Version.Kind != IccVersionKind.Version2
                && profile.Header.Version.Kind != IccVersionKind.Version4
                && profile.Header.Version.Kind != IccVersionKind.Version5))
        {
            IntentSelection unavailable = new IntentSelection();
            unavailable.RequestedIntent = intent;
            unavailable.SelectedIntent = intent;
            unavailable.Message = profile.Header.Version == null
                ? "The profile version is unavailable."
                : "The profile version is unsupported for transform selection.";
            return unavailable;
        }
        if (profile.Header.Version.Kind == IccVersionKind.Version5)
        {
            return IntentSelector.SelectVersionFiveTransform(profile, direction,
                intent, IccIntentFallback.AllowAny);
        }
        return IntentSelector.SelectIcc1Transform(profile, direction, intent,
            IccIntentFallback.AllowAny, false);
    }

    internal static void ValidateProfile(IccProfile profile)
    {
        if (profile == null)
        {
            throw new IccProfileValidationException("A profile cannot be null when it is described.");
        }
        if (profile.Header == null)
        {
            throw new IccProfileValidationException("A profile header cannot be null when it is described.");
        }
        if (profile.Tags == null)
        {
            throw new IccProfileValidationException("A profile tag collection cannot be null when it is described.");
        }
    }

    internal static void ValidateInspectionArguments(IccInspectionDetail detail, int maximumValues)
    {
        if (detail != IccInspectionDetail.Summary && detail != IccInspectionDetail.Normal
            && detail != IccInspectionDetail.Detailed && detail != IccInspectionDetail.Raw)
        {
            throw new IccProfileValidationException("The inspection detail value is unsupported.");
        }
        if (maximumValues < 0)
        {
            throw new IccProfileValidationException("The repeated-value inspection limit cannot be negative.");
        }
    }
}
