using System;
using System.Collections.Generic;

namespace uri;

internal static class UriValidatorCore
{
    internal static UriValidationResult ValidateReference(
        ReadOnlySpan<char> text,
        UriValidationOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateOptions(options);
        if (optionFailure != null)
        {
            return CreateValidationFailure(new UriReference(), optionFailure);
        }

        if (options == null)
        {
            UriDiagnostic invalidOptions = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.Uri);
            return CreateValidationFailure(new UriReference(), invalidOptions);
        }

        UriValidationOptions validOptions = options;
        UriDiagnostic? inputLimit = LimitGuard.CheckInputCharacters(
            text.Length,
            validOptions.Limits,
            DiagnosticDomain.Uri);
        if (inputLimit != null)
        {
            return CreateValidationFailure(new UriReference(), inputLimit);
        }

        UriParseResult parsed = UriParserCore.ParseReference(text);
        if (!parsed.Success)
        {
            UriDiagnostic? parseFailure = parsed.Diagnostic;
            if (parseFailure == null)
            {
                parseFailure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
            }

            return CreateValidationFailure(new UriReference(), parseFailure);
        }

        UriDiagnostic[] diagnostics = CollectComponentDiagnostics(
            parsed.Reference,
            validOptions);
        if (diagnostics.Length > 0)
        {
            return CreateValidationFailure(parsed.Reference, diagnostics[0]);
        }

        return new UriValidationResult(
            true,
            parsed.Reference,
            null,
            new UriDiagnostic[0]);
    }

    internal static UriValidationResult ValidateComponents(
        UriReference? reference,
        UriValidationOptions? options)
    {
        UriDiagnostic[] diagnostics = ValidateAllDiagnostics(reference, options);
        UriReference value;
        if (reference == null)
        {
            value = new UriReference();
        }
        else
        {
            value = reference;
        }

        if (diagnostics.Length > 0)
        {
            return CreateValidationFailure(value, diagnostics[0]);
        }

        return new UriValidationResult(true, value, null, new UriDiagnostic[0]);
    }

    internal static UriDiagnostic[] ValidateAllDiagnostics(
        UriReference? reference,
        UriValidationOptions? options)
    {
        UriDiagnostic? optionFailure = ValidateOptions(options);
        if (optionFailure != null)
        {
            return new UriDiagnostic[] { optionFailure };
        }

        if (reference == null)
        {
            return new UriDiagnostic[]
            {
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri)
            };
        }

        if (options == null)
        {
            return new UriDiagnostic[]
            {
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri)
            };
        }

        UriValidationOptions validOptions = options;
        return CollectComponentDiagnostics(reference, validOptions);
    }

    internal static UriDiagnostic[] CollectComponentDiagnostics(
        UriReference reference,
        UriValidationOptions options)
    {
        List<UriDiagnostic> diagnostics = new List<UriDiagnostic>();
        ValidateSchemeComponent(reference, options, diagnostics);
        ValidateAuthorityComponent(reference, options, diagnostics);
        ValidatePathComponent(reference, options, diagnostics);
        ValidateQueryComponent(reference, options, diagnostics);
        ValidateFragmentComponent(reference, options, diagnostics);

        if (!IsReferenceKindConsistent(reference))
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Input));
        }

        AddRequirementDiagnostics(reference, options, diagnostics);
        SortDiagnosticsByComponent(diagnostics);
        return diagnostics.ToArray();
    }

    internal static UriDiagnostic? ValidateOptions(UriValidationOptions? options)
    {
        if (options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        if (options.RequireAbsoluteUri && options.RequireFragment)
        {
            return DiagnosticFactory.CreateIncompatibleOptions(
                DiagnosticDomain.Uri);
        }

        return null;
    }

    internal static void ValidateSchemeComponent(
        UriReference reference,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? scheme = reference.Scheme;
        if (scheme == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Scheme));
            return;
        }

        if (!reference.HasScheme)
        {
            if (scheme.Length != 0)
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Scheme));
            }

            return;
        }

        if (!CheckComponentLimit(
            scheme.Length,
            options,
            UriComponent.Scheme,
            diagnostics))
        {
            return;
        }

        UriDiagnostic? invalid = ValidateSchemeValue(scheme.AsSpan());
        if (invalid != null)
        {
            diagnostics.Add(invalid);
        }
    }

    internal static UriDiagnostic? ValidateSchemeValue(ReadOnlySpan<char> scheme)
    {
        if (scheme.Length == 0)
        {
            return DiagnosticFactory.CreateInvalidScheme(0, 0);
        }

        if (!UriGrammar.IsSchemeStartCharacter(scheme[0]))
        {
            return DiagnosticFactory.CreateInvalidScheme(0, 1);
        }

        int offset = 1;
        while (offset < scheme.Length)
        {
            if (!UriGrammar.IsSchemeCharacter(scheme[offset]))
            {
                return DiagnosticFactory.CreateInvalidScheme(offset, 1);
            }

            offset = offset + 1;
        }

        return null;
    }

    internal static void ValidateAuthorityComponent(
        UriReference reference,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        UriAuthority? authority = reference.Authority;
        if (authority == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Authority));
            return;
        }

        if (!reference.HasAuthority)
        {
            if (!IsEmptyAuthority(authority))
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Authority));
            }

            return;
        }

        ValidateUserInfoComponent(authority, options, diagnostics);
        ValidateHostComponent(authority, options, diagnostics);
        ValidatePortComponent(authority, options, diagnostics);
    }

    internal static void ValidateUserInfoComponent(
        UriAuthority authority,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? userInfo = authority.UserInfo;
        if (userInfo == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.UserInfo));
            return;
        }

        if (!authority.HasUserInfo)
        {
            if (userInfo.Length != 0)
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.UserInfo));
            }

            return;
        }

        if (!CheckComponentLimit(
            userInfo.Length,
            options,
            UriComponent.UserInfo,
            diagnostics))
        {
            return;
        }

        UriDiagnostic? invalid = AuthorityParser.ValidateUserInfo(userInfo.AsSpan(), 0);
        if (invalid != null)
        {
            diagnostics.Add(invalid);
        }
    }

    internal static void ValidateHostComponent(
        UriAuthority authority,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? host = authority.Host;
        byte[]? addressBytes = authority.AddressBytes;
        if (host == null || addressBytes == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
            return;
        }

        if (!CheckComponentLimit(
            host.Length,
            options,
            UriComponent.Host,
            diagnostics))
        {
            return;
        }

        switch (authority.HostKind)
        {
            case UriHostKind.RegisteredName:
                IpV4ParseResult ipv4Candidate = IpV4Parser.Parse(host.AsSpan(), 0);
                if (ipv4Candidate.Success)
                {
                    diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
                }
                else
                {
                    UriDiagnostic? registeredName = AuthorityParser.ValidateRegisteredName(
                        host.AsSpan(),
                        0);
                    if (registeredName != null)
                    {
                        diagnostics.Add(registeredName);
                    }
                    else if (addressBytes.Length != 0)
                    {
                        diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
                    }
                }

                break;
            case UriHostKind.Ipv4:
                IpV4ParseResult ipv4 = IpV4Parser.Parse(host.AsSpan(), 0);
                if (!ipv4.Success)
                {
                    AddDiagnosticIfPresent(diagnostics, ipv4.Diagnostic);
                }
                else if (!AreBytesEqual(addressBytes, ipv4.AddressBytes))
                {
                    diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
                }

                break;
            case UriHostKind.Ipv6:
                IpV6ParseResult ipv6 = IpV6Parser.Parse(host.AsSpan(), 0);
                if (!ipv6.Success)
                {
                    AddDiagnosticIfPresent(diagnostics, ipv6.Diagnostic);
                }
                else if (!AreBytesEqual(addressBytes, ipv6.AddressBytes))
                {
                    diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
                }

                break;
            case UriHostKind.IpvFuture:
                IpFutureParseResult ipvFuture = IpFutureParser.Parse(host.AsSpan(), 0);
                if (!ipvFuture.Success)
                {
                    AddDiagnosticIfPresent(diagnostics, ipvFuture.Diagnostic);
                }
                else if (addressBytes.Length != 0)
                {
                    diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
                }

                break;
            default:
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Host));
                break;
        }
    }

    internal static void ValidatePortComponent(
        UriAuthority authority,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? port = authority.Port;
        if (port == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Port));
            return;
        }

        if (!authority.HasPort)
        {
            if (port.Length != 0)
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Port));
            }

            return;
        }

        if (!CheckComponentLimit(
            port.Length,
            options,
            UriComponent.Port,
            diagnostics))
        {
            return;
        }

        int offset = 0;
        while (offset < port.Length)
        {
            if (!Ascii.IsDigit(port[offset]))
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidPort(offset, 1));
                return;
            }

            offset = offset + 1;
        }
    }

    internal static void ValidatePathComponent(
        UriReference reference,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? path = reference.Path;
        if (path == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Path));
            return;
        }

        if (!CheckComponentLimit(
            path.Length,
            options,
            UriComponent.Path,
            diagnostics))
        {
            return;
        }

        PathParseResult parsed;
        if (reference.HasAuthority)
        {
            parsed = PathParser.ParseAbEmpty(path.AsSpan(), 0);
        }
        else if (path.Length == 0)
        {
            parsed = PathParser.ParseEmpty(path.AsSpan(), 0);
        }
        else if (path[0] == '/')
        {
            parsed = PathParser.ParseAbsolute(path.AsSpan(), 0);
        }
        else if (reference.HasScheme)
        {
            parsed = PathParser.ParseRootless(path.AsSpan(), 0);
        }
        else
        {
            parsed = PathParser.ParseNoScheme(path.AsSpan(), 0);
        }

        if (!parsed.Success)
        {
            AddDiagnosticIfPresent(diagnostics, parsed.Diagnostic);
            return;
        }

        UriDiagnostic? segmentLimit = LimitGuard.CheckPathSegments(
            parsed.SegmentCount,
            options.Limits,
            DiagnosticDomain.Uri,
            0);
        AddDiagnosticIfPresent(diagnostics, segmentLimit);
    }

    internal static void ValidateQueryComponent(
        UriReference reference,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? query = reference.Query;
        if (query == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Query));
            return;
        }

        if (!reference.HasQuery)
        {
            if (query.Length != 0)
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Query));
            }

            return;
        }

        if (!CheckComponentLimit(
            query.Length,
            options,
            UriComponent.Query,
            diagnostics))
        {
            return;
        }

        UriDiagnostic? invalid = ValidateQueryOrFragmentValue(
            query.AsSpan(),
            UriComponent.Query);
        AddDiagnosticIfPresent(diagnostics, invalid);
    }

    internal static void ValidateFragmentComponent(
        UriReference reference,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? fragment = reference.Fragment;
        if (fragment == null)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Fragment));
            return;
        }

        if (!reference.HasFragment)
        {
            if (fragment.Length != 0)
            {
                diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(UriComponent.Fragment));
            }

            return;
        }

        if (!CheckComponentLimit(
            fragment.Length,
            options,
            UriComponent.Fragment,
            diagnostics))
        {
            return;
        }

        UriDiagnostic? invalid = ValidateQueryOrFragmentValue(
            fragment.AsSpan(),
            UriComponent.Fragment);
        AddDiagnosticIfPresent(diagnostics, invalid);
    }

    internal static UriDiagnostic? ValidateQueryOrFragmentValue(
        ReadOnlySpan<char> text,
        UriComponent component)
    {
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    0,
                    component);
                if (!triplet.Success)
                {
                    UriDiagnostic? percent = triplet.Diagnostic;
                    if (percent != null)
                    {
                        return DiagnosticFactory.CreateUriInvalidPercentEncoding(
                            component,
                            percent.Range.Offset,
                            percent.Range.Length);
                    }

                    return DiagnosticFactory.CreateUriInvalidPercentEncoding(
                        component,
                        offset,
                        1);
                }

                offset = triplet.NextOffset;
                continue;
            }

            bool allowed;
            if (component == UriComponent.Query)
            {
                allowed = PercentAllowlist.IsQueryCharacter(text[offset]);
            }
            else
            {
                allowed = PercentAllowlist.IsFragmentCharacter(text[offset]);
            }

            if (!allowed)
            {
                if (component == UriComponent.Query)
                {
                    return DiagnosticFactory.CreateInvalidQuery(offset, 1);
                }

                return DiagnosticFactory.CreateInvalidFragment(offset, 1);
            }

            offset = offset + 1;
        }

        return null;
    }

    internal static void AddRequirementDiagnostics(
        UriReference reference,
        UriValidationOptions options,
        List<UriDiagnostic> diagnostics)
    {
        string? scheme = reference.Scheme;
        bool hasNonemptyScheme = reference.HasScheme &&
            scheme != null &&
            scheme.Length > 0;
        if ((options.RequireScheme || options.RequireAbsoluteUri) && !hasNonemptyScheme)
        {
            diagnostics.Add(DiagnosticFactory.CreateMissingRequiredComponent(
                UriComponent.Scheme));
        }

        if (options.RequireAuthority && !reference.HasAuthority)
        {
            diagnostics.Add(DiagnosticFactory.CreateMissingRequiredComponent(
                UriComponent.Authority));
        }

        UriAuthority? authority = reference.Authority;
        bool hasNonemptyHost = reference.HasAuthority &&
            authority != null &&
            authority.Host != null &&
            authority.Host.Length > 0 &&
            authority.HostKind != UriHostKind.Absent;
        if (options.RequireHost && !hasNonemptyHost)
        {
            diagnostics.Add(DiagnosticFactory.CreateMissingRequiredComponent(
                UriComponent.Host));
        }

        string? path = reference.Path;
        if (options.RequirePath && (path == null || path.Length == 0))
        {
            diagnostics.Add(DiagnosticFactory.CreateMissingRequiredComponent(
                UriComponent.Path));
        }

        if (options.RequireQuery && !reference.HasQuery)
        {
            diagnostics.Add(DiagnosticFactory.CreateMissingRequiredComponent(
                UriComponent.Query));
        }

        if (options.RequireFragment && !reference.HasFragment)
        {
            diagnostics.Add(DiagnosticFactory.CreateMissingRequiredComponent(
                UriComponent.Fragment));
        }

        if (options.RequireAbsoluteUri && reference.HasFragment)
        {
            diagnostics.Add(DiagnosticFactory.CreateInvalidComponent(
                UriComponent.Fragment));
        }
    }

    internal static bool CheckComponentLimit(
        int length,
        UriValidationOptions options,
        UriComponent component,
        List<UriDiagnostic> diagnostics)
    {
        UriDiagnostic? limit = LimitGuard.CheckComponentCharacters(
            length,
            options.Limits,
            DiagnosticDomain.Uri,
            component,
            0);
        if (limit != null)
        {
            diagnostics.Add(limit);
            return false;
        }

        return true;
    }

    internal static bool IsEmptyAuthority(UriAuthority authority)
    {
        return !authority.HasUserInfo &&
            authority.UserInfo != null &&
            authority.UserInfo.Length == 0 &&
            authority.HostKind == UriHostKind.Absent &&
            authority.Host != null &&
            authority.Host.Length == 0 &&
            authority.AddressBytes != null &&
            authority.AddressBytes.Length == 0 &&
            !authority.HasPort &&
            authority.Port != null &&
            authority.Port.Length == 0;
    }

    internal static bool IsReferenceKindConsistent(UriReference reference)
    {
        if (reference.HasScheme)
        {
            if (reference.Kind == UriReferenceKind.Uri)
            {
                return true;
            }

            return reference.Kind == UriReferenceKind.AbsoluteUri && !reference.HasFragment;
        }

        if (reference.HasAuthority)
        {
            return reference.Kind == UriReferenceKind.NetworkPathReference;
        }

        string? path = reference.Path;
        if (path == null)
        {
            return false;
        }

        if (path.Length > 0)
        {
            if (path[0] == '/')
            {
                return reference.Kind == UriReferenceKind.AbsolutePathReference;
            }

            return reference.Kind == UriReferenceKind.RelativePathReference;
        }

        if (reference.HasQuery)
        {
            return reference.Kind == UriReferenceKind.RelativeReference;
        }

        if (reference.HasFragment)
        {
            return reference.Kind == UriReferenceKind.SameDocumentReference;
        }

        return reference.Kind == UriReferenceKind.EmptyReference;
    }

    internal static bool AreBytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void AddDiagnosticIfPresent(
        List<UriDiagnostic> diagnostics,
        UriDiagnostic? diagnostic)
    {
        if (diagnostic != null)
        {
            diagnostics.Add(diagnostic);
        }
    }

    internal static void SortDiagnosticsByComponent(List<UriDiagnostic> diagnostics)
    {
        int index = 1;
        while (index < diagnostics.Count)
        {
            UriDiagnostic current = diagnostics[index];
            int destination = index;
            while (destination > 0 &&
                (int)diagnostics[destination - 1].Component > (int)current.Component)
            {
                diagnostics[destination] = diagnostics[destination - 1];
                destination = destination - 1;
            }

            diagnostics[destination] = current;
            index = index + 1;
        }
    }

    internal static UriValidationResult CreateValidationFailure(
        UriReference reference,
        UriDiagnostic diagnostic)
    {
        return new UriValidationResult(
            false,
            reference,
            diagnostic,
            new UriDiagnostic[] { diagnostic });
    }
}
