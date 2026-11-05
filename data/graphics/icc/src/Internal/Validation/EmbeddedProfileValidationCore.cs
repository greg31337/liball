namespace icc;

internal static class EmbeddedProfileValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile,
        IccValidationPolicy policy)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile == null || profile.Header == null || profile.Tags == null)
        {
            return issues.ToArray();
        }

        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Data != null
                && (tag.Signature == IccTagSignatures.EmbeddedIccMaxProfile
                    || tag.Data.Kind == IccTagDataKind.EmbeddedProfile
                    || tag.Data.EmbeddedProfile != null))
            {
                ValidateContainer(profile, tag, index, policy, issues);
            }
            index = index + 1;
        }
        return issues.ToArray();
    }

    internal static void ValidateContainer(IccProfile outer, IccTag tag, int tagIndex,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        string path = $"Tags[{tagIndex}].Data.EmbeddedProfile";
        if (tag.Signature != IccTagSignatures.EmbeddedIccMaxProfile)
        {
            issues.Add(CreateIssue(tag, tagIndex, path,
                "An embeddedProfileType model is stored outside the ICC5 embedded-profile tag.",
                "Store the complete ICC.2 replacement in an ICC5 tag."));
        }
        if (!IsIcc1Generation(outer.Header.Version))
        {
            issues.Add(CreateIssue(tag, tagIndex, path,
                "The embedded-profile container shall be an ICC.1 version 2 or version 4 profile.",
                "Move the ICC.2 replacement into an ICC.1 version 2 or version 4 fallback profile."));
        }
        if (!IsReplacementClass(outer.Header.ProfileClass))
        {
            issues.Add(CreateIssue(tag, tagIndex, path,
                "The containing profile class cannot have an ICC.2 logical replacement.",
                "Use an ICC.1 input, display, output, DeviceLink, ColorSpace, abstract, or NamedColor class."));
        }

        IccProfile? embedded = tag.Data.EmbeddedProfile;
        if (embedded == null)
        {
            if (tag.Data.Kind == IccTagDataKind.EmbeddedProfile)
            {
                issues.Add(CreateIssue(tag, tagIndex, path,
                    "The embedded-profile representation is missing its ICC.2 profile model.",
                    "Provide the complete decoded ICC.2 profile or retain the tag as raw fallback data."));
            }
            return;
        }
        if (embedded.Header == null)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header",
                "The embedded ICC.2 profile header is missing.",
                "Provide the complete embedded profile header."));
            return;
        }

        ValidateEmbeddedHeader(outer.Header, embedded.Header, tag, tagIndex,
            path, issues);
        ValidateContainerSize(embedded, tag, tagIndex, path, policy, issues);
        ValidateEncodedProfile(embedded, tag, tagIndex, path, policy, issues);
    }

    internal static void ValidateEmbeddedHeader(IccProfileHeader outer,
        IccProfileHeader embedded, IccTag tag, int tagIndex, string path,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (embedded.Version == null || embedded.Version.Kind != IccVersionKind.Version5
            || !embedded.Version.IsSupported)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.Version",
                "The embeddedProfileType shall contain a supported ICC.2 version 5 profile.",
                "Embed one complete supported ICC.2 version 5 profile."));
        }
        if (!IsReplacementClass(embedded.ProfileClass))
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.ProfileClass",
                "The embedded ICC.2 profile class is not representable by an ICC.1 fallback profile.",
                "Use a profile class available in both ICC.1 and ICC.2."));
        }
        bool embeddedFlag = (embedded.ProfileFlags
            & IccConstants.EmbeddedProfileFlag) != 0;
        if (!embeddedFlag || !embedded.IsEmbedded)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.ProfileFlags",
                "The embedded ICC.2 profile shall set embedded-profile flag bit 0.",
                "Set bit 0 in the embedded profile flags and synchronize the decoded flag field."));
        }
        bool independentRestriction = (embedded.ProfileFlags
            & IccConstants.UseIndependentOfDataFlag) != 0;
        if (independentRestriction || embedded.RequiresEmbeddedData)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.ProfileFlags",
                "The embedded ICC.2 profile shall clear the use-independent-of-data restriction in flag bit 1.",
                "Clear bit 1 so the replacement profile can be used independently of containing colour data."));
        }
        if (embedded.ProfileClassSignature != outer.ProfileClassSignature
            || embedded.ProfileClass != outer.ProfileClass)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.ProfileClass",
                "The embedded ICC.2 profile class does not equal the containing ICC.1 profile class.",
                "Use the same registered profile class in the fallback and replacement profiles."));
        }
        if (embedded.DataColorSpaceSignature != outer.DataColorSpaceSignature
            || embedded.DataColorSpace != HeaderCodec.DecodeDataColorSpace(
                outer.DataColorSpaceSignature, IccVersionKind.Version5))
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.DataColorSpace",
                "The embedded ICC.2 device colour space does not equal the containing ICC.1 device colour space.",
                "Use the same device-space signature and channel interpretation in both profiles."));
        }
    }

    internal static void ValidateContainerSize(IccProfile embedded, IccTag tag,
        int tagIndex, string path, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int declaredSize = embedded.Header.DeclaredSize;
        long requiredPayloadSize = (long)EmbeddedProfileTagCodec.TypeHeaderSize
            + declaredSize;
        if (declaredSize < IccConstants.MinimumProfileSize
            || (declaredSize & (IccConstants.TagPayloadAlignment - 1)) != 0
            || declaredSize > policy.ResourceLimits.MaximumProfileBytes)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Header.DeclaredSize",
                "The embedded ICC.2 profile size is below the minimum, unaligned, or exceeds the configured profile limit.",
                "Use the exact aligned size of one bounded complete ICC.2 profile."));
        }
        if (tag.TableEntry == null || tag.TableEntry.Size != requiredPayloadSize)
        {
            issues.Add(CreateIssue(tag, tagIndex, path,
                $"The embedded-profile tag shall contain exactly an 8-byte type header and the declared {declaredSize}-byte ICC.2 profile.",
                "Set the tag size to the exact embeddedProfileType header plus complete profile size."));
        }
        if (embedded.OriginalBytes == null
            || embedded.OriginalBytes.Length != declaredSize)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".OriginalBytes",
                "The embedded profile does not retain exactly its complete declared byte range.",
                "Retain the embedded ICC.2 profile in its entirety without prefixes or trailers."));
        }
        if (embedded.TrailingBytes == null || embedded.TrailingBytes.Length != 0)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".TrailingBytes",
                "The embedded profile contains bytes outside its declared profile size.",
                "Remove every byte following the embedded profile's declared end."));
        }
        if (embedded.Location != null && embedded.Location.Length > 0
            && embedded.Location.Length != declaredSize)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Location",
                "The embedded profile location length does not equal its declared size.",
                "Set the embedded location to the exact complete profile byte range."));
        }
        if (embedded.Location != null && embedded.Location.Offset >= 0
            && tag.TableEntry != null && tag.TableEntry.Offset >= 0
            && embedded.Location.Offset != tag.TableEntry.Offset
                + EmbeddedProfileTagCodec.TypeHeaderSize)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".Location.Offset",
                "The embedded profile location does not begin immediately after the tag type header.",
                "Set the embedded location to tag payload offset plus eight bytes."));
        }
    }

    internal static void ValidateEncodedProfile(IccProfile embedded, IccTag tag,
        int tagIndex, string path, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        byte[] bytes = embedded.OriginalBytes;
        if (bytes == null || bytes.Length < IccConstants.MinimumProfileSize
            || bytes.Length > policy.ResourceLimits.MaximumProfileBytes)
        {
            return;
        }
        uint encodedSize = EmbeddedProfileTagCodec.ReadUInt32(bytes, 0);
        uint encodedVersion = EmbeddedProfileTagCodec.ReadUInt32(bytes, 8);
        uint encodedClass = EmbeddedProfileTagCodec.ReadUInt32(bytes, 12);
        uint encodedSpace = EmbeddedProfileTagCodec.ReadUInt32(bytes, 16);
        uint encodedSignature = EmbeddedProfileTagCodec.ReadUInt32(bytes, 36);
        uint encodedFlags = EmbeddedProfileTagCodec.ReadUInt32(bytes, 44);
        if (encodedSize != bytes.Length || encodedSize != embedded.Header.DeclaredSize
            || embedded.Header.Version == null
            || encodedVersion != embedded.Header.Version.PackedValue
            || encodedClass != embedded.Header.ProfileClassSignature
            || encodedSpace != embedded.Header.DataColorSpaceSignature
            || encodedSignature != IccConstants.ProfileFileSignature
            || encodedFlags != embedded.Header.ProfileFlags)
        {
            issues.Add(CreateIssue(tag, tagIndex, path + ".OriginalBytes",
                "The retained embedded-profile header bytes disagree with its decoded size, version, class, device space, flags, or file signature.",
                "Decode the model from the complete retained profile bytes or regenerate both representations consistently."));
        }

        if (tag.Data.RawData != null && tag.Data.RawData.Bytes != null
            && tag.Data.RawData.Bytes.Length != 0)
        {
            byte[] payload = tag.Data.RawData.Bytes;
            bool exact = payload.Length == bytes.Length
                + EmbeddedProfileTagCodec.TypeHeaderSize;
            int index = 0;
            while (exact && index < bytes.Length)
            {
                exact = payload[index + EmbeddedProfileTagCodec.TypeHeaderSize]
                    == bytes[index];
                index = index + 1;
            }
            if (!exact)
            {
                issues.Add(CreateIssue(tag, tagIndex, path + ".OriginalBytes",
                    "The retained tag payload does not contain the embedded ICC.2 profile in its entirety.",
                    "Make bytes 8 through the tag end exactly equal the complete embedded profile."));
            }
        }
    }

    internal static bool IsIcc1Generation(IccVersion? version)
    {
        return version != null && version.IsSupported
            && (version.Kind == IccVersionKind.Version2
                || version.Kind == IccVersionKind.Version4);
    }

    internal static bool IsReplacementClass(IccProfileClass profileClass)
    {
        return profileClass == IccProfileClass.Input
            || profileClass == IccProfileClass.Display
            || profileClass == IccProfileClass.Output
            || profileClass == IccProfileClass.DeviceLink
            || profileClass == IccProfileClass.ColorSpace
            || profileClass == IccProfileClass.Abstract
            || profileClass == IccProfileClass.NamedColor;
    }

    internal static IccValidationIssue CreateIssue(IccTag tag, int tagIndex,
        string path, string message, string remediation)
    {
        IccValidationIssue issue = new IccValidationIssue();
        issue.RuleId = IccValidationRuleIds.EmbeddedProfile;
        issue.Severity = IccValidationSeverity.Error;
        issue.Message = message;
        issue.Path = path;
        issue.Remediation = remediation;
        if (tag.TableEntry != null && tag.TableEntry.Offset >= 0)
        {
            issue.Location.Offset = tag.TableEntry.Offset;
            issue.Location.Length = tag.TableEntry.Size < 0 ? 0 : tag.TableEntry.Size;
        }
        return issue;
    }
}
