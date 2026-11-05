using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace html;

internal static class HtmlSanitizerUrlAttributes
{
    internal static HtmlSanitizerUrlDecision Evaluate(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        return Evaluate(element, attribute, policy,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision Evaluate(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy,
        HtmlLimits limits)
    {
        return Evaluate(element, attribute, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision Evaluate(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        Validate(element, attribute, policy);
        HtmlSanitizerUrls.ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlUrlAttributeKind kind = HtmlDomUrls.GetAttributeKind(element,
            attribute);
        switch (kind)
        {
            case HtmlUrlAttributeKind.Single:
                return HtmlSanitizerUrls.Evaluate(element, attribute, policy,
                    limits, cancellationToken);
            case HtmlUrlAttributeKind.SpaceSeparated:
                return EvaluateSpaceSeparated(element, attribute, policy,
                    limits, cancellationToken);
            case HtmlUrlAttributeKind.SourceSet:
                return EvaluateSourceSet(element, attribute, policy, limits,
                    cancellationToken);
            case HtmlUrlAttributeKind.Refresh:
                return EvaluateRefresh(element, attribute, policy, limits,
                    cancellationToken);
            default:
                return new HtmlSanitizerUrlDecision(false, "", false);
        }
    }

    internal static HtmlSanitizerUrlDecision EvaluateSpaceSeparated(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        return EvaluateSpaceSeparated(element, attribute, policy,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision EvaluateSpaceSeparated(
        HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return EvaluateSpaceSeparated(element, attribute, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision EvaluateSpaceSeparated(
        HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        string[] values = HtmlTokenLists.ParseSpaceSeparated(
            attribute.GetValue());
        List<string> accepted = new List<string>();
        int index = 0;
        while (index < values.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlSanitizerUrlDecision decision =
                HtmlSanitizerUrls.EvaluateValue(element, attribute,
                    values[index], policy, limits, cancellationToken);
            if (decision.IsAllowed())
            {
                accepted.Add(decision.GetValue());
            }

            index = index + 1;
        }

        if (accepted.Count == 0)
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        string result = HtmlTokenLists.SerializeSpaceSeparated(
            accepted.ToArray());
        return new HtmlSanitizerUrlDecision(true, result,
            result != attribute.GetValue());
    }

    internal static HtmlSanitizerUrlDecision EvaluateSourceSet(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        return EvaluateSourceSet(element, attribute, policy,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision EvaluateSourceSet(
        HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return EvaluateSourceSet(element, attribute, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision EvaluateSourceSet(
        HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        HtmlSrcsetResult parsed = HtmlSrcset.Parse(attribute.GetValue(),
            limits, cancellationToken);
        if (!parsed.IsComplete())
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        HtmlSrcsetCandidate[] candidates = parsed.GetCandidates();
        List<HtmlSrcsetCandidate> accepted =
            new List<HtmlSrcsetCandidate>();
        int index = 0;
        while (index < candidates.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlSrcsetCandidate candidate = candidates[index];
            HtmlSanitizerUrlDecision decision =
                HtmlSanitizerUrls.EvaluateValue(element, attribute,
                    candidate.GetUrl(), policy, limits, cancellationToken);
            if (decision.IsAllowed())
            {
                accepted.Add(CopyCandidate(candidate, decision.GetValue()));
            }

            index = index + 1;
        }

        if (accepted.Count == 0)
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        string result = HtmlSrcset.Serialize(accepted.ToArray());
        return new HtmlSanitizerUrlDecision(true, result,
            result != attribute.GetValue());
    }

    internal static HtmlSrcsetCandidate CopyCandidate(HtmlSrcsetCandidate source,
        string url)
    {
        if (source.HasWidth())
        {
            return HtmlSrcsetCandidate.CreateWidth(url, source.GetWidth());
        }

        if (source.HasDensity())
        {
            return HtmlSrcsetCandidate.CreateDensity(url,
                source.GetDensity());
        }

        return HtmlSrcsetCandidate.Create(url);
    }

    internal static HtmlSanitizerUrlDecision EvaluateRefresh(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        return EvaluateRefresh(element, attribute, policy,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerUrlDecision EvaluateRefresh(
        HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return EvaluateRefresh(element, attribute, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerUrlDecision EvaluateRefresh(
        HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        HtmlRefreshResult parsed = HtmlRefresh.Parse(attribute.GetValue(),
            limits, cancellationToken);
        if (!parsed.IsSuccess() || parsed.WasLimitExceeded()
            || !parsed.HasUrl())
        {
            return new HtmlSanitizerUrlDecision(false, "", false);
        }

        HtmlSanitizerUrlDecision decision =
            HtmlSanitizerUrls.EvaluateValue(element, attribute,
                parsed.GetUrl(), policy, limits, cancellationToken);
        if (!decision.IsAllowed())
        {
            return decision;
        }

        string result = parsed.GetDelaySeconds().ToString(
            CultureInfo.InvariantCulture) + ";url=" + decision.GetValue();
        return new HtmlSanitizerUrlDecision(true, result,
            result != attribute.GetValue());
    }

    internal static void Validate(HtmlNode element, HtmlAttribute attribute,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }
    }
}
