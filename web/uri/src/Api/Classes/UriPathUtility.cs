using System;

namespace uri;

/// <summary>
/// Provides bounded URI path splitting, joining, common-base, and dot-segment operations.
/// </summary>
public class UriPathUtility : IUriPathUtility
{
    /// <summary>Initializes a stateless URI path utility.</summary>
    public UriPathUtility()
    {
    }

    /// <summary>Removes literal dot segments using the RFC 3986 algorithm.</summary>
    /// <param name="path">The URI path text.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The path with literal dot segments removed.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    public string RemoveDotSegments(string path, OperationLimits limits)
    {
        UriDiagnostic? inputFailure = GetSinglePathDiagnostic(path, limits);
        if (inputFailure != null)
        {
            throw CreateValidationException(inputFailure);
        }

        string result = UriPathUtilityCore.RemoveDotSegments(path);
        UriDiagnostic? outputFailure = CheckOutputLength(result.Length, limits);
        if (outputFailure != null)
        {
            throw CreateValidationException(outputFailure);
        }

        return result;
    }

    /// <summary>Joins path tokens while sharing one literal separator at each seam.</summary>
    /// <param name="paths">The URI path tokens to join.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The joined URI path.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    public string JoinPaths(string[] paths, OperationLimits limits)
    {
        UriDiagnostic? inputFailure = GetPathCollectionDiagnostic(paths, limits);
        if (inputFailure != null)
        {
            throw CreateValidationException(inputFailure);
        }

        long outputLength = UriPathUtilityCore.MeasureJoinedPath(paths);
        UriDiagnostic? outputFailure = CheckOutputLength(outputLength, limits);
        if (outputFailure != null)
        {
            throw CreateValidationException(outputFailure);
        }

        long segmentCount = UriPathUtilityCore.MeasureJoinedPathSegments(paths);
        UriDiagnostic? segmentFailure = CheckSegmentCount(segmentCount, limits);
        if (segmentFailure != null)
        {
            throw CreateValidationException(segmentFailure);
        }

        return UriPathUtilityCore.JoinPaths(paths);
    }

    /// <summary>Splits a path at literal separators while preserving empty segments.</summary>
    /// <param name="path">The URI path text.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The ordered path segments.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    public string[] SplitPathSegments(string path, OperationLimits limits)
    {
        UriDiagnostic? inputFailure = GetSinglePathDiagnostic(path, limits);
        if (inputFailure != null)
        {
            throw CreateValidationException(inputFailure);
        }

        int outputLength = path.Length - PathParser.CountSlashes(path.AsSpan());
        UriDiagnostic? outputFailure = CheckOutputLength(outputLength, limits);
        if (outputFailure != null)
        {
            throw CreateValidationException(outputFailure);
        }

        return UriPathUtilityCore.SplitPathSegments(path);
    }

    /// <summary>Finds the longest ordinal shared directory prefix.</summary>
    /// <param name="firstPath">The first URI path.</param>
    /// <param name="secondPath">The second URI path.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The shared base directory, ending in a literal separator when nonempty.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    public string FindCommonBasePath(
        string firstPath,
        string secondPath,
        OperationLimits limits)
    {
        string[] paths = new string[] { firstPath, secondPath };
        UriDiagnostic? inputFailure = GetPathCollectionDiagnostic(paths, limits);
        if (inputFailure != null)
        {
            throw CreateValidationException(inputFailure);
        }

        string result = UriPathUtilityCore.FindCommonBasePath(
            firstPath,
            secondPath);
        UriDiagnostic? outputFailure = CheckOutputLength(result.Length, limits);
        if (outputFailure != null)
        {
            throw CreateValidationException(outputFailure);
        }

        return result;
    }

    internal UriDiagnostic? GetSinglePathDiagnostic(
        string? path,
        OperationLimits? limits)
    {
        if (path == null || limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            path.Length,
            limits,
            DiagnosticDomain.Uri);
        if (inputLimit != null)
        {
            return inputLimit;
        }

        return GetPathContentDiagnostic(path, limits);
    }

    internal UriDiagnostic? GetPathCollectionDiagnostic(
        string[]? paths,
        OperationLimits? limits)
    {
        if (paths == null || limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        long inputLength = 0;
        int offset = 0;
        while (offset < paths.Length)
        {
            string? path = paths[offset];
            if (path == null)
            {
                return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
            }

            inputLength = inputLength + path.Length;
            offset = offset + 1;
        }

        UriDiagnostic? inputLimit = CheckInputLength(inputLength, limits);
        if (inputLimit != null)
        {
            return inputLimit;
        }

        offset = 0;
        while (offset < paths.Length)
        {
            UriDiagnostic? pathFailure = GetPathContentDiagnostic(
                paths[offset],
                limits);
            if (pathFailure != null)
            {
                return pathFailure;
            }

            offset = offset + 1;
        }

        return null;
    }

    internal UriDiagnostic? GetPathContentDiagnostic(
        string path,
        OperationLimits limits)
    {
        UriDiagnostic? componentLimit = LimitGuard.CheckComponentCharacters(
            path.Length,
            limits,
            DiagnosticDomain.Uri,
            UriComponent.Path,
            0);
        if (componentLimit != null)
        {
            return componentLimit;
        }

        int segmentCount = 0;
        if (path.Length > 0)
        {
            segmentCount = PathParser.CountSlashes(path.AsSpan()) + 1;
        }

        UriDiagnostic? segmentLimit = LimitGuard.CheckPathSegments(
            segmentCount,
            limits,
            DiagnosticDomain.Uri,
            0);
        if (segmentLimit != null)
        {
            return segmentLimit;
        }

        return PathParser.ValidatePathCharacters(path.AsSpan(), 0, false);
    }

    internal UriDiagnostic? CheckInputLength(
        long inputLength,
        OperationLimits limits)
    {
        if (inputLength > int.MaxValue)
        {
            return DiagnosticFactory.CreateLimitExceeded(
                DiagnosticDomain.Uri,
                UriComponent.Input,
                0,
                0);
        }

        return LimitGuard.CheckInputCharacters(
            (int)inputLength,
            limits,
            DiagnosticDomain.Uri);
    }

    internal UriDiagnostic? CheckOutputLength(
        long outputLength,
        OperationLimits limits)
    {
        if (outputLength > int.MaxValue)
        {
            return DiagnosticFactory.CreateLimitExceeded(
                DiagnosticDomain.Uri,
                UriComponent.Output,
                0,
                0);
        }

        return LimitGuard.CheckOutputCharacters(
            (int)outputLength,
            limits,
            DiagnosticDomain.Uri);
    }

    internal UriDiagnostic? CheckSegmentCount(
        long segmentCount,
        OperationLimits limits)
    {
        if (segmentCount > int.MaxValue)
        {
            return DiagnosticFactory.CreateLimitExceeded(
                DiagnosticDomain.Uri,
                UriComponent.Path,
                0,
                0);
        }

        return LimitGuard.CheckPathSegments(
            (int)segmentCount,
            limits,
            DiagnosticDomain.Uri,
            0);
    }

    internal UriValidationException CreateValidationException(
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

        return new UriValidationException(failure);
    }
}
