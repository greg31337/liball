using System;
using System.Collections.Generic;
using System.Threading;

namespace html;

internal class HtmlSanitizerAttributeApplyResult
{
    internal HtmlSanitizerStyleChange[] styleChanges;

    internal HtmlSanitizerAttributeApplyResult(
        HtmlSanitizerStyleChange[] styleChangesValue)
    {
        if (styleChangesValue == null)
        {
            throw new ArgumentNullException("styleChangesValue");
        }

        this.styleChanges = CopyChanges(styleChangesValue);
    }

    internal HtmlSanitizerStyleChange[] GetStyleChanges()
    {
        return CopyChanges(this.styleChanges);
    }

    internal static HtmlSanitizerStyleChange[] CopyChanges(
        HtmlSanitizerStyleChange[] values)
    {
        HtmlSanitizerStyleChange[] result =
            new HtmlSanitizerStyleChange[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A style change collection cannot contain null.",
                    "styleChangesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal static class HtmlSanitizerAttributes
{
    internal static void Apply(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy)
    {
        ApplyWithResult(element, policy, 0, HtmlLimits.CreateDefault());
    }

    internal static void Apply(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth)
    {
        ApplyWithResult(element, policy, nestedMarkupDepth,
            HtmlLimits.CreateDefault());
    }

    internal static void Apply(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlLimits limits)
    {
        ApplyWithResult(element, policy, nestedMarkupDepth, limits,
            CancellationToken.None);
    }

    internal static void Apply(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        ApplyWithResult(element, policy, nestedMarkupDepth, limits,
            cancellationToken);
    }

    internal static HtmlSanitizerAttributeApplyResult ApplyWithResult(
        HtmlNode element, HtmlCompiledSanitizerPolicy policy)
    {
        return ApplyWithResult(element, policy, 0,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerAttributeApplyResult ApplyWithResult(
        HtmlNode element, HtmlCompiledSanitizerPolicy policy,
        int nestedMarkupDepth)
    {
        return ApplyWithResult(element, policy, nestedMarkupDepth,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerAttributeApplyResult ApplyWithResult(
        HtmlNode element, HtmlCompiledSanitizerPolicy policy,
        int nestedMarkupDepth, HtmlLimits limits)
    {
        return ApplyWithResult(element, policy, nestedMarkupDepth, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerAttributeApplyResult ApplyWithResult(
        HtmlNode element, HtmlCompiledSanitizerPolicy policy,
        int nestedMarkupDepth, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateElementAndPolicy(element, policy);
        HtmlSanitizerUrls.ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (nestedMarkupDepth < 0)
        {
            throw new ArgumentOutOfRangeException("nestedMarkupDepth");
        }

        HtmlAttribute[] original = element.GetAttributesStorage();
        HtmlSanitizerAttributeRule[] rules =
            policy.GetSanitizerAttributeRules();
        HtmlSanitizerAttributeValueRule[] valueRules =
            policy.GetSanitizerAttributeValueRules();
        HtmlSanitizerAttributeTokenRule[] tokenRules =
            policy.GetSanitizerAttributeTokenRules();
        HtmlSanitizerForcedAttributeRule[] legacyForced =
            policy.GetForcedAttributeRules();
        HtmlAttribute[] result = new HtmlAttribute[original.Length
            + valueRules.Length + tokenRules.Length + legacyForced.Length + 1];
        bool preserveRel = WillHaveBlankTarget(element, original, valueRules,
            legacyForced, policy);
        int count = 0;
        int index = 0;
        while (index < original.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlAttribute attribute = original[index];
            if (!HtmlSanitizerPolicyNames.IsSafeAttributeLocalName(
                attribute.GetNamespaceUri(), attribute.GetLocalName()))
            {
                index = index + 1;
                continue;
            }

            if (IsDisallowedCustomizedBuiltInAttribute(element, attribute,
                policy))
            {
                index = index + 1;
                continue;
            }

            HtmlSanitizerAttributeRule? rule = FindRule(element, attribute,
                rules, true);
            if (rule == null && IsLegacyElementAttributeAllowed(element,
                attribute, policy))
            {
                count = AddIfUnique(result, count, attribute);
            }
            else
            {
                if (rule == null)
                {
                    rule = FindRule(element, attribute, rules, false);
                }

                if (rule != null)
                {
                    count = ApplyRule(result, count, attribute, rule,
                        element, policy);
                }
                else if (policy.IsAttributeAllowedInNamespaces(
                    attribute.GetNamespaceUri(), attribute.GetLocalName(),
                    element.GetNamespaceUriStorage(),
                    element.GetLocalNameStorage())
                    || preserveRel && IsRelAttribute(attribute))
                {
                    count = AddIfUnique(result, count, attribute);
                }
            }

            index = index + 1;
        }

        count = ApplyValueRules(element, result, count, valueRules, policy);
        count = ApplyLegacyForcedRules(element, result, count,
            legacyForced, policy);
        count = ApplyTokenRules(element, result, count, tokenRules);
        count = ApplyLinkHardening(element, result, count);
        count = ApplyUrlPolicies(element, result, count, policy, limits,
            cancellationToken);
        List<HtmlSanitizerStyleChange> styleChanges =
            new List<HtmlSanitizerStyleChange>();
        count = ApplyStyleAttributes(result, count, policy, limits,
            styleChanges, cancellationToken);
        count = ApplyNestedMarkupAttributes(element, result, count, policy,
            nestedMarkupDepth, limits, cancellationToken);
        int clobberIndex = 0;
        while (clobberIndex < count)
        {
            result[clobberIndex] =
                HtmlSanitizerClobbering.RewriteAttribute(element,
                    result[clobberIndex], policy);
            clobberIndex = clobberIndex + 1;
        }

        HtmlAttribute[] finalAttributes = CopyAttributes(result, count);
        if (!AttributesEqual(original, finalAttributes))
        {
            element.ReplaceAttributesStorage(finalAttributes);
        }

        return new HtmlSanitizerAttributeApplyResult(
            styleChanges.ToArray());
    }

    internal static int ApplyStyleAttributes(HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy,
        List<HtmlSanitizerStyleChange> changes)
    {
        return ApplyStyleAttributes(values, count, policy,
            HtmlLimits.CreateDefault(), changes);
    }

    internal static int ApplyStyleAttributes(HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        List<HtmlSanitizerStyleChange> changes)
    {
        return ApplyStyleAttributes(values, count, policy, limits, changes,
            CancellationToken.None);
    }

    internal static int ApplyStyleAttributes(HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        List<HtmlSanitizerStyleChange> changes,
        CancellationToken cancellationToken)
    {
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlAttribute attribute = values[inputIndex];
            if (!IsStyleAttribute(attribute))
            {
                values[outputIndex] = attribute;
                outputIndex = outputIndex + 1;
                inputIndex = inputIndex + 1;
                continue;
            }

            HtmlSanitizerStyleResult sanitized =
                HtmlSanitizerStyles.Sanitize(attribute.GetValue(), policy,
                    limits, cancellationToken);
            HtmlSanitizerStyleChange[] styleChanges =
                sanitized.GetChanges();
            int changeIndex = 0;
            while (changeIndex < styleChanges.Length)
            {
                changes.Add(styleChanges[changeIndex]);
                changeIndex = changeIndex + 1;
            }

            if (sanitized.IsSuccess()
                && sanitized.GetValue().Length > 0)
            {
                values[outputIndex] = sanitized.GetValue()
                        == attribute.GetValue()
                    ? attribute : CopyAttributeWithValue(attribute,
                        sanitized.GetValue());
                outputIndex = outputIndex + 1;
            }

            inputIndex = inputIndex + 1;
        }

        return outputIndex;
    }

    internal static bool IsStyleAttribute(HtmlAttribute attribute)
    {
        return attribute.GetNamespaceUri().Length == 0
            && HtmlSanitizerPolicyNames.NormalizeAttributeLocalName("",
                attribute.GetLocalName(), "attribute") == "style";
    }

    internal static int ApplyNestedMarkupAttributes(HtmlNode element,
        HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth)
    {
        return ApplyNestedMarkupAttributes(element, values, count, policy,
            nestedMarkupDepth, HtmlLimits.CreateDefault());
    }

    internal static int ApplyNestedMarkupAttributes(HtmlNode element,
        HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlLimits limits)
    {
        return ApplyNestedMarkupAttributes(element, values, count, policy,
            nestedMarkupDepth, limits, CancellationToken.None);
    }

    internal static int ApplyNestedMarkupAttributes(HtmlNode element,
        HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlAttribute attribute = values[inputIndex];
            string value = attribute.GetValue();
            bool keep = true;
            if (IsSrcdocAttribute(element, attribute))
            {
                HtmlSanitizerNestedMarkupResult nested =
                    HtmlSanitizerNestedContent.SanitizeSrcdoc(value, policy,
                        nestedMarkupDepth + 1, limits, cancellationToken);
                keep = nested.IsSuccess();
                value = nested.GetValue();
            }

            if (keep)
            {
                if (policy.GetNeutralizeServerTemplateMarkers())
                {
                    value = HtmlSanitizerTemplateMarkers.Neutralize(value);
                }

                values[outputIndex] = value == attribute.GetValue()
                    ? attribute : CopyAttributeWithValue(attribute, value);
                outputIndex = outputIndex + 1;
            }

            inputIndex = inputIndex + 1;
        }

        return outputIndex;
    }

    internal static bool IsSrcdocAttribute(HtmlNode element,
        HtmlAttribute attribute)
    {
        return element.GetNamespaceUriStorage()
                == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri()
            && HtmlSanitizerPolicyNames.NormalizeElementLocalName(
                element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage(), "element") == "iframe"
            && attribute.GetNamespaceUri().Length == 0
            && HtmlSanitizerPolicyNames.NormalizeAttributeLocalName("",
                attribute.GetLocalName(), "attribute") == "srcdoc";
    }

    internal static int ApplyTokenRules(HtmlNode element, HtmlAttribute[] result,
        int count, HtmlSanitizerAttributeTokenRule[] rules)
    {
        string namespaceUri = element.GetNamespaceUriStorage();
        string localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(
            namespaceUri, element.GetLocalNameStorage(), "element");
        int index = 0;
        while (index < rules.Length)
        {
            HtmlSanitizerAttributeTokenRule rule = rules[index];
            if (rule.GetElementNamespaceUri() == namespaceUri
                && rule.GetElementLocalName() == localName)
            {
                int existing = FindAttribute(result, count,
                    rule.GetAttributeNamespaceUri(),
                    rule.GetAttributeLocalName());
                string[] forced = rule.GetTokens();
                if (existing < 0)
                {
                    string name = CreateQualifiedName(
                        rule.GetAttributePrefix(),
                        rule.GetAttributeLocalName());
                    result[count] = HtmlAttribute.CreateNamespaced(name,
                        rule.GetAttributeLocalName(),
                        rule.GetAttributePrefix(),
                        rule.GetAttributeNamespaceUri(),
                        HtmlTokenLists.SerializeSpaceSeparated(forced),
                        HtmlSourceSpan.CreateUnknown());
                    count = count + 1;
                }
                else
                {
                    HtmlAttribute attribute = result[existing];
                    string value = MergeTokens(attribute.GetValue(), forced,
                        false);
                    result[existing] = CopyAttributeWithValue(attribute,
                        value);
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal static int ApplyLinkHardening(HtmlNode element, HtmlAttribute[] result,
        int count)
    {
        if (!IsHtmlLink(element))
        {
            return count;
        }

        int targetIndex = FindAttribute(result, count, "", "target");
        if (targetIndex < 0 || !IsBlankTarget(
            result[targetIndex].GetValue()))
        {
            return count;
        }

        string[] forced = new string[] { "noopener", "noreferrer" };
        int relIndex = FindAttribute(result, count, "", "rel");
        if (relIndex < 0)
        {
            result[count] = HtmlAttribute.Create("rel",
                HtmlTokenLists.SerializeSpaceSeparated(forced),
                HtmlSourceSpan.CreateUnknown());
            return count + 1;
        }

        HtmlAttribute rel = result[relIndex];
        result[relIndex] = CopyAttributeWithValue(rel,
            MergeTokens(rel.GetValue(), forced, true));
        return count;
    }

    internal static string MergeTokens(string value, string[] forced,
        bool removeOpener)
    {
        string[] current = HtmlTokenLists.ParseSpaceSeparated(value);
        string[] merged = new string[current.Length + forced.Length];
        int count = 0;
        int index = 0;
        while (index < current.Length)
        {
            string token = current[index];
            if ((!removeOpener || !Ascii.EqualsIgnoreCase(token.AsSpan(),
                    "opener".AsSpan()))
                && !HtmlTokenLists.ContainsAsciiInsensitive(merged, count,
                    token))
            {
                merged[count] = token;
                count = count + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < forced.Length)
        {
            if (!HtmlTokenLists.ContainsAsciiInsensitive(merged, count,
                forced[index]))
            {
                merged[count] = forced[index];
                count = count + 1;
            }

            index = index + 1;
        }

        string[] tokens = new string[count];
        Array.Copy(merged, tokens, count);
        return HtmlTokenLists.SerializeSpaceSeparated(tokens);
    }

    internal static HtmlAttribute CopyAttributeWithValue(HtmlAttribute attribute,
        string value)
    {
        return HtmlAttribute.CreateNamespaced(attribute.GetName(),
            attribute.GetLocalName(), attribute.GetPrefix(),
            attribute.GetNamespaceUri(), value, attribute.GetSourceSpan());
    }

    internal static bool WillHaveBlankTarget(HtmlNode element,
        HtmlAttribute[] original, HtmlSanitizerAttributeValueRule[] valueRules,
        HtmlSanitizerForcedAttributeRule[] legacyForced,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (!IsHtmlLink(element))
        {
            return false;
        }

        string value = "";
        bool hasValue = false;
        int originalIndex = FindAttribute(original, original.Length, "",
            "target");
        if (originalIndex >= 0 && policy.IsAttributeAllowedInNamespaces("", "target",
            element.GetNamespaceUriStorage(), element.GetLocalNameStorage()))
        {
            value = original[originalIndex].GetValue();
            hasValue = true;
        }

        string elementNamespace = element.GetNamespaceUriStorage();
        string elementLocal = HtmlSanitizerPolicyNames.NormalizeElementLocalName(
            elementNamespace, element.GetLocalNameStorage(), "element");
        int index = 0;
        while (index < valueRules.Length)
        {
            HtmlSanitizerAttributeValueRule rule = valueRules[index];
            if (rule.GetElementNamespaceUri() == elementNamespace
                && rule.GetElementLocalName() == elementLocal
                && rule.GetAttributeNamespaceUri().Length == 0
                && rule.GetAttributeLocalName() == "target"
                && (!hasValue || rule.IsForced()))
            {
                value = rule.GetValue();
                hasValue = true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < legacyForced.Length)
        {
            HtmlSanitizerForcedAttributeRule rule = legacyForced[index];
            if (rule.GetElementName() == elementLocal
                && rule.GetAttributeName() == "target")
            {
                value = rule.GetValue();
                hasValue = true;
            }

            index = index + 1;
        }

        return hasValue && IsBlankTarget(value);
    }

    internal static bool IsHtmlLink(HtmlNode element)
    {
        if (element.GetNamespaceUriStorage()
            != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            return false;
        }

        string localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(
            element.GetNamespaceUriStorage(), element.GetLocalNameStorage(),
            "element");
        return localName == "a" || localName == "area";
    }

    internal static bool IsRelAttribute(HtmlAttribute attribute)
    {
        return attribute.GetNamespaceUri().Length == 0
            && HtmlSanitizerPolicyNames.NormalizeAttributeLocalName("",
                attribute.GetLocalName(), "attribute") == "rel";
    }

    internal static bool IsBlankTarget(string value)
    {
        return Ascii.EqualsIgnoreCase(TrimHtmlWhitespace(value).AsSpan(),
            "_blank".AsSpan());
    }

    internal static int ApplyUrlPolicies(HtmlNode element, HtmlAttribute[] values,
        int count, HtmlCompiledSanitizerPolicy policy)
    {
        return ApplyUrlPolicies(element, values, count, policy,
            HtmlLimits.CreateDefault());
    }

    internal static int ApplyUrlPolicies(HtmlNode element,
        HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return ApplyUrlPolicies(element, values, count, policy, limits,
            CancellationToken.None);
    }

    internal static int ApplyUrlPolicies(HtmlNode element,
        HtmlAttribute[] values, int count,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        int inputIndex = 0;
        int outputIndex = 0;
        while (inputIndex < count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlAttribute attribute = values[inputIndex];
            HtmlUrlAttributeKind kind = HtmlDomUrls.GetAttributeKind(element,
                attribute);
            if (kind != HtmlUrlAttributeKind.None)
            {
                HtmlSanitizerUrlDecision decision =
                    HtmlSanitizerUrlAttributes.Evaluate(element, attribute,
                        policy, limits, cancellationToken);
                if (decision.IsAllowed())
                {
                    values[outputIndex] = HtmlAttribute.CreateNamespaced(
                        attribute.GetName(), attribute.GetLocalName(),
                        attribute.GetPrefix(), attribute.GetNamespaceUri(),
                        decision.GetValue(), attribute.GetSourceSpan());
                    outputIndex = outputIndex + 1;
                }
            }
            else
            {
                values[outputIndex] = attribute;
                outputIndex = outputIndex + 1;
            }

            inputIndex = inputIndex + 1;
        }

        return outputIndex;
    }

    internal static bool IsDisallowedCustomizedBuiltInAttribute(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        string namespaceUri = attribute.GetNamespaceUri();
        string localName =
            HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(
                namespaceUri, attribute.GetLocalName(), "attribute");
        return HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(
                namespaceUri, localName)
            && !policy.IsCustomizedBuiltInElementAllowed(
                element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage(), attribute.GetValue());
    }

    internal static HtmlSanitizerAttributeRule? FindRule(HtmlNode element,
        HtmlAttribute attribute, HtmlSanitizerAttributeRule[] rules,
        bool elementSpecific)
    {
        string elementNamespace = element.GetNamespaceUriStorage();
        string elementLocal = HtmlSanitizerPolicyNames.NormalizeElementLocalName(
            elementNamespace, element.GetLocalNameStorage(), "element");
        string attributeNamespace = attribute.GetNamespaceUri();
        string attributeLocal =
            HtmlSanitizerPolicyNames.NormalizeAttributeLocalName(
                attributeNamespace, attribute.GetLocalName(), "attribute");
        int index = 0;
        while (index < rules.Length)
        {
            HtmlSanitizerAttributeRule rule = rules[index];
            if (rule.IsElementSpecific() == elementSpecific
                && rule.GetAttributeNamespaceUri() == attributeNamespace
                && rule.GetAttributeLocalName() == attributeLocal
                && (!elementSpecific
                    || rule.GetElementNamespaceUri() == elementNamespace
                    && rule.GetElementLocalName() == elementLocal))
            {
                return rule;
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool IsLegacyElementAttributeAllowed(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        if (element.GetNamespaceUriStorage()
                != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri()
            || attribute.GetNamespaceUri().Length != 0)
        {
            return false;
        }

        string elementName =
            HtmlSanitizerPolicyNames.NormalizeElementLocalName(
                element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage(), "element");
        string attributeName =
            HtmlSanitizerPolicyNames.NormalizeAttributeLocalName("",
                attribute.GetLocalName(), "attribute");
        HtmlSanitizerElementAttributeRule[] rules =
            policy.GetElementAttributeRules();
        int index = 0;
        while (index < rules.Length)
        {
            if (rules[index].GetElementName() == elementName
                && rules[index].GetAttributeName() == attributeName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int ApplyRule(HtmlAttribute[] result, int count,
        HtmlAttribute attribute, HtmlSanitizerAttributeRule rule,
        HtmlNode element, HtmlCompiledSanitizerPolicy policy)
    {
        HtmlSanitizerAttributeAction action = rule.GetAction();
        if (action == HtmlSanitizerAttributeAction.Drop)
        {
            return count;
        }

        if (action == HtmlSanitizerAttributeAction.Keep)
        {
            return AddIfUnique(result, count, attribute);
        }

        if (action == HtmlSanitizerAttributeAction.Normalize)
        {
            HtmlAttribute normalized = HtmlAttribute.CreateNamespaced(
                attribute.GetName(), attribute.GetLocalName(),
                attribute.GetPrefix(), attribute.GetNamespaceUri(),
                NormalizeValue(attribute.GetValue(), rule.GetNormalization()),
                attribute.GetSourceSpan());
            return AddIfUnique(result, count, normalized);
        }

        if (action == HtmlSanitizerAttributeAction.Validate)
        {
            return IsValidAttributeValue(attribute.GetValue(),
                rule.GetValidation())
                ? AddIfUnique(result, count, attribute) : count;
        }

        if (action == HtmlSanitizerAttributeAction.Prefix)
        {
            string value = attribute.GetValue();
            string prefix = rule.GetValuePrefix();
            string prefixed = value.StartsWith(prefix,
                StringComparison.Ordinal) ? value : prefix + value;
            HtmlAttribute transformed = HtmlAttribute.CreateNamespaced(
                attribute.GetName(), attribute.GetLocalName(),
                attribute.GetPrefix(), attribute.GetNamespaceUri(), prefixed,
                attribute.GetSourceSpan());
            return AddIfUnique(result, count, transformed);
        }

        string name = CreateQualifiedName(rule.GetTargetPrefix(),
            rule.GetTargetLocalName());
        if (HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(
            rule.GetTargetNamespaceUri(), rule.GetTargetLocalName())
            && !policy.IsCustomizedBuiltInElementAllowed(
                element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage(), attribute.GetValue()))
        {
            return count;
        }

        HtmlAttribute renamed = HtmlAttribute.CreateNamespaced(name,
            rule.GetTargetLocalName(), rule.GetTargetPrefix(),
            rule.GetTargetNamespaceUri(), attribute.GetValue(),
            attribute.GetSourceSpan());
        return AddIfUnique(result, count, renamed);
    }

    internal static bool IsValidAttributeValue(string value,
        HtmlSanitizerAttributeValidation validation)
    {
        HtmlSanitizerPolicyNames.ValidateAttributeValidation(validation,
            "validation");
        if (validation == HtmlSanitizerAttributeValidation.NonEmpty)
        {
            return TrimHtmlWhitespace(value).Length > 0;
        }

        if (validation == HtmlSanitizerAttributeValidation.SingleToken)
        {
            if (value.Length == 0)
            {
                return false;
            }

            int tokenIndex = 0;
            while (tokenIndex < value.Length)
            {
                char current = value[tokenIndex];
                if (Ascii.IsHtmlWhitespace(current) || current < 32
                    || current == 127)
                {
                    return false;
                }

                tokenIndex = tokenIndex + 1;
            }

            return true;
        }

        if (validation == HtmlSanitizerAttributeValidation.Integer)
        {
            int integerIndex = value.Length > 0
                && (value[0] == '+' || value[0] == '-') ? 1 : 0;
            if (integerIndex == value.Length)
            {
                return false;
            }

            while (integerIndex < value.Length)
            {
                if (!Ascii.IsDigit(value[integerIndex]))
                {
                    return false;
                }

                integerIndex = integerIndex + 1;
            }

            return value.Length > 0;
        }

        HtmlLanguageTagResult language = HtmlLanguageTags.Parse(value,
            HtmlLimits.CreateDefault(), CancellationToken.None);
        return language.IsSuccess();
    }

    internal static string NormalizeValue(string value,
        HtmlSanitizerAttributeNormalization normalization)
    {
        switch (normalization)
        {
            case HtmlSanitizerAttributeNormalization.Trim:
                return TrimHtmlWhitespace(value);
            case HtmlSanitizerAttributeNormalization.LowercaseAscii:
                return FoldAscii(value);
            case HtmlSanitizerAttributeNormalization.CollapseHtmlWhitespace:
                return HtmlTokenLists.SerializeSpaceSeparated(
                    HtmlTokenLists.ParseSpaceSeparated(value));
            case HtmlSanitizerAttributeNormalization.SpaceSeparatedTokens:
                return HtmlTokenLists.SerializeSpaceSeparated(
                    HtmlTokenLists.ParseSpaceSeparated(value));
            default:
                throw new InvalidOperationException(
                    "A compiled policy has an unknown attribute normalization.");
        }
    }

    internal static int ApplyValueRules(HtmlNode element, HtmlAttribute[] result,
        int count, HtmlSanitizerAttributeValueRule[] rules,
        HtmlCompiledSanitizerPolicy policy)
    {
        string namespaceUri = element.GetNamespaceUriStorage();
        string localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(
            namespaceUri, element.GetLocalNameStorage(), "element");
        int index = 0;
        while (index < rules.Length)
        {
            HtmlSanitizerAttributeValueRule rule = rules[index];
            if (rule.GetElementNamespaceUri() == namespaceUri
                && rule.GetElementLocalName() == localName)
            {
                if (HtmlSanitizerMarkupNames.IsCustomizedBuiltInAttribute(
                    rule.GetAttributeNamespaceUri(),
                    rule.GetAttributeLocalName())
                    && !policy.IsCustomizedBuiltInElementAllowed(namespaceUri,
                        localName, rule.GetValue()))
                {
                    index = index + 1;
                    continue;
                }

                int existing = FindAttribute(result, count,
                    rule.GetAttributeNamespaceUri(),
                    rule.GetAttributeLocalName());
                if (existing < 0 || rule.IsForced())
                {
                    HtmlAttribute attribute = CreateValueAttribute(rule);
                    if (existing < 0)
                    {
                        result[count] = attribute;
                        count = count + 1;
                    }
                    else
                    {
                        result[existing] = attribute;
                    }
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal static int ApplyLegacyForcedRules(HtmlNode element,
        HtmlAttribute[] result, int count,
        HtmlSanitizerForcedAttributeRule[] rules,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (element.GetNamespaceUriStorage()
            != HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            return count;
        }

        string localName = HtmlSanitizerPolicyNames.NormalizeElementLocalName(
            element.GetNamespaceUriStorage(), element.GetLocalNameStorage(),
            "element");
        int index = 0;
        while (index < rules.Length)
        {
            HtmlSanitizerForcedAttributeRule rule = rules[index];
            if (rule.GetElementName() == localName)
            {
                if (rule.GetAttributeName() == "is"
                    && !policy.IsCustomizedBuiltInElementAllowed(
                        element.GetNamespaceUriStorage(), localName,
                        rule.GetValue()))
                {
                    index = index + 1;
                    continue;
                }

                HtmlAttribute attribute = HtmlAttribute.Create(
                    rule.GetAttributeName(), rule.GetValue(),
                    HtmlSourceSpan.CreateUnknown());
                int existing = FindAttribute(result, count, "",
                    rule.GetAttributeName());
                if (existing < 0)
                {
                    result[count] = attribute;
                    count = count + 1;
                }
                else
                {
                    result[existing] = attribute;
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal static HtmlAttribute CreateValueAttribute(
        HtmlSanitizerAttributeValueRule rule)
    {
        string name = CreateQualifiedName(rule.GetAttributePrefix(),
            rule.GetAttributeLocalName());
        return HtmlAttribute.CreateNamespaced(name,
            rule.GetAttributeLocalName(), rule.GetAttributePrefix(),
            rule.GetAttributeNamespaceUri(), rule.GetValue(),
            HtmlSourceSpan.CreateUnknown());
    }

    internal static string CreateQualifiedName(string prefix, string localName)
    {
        return prefix.Length == 0 ? localName : prefix + ":" + localName;
    }

    internal static int AddIfUnique(HtmlAttribute[] values, int count,
        HtmlAttribute value)
    {
        if (FindAttribute(values, count, value.GetNamespaceUri(),
            value.GetLocalName()) >= 0)
        {
            return count;
        }

        values[count] = value;
        return count + 1;
    }

    internal static int FindAttribute(HtmlAttribute[] values, int count,
        string namespaceUri, string localName)
    {
        int index = 0;
        while (index < count)
        {
            if (values[index].GetNamespaceUri() == namespaceUri
                && values[index].GetLocalName() == localName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static HtmlAttribute[] CopyAttributes(HtmlAttribute[] values, int count)
    {
        HtmlAttribute[] result = new HtmlAttribute[count];
        Array.Copy(values, result, count);
        return result;
    }

    internal static bool AttributesEqual(HtmlAttribute[] left, HtmlAttribute[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            HtmlAttribute first = left[index];
            HtmlAttribute second = right[index];
            if (first.GetName() != second.GetName()
                || first.GetLocalName() != second.GetLocalName()
                || first.GetPrefix() != second.GetPrefix()
                || first.GetNamespaceUri() != second.GetNamespaceUri()
                || first.GetValue() != second.GetValue()
                || first.GetSourceSpan() != second.GetSourceSpan())
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string TrimHtmlWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && Ascii.IsHtmlWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && Ascii.IsHtmlWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return value.Substring(start, end - start);
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string(result);
    }

    internal static void ValidateElementAndPolicy(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element)
        {
            throw new ArgumentException(
                "Attribute sanitization requires an element node.",
                "element");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }
}
