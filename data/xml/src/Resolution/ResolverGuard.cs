using System;
using System.Collections.Generic;
using System.IO;

namespace xml;

internal class ResolverGuardScope : IDisposable
{
    internal ResolverGuard Guard;
    internal Uri InitialUri;
    internal Uri CurrentUri;
    internal List<string> RedirectKeys;
    internal int RedirectCount;
    internal long ResourceBytes;
    internal bool Active;

    internal ResolverGuardScope(ResolverGuard guard, Uri initialUri)
    {
        Guard = guard;
        InitialUri = initialUri;
        CurrentUri = initialUri;
        RedirectKeys = new List<string>();
        RedirectKeys.Add(UriTools.ResourceKey(initialUri));
        RedirectCount = 0;
        ResourceBytes = 0;
        Active = true;
    }

    public void Dispose()
    {
        if (Active)
        {
            Guard.CompleteXmlResource(this);
        }
    }
}

internal class ResolverGuard
{
    internal XmlResolverPolicy Policy;
    internal IXmlErrorReporter? ErrorReporter;
    internal Dictionary<string, bool> ActiveResourceKeys;
    internal int ResourceDepth;
    internal long WorkUnits;

    internal ResolverGuard(XmlResolverPolicy policy, IXmlErrorReporter? errorReporter)
    {
        Guard.NotNull(policy, "policy");
        Guard.NonNegativeInt(policy.MaximumRedirects, "policy.MaximumRedirects");
        Guard.NonNegativeInt(policy.MaximumDepth, "policy.MaximumDepth");
        Guard.NonNegative(policy.MaximumResourceBytes, "policy.MaximumResourceBytes");
        Guard.NonNegative(policy.MaximumWorkUnits, "policy.MaximumWorkUnits");
        Policy = policy;
        ErrorReporter = errorReporter;
        ActiveResourceKeys = new Dictionary<string, bool>(StringComparer.Ordinal);
        ResourceDepth = 0;
        WorkUnits = 0;
    }

    internal ResolverGuardScope BeginXmlResource(string reference, string? baseUri)
    {
        Guard.NotNull(reference, "reference");
        if (Policy.Access != XmlResourceAccess.Allowed)
        {
            throw CreateXmlResourceError("External resource access is denied by policy", XmlErrorCodes.ResourceDenied, baseUri);
        }

        if (UriTools.HasFragment(reference) || baseUri != null && UriTools.HasFragment(baseUri))
        {
            throw CreateXmlResourceError("External resource URIs must not contain fragment identifiers", XmlErrorCodes.ResourceDenied, baseUri);
        }

        Uri requested = ResolveXmlResourceUri(reference, baseUri);
        ValidateXmlResourceUriAccess(requested);
        ConsumeXmlResourceWork(reference.Length + (baseUri == null ? 0 : baseUri.Length) + 1);
        int attemptedDepth = ResourceDepth + 1;
        if (attemptedDepth > Policy.MaximumDepth)
        {
            throw CreateXmlResourceError("External resource depth exceeds the configured limit of " + Policy.MaximumDepth, XmlErrorCodes.LimitExceeded, requested.AbsoluteUri);
        }

        string key = UriTools.ResourceKey(requested);
        if (ActiveResourceKeys.ContainsKey(key))
        {
            throw CreateXmlResourceError("External resource resolution cycle detected for '" + requested.AbsoluteUri + "'", XmlErrorCodes.ResourceCycle, requested.AbsoluteUri);
        }

        ActiveResourceKeys.Add(key, true);
        ResourceDepth = attemptedDepth;
        return new ResolverGuardScope(this, requested);
    }

    internal Uri FollowXmlResourceRedirect(ResolverGuardScope scope, string reference)
    {
        ValidateXmlResourceScope(scope);
        Guard.NotNull(reference, "reference");
        if (!Policy.AllowRedirects)
        {
            throw CreateXmlResourceError("External resource redirects are denied by policy", XmlErrorCodes.ResourceDenied, scope.CurrentUri.AbsoluteUri);
        }

        int attemptedRedirects = scope.RedirectCount + 1;
        if (attemptedRedirects > Policy.MaximumRedirects)
        {
            throw CreateXmlResourceError("External resource redirects exceed the configured limit of " + Policy.MaximumRedirects, XmlErrorCodes.LimitExceeded, scope.CurrentUri.AbsoluteUri);
        }

        Uri redirected = UriTools.ResolveReferenceWithFragmentPolicy(scope.CurrentUri, reference, UriFragmentPolicy.Reject);
        ValidateXmlResourceUriAccess(redirected);
        ConsumeXmlResourceWork(reference.Length + 1);
        string redirectedKey = UriTools.ResourceKey(redirected);
        if (ContainsXmlResourceKey(scope.RedirectKeys, redirectedKey))
        {
            throw CreateXmlResourceError("External resource redirect cycle detected for '" + redirected.AbsoluteUri + "'", XmlErrorCodes.ResourceCycle, redirected.AbsoluteUri);
        }

        string currentKey = UriTools.ResourceKey(scope.CurrentUri);
        ActiveResourceKeys.Remove(currentKey);
        if (ActiveResourceKeys.ContainsKey(redirectedKey))
        {
            ActiveResourceKeys.Add(currentKey, true);
            throw CreateXmlResourceError("External resource resolution cycle detected for '" + redirected.AbsoluteUri + "'", XmlErrorCodes.ResourceCycle, redirected.AbsoluteUri);
        }

        ActiveResourceKeys.Add(redirectedKey, true);
        scope.CurrentUri = redirected;
        scope.RedirectKeys.Add(redirectedKey);
        scope.RedirectCount = attemptedRedirects;
        return redirected;
    }

    internal void ValidateXmlResolvedResource(ResolverGuardScope scope, XmlResourceKind expectedKind, XmlResolvedResource resource)
    {
        ValidateXmlResourceScope(scope);
        Guard.NotNull(resource, "resource");
        if (resource.Access != XmlResourceAccess.Allowed)
        {
            throw CreateXmlResourceError("The resolver denied access to '" + scope.CurrentUri.AbsoluteUri + "'", XmlErrorCodes.ResourceDenied, scope.CurrentUri.AbsoluteUri);
        }

        if (resource.Kind != expectedKind)
        {
            throw CreateXmlResourceError("The resolver returned a resource of the wrong kind", XmlErrorCodes.ResourceNotFound, scope.CurrentUri.AbsoluteUri);
        }

        Uri absolute = resource.AbsoluteUri == null ? scope.CurrentUri : UriTools.CreateAbsolute(resource.AbsoluteUri);
        absolute = UriTools.ApplyFragmentPolicy(absolute, UriFragmentPolicy.Reject);
        ValidateXmlResourceUriAccess(absolute);
        resource.RequestedUri = resource.RequestedUri == null ? scope.InitialUri.AbsoluteUri : resource.RequestedUri;
        resource.AbsoluteUri = UriTools.CanonicalAbsoluteUri(absolute);
        XmlInput? input = resource.Input;
        if (input == null)
        {
            throw CreateXmlResourceError("The resolver allowed a resource without supplying input", XmlErrorCodes.ResourceNotFound, resource.AbsoluteUri);
        }

        if (input.BaseUri == null)
        {
            input.BaseUri = resource.AbsoluteUri;
        }

        if (resource.MediaType == null)
        {
            resource.MediaType = input.MediaType;
        }
        else if (input.MediaType == null)
        {
            input.MediaType = resource.MediaType;
        }

        if (resource.EncodingName == null)
        {
            resource.EncodingName = input.EncodingName;
        }
        else if (input.EncodingName == null)
        {
            input.EncodingName = resource.EncodingName;
        }

        if (resource.OwnsResource)
        {
            input.OwnsResource = true;
        }
        else if (input.OwnsResource)
        {
            resource.OwnsResource = true;
        }

        long knownBytes = KnownXmlResourceBytes(input);
        if (knownBytes >= 0 && scope.ResourceBytes == 0)
        {
            ObserveXmlResourceBytes(scope, knownBytes);
        }
    }

    internal void ObserveXmlResourceBytes(ResolverGuardScope scope, long byteCount)
    {
        ValidateXmlResourceScope(scope);
        Guard.NonNegative(byteCount, "byteCount");
        long attempted = SaturatingXmlResourceSum(scope.ResourceBytes, byteCount);
        if (attempted > Policy.MaximumResourceBytes)
        {
            throw CreateXmlResourceError("External resource bytes exceed the configured limit of " + Policy.MaximumResourceBytes, XmlErrorCodes.LimitExceeded, scope.CurrentUri.AbsoluteUri);
        }

        ConsumeXmlResourceWork(byteCount);
        scope.ResourceBytes = attempted;
    }

    internal void ConsumeXmlResourceWork(long units)
    {
        Guard.NonNegative(units, "units");
        long attempted = SaturatingXmlResourceSum(WorkUnits, units);
        if (attempted > Policy.MaximumWorkUnits)
        {
            throw CreateXmlResourceError("External resource work exceeds the configured limit of " + Policy.MaximumWorkUnits, XmlErrorCodes.LimitExceeded, null);
        }

        WorkUnits = attempted;
    }

    internal int ActiveXmlResourceDepth()
    {
        return ResourceDepth;
    }

    internal long ConsumedXmlResourceWork()
    {
        return WorkUnits;
    }

    internal void CompleteXmlResource(ResolverGuardScope scope)
    {
        ValidateXmlResourceScope(scope);
        ActiveResourceKeys.Remove(UriTools.ResourceKey(scope.CurrentUri));
        ResourceDepth = ResourceDepth - 1;
        scope.Active = false;
    }

    internal Uri ResolveXmlResourceUri(string reference, string? baseUri)
    {
        if (baseUri == null)
        {
            Uri absolute = UriTools.CreateAbsolute(reference);
            return UriTools.ApplyFragmentPolicy(absolute, UriFragmentPolicy.Reject);
        }

        Uri baseValue = UriTools.CreateAbsolute(baseUri);
        return UriTools.ResolveReferenceWithFragmentPolicy(baseValue, reference, UriFragmentPolicy.Reject);
    }

    internal void ValidateXmlResourceUriAccess(Uri value)
    {
        string[]? allowedSchemes = Policy.AllowedSchemes;
        if (allowedSchemes != null && !ContainsXmlResourceScheme(allowedSchemes, value.Scheme))
        {
            throw CreateXmlResourceError("URI scheme '" + value.Scheme + "' is denied by policy", XmlErrorCodes.ResourceDenied, value.AbsoluteUri);
        }

        string[]? allowedRoots = Policy.AllowedRoots;
        if (value.IsFile && allowedRoots != null && !IsXmlResourceUnderAllowedRoot(value, allowedRoots))
        {
            throw CreateXmlResourceError("File URI '" + value.AbsoluteUri + "' is outside the allowed roots", XmlErrorCodes.ResourceDenied, value.AbsoluteUri);
        }
    }

    internal bool ContainsXmlResourceScheme(string[] schemes, string candidate)
    {
        int index = 0;
        while (index < schemes.Length)
        {
            string? scheme = schemes[index];
            if (scheme == null || scheme.Length == 0)
            {
                throw new ArgumentException("Allowed URI schemes must be non-empty.", "policy.AllowedSchemes");
            }

            if (string.Equals(scheme, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXmlResourceUnderAllowedRoot(Uri value, string[] roots)
    {
        string candidate = Path.GetFullPath(value.LocalPath);
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        int index = 0;
        while (index < roots.Length)
        {
            string? rootValue = roots[index];
            if (rootValue == null || rootValue.Length == 0)
            {
                throw new ArgumentException("Allowed file roots must be non-empty.", "policy.AllowedRoots");
            }

            string root = NormalizeXmlResourceRoot(rootValue);
            if (string.Equals(candidate, root, comparison) || StartsWithXmlResourceRoot(candidate, root, comparison))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string NormalizeXmlResourceRoot(string value)
    {
        string path = value.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? UriTools.CreateAbsolute(value).LocalPath : value;
        string fullPath = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }

    internal bool StartsWithXmlResourceRoot(string candidate, string root, StringComparison comparison)
    {
        if (!candidate.StartsWith(root, comparison) || candidate.Length <= root.Length)
        {
            return false;
        }

        if (root.Length > 0 && (root[root.Length - 1] == Path.DirectorySeparatorChar || root[root.Length - 1] == Path.AltDirectorySeparatorChar))
        {
            return true;
        }

        char separator = candidate[root.Length];
        return separator == Path.DirectorySeparatorChar || separator == Path.AltDirectorySeparatorChar;
    }

    internal bool ContainsXmlResourceKey(List<string> keys, string candidate)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (keys[index] == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateXmlResourceScope(ResolverGuardScope scope)
    {
        Guard.NotNull(scope, "scope");
        if (!object.ReferenceEquals(scope.Guard, this) || !scope.Active)
        {
            throw new InvalidOperationException("The resolver scope is not active for this guard.");
        }
    }

    internal long KnownXmlResourceBytes(XmlInput input)
    {
        if (input.Kind == XmlInputKind.String)
        {
            return input.Text == null ? 0 : input.Text.Length;
        }

        if (input.Kind == XmlInputKind.CharacterMemory)
        {
            return input.Characters.Length;
        }

        if (input.Kind == XmlInputKind.ByteMemory)
        {
            return input.Bytes.Length;
        }

        return -1;
    }

    internal long SaturatingXmlResourceSum(long left, long right)
    {
        return right > long.MaxValue - left ? long.MaxValue : left + right;
    }

    internal XmlResourceException CreateXmlResourceError(string message, string code, string? sourceUri)
    {
        SourceLocation location = new SourceLocation();
        location.SourceUri = sourceUri;
        location.CharacterOffset = 0;
        location.ByteOffset = -1;
        location.Line = 1;
        location.Column = 1;
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = code == XmlErrorCodes.LimitExceeded ? XmlErrorCategory.Limit : XmlErrorCategory.Resource;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = range;
        IXmlErrorReporter? reporter = ErrorReporter;
        if (reporter != null)
        {
            reporter.ReportXmlDiagnostic(diagnostic);
        }

        return new XmlResourceException(message, diagnostic, location);
    }
}
