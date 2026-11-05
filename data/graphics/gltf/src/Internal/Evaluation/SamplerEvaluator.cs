using System;

namespace gltf;

internal class SamplerEvaluation
{
    internal bool UsesDefaultSampler;
    internal bool AutomaticMagnificationFilter = true;
    internal bool AutomaticMinificationFilter = true;
    internal GltfSamplerFilter MagnificationFilter = GltfSamplerFilter.Linear;
    internal GltfSamplerFilter MinificationFilter = GltfSamplerFilter.Linear;
    internal GltfSamplerWrap WrapS = GltfSamplerWrap.Repeat;
    internal GltfSamplerWrap WrapT = GltfSamplerWrap.Repeat;
    internal bool MagnificationFilterValid = true;
    internal bool MinificationFilterValid = true;
    internal bool WrapSValid = true;
    internal bool WrapTValid = true;
    internal bool Valid = true;
    internal bool MinificationUsesMipmaps;
}

internal class SamplerEvaluator
{
    internal SamplerEvaluation EvaluateSampler(GltfSampler? sampler)
    {
        SamplerEvaluation result = new SamplerEvaluation();
        if (sampler == null)
        {
            result.UsesDefaultSampler = true;
            return result;
        }

        if (sampler.HasMagnificationFilter)
        {
            result.AutomaticMagnificationFilter = false;
            result.MagnificationFilter = sampler.MagnificationFilter;
            result.MagnificationFilterValid = IsLegalMagnificationFilter(
                sampler.MagnificationFilter);
        }

        if (sampler.HasMinificationFilter)
        {
            result.AutomaticMinificationFilter = false;
            result.MinificationFilter = sampler.MinificationFilter;
            result.MinificationFilterValid = IsLegalMinificationFilter(
                sampler.MinificationFilter);
            if (result.MinificationFilterValid)
            {
                result.MinificationUsesMipmaps = IsMipmapMinificationFilter(
                    sampler.MinificationFilter);
            }
        }

        if (sampler.HasWrapS)
        {
            result.WrapS = sampler.WrapS;
            result.WrapSValid = IsLegalSamplerWrap(sampler.WrapS);
        }

        if (sampler.HasWrapT)
        {
            result.WrapT = sampler.WrapT;
            result.WrapTValid = IsLegalSamplerWrap(sampler.WrapT);
        }

        result.Valid = result.MagnificationFilterValid
            && result.MinificationFilterValid
            && result.WrapSValid
            && result.WrapTValid;
        return result;
    }

    internal GltfValidationReport ValidateDocumentSamplers(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        if (document.Samplers == null)
        {
            AddSamplerIssue(
                report,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The sampler collection is missing.",
                "/samplers");
            return report;
        }

        int index = 0;
        while (index < document.Samplers.Count)
        {
            GltfSampler sampler = document.Samplers[index];
            string pointer = $"/samplers/{index}";
            if (sampler == null)
            {
                AddSamplerIssue(
                    report,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The sampler is null.",
                    pointer);
            }
            else
            {
                ValidateSamplerFields(sampler, pointer, report);
            }

            index++;
        }

        return report;
    }

    internal void ValidateSamplerFields(
        GltfSampler sampler,
        string pointer,
        GltfValidationReport report)
    {
        if (sampler.HasMagnificationFilter
            && !IsLegalMagnificationFilter(sampler.MagnificationFilter))
        {
            AddSamplerIssue(
                report,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Warning,
                $"Invalid magnification filter value {(int)sampler.MagnificationFilter}.",
                $"{pointer}/magFilter");
        }

        if (sampler.HasMinificationFilter
            && !IsLegalMinificationFilter(sampler.MinificationFilter))
        {
            AddSamplerIssue(
                report,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Warning,
                $"Invalid minification filter value {(int)sampler.MinificationFilter}.",
                $"{pointer}/minFilter");
        }

        if (sampler.HasWrapS && !IsLegalSamplerWrap(sampler.WrapS))
        {
            AddSamplerIssue(
                report,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Warning,
                $"Invalid S wrapping value {(int)sampler.WrapS}.",
                $"{pointer}/wrapS");
        }

        if (sampler.HasWrapT && !IsLegalSamplerWrap(sampler.WrapT))
        {
            AddSamplerIssue(
                report,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Warning,
                $"Invalid T wrapping value {(int)sampler.WrapT}.",
                $"{pointer}/wrapT");
        }
    }

    internal bool IsLegalMagnificationFilter(GltfSamplerFilter filter)
    {
        return filter == GltfSamplerFilter.Nearest
            || filter == GltfSamplerFilter.Linear;
    }

    internal bool IsLegalMinificationFilter(GltfSamplerFilter filter)
    {
        switch (filter)
        {
            case GltfSamplerFilter.Nearest:
            case GltfSamplerFilter.Linear:
            case GltfSamplerFilter.NearestMipmapNearest:
            case GltfSamplerFilter.LinearMipmapNearest:
            case GltfSamplerFilter.NearestMipmapLinear:
            case GltfSamplerFilter.LinearMipmapLinear:
                return true;
            default:
                return false;
        }
    }

    internal bool IsMipmapMinificationFilter(GltfSamplerFilter filter)
    {
        return filter == GltfSamplerFilter.NearestMipmapNearest
            || filter == GltfSamplerFilter.LinearMipmapNearest
            || filter == GltfSamplerFilter.NearestMipmapLinear
            || filter == GltfSamplerFilter.LinearMipmapLinear;
    }

    internal bool IsLegalSamplerWrap(GltfSamplerWrap wrap)
    {
        return wrap == GltfSamplerWrap.ClampToEdge
            || wrap == GltfSamplerWrap.MirroredRepeat
            || wrap == GltfSamplerWrap.Repeat;
    }

    internal void AddSamplerIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
