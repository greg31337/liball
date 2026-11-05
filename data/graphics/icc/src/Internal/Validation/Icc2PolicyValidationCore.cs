namespace icc;

internal static class Icc2PolicyValidationCore
{
    internal static IccValidationIssue[] Validate(IccProfile profile,
        IccValidationPolicy policy)
    {
        System.Collections.Generic.List<IccValidationIssue> issues =
            new System.Collections.Generic.List<IccValidationIssue>();
        if (profile == null || profile.Header == null || profile.Header.Version == null
            || profile.Tags == null
            || profile.Header.Version.Kind != IccVersionKind.Version5)
        {
            return issues.ToArray();
        }

        ValidateRequiredTags(profile, policy, issues);
        int index = 0;
        while (index < profile.Tags.Length)
        {
            IccTag tag = profile.Tags[index];
            if (tag != null)
            {
                ValidateTagPermission(tag, index, policy, issues);
                if (tag.Data != null)
                {
                    ValidatePolicyTagData(tag, index, tag.Data, "Data", 0,
                        policy, issues);
                }
            }
            index = index + 1;
        }
        ValidateSubclass(profile, policy, issues);
        return issues.ToArray();
    }

    internal static void ValidateRequiredTags(IccProfile profile,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 0;
        while (index < policy.Icc2RequiredTagSignatures.Length)
        {
            uint signature = policy.Icc2RequiredTagSignatures[index];
            if (!HasTag(profile, signature))
            {
                issues.Add(Icc2ValidationCore.CreateProfileIssue(
                    IccValidationRuleIds.RequiredTag,
                    $"The selected ICC.2 policy requires tag '{FourCcCodec.Format(signature)}'.",
                    $"Tags['{FourCcCodec.Format(signature)}']",
                    "Add the policy-required ICC.2 tag or select a different conformance policy."));
            }
            index = index + 1;
        }
    }

    internal static void ValidateTagPermission(IccTag tag, int index,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        bool forbidden = ContainsSignature(policy.Icc2ForbiddenTagSignatures,
            tag.Signature);
        bool listed = ContainsSignature(policy.Icc2RequiredTagSignatures, tag.Signature)
            || ContainsSignature(policy.Icc2OptionalTagSignatures, tag.Signature);
        if (forbidden || (policy.Icc2ForbidUnlistedTags && !listed))
        {
            string reason = forbidden ? "is explicitly forbidden"
                : "is absent from the required and optional tag lists";
            issues.Add(Icc2ValidationCore.CreateTagIssue(
                IccValidationRuleIds.ForbiddenTag,
                $"ICC.2 tag '{FourCcCodec.Format(tag.Signature)}' {reason} in the selected policy.",
                tag, index, "Data",
                "Remove the tag or include it in the selected ICC.2 policy."));
        }
    }

    internal static void ValidatePolicyTagData(IccTag tag, int tagIndex,
        IccTagData data, string path, int depth, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (depth > policy.ResourceLimits.MaximumNestingDepth)
        {
            return;
        }
        if (data.MultiProcessElements != null)
        {
            ValidatePolicyMpe(tag, tagIndex, data.MultiProcessElements,
                path + ".MultiProcessElements", depth, policy, issues);
        }
        if (data.TagArray != null && data.TagArray.Values != null)
        {
            int index = 0;
            while (index < data.TagArray.Values.Length)
            {
                IccTagData item = data.TagArray.Values[index];
                if (item != null)
                {
                    ValidatePolicyTagData(tag, tagIndex, item,
                        $"{path}.TagArray.Values[{index}]", depth + 1,
                        policy, issues);
                }
                index = index + 1;
            }
        }
        if (data.TagStructure != null && data.TagStructure.Members != null)
        {
            int index = 0;
            while (index < data.TagStructure.Members.Length)
            {
                IccStructureMember member = data.TagStructure.Members[index];
                if (member != null && member.Data != null)
                {
                    ValidatePolicyTagData(tag, tagIndex, member.Data,
                        $"{path}.TagStructure.Members[{index}].Data", depth + 1,
                        policy, issues);
                }
                index = index + 1;
            }
        }
        if (data.TagArray == null && data.TagStructure == null)
        {
            ValidateProjectedPipelines(tag, tagIndex, data, path, depth,
                policy, issues);
        }
    }

    internal static void ValidateProjectedPipelines(IccTag tag, int tagIndex,
        IccTagData data, string path, int depth, IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        if (data.ProfileConnectionConditions != null)
        {
            IccProfileConnectionConditions pcc = data.ProfileConnectionConditions;
            if (pcc.ToStandardPcs != null)
            {
                ValidatePolicyMpe(tag, tagIndex, pcc.ToStandardPcs,
                    path + ".ProfileConnectionConditions.ToStandardPcs", depth + 1,
                    policy, issues);
            }
            if (pcc.FromStandardPcs != null)
            {
                ValidatePolicyMpe(tag, tagIndex, pcc.FromStandardPcs,
                    path + ".ProfileConnectionConditions.FromStandardPcs", depth + 1,
                    policy, issues);
            }
        }
        if (data.Brdf != null)
        {
            if (data.Brdf.ColorimetricTransform != null)
            {
                ValidatePolicyMpe(tag, tagIndex, data.Brdf.ColorimetricTransform,
                    path + ".Brdf.ColorimetricTransform", depth + 1, policy, issues);
            }
            if (data.Brdf.SpectralTransform != null)
            {
                ValidatePolicyMpe(tag, tagIndex, data.Brdf.SpectralTransform,
                    path + ".Brdf.SpectralTransform", depth + 1, policy, issues);
            }
        }
    }

    internal static void ValidatePolicyMpe(IccTag tag, int tagIndex,
        IccMultiProcessElements mpe, string path, int depth,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        ValidateChannelRules(tag, tagIndex, 0, mpe.InputChannels,
            mpe.OutputChannels, path, policy, issues);
        if (mpe.Elements == null)
        {
            return;
        }
        int index = 0;
        while (index < mpe.Elements.Length)
        {
            IccProcessingElement element = mpe.Elements[index];
            if (element != null)
            {
                string elementPath = $"{path}.Elements[{index}]";
                if ((policy.Icc2ForbidUnlistedProcessingElements
                        || policy.Icc2SupportedProcessingElementSignatures.Length != 0)
                    && !ContainsSignature(
                        policy.Icc2SupportedProcessingElementSignatures,
                        element.Signature))
                {
                    issues.Add(Icc2ValidationCore.CreateTagIssue(
                        IccValidationRuleIds.ForbiddenTag,
                        $"ICC.2 processing element '{FourCcCodec.Format(element.Signature)}' is not supported by the selected policy.",
                        tag, tagIndex, elementPath,
                        "Replace the processing element or add its signature to the policy support list."));
                }
                ValidateChannelRules(tag, tagIndex, element.Signature,
                    element.InputChannels, element.OutputChannels, elementPath,
                    policy, issues);
                if (depth < policy.ResourceLimits.MaximumNestingDepth
                    && element.Calculator != null
                    && element.Calculator.SubElements != null)
                {
                    ValidateCalculatorElements(tag, tagIndex,
                        element.Calculator.SubElements,
                        elementPath + ".Calculator.SubElements", depth + 1,
                        policy, issues);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateCalculatorElements(IccTag tag, int tagIndex,
        IccProcessingElement[] elements, string path, int depth,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 0;
        while (index < elements.Length)
        {
            IccProcessingElement element = elements[index];
            if (element != null)
            {
                string elementPath = $"{path}[{index}]";
                if ((policy.Icc2ForbidUnlistedProcessingElements
                        || policy.Icc2SupportedProcessingElementSignatures.Length != 0)
                    && !ContainsSignature(
                        policy.Icc2SupportedProcessingElementSignatures,
                        element.Signature))
                {
                    issues.Add(Icc2ValidationCore.CreateTagIssue(
                        IccValidationRuleIds.ForbiddenTag,
                        $"ICC.2 calculator sub-element '{FourCcCodec.Format(element.Signature)}' is not supported by the selected policy.",
                        tag, tagIndex, elementPath,
                        "Replace the sub-element or add its signature to the policy support list."));
                }
                ValidateChannelRules(tag, tagIndex, element.Signature,
                    element.InputChannels, element.OutputChannels, elementPath,
                    policy, issues);
                if (depth < policy.ResourceLimits.MaximumNestingDepth
                    && element.Calculator != null
                    && element.Calculator.SubElements != null)
                {
                    ValidateCalculatorElements(tag, tagIndex,
                        element.Calculator.SubElements,
                        elementPath + ".Calculator.SubElements", depth + 1,
                        policy, issues);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateChannelRules(IccTag tag, int tagIndex,
        uint elementSignature, int inputChannels, int outputChannels, string path,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        int index = 0;
        while (index < policy.Icc2ChannelRules.Length)
        {
            IccValidationChannelRule rule = policy.Icc2ChannelRules[index];
            bool tagMatches = rule.TagSignature == 0
                || rule.TagSignature == tag.Signature;
            if (tagMatches && rule.ProcessingElementSignature == elementSignature
                && ((rule.InputChannels != 0
                        && rule.InputChannels != inputChannels)
                    || (rule.OutputChannels != 0
                        && rule.OutputChannels != outputChannels)))
            {
                string subject = elementSignature == 0 ? "pipeline"
                    : $"processing element '{FourCcCodec.Format(elementSignature)}'";
                issues.Add(Icc2ValidationCore.CreateTagIssue(
                    IccValidationRuleIds.ChannelCount,
                    $"The selected ICC.2 policy requires {subject} channels {FormatChannels(rule.InputChannels, rule.OutputChannels)}, not {inputChannels}-to-{outputChannels}.",
                    tag, tagIndex, path,
                    "Match the input and output channel counts declared by the selected policy."));
            }
            index = index + 1;
        }
    }

    internal static void ValidateSubclass(IccProfile profile,
        IccValidationPolicy policy,
        System.Collections.Generic.List<IccValidationIssue> issues)
    {
        IccProfileHeader header = profile.Header;
        if (header.ProfileSubclassSignature == 0)
        {
            if (policy.Icc2RequireProfileSubclass)
            {
                issues.Add(Icc2ValidationCore.CreateProfileIssue(
                    IccValidationRuleIds.HeaderProfileSubclass,
                    "The selected ICC.2 policy requires a profile subclass.",
                    "Header.ProfileSubclassSignature",
                    "Declare a policy-supported profile subclass and subclass ICS version."));
            }
            return;
        }
        if (policy.Icc2ProfileSubclassRules.Length == 0)
        {
            return;
        }
        bool signatureFound = false;
        bool contextFound = false;
        int index = 0;
        while (index < policy.Icc2ProfileSubclassRules.Length)
        {
            IccValidationSubclassRule rule = policy.Icc2ProfileSubclassRules[index];
            if (rule.ProfileSubclassSignature == header.ProfileSubclassSignature)
            {
                signatureFound = true;
                if (SubclassContextMatches(header, rule))
                {
                    contextFound = true;
                }
            }
            index = index + 1;
        }
        if (!contextFound)
        {
            string message = signatureFound
                ? $"ICC.2 subclass '{FourCcCodec.Format(header.ProfileSubclassSignature)}' does not satisfy a policy version, class, or colour-space constraint."
                : $"ICC.2 subclass '{FourCcCodec.Format(header.ProfileSubclassSignature)}' is not supported by the selected policy.";
            issues.Add(Icc2ValidationCore.CreateProfileIssue(
                IccValidationRuleIds.HeaderProfileSubclass, message,
                "Header.ProfileSubclass",
                "Use a subclass signature, version, profile class, and connection spaces accepted by the selected policy."));
        }
    }

    internal static bool SubclassContextMatches(IccProfileHeader header,
        IccValidationSubclassRule rule)
    {
        bool minimum = header.Version.SubclassMajor > rule.MinimumMajorVersion
            || (header.Version.SubclassMajor == rule.MinimumMajorVersion
                && header.Version.SubclassMinor >= rule.MinimumMinorVersion);
        bool maximum = header.Version.SubclassMajor < rule.MaximumMajorVersion
            || (header.Version.SubclassMajor == rule.MaximumMajorVersion
                && header.Version.SubclassMinor <= rule.MaximumMinorVersion);
        return minimum && maximum
            && (rule.ProfileClass == IccProfileClass.Unknown
                || rule.ProfileClass == header.ProfileClass)
            && (rule.DataColorSpaceSignature == 0
                || rule.DataColorSpaceSignature == header.DataColorSpaceSignature)
            && (rule.ProfileConnectionSpaceSignature == 0
                || rule.ProfileConnectionSpaceSignature
                    == header.ProfileConnectionSpaceSignature)
            && (rule.SpectralPcsSignature == 0
                || rule.SpectralPcsSignature == header.SpectralPcsSignature)
            && (rule.MaterialConnectionSpaceSignature == 0
                || rule.MaterialConnectionSpaceSignature
                    == header.MaterialConnectionSpaceSignature);
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

    internal static bool ContainsSignature(uint[] signatures, uint signature)
    {
        int index = 0;
        while (index < signatures.Length)
        {
            if (signatures[index] == signature)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static string FormatChannels(int inputChannels, int outputChannels)
    {
        string input = inputChannels == 0 ? "any" : inputChannels.ToString();
        string output = outputChannels == 0 ? "any" : outputChannels.ToString();
        return input + "-to-" + output;
    }
}
