using System;

namespace uri;

internal static class UriRelativizerCore
{
    internal static UriRelativizationCoreResult RelativizeReferenceText(
        string? baseText,
        string? targetText,
        UriRelativizationOptions? options)
    {
        UriDiagnostic? optionsFailure = GetRelativizationOptionsDiagnostic(options);
        if (optionsFailure != null)
        {
            return CreateRelativizationFailure(optionsFailure);
        }

        if (baseText == null || targetText == null || options == null)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = options.Limits;
        UriValidationResult baseValidation = UriValidatorCore.ValidateReference(
            baseText.AsSpan(),
            validationOptions);
        if (!baseValidation.Success)
        {
            return CreateRelativizationFailure(baseValidation.Diagnostic);
        }

        if (!baseValidation.Reference.HasScheme)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateUnresolvableReference(UriComponent.Scheme));
        }

        UriValidationResult targetValidation = UriValidatorCore.ValidateReference(
            targetText.AsSpan(),
            validationOptions);
        if (!targetValidation.Success)
        {
            return CreateRelativizationFailure(targetValidation.Diagnostic);
        }

        if (!targetValidation.Reference.HasScheme)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateUnresolvableReference(UriComponent.Scheme));
        }

        return RelativizeReferences(
            baseValidation.Reference,
            targetValidation.Reference,
            options);
    }

    internal static UriRelativizationCoreResult RelativizeReferences(
        UriReference? baseReference,
        UriReference? target,
        UriRelativizationOptions? options)
    {
        UriDiagnostic? optionsFailure = GetRelativizationOptionsDiagnostic(options);
        if (optionsFailure != null)
        {
            return CreateRelativizationFailure(optionsFailure);
        }

        if (baseReference == null ||
            target == null ||
            options == null ||
            baseReference.Authority == null ||
            target.Authority == null ||
            baseReference.Path == null ||
            target.Path == null ||
            !baseReference.HasScheme ||
            !target.HasScheme)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        if (baseReference.HasFragment)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateUnresolvableReference(UriComponent.Fragment));
        }

        UriReference selectionBase = NormalizeForRelativizationSelection(
            baseReference,
            options);
        UriReference selectionTarget = NormalizeForRelativizationSelection(
            target,
            options);

        if (options.Mode == UriRelativizationMode.Shortest)
        {
            return SelectShortestReference(
                baseReference,
                target,
                selectionBase,
                selectionTarget,
                options);
        }

        if (options.Mode == UriRelativizationMode.AbsolutePath)
        {
            return SelectAbsolutePathReference(
                baseReference,
                target,
                selectionBase,
                selectionTarget,
                options);
        }

        if (options.Mode == UriRelativizationMode.NetworkPath)
        {
            return SelectNetworkPathReference(
                baseReference,
                target,
                selectionBase,
                selectionTarget,
                options);
        }

        if (options.Mode == UriRelativizationMode.AbsoluteUri)
        {
            return VerifyRelativizationCandidate(
                baseReference,
                target,
                CreateAbsoluteReference(selectionTarget),
                options);
        }

        return SelectRelativePathReference(
            baseReference,
            target,
            selectionBase,
            selectionTarget,
            options);
    }

    internal static UriReference CreateInverseReference(
        UriReference? baseReference,
        UriReference? target)
    {
        ValidateRelativizationInputs(baseReference, target);
        if (baseReference == null || target == null)
        {
            throw new ArgumentException("An absolute base URI and target URI are required.");
        }

        UriReference? candidate = CreateSameDocumentReference(
            baseReference,
            target);
        if (candidate != null)
        {
            return candidate;
        }

        candidate = CreateQueryReference(baseReference, target);
        if (candidate != null)
        {
            return candidate;
        }

        candidate = CreateRelativePathReference(baseReference, target);
        if (candidate != null)
        {
            return candidate;
        }

        candidate = CreateAbsolutePathReference(baseReference, target);
        if (candidate != null)
        {
            return candidate;
        }

        candidate = CreateNetworkPathReference(baseReference, target);
        if (candidate != null)
        {
            return candidate;
        }

        return CreateAbsoluteReference(target);
    }

    internal static UriRelativizationCoreResult SelectRelativePathReference(
        UriReference baseReference,
        UriReference target,
        UriReference selectionBase,
        UriReference selectionTarget,
        UriRelativizationOptions options)
    {
        UriRelativizationCoreResult result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateSameDocumentReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateQueryReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateRelativePathReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsolutePathReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateNetworkPathReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        return VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsoluteReference(selectionTarget),
            options);
    }

    internal static UriRelativizationCoreResult SelectAbsolutePathReference(
        UriReference baseReference,
        UriReference target,
        UriReference selectionBase,
        UriReference selectionTarget,
        UriRelativizationOptions options)
    {
        UriRelativizationCoreResult result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsolutePathReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateNetworkPathReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        return VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsoluteReference(selectionTarget),
            options);
    }

    internal static UriRelativizationCoreResult SelectNetworkPathReference(
        UriReference baseReference,
        UriReference target,
        UriReference selectionBase,
        UriReference selectionTarget,
        UriRelativizationOptions options)
    {
        UriRelativizationCoreResult result = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateNetworkPathReference(selectionBase, selectionTarget),
            options);
        if (result.Success)
        {
            return result;
        }

        return VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsoluteReference(selectionTarget),
            options);
    }

    internal static UriRelativizationCoreResult SelectShortestReference(
        UriReference baseReference,
        UriReference target,
        UriReference selectionBase,
        UriReference selectionTarget,
        UriRelativizationOptions options)
    {
        UriRelativizationCoreResult best = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateSameDocumentReference(selectionBase, selectionTarget),
            options);
        UriRelativizationCoreResult candidate = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateQueryReference(selectionBase, selectionTarget),
            options);
        best = ChooseShorterReference(best, candidate);

        candidate = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateRelativePathReference(selectionBase, selectionTarget),
            options);
        best = ChooseShorterReference(best, candidate);

        candidate = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsolutePathReference(selectionBase, selectionTarget),
            options);
        best = ChooseShorterReference(best, candidate);

        candidate = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateNetworkPathReference(selectionBase, selectionTarget),
            options);
        best = ChooseShorterReference(best, candidate);

        candidate = VerifyRelativizationCandidate(
            baseReference,
            target,
            CreateAbsoluteReference(selectionTarget),
            options);
        if (!best.Success)
        {
            return candidate;
        }

        return ChooseShorterReference(best, candidate);
    }

    internal static UriRelativizationCoreResult ChooseShorterReference(
        UriRelativizationCoreResult current,
        UriRelativizationCoreResult candidate)
    {
        if (!candidate.Success)
        {
            return current;
        }

        if (!current.Success || candidate.Length < current.Length)
        {
            return candidate;
        }

        return current;
    }

    internal static UriReference? CreateSameDocumentReference(
        UriReference baseReference,
        UriReference target)
    {
        if (!SchemesEqual(baseReference, target) ||
            !AuthoritiesEqual(baseReference, target) ||
            baseReference.Path != target.Path ||
            !QueriesEqual(baseReference, target))
        {
            return null;
        }

        UriReference candidate = CreateEmptyRelativeReference();
        CopyFragment(target, candidate);
        candidate.Kind = UriNormalizerCore.ClassifyNormalizedReference(
            candidate,
            UriReferenceKind.RelativeReference);
        return candidate;
    }

    internal static UriReference? CreateQueryReference(
        UriReference baseReference,
        UriReference target)
    {
        if (!SchemesEqual(baseReference, target) ||
            !AuthoritiesEqual(baseReference, target) ||
            baseReference.Path != target.Path ||
            !target.HasQuery ||
            QueriesEqual(baseReference, target))
        {
            return null;
        }

        UriReference candidate = CreateEmptyRelativeReference();
        CopyQueryAndFragment(target, candidate);
        candidate.Kind = UriReferenceKind.RelativeReference;
        return candidate;
    }

    internal static UriReference? CreateRelativePathReference(
        UriReference baseReference,
        UriReference target)
    {
        if (!SchemesEqual(baseReference, target) ||
            !AuthoritiesEqual(baseReference, target) ||
            target.Path.Length == 0)
        {
            return null;
        }

        string baseDirectory = GetBaseDirectory(baseReference);
        int boundary = FindCommonDirectoryBoundary(
            baseDirectory,
            target.Path);
        if (target.Path[0] != '/' &&
            baseDirectory.Length > 0 &&
            boundary == 0)
        {
            return null;
        }

        string relativePath = BuildRelativePath(
            baseDirectory,
            target.Path,
            boundary);
        relativePath = GuardRelativePath(relativePath);

        UriReference candidate = CreateEmptyRelativeReference();
        candidate.Path = relativePath;
        CopyQueryAndFragment(target, candidate);
        candidate.Kind = UriReferenceKind.RelativePathReference;
        return candidate;
    }

    internal static UriReference? CreateAbsolutePathReference(
        UriReference baseReference,
        UriReference target)
    {
        if (!SchemesEqual(baseReference, target) ||
            !AuthoritiesEqual(baseReference, target) ||
            target.Path.Length == 0 ||
            target.Path[0] != '/')
        {
            return null;
        }

        UriReference candidate = CreateEmptyRelativeReference();
        candidate.Path = target.Path;
        CopyQueryAndFragment(target, candidate);
        candidate.Kind = UriReferenceKind.AbsolutePathReference;
        return candidate;
    }

    internal static UriReference? CreateNetworkPathReference(
        UriReference baseReference,
        UriReference target)
    {
        if (!SchemesEqual(baseReference, target) || !target.HasAuthority)
        {
            return null;
        }

        UriReference candidate = CreateEmptyRelativeReference();
        candidate.HasAuthority = true;
        candidate.Authority = UriResolverCore.CloneAuthority(target.Authority);
        candidate.Path = target.Path;
        CopyQueryAndFragment(target, candidate);
        candidate.Kind = UriReferenceKind.NetworkPathReference;
        return candidate;
    }

    internal static UriReference CreateAbsoluteReference(UriReference target)
    {
        UriReference candidate = new UriReference();
        candidate.HasScheme = target.HasScheme;
        candidate.Scheme = target.Scheme;
        candidate.HasAuthority = target.HasAuthority;
        candidate.Authority = UriResolverCore.CloneAuthority(target.Authority);
        candidate.Path = target.Path;
        candidate.HasQuery = target.HasQuery;
        candidate.Query = target.Query;
        candidate.HasFragment = target.HasFragment;
        candidate.Fragment = target.Fragment;
        candidate.OriginalText = "";
        candidate.Kind = target.Kind;
        return candidate;
    }

    internal static string GetBaseDirectory(UriReference baseReference)
    {
        if (baseReference.HasAuthority && baseReference.Path.Length == 0)
        {
            return "/";
        }

        int slash = baseReference.Path.LastIndexOf('/');
        if (slash < 0)
        {
            return "";
        }

        return baseReference.Path.Substring(0, slash + 1);
    }

    internal static int FindCommonDirectoryBoundary(
        string baseDirectory,
        string targetPath)
    {
        int limit = baseDirectory.Length;
        if (targetPath.Length < limit)
        {
            limit = targetPath.Length;
        }

        int offset = 0;
        while (offset < limit &&
            baseDirectory[offset] == targetPath[offset])
        {
            offset = offset + 1;
        }

        if (offset == baseDirectory.Length)
        {
            return offset;
        }

        if (offset == targetPath.Length &&
            offset > 0 &&
            targetPath[offset - 1] == '/')
        {
            return offset;
        }

        while (offset > 0 && targetPath[offset - 1] != '/')
        {
            offset = offset - 1;
        }

        return offset;
    }

    internal static string BuildRelativePath(
        string baseDirectory,
        string targetPath,
        int commonBoundary)
    {
        string relativePath = "";
        int offset = commonBoundary;
        while (offset < baseDirectory.Length)
        {
            if (baseDirectory[offset] == '/')
            {
                relativePath = relativePath + "../";
            }

            offset = offset + 1;
        }

        relativePath = relativePath + targetPath.Substring(commonBoundary);
        if (relativePath.Length == 0)
        {
            return "./";
        }

        return relativePath;
    }

    internal static string GuardRelativePath(string path)
    {
        if (path.Length == 0)
        {
            return "./";
        }

        if (path[0] == '/')
        {
            return "./" + path;
        }

        int offset = 0;
        while (offset < path.Length && path[offset] != '/')
        {
            if (path[offset] == ':')
            {
                return "./" + path;
            }

            offset = offset + 1;
        }

        return path;
    }

    internal static bool SchemesEqual(
        UriReference left,
        UriReference right)
    {
        return left.HasScheme == right.HasScheme &&
            (!left.HasScheme || left.Scheme == right.Scheme);
    }

    internal static bool AuthoritiesEqual(
        UriReference left,
        UriReference right)
    {
        if (left.HasAuthority != right.HasAuthority)
        {
            return false;
        }

        if (!left.HasAuthority)
        {
            return true;
        }

        UriAuthority leftAuthority = left.Authority;
        UriAuthority rightAuthority = right.Authority;
        return leftAuthority.HasUserInfo == rightAuthority.HasUserInfo &&
            (!leftAuthority.HasUserInfo ||
                leftAuthority.UserInfo == rightAuthority.UserInfo) &&
            leftAuthority.HostKind == rightAuthority.HostKind &&
            leftAuthority.Host == rightAuthority.Host &&
            leftAuthority.HasPort == rightAuthority.HasPort &&
            (!leftAuthority.HasPort ||
                leftAuthority.Port == rightAuthority.Port);
    }

    internal static bool QueriesEqual(
        UriReference left,
        UriReference right)
    {
        return left.HasQuery == right.HasQuery &&
            (!left.HasQuery || left.Query == right.Query);
    }

    internal static void CopyQueryAndFragment(
        UriReference source,
        UriReference target)
    {
        target.HasQuery = source.HasQuery;
        target.Query = source.Query;
        CopyFragment(source, target);
    }

    internal static void CopyFragment(
        UriReference source,
        UriReference target)
    {
        target.HasFragment = source.HasFragment;
        target.Fragment = source.Fragment;
    }

    internal static UriReference CreateEmptyRelativeReference()
    {
        UriReference candidate = new UriReference();
        candidate.OriginalText = "";
        return candidate;
    }

    internal static UriRelativizationCoreResult VerifyRelativizationCandidate(
        UriReference baseReference,
        UriReference target,
        UriReference? candidate,
        UriRelativizationOptions options)
    {
        if (candidate == null)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateUnresolvableReference(UriComponent.Input));
        }

        UriCompositionResult composition = UriComposerCore.RecomposeParsedReference(
            candidate,
            options.Limits);
        if (!composition.Success)
        {
            return CreateRelativizationFailure(composition.Diagnostic);
        }

        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = options.Limits;
        UriValidationResult reparsed = UriValidatorCore.ValidateReference(
            composition.Text.AsSpan(),
            validationOptions);
        if (!reparsed.Success)
        {
            return CreateRelativizationFailure(reparsed.Diagnostic);
        }

        UriReference resolved = UriResolverCore.TransformStrictReference(
            baseReference,
            reparsed.Reference);
        UriComparisonCoreResult comparison = CompareRelativizationRoundTrip(
            resolved,
            target,
            options);
        if (!comparison.Equal)
        {
            return CreateRelativizationFailure(
                DiagnosticFactory.CreateUnresolvableReference(
                    comparison.DifferentComponent));
        }

        return new UriRelativizationCoreResult(
            true,
            candidate,
            composition.Text,
            composition.Length,
            null);
    }

    internal static UriComparisonCoreResult CompareRelativizationRoundTrip(
        UriReference resolved,
        UriReference target,
        UriRelativizationOptions options)
    {
        if (options.ComparisonLevel == UriComparisonLevel.Exact)
        {
            return UriComparerCore.CompareExact(resolved, target);
        }

        if (options.ComparisonLevel == UriComparisonLevel.Syntax)
        {
            return UriComparerCore.CompareSyntax(resolved, target);
        }

        if (options.Profile == null)
        {
            return UriComparerCore.CompareWithBuiltInSchemeProfiles(
                resolved,
                target);
        }

        return UriComparerCore.CompareWithSchemeProfile(
            resolved,
            target,
            options.Profile);
    }

    internal static UriReference NormalizeForRelativizationSelection(
        UriReference reference,
        UriRelativizationOptions options)
    {
        if (options.ComparisonLevel == UriComparisonLevel.Exact)
        {
            return reference;
        }

        if (options.ComparisonLevel == UriComparisonLevel.Syntax)
        {
            return UriNormalizerCore.NormalizeSyntaxReference(reference);
        }

        if (options.Profile == null)
        {
            return UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(reference);
        }

        return UriNormalizerCore.NormalizeWithSchemeProfile(
            reference,
            options.Profile);
    }

    internal static UriDiagnostic? GetRelativizationOptionsDiagnostic(
        UriRelativizationOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        if (options.Mode != UriRelativizationMode.RelativePath &&
            options.Mode != UriRelativizationMode.Shortest &&
            options.Mode != UriRelativizationMode.AbsolutePath &&
            options.Mode != UriRelativizationMode.NetworkPath &&
            options.Mode != UriRelativizationMode.AbsoluteUri)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        if (options.ComparisonLevel != UriComparisonLevel.Exact &&
            options.ComparisonLevel != UriComparisonLevel.Syntax &&
            options.ComparisonLevel != UriComparisonLevel.SchemeProfile)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        if (options.ComparisonLevel == UriComparisonLevel.SchemeProfile &&
            options.Profile != null &&
            !UriNormalizerCore.IsValidSchemeProfile(options.Profile))
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        return null;
    }

    internal static UriRelativizationCoreResult CreateRelativizationFailure(
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

        return new UriRelativizationCoreResult(
            false,
            new UriReference(),
            "",
            0,
            failure);
    }

    internal static void ValidateRelativizationInputs(
        UriReference? baseReference,
        UriReference? target)
    {
        if (baseReference == null ||
            target == null ||
            baseReference.Authority == null ||
            target.Authority == null ||
            baseReference.Path == null ||
            target.Path == null ||
            !baseReference.HasScheme ||
            !target.HasScheme)
        {
            throw new ArgumentException("An absolute base URI and target URI are required.");
        }
    }
}
