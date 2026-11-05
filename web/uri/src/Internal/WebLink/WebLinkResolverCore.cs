using System;

namespace uri;

internal static class WebLinkResolverCore
{
    internal const string InvalidRepresentationContextMessage = "The Web Link representation context is not a valid absolute URI.";
    internal const string InvalidTargetMessage = "The Web Link target is not a valid URI-reference.";
    internal const string UnresolvableTargetMessage = "The Web Link target cannot be resolved to an absolute URI without an effective context.";
    internal const string UnhonoredAnchorMessage = "The Web Link anchor cannot be honored with the supplied representation context.";

    internal static WebLinkResolutionResult[] ResolveWebLinks(
        WebLinkSet? linkSet,
        string? representationContext,
        OperationLimits? limits)
    {
        if (linkSet == null || linkSet.Links == null || limits == null)
        {
            return new WebLinkResolutionResult[]
            {
                CreateResolutionFailure(
                    new WebLink(),
                    "",
                    false,
                    "",
                    false,
                    DiagnosticFactory.CreateInvalidInput(
                        DiagnosticDomain.WebLink))
            };
        }

        WebLinkResolutionResult[] results =
            new WebLinkResolutionResult[linkSet.Links.Length];
        int index = 0;
        while (index < linkSet.Links.Length)
        {
            WebLink? link = linkSet.Links[index];
            UriDiagnostic? linkLimit = LimitGuard.CheckWebLinks(
                index + 1,
                limits,
                index);
            if (linkLimit != null)
            {
                results[index] = CreateResolutionFailure(
                    link ?? new WebLink(),
                    "",
                    false,
                    "",
                    false,
                    linkLimit);
            }
            else
            {
                results[index] = ResolveWebLink(
                    link,
                    representationContext,
                    limits);
            }

            index = index + 1;
        }

        return results;
    }

    internal static WebLinkResolutionResult ResolveWebLink(
        WebLink? link,
        string? representationContext,
        OperationLimits? limits)
    {
        if (link == null || link.Target == null || link.Anchor == null ||
            limits == null)
        {
            return CreateResolutionFailure(
                link ?? new WebLink(),
                "",
                false,
                "",
                false,
                DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.WebLink));
        }

        UriResolutionCoreResult representation =
            ValidateRepresentationContext(
                representationContext,
                limits);
        if (!representation.Success && representationContext != null)
        {
            return CreateResolutionFailure(
                link,
                "",
                false,
                "",
                !link.HasAnchor,
                representation.Diagnostic ??
                    CreateContextFailure());
        }

        UriReference? representationReference = null;
        string representationText = "";
        bool hasRepresentation = representationContext != null &&
            representation.Success;
        if (hasRepresentation)
        {
            representationReference = representation.Reference;
            representationText = representation.Text;
        }

        UriReference? effectiveContextReference =
            representationReference;
        string effectiveContextText = representationText;
        bool hasEffectiveContext = hasRepresentation;
        bool anchorHonored = true;
        UriDiagnostic? anchorFailure = null;

        if (link.HasAnchor)
        {
            UriResolutionCoreResult anchor = ResolveAnchor(
                link.Anchor,
                representationReference,
                limits);
            if (!anchor.Success)
            {
                anchorHonored = false;
                anchorFailure = anchor.Diagnostic ??
                    CreateAnchorFailure(0, 0);
                effectiveContextReference = null;
                effectiveContextText = "";
                hasEffectiveContext = false;
            }
            else
            {
                effectiveContextReference = anchor.Reference;
                effectiveContextText = anchor.Text;
                hasEffectiveContext = true;
            }
        }

        UriResolutionCoreResult target = ResolveTarget(
            link.Target,
            effectiveContextReference,
            limits);
        if (anchorFailure != null)
        {
            return CreateResolutionFailure(
                link,
                target.Success ? target.Text : "",
                false,
                "",
                false,
                anchorFailure);
        }

        if (!target.Success)
        {
            return CreateResolutionFailure(
                link,
                "",
                hasEffectiveContext,
                effectiveContextText,
                anchorHonored,
                target.Diagnostic ??
                    CreateTargetResolutionFailure());
        }

        return new WebLinkResolutionResult(
            true,
            link,
            target.Text,
            hasEffectiveContext,
            effectiveContextText,
            anchorHonored,
            null);
    }

    internal static UriResolutionCoreResult ValidateRepresentationContext(
        string? representationContext,
        OperationLimits limits)
    {
        if (representationContext == null)
        {
            return new UriResolutionCoreResult(
                true,
                new UriReference(),
                "",
                0,
                null);
        }

        UriValidationOptions options = CreateValidationOptions(limits);
        options.RequireAbsoluteUri = true;
        UriTextResult converted =
            WebLinkIriCore.ConvertContextIriToHttpUri(
                representationContext,
                limits,
                0);
        if (!converted.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapContextFailure(converted.Diagnostic));
        }

        UriValidationResult validation = UriValidatorCore.ValidateReference(
            converted.Text.AsSpan(),
            options);
        if (!validation.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapContextFailure(validation.Diagnostic));
        }

        UriCompositionResult composition =
            UriComposerCore.RecomposeParsedReference(
                validation.Reference,
                limits);
        if (!composition.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapOutputFailure(composition.Diagnostic));
        }

        return new UriResolutionCoreResult(
            true,
            validation.Reference,
            composition.Text,
            composition.Length,
            null);
    }

    internal static UriResolutionCoreResult ResolveAnchor(
        string anchorText,
        UriReference? representationContext,
        OperationLimits limits)
    {
        UriTextResult converted =
            WebLinkIriCore.ConvertContextIriToHttpUri(
                anchorText,
                limits,
                0);
        if (!converted.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapAnchorFailure(converted.Diagnostic));
        }

        UriValidationResult validation = ValidateReferenceText(
            converted.Text,
            limits);
        if (!validation.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapAnchorFailure(validation.Diagnostic));
        }

        if (!validation.Reference.HasScheme &&
            representationContext == null)
        {
            return UriResolverCore.CreateResolutionFailure(
                CreateAnchorFailure(0, anchorText.Length));
        }

        UriResolutionCoreResult resolved = ResolveParsedReference(
            validation.Reference,
            representationContext,
            limits);
        if (!resolved.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapAnchorFailure(resolved.Diagnostic));
        }

        return resolved;
    }

    internal static UriResolutionCoreResult ResolveTarget(
        string targetText,
        UriReference? effectiveContext,
        OperationLimits limits)
    {
        UriTextResult converted =
            WebLinkIriCore.ConvertTargetIriToHttpUri(
                targetText,
                limits,
                0);
        if (!converted.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapTargetFailure(converted.Diagnostic));
        }

        UriValidationResult validation = ValidateReferenceText(
            converted.Text,
            limits);
        if (!validation.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapTargetFailure(validation.Diagnostic));
        }

        if (!validation.Reference.HasScheme && effectiveContext == null)
        {
            return UriResolverCore.CreateResolutionFailure(
                CreateTargetResolutionFailure());
        }

        UriResolutionCoreResult resolved = ResolveParsedReference(
            validation.Reference,
            effectiveContext,
            limits);
        if (!resolved.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapTargetFailure(resolved.Diagnostic));
        }

        return resolved;
    }

    internal static UriResolutionCoreResult ResolveParsedReference(
        UriReference reference,
        UriReference? baseReference,
        OperationLimits limits)
    {
        UriReference target;
        if (reference.HasScheme)
        {
            target = UriResolverCore.TransformStrictReference(
                reference,
                reference);
        }
        else if (baseReference != null)
        {
            target = UriResolverCore.TransformStrictReference(
                baseReference,
                reference);
        }
        else
        {
            return UriResolverCore.CreateResolutionFailure(
                CreateTargetResolutionFailure());
        }

        if (!target.HasScheme)
        {
            return UriResolverCore.CreateResolutionFailure(
                CreateTargetResolutionFailure());
        }

        UriCompositionResult composition =
            UriComposerCore.RecomposeParsedReference(target, limits);
        if (!composition.Success)
        {
            return UriResolverCore.CreateResolutionFailure(
                MapOutputFailure(composition.Diagnostic));
        }

        return new UriResolutionCoreResult(
            true,
            target,
            composition.Text,
            composition.Length,
            null);
    }

    internal static UriValidationResult ValidateReferenceText(
        string text,
        OperationLimits limits)
    {
        return UriValidatorCore.ValidateReference(
            text.AsSpan(),
            CreateValidationOptions(limits));
    }

    internal static UriValidationOptions CreateValidationOptions(
        OperationLimits limits)
    {
        UriValidationOptions options = new UriValidationOptions();
        options.Limits = limits;
        return options;
    }

    internal static UriDiagnostic MapContextFailure(
        UriDiagnostic? diagnostic)
    {
        if (IsLimitFailure(diagnostic))
        {
            return MapOutputFailure(diagnostic);
        }

        int offset = 0;
        int length = 0;
        if (diagnostic != null)
        {
            offset = diagnostic.Range.Offset;
            length = diagnostic.Range.Length;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.LinkContext,
            offset,
            length,
            InvalidRepresentationContextMessage);
    }

    internal static UriDiagnostic MapAnchorFailure(
        UriDiagnostic? diagnostic)
    {
        if (IsLimitFailure(diagnostic))
        {
            return MapOutputFailure(diagnostic);
        }

        int offset = 0;
        int length = 0;
        if (diagnostic != null)
        {
            offset = diagnostic.Range.Offset;
            length = diagnostic.Range.Length;
        }

        return CreateAnchorFailure(offset, length);
    }

    internal static UriDiagnostic MapTargetFailure(
        UriDiagnostic? diagnostic)
    {
        if (IsLimitFailure(diagnostic))
        {
            return MapOutputFailure(diagnostic);
        }

        int offset = 0;
        int length = 0;
        if (diagnostic != null)
        {
            offset = diagnostic.Range.Offset;
            length = diagnostic.Range.Length;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            offset,
            length,
            InvalidTargetMessage);
    }

    internal static UriDiagnostic MapOutputFailure(
        UriDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.WebLink);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            diagnostic.Code,
            diagnostic.Component,
            diagnostic.Range.Offset,
            diagnostic.Range.Length,
            diagnostic.Message);
    }

    internal static bool IsLimitFailure(UriDiagnostic? diagnostic)
    {
        return diagnostic != null &&
            diagnostic.Code == UriDiagnosticCode.LimitExceeded;
    }

    internal static UriDiagnostic CreateContextFailure()
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.LinkContext,
            0,
            0,
            InvalidRepresentationContextMessage);
    }

    internal static UriDiagnostic CreateAnchorFailure(
        int offset,
        int length)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.UnhonoredAnchor,
            UriComponent.LinkContext,
            offset,
            length,
            UnhonoredAnchorMessage);
    }

    internal static UriDiagnostic CreateTargetResolutionFailure()
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.LinkTarget,
            0,
            0,
            UnresolvableTargetMessage);
    }

    internal static WebLinkResolutionResult CreateResolutionFailure(
        WebLink link,
        string target,
        bool hasContext,
        string context,
        bool anchorHonored,
        UriDiagnostic diagnostic)
    {
        return new WebLinkResolutionResult(
            false,
            link,
            target,
            hasContext,
            context,
            anchorHonored,
            diagnostic);
    }
}
