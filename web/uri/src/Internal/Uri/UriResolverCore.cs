using System;

namespace uri;

internal static class UriResolverCore
{
    internal static UriResolutionCoreResult ResolveReferenceText(
        string? baseText,
        string? referenceText,
        UriResolutionOptions? options)
    {
        if (baseText == null ||
            referenceText == null ||
            options == null ||
            options.Limits == null ||
            (options.Mode != UriResolutionMode.Strict &&
                options.Mode != UriResolutionMode.Compatible))
        {
            return CreateResolutionFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = options.Limits;
        UriValidationResult baseValidation = UriValidatorCore.ValidateReference(
            baseText.AsSpan(),
            validationOptions);
        if (!baseValidation.Success)
        {
            return CreateResolutionFailure(baseValidation.Diagnostic);
        }

        UriReference baseReference = baseValidation.Reference;
        if (!baseReference.HasScheme)
        {
            return CreateResolutionFailure(
                DiagnosticFactory.CreateUnresolvableReference(UriComponent.Scheme));
        }

        if (baseReference.HasFragment)
        {
            return CreateResolutionFailure(
                DiagnosticFactory.CreateUnresolvableReference(UriComponent.Fragment));
        }

        UriValidationResult referenceValidation = UriValidatorCore.ValidateReference(
            referenceText.AsSpan(),
            validationOptions);
        if (!referenceValidation.Success)
        {
            return CreateResolutionFailure(referenceValidation.Diagnostic);
        }

        UriReference target = TransformReferenceWithMode(
            baseReference,
            referenceValidation.Reference,
            options.Mode);
        UriCompositionResult composed = UriComposerCore.RecomposeParsedReference(
            target,
            options.Limits);
        if (!composed.Success)
        {
            return CreateResolutionFailure(composed.Diagnostic);
        }

        return new UriResolutionCoreResult(
            true,
            target,
            composed.Text,
            composed.Length,
            null);
    }

    internal static UriReference TransformReferenceWithMode(
        UriReference? baseReference,
        UriReference? reference,
        UriResolutionMode mode)
    {
        if (mode == UriResolutionMode.Strict)
        {
            return TransformStrictReference(baseReference, reference);
        }

        if (mode == UriResolutionMode.Compatible)
        {
            return TransformCompatibleReference(baseReference, reference);
        }

        throw new ArgumentException("The URI resolution mode is invalid.");
    }

    internal static UriReference TransformCompatibleReference(
        UriReference? baseReference,
        UriReference? reference)
    {
        if (baseReference == null || reference == null)
        {
            throw new ArgumentException("A base URI and URI reference are required.");
        }

        if (!baseReference.HasScheme ||
            !reference.HasScheme ||
            !AsciiCase.EqualsIgnoreCase(baseReference.Scheme, reference.Scheme))
        {
            return TransformStrictReference(baseReference, reference);
        }

        UriReference schemeLessReference = CloneWithoutScheme(reference);
        return TransformStrictReference(baseReference, schemeLessReference);
    }

    internal static string MergePaths(
        UriReference? baseReference,
        string? referencePath)
    {
        if (baseReference == null ||
            baseReference.Path == null ||
            referencePath == null)
        {
            throw new ArgumentException("A base reference and relative path are required.");
        }

        if (baseReference.HasAuthority && baseReference.Path.Length == 0)
        {
            return "/" + referencePath;
        }

        int lastSlash = baseReference.Path.LastIndexOf('/');
        if (lastSlash < 0)
        {
            return referencePath;
        }

        return baseReference.Path.Substring(0, lastSlash + 1) + referencePath;
    }

    internal static UriReference TransformStrictReference(
        UriReference? baseReference,
        UriReference? reference)
    {
        if (baseReference == null ||
            reference == null ||
            baseReference.Authority == null ||
            reference.Authority == null ||
            baseReference.Path == null ||
            reference.Path == null)
        {
            throw new ArgumentException("A base URI and URI reference are required.");
        }

        UriReference target = new UriReference();
        if (reference.HasScheme)
        {
            target.HasScheme = true;
            target.Scheme = reference.Scheme;
            target.HasAuthority = reference.HasAuthority;
            target.Authority = CloneAuthority(reference.Authority);
            target.Path = PathAlgorithm.RemoveDotSegments(reference.Path.AsSpan());
            CopyQuery(reference, target);
        }
        else
        {
            target.HasScheme = baseReference.HasScheme;
            target.Scheme = baseReference.Scheme;
            if (reference.HasAuthority)
            {
                target.HasAuthority = true;
                target.Authority = CloneAuthority(reference.Authority);
                target.Path = PathAlgorithm.RemoveDotSegments(reference.Path.AsSpan());
                CopyQuery(reference, target);
            }
            else
            {
                target.HasAuthority = baseReference.HasAuthority;
                target.Authority = CloneAuthority(baseReference.Authority);
                if (reference.Path.Length == 0)
                {
                    target.Path = baseReference.Path;
                    if (reference.HasQuery)
                    {
                        CopyQuery(reference, target);
                    }
                    else
                    {
                        CopyQuery(baseReference, target);
                    }
                }
                else
                {
                    if (reference.Path[0] == '/')
                    {
                        target.Path = PathAlgorithm.RemoveDotSegments(
                            reference.Path.AsSpan());
                    }
                    else
                    {
                        string mergedPath = MergePaths(
                            baseReference,
                            reference.Path);
                        target.Path = PathAlgorithm.RemoveDotSegments(
                            mergedPath.AsSpan());
                    }

                    CopyQuery(reference, target);
                }
            }
        }

        target.HasFragment = reference.HasFragment;
        if (reference.HasFragment)
        {
            target.Fragment = reference.Fragment;
        }

        target.Kind = UriReferenceKind.Uri;
        target.OriginalText = "";
        return target;
    }

    internal static UriAuthority CloneAuthority(UriAuthority? source)
    {
        if (source == null || source.AddressBytes == null)
        {
            throw new ArgumentException("A URI authority is required.");
        }

        UriAuthority copy = new UriAuthority();
        copy.HasUserInfo = source.HasUserInfo;
        copy.UserInfo = source.UserInfo;
        copy.HostKind = source.HostKind;
        copy.Host = source.Host;
        copy.AddressBytes = new byte[source.AddressBytes.Length];
        int offset = 0;
        while (offset < source.AddressBytes.Length)
        {
            copy.AddressBytes[offset] = source.AddressBytes[offset];
            offset = offset + 1;
        }

        copy.HasPort = source.HasPort;
        copy.Port = source.Port;
        return copy;
    }

    internal static UriReference CloneWithoutScheme(UriReference source)
    {
        UriReference copy = new UriReference();
        copy.HasScheme = false;
        copy.Scheme = "";
        copy.HasAuthority = source.HasAuthority;
        copy.Authority = CloneAuthority(source.Authority);
        copy.Path = source.Path;
        copy.HasQuery = source.HasQuery;
        copy.Query = source.Query;
        copy.HasFragment = source.HasFragment;
        copy.Fragment = source.Fragment;
        copy.OriginalText = source.OriginalText;
        copy.Kind = UriNormalizerCore.ClassifyNormalizedReference(
            copy,
            source.Kind);
        return copy;
    }

    internal static void CopyQuery(UriReference source, UriReference target)
    {
        target.HasQuery = source.HasQuery;
        if (source.HasQuery)
        {
            target.Query = source.Query;
        }
        else
        {
            target.Query = "";
        }
    }

    internal static UriResolutionCoreResult CreateResolutionFailure(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriResolutionCoreResult(
            false,
            new UriReference(),
            "",
            0,
            failure);
    }
}
