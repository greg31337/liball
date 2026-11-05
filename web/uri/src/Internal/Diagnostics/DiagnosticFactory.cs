namespace uri;

internal static class DiagnosticFactory
{
    internal const string InvalidInputMessage = "The supplied input is not valid for the requested operation.";
    internal const string InvalidCharacterMessage = "The input contains a character that is not allowed in this component.";
    internal const string UnexpectedEndMessage = "The input ended before the current grammar element was complete.";
    internal const string InvalidPercentEncodingMessage = "The percent-encoded sequence is incomplete or contains a non-hexadecimal digit.";
    internal const string InvalidUnicodeMessage = "The input contains an invalid UTF-16 scalar representation.";
    internal const string InvalidUtf8Message = "The decoded octets are not valid strict UTF-8.";
    internal const string LimitExceededMessage = "The operation exceeded a configured processing limit.";
    internal const string MissingRequiredComponentMessage = "A required URI component is missing.";
    internal const string InvalidComponentMessage = "The URI component model is inconsistent.";
    internal const string InvalidSchemeMessage = "The scheme is invalid.";
    internal const string InvalidUserInfoMessage = "The user information is invalid.";
    internal const string InvalidHostMessage = "The host is invalid.";
    internal const string InvalidPortMessage = "The port is invalid.";
    internal const string InvalidPathMessage = "The path is invalid.";
    internal const string InvalidQueryMessage = "The query is invalid.";
    internal const string InvalidFragmentMessage = "The fragment is invalid.";
    internal const string InvalidIpv4AddressMessage = "The IPv4 address is invalid.";
    internal const string InvalidIpv6AddressMessage = "The IPv6 address is invalid.";
    internal const string InvalidIpvFutureAddressMessage = "The IPvFuture address is invalid.";
    internal const string ZoneIdentifierNotAllowedMessage = "IPv6 zone identifiers are not allowed by the URI grammar.";
    internal const string UnresolvableReferenceMessage = "The URI reference cannot be resolved under the selected policy.";
    internal const string IncompatibleOptionsMessage = "The selected options are incompatible.";

    internal static UriDiagnostic CreateError(
        DiagnosticDomain domain,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        return new UriDiagnostic(
            domain,
            code,
            DiagnosticSeverity.Error,
            component,
            new TextRange(offset, length),
            message);
    }

    internal static UriDiagnostic CreateWarning(
        DiagnosticDomain domain,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        return new UriDiagnostic(
            domain,
            code,
            DiagnosticSeverity.Warning,
            component,
            new TextRange(offset, length),
            message);
    }

    internal static UriDiagnostic CreateInvalidInput(DiagnosticDomain domain)
    {
        return CreateError(
            domain,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            InvalidInputMessage);
    }

    internal static UriDiagnostic CreateInvalidCharacter(
        DiagnosticDomain domain,
        UriComponent component,
        int offset)
    {
        return CreateError(
            domain,
            UriDiagnosticCode.InvalidCharacter,
            component,
            offset,
            1,
            InvalidCharacterMessage);
    }

    internal static UriDiagnostic CreateUnexpectedEnd(
        DiagnosticDomain domain,
        UriComponent component,
        int offset)
    {
        return CreateError(
            domain,
            UriDiagnosticCode.UnexpectedEnd,
            component,
            offset,
            0,
            UnexpectedEndMessage);
    }

    internal static UriDiagnostic CreateInvalidPercentEncoding(UriComponent component, int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.PercentEncoding,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            offset,
            length,
            InvalidPercentEncodingMessage);
    }

    internal static UriDiagnostic CreateInvalidUnicode(
        DiagnosticDomain domain,
        UriComponent component,
        int offset,
        int length)
    {
        return CreateError(
            domain,
            UriDiagnosticCode.InvalidUnicode,
            component,
            offset,
            length,
            InvalidUnicodeMessage);
    }

    internal static UriDiagnostic CreateInvalidUtf8(UriComponent component, int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.PercentEncoding,
            UriDiagnosticCode.InvalidUtf8,
            component,
            offset,
            length,
            InvalidUtf8Message);
    }

    internal static UriDiagnostic CreateLimitExceeded(
        DiagnosticDomain domain,
        UriComponent component,
        int offset,
        int length)
    {
        return CreateError(
            domain,
            UriDiagnosticCode.LimitExceeded,
            component,
            offset,
            length,
            LimitExceededMessage);
    }

    internal static UriDiagnostic CreateMissingRequiredComponent(UriComponent component)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.MissingRequiredComponent,
            component,
            0,
            0,
            MissingRequiredComponentMessage);
    }

    internal static UriDiagnostic CreateInvalidComponent(UriComponent component)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidComponent,
            component,
            0,
            0,
            InvalidComponentMessage);
    }

    internal static UriDiagnostic CreateUnresolvableReference(
        UriComponent component)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.UnresolvableReference,
            component,
            0,
            0,
            UnresolvableReferenceMessage);
    }

    internal static UriDiagnostic CreateIncompatibleOptions(
        DiagnosticDomain domain)
    {
        return CreateError(
            domain,
            UriDiagnosticCode.IncompatibleOptions,
            UriComponent.Input,
            0,
            0,
            IncompatibleOptionsMessage);
    }

    internal static UriDiagnostic CreateInvalidScheme(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidScheme,
            UriComponent.Scheme,
            offset,
            length,
            InvalidSchemeMessage);
    }

    internal static UriDiagnostic CreateUriInvalidPercentEncoding(
        UriComponent component,
        int offset,
        int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            offset,
            length,
            InvalidPercentEncodingMessage);
    }

    internal static UriDiagnostic CreateInvalidUserInfo(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidUserInfo,
            UriComponent.UserInfo,
            offset,
            length,
            InvalidUserInfoMessage);
    }

    internal static UriDiagnostic CreateInvalidHost(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidHost,
            UriComponent.Host,
            offset,
            length,
            InvalidHostMessage);
    }

    internal static UriDiagnostic CreateInvalidPort(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidPort,
            UriComponent.Port,
            offset,
            length,
            InvalidPortMessage);
    }

    internal static UriDiagnostic CreateInvalidPath(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidPath,
            UriComponent.Path,
            offset,
            length,
            InvalidPathMessage);
    }

    internal static UriDiagnostic CreateInvalidQuery(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidQuery,
            UriComponent.Query,
            offset,
            length,
            InvalidQueryMessage);
    }

    internal static UriDiagnostic CreateInvalidFragment(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidFragment,
            UriComponent.Fragment,
            offset,
            length,
            InvalidFragmentMessage);
    }

    internal static UriDiagnostic CreateInvalidIpv4Address(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidIpv4Address,
            UriComponent.Host,
            offset,
            length,
            InvalidIpv4AddressMessage);
    }

    internal static UriDiagnostic CreateInvalidIpv6Address(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidIpv6Address,
            UriComponent.Host,
            offset,
            length,
            InvalidIpv6AddressMessage);
    }

    internal static UriDiagnostic CreateInvalidIpvFutureAddress(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidIpvFutureAddress,
            UriComponent.Host,
            offset,
            length,
            InvalidIpvFutureAddressMessage);
    }

    internal static UriDiagnostic CreateZoneIdentifierNotAllowed(int offset, int length)
    {
        return CreateError(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.ZoneIdentifierNotAllowed,
            UriComponent.Host,
            offset,
            length,
            ZoneIdentifierNotAllowedMessage);
    }
}
