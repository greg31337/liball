namespace icc;

internal static class Icc1ConformanceValidationCore
{
    internal static readonly uint[] MatrixTags = new uint[]
    {
        IccTagSignatures.RedMatrixColumn,
        IccTagSignatures.GreenMatrixColumn,
        IccTagSignatures.BlueMatrixColumn,
        IccTagSignatures.RedTrc,
        IccTagSignatures.GreenTrc,
        IccTagSignatures.BlueTrc
    };

    internal static readonly uint[] OutputLutTags = new uint[]
    {
        IccTagSignatures.AToB0,
        IccTagSignatures.AToB1,
        IccTagSignatures.AToB2,
        IccTagSignatures.BToA0,
        IccTagSignatures.BToA1,
        IccTagSignatures.BToA2,
        IccTagSignatures.Gamut
    };

    internal static IccValidationIssue[] Validate(IccProfile profile)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile.Header == null || profile.Header.Version == null
            || profile.Tags == null)
        {
            return issues.ToArray();
        }
        IccVersionKind version = profile.Header.Version.Kind;
        if (version != IccVersionKind.Version2 && version != IccVersionKind.Version4)
        {
            return issues.ToArray();
        }
        ValidateTagCompatibility(profile, version, issues);
        ValidateRequiredModel(profile, issues);
        return issues.ToArray();
    }

    internal static void ValidateTagCompatibility(IccProfile profile, IccVersionKind version,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null)
            {
                Icc1TagDefinition? definition = Icc1TagRegistry.FindDefinition(tag.Signature);
                if (definition != null)
                {
                    uint[] permittedTypes = Icc1TagRegistry.GetPermittedTypes(definition, version);
                    if (permittedTypes.Length == 0)
                    {
                        issues.Add(CreateTagIssue(IccValidationRuleIds.ForbiddenTag,
                            $"Tag '{FourCcCodec.Format(tag.Signature)}' is not defined for ICC {Icc1TagRegistry.DescribeVersion(version)} profiles.",
                            tag, index,
                            "Remove the tag or select a profile generation in which it is defined."));
                    }
                    else
                    {
                        uint typeSignature = Icc1TagValidationCore.ResolveTypeSignature(tag);
                        if (typeSignature != 0
                            && !Icc1TagRegistry.ContainsType(permittedTypes, typeSignature))
                        {
                            issues.Add(CreateTagIssue(IccValidationRuleIds.TagTypeCompatibility,
                                $"Tag '{FourCcCodec.Format(tag.Signature)}' contains type '{FourCcCodec.Format(typeSignature)}', but ICC {Icc1TagRegistry.DescribeVersion(version)} permits {Icc1TagRegistry.FormatPermittedTypes(permittedTypes)}.",
                                tag, index,
                                "Encode the tag with one of the permitted generation-specific tag types."));
                        }
                        if (!Icc1TagRegistry.IsProfileClassAllowed(definition,
                            profile.Header.ProfileClass))
                        {
                            issues.Add(CreateTagIssue(IccValidationRuleIds.ForbiddenTag,
                                $"Tag '{FourCcCodec.Format(tag.Signature)}' is forbidden in the {profile.Header.ProfileClass} profile class.",
                                tag, index,
                                "Remove the tag or use a profile class for which the registered tag is permitted."));
                        }
                    }
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateRequiredModel(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccProfileClass profileClass = profile.Header.ProfileClass;
        AddRequiredTag(profile, IccTagSignatures.ProfileDescription, "all ICC.1 profile models",
            issues);
        AddRequiredTag(profile, IccTagSignatures.Copyright, "all ICC.1 profile models", issues);
        if (profileClass != IccProfileClass.DeviceLink)
        {
            AddRequiredTag(profile, IccTagSignatures.MediaWhitePoint,
                "all non-DeviceLink ICC.1 profile models", issues);
        }

        switch (profileClass)
        {
            case IccProfileClass.Input:
                ValidateDeviceModel(profile, false, issues);
                break;
            case IccProfileClass.Display:
                ValidateDeviceModel(profile, true, issues);
                break;
            case IccProfileClass.Output:
                ValidateOutputModel(profile, issues);
                break;
            case IccProfileClass.DeviceLink:
                ValidateDeviceLinkModel(profile, issues);
                break;
            case IccProfileClass.ColorSpace:
                ValidateColorSpaceModel(profile, issues);
                break;
            case IccProfileClass.Abstract:
                AddRequiredTag(profile, IccTagSignatures.AToB0,
                    "Abstract PCS-to-PCS model", issues);
                break;
            case IccProfileClass.NamedColor:
                AddRequiredTag(profile, IccTagSignatures.NamedColor2,
                    "NamedColor model", issues);
                break;
            default:
                break;
        }
    }

    internal static void ValidateDeviceModel(IccProfile profile, bool display,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Gray)
        {
            AddRequiredTag(profile, IccTagSignatures.GrayTrc,
                display ? "monochrome Display model" : "monochrome Input model", issues);
            return;
        }
        if (HasAnyTag(profile, MatrixTags))
        {
            AddRequiredTags(profile, MatrixTags,
                display ? "three-component matrix/TRC Display model"
                    : "three-component matrix/TRC Input model", issues);
            int channels = ColorSpaceChannelCount.Decode(
                profile.Header.DataColorSpaceSignature);
            if (channels != 3 || profile.Header.ProfileConnectionSpaceSignature
                != IccColorSpaceSignatures.Xyz)
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.ProfileClassSemantics,
                    "An ICC.1 matrix/TRC model requires a three-component data space and PCSXYZ.",
                    "Header.ProfileConnectionSpace",
                    "Use a three-component data colour space with PCSXYZ, or encode the profile as a LUT model."));
            }
            return;
        }
        AddRequiredTag(profile, IccTagSignatures.AToB0,
            display ? "N-component LUT Display model" : "N-component LUT Input model", issues);
        if (display)
        {
            AddRequiredTag(profile, IccTagSignatures.BToA0,
                "N-component LUT Display model", issues);
        }
    }

    internal static void ValidateOutputModel(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (profile.Header.DataColorSpaceSignature == IccColorSpaceSignatures.Gray)
        {
            AddRequiredTag(profile, IccTagSignatures.GrayTrc,
                "monochrome Output model", issues);
            return;
        }
        AddRequiredTags(profile, OutputLutTags, "N-component LUT Output model", issues);
        if (IsXclr(profile.Header.DataColorSpaceSignature))
        {
            AddRequiredTag(profile, IccTagSignatures.ColorantTable,
                "xCLR Output model", issues);
        }
    }

    internal static void ValidateColorSpaceModel(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (HasAnyTag(profile, MatrixTags))
        {
            AddRequiredTags(profile, MatrixTags, "three-component matrix/TRC ColorSpace model",
                issues);
            int channels = ColorSpaceChannelCount.Decode(
                profile.Header.DataColorSpaceSignature);
            if (channels != 3 || profile.Header.ProfileConnectionSpaceSignature
                != IccColorSpaceSignatures.Xyz)
            {
                issues.Add(CreateProfileIssue(IccValidationRuleIds.ProfileClassSemantics,
                    "An ICC.1 matrix/TRC ColorSpace model requires a three-component data space and PCSXYZ.",
                    "Header.ProfileConnectionSpace",
                    "Use a three-component data colour space with PCSXYZ, or encode the profile as a LUT model."));
            }
            return;
        }

        AddRequiredTag(profile, IccTagSignatures.AToB0,
            "ColorSpace forward LUT model", issues);
        AddRequiredTag(profile, IccTagSignatures.BToA0,
            "ColorSpace reverse LUT model", issues);
    }

    internal static void ValidateDeviceLinkModel(IccProfile profile,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        AddRequiredTag(profile, IccTagSignatures.ProfileSequenceDescription,
            "DeviceLink model", issues);
        AddRequiredTag(profile, IccTagSignatures.AToB0, "DeviceLink model", issues);
        if (IsXclr(profile.Header.DataColorSpaceSignature))
        {
            AddRequiredTag(profile, IccTagSignatures.ColorantTable,
                "xCLR DeviceLink input model", issues);
        }
        if (IsXclr(profile.Header.ProfileConnectionSpaceSignature))
        {
            AddRequiredTag(profile, IccTagSignatures.ColorantTableOut,
                "xCLR DeviceLink output model", issues);
        }
    }

    internal static void AddRequiredTags(IccProfile profile, uint[] signatures,
        string model, System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 0;
        while (index < signatures.Length)
        {
            AddRequiredTag(profile, signatures[index], model, issues);
            index = index + 1;
        }
    }

    internal static void AddRequiredTag(IccProfile profile, uint signature, string model,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (HasTag(profile, signature))
        {
            return;
        }
        string formatted = FourCcCodec.Format(signature);
        issues.Add(CreateProfileIssue(IccValidationRuleIds.RequiredTag,
            $"Required tag '{formatted}' is missing from the {model}.",
            $"Tags['{formatted}']",
            $"Add a generation-appropriate '{formatted}' tag for the selected profile model."));
    }

    internal static bool HasAnyTag(IccProfile profile, uint[] signatures)
    {
        int index = 0;
        while (index < signatures.Length)
        {
            if (HasTag(profile, signatures[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool HasTag(IccProfile profile, uint signature)
    {
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null && tag.Signature == signature)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsXclr(uint signature)
    {
        if ((signature & 0x00ffffff) != 0x00434c52)
        {
            return false;
        }
        uint leading = signature >> 24;
        return (leading >= 0x32 && leading <= 0x39)
            || (leading >= 0x41 && leading <= 0x46);
    }

    internal static IccValidationIssue CreateTagIssue(string ruleId, string message,
        IccTag tag, int index, string remediation)
    {
        IccValidationIssue issue = CreateProfileIssue(ruleId, message,
            $"Tags[{index}]", remediation);
        if (tag.TableEntry != null && tag.TableEntry.Offset >= 0)
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
