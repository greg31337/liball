using System;

namespace uri;

internal static class IriComponentScanner
{
    internal const int UserInfoKind = 1;
    internal const int RegisteredNameKind = 2;
    internal const int PathKind = 3;
    internal const int QueryKind = 4;
    internal const int FragmentKind = 5;

    internal static UriDiagnostic? ValidateUserInfo(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        return ValidateCharacters(text, baseOffset, UriComponent.UserInfo, UserInfoKind, false);
    }

    internal static UriDiagnostic? ValidateRegisteredName(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        return ValidateCharacters(text, baseOffset, UriComponent.Host, RegisteredNameKind, false);
    }

    internal static UriDiagnostic? ValidatePath(
        ReadOnlySpan<char> text,
        int baseOffset,
        bool forbidColonInFirstSegment)
    {
        return ValidateCharacters(
            text,
            baseOffset,
            UriComponent.Path,
            PathKind,
            forbidColonInFirstSegment);
    }

    internal static UriDiagnostic? ValidateQuery(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        return ValidateCharacters(text, baseOffset, UriComponent.Query, QueryKind, false);
    }

    internal static UriDiagnostic? ValidateFragment(
        ReadOnlySpan<char> text,
        int baseOffset)
    {
        return ValidateCharacters(text, baseOffset, UriComponent.Fragment, FragmentKind, false);
    }

    internal static UriDiagnostic? ValidateCharacters(
        ReadOnlySpan<char> text,
        int baseOffset,
        UriComponent component,
        int kind,
        bool forbidColonInFirstSegment)
    {
        bool firstSegment = true;
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                PercentTripletResult triplet = PercentScanner.ScanTriplet(
                    text,
                    offset,
                    baseOffset,
                    component);
                if (!triplet.Success)
                {
                    return CreatePercentFailure(triplet, component, baseOffset, offset);
                }

                offset = triplet.NextOffset;
                continue;
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.Iri,
                    component,
                    UriParserCore.GetAbsoluteOffset(baseOffset, scalar.ErrorOffset),
                    scalar.ErrorLength);
            }

            if (!IsAllowed(scalar.ScalarValue, kind))
            {
                return CreateComponentFailure(
                    component,
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    scalar.CodeUnitLength);
            }

            if (forbidColonInFirstSegment && firstSegment && scalar.ScalarValue == ':')
            {
                return CreateComponentFailure(
                    component,
                    UriParserCore.GetAbsoluteOffset(baseOffset, offset),
                    scalar.CodeUnitLength);
            }

            if (scalar.ScalarValue == '/')
            {
                firstSegment = false;
            }

            offset = offset + scalar.CodeUnitLength;
        }

        return null;
    }

    internal static bool IsAllowed(int scalarValue, int kind)
    {
        if (kind == UserInfoKind)
        {
            return IriGrammar.IsUserInfoCharacter(scalarValue);
        }

        if (kind == RegisteredNameKind)
        {
            return IriGrammar.IsRegisteredNameCharacter(scalarValue);
        }

        if (kind == PathKind)
        {
            return IriGrammar.IsPathCharacter(scalarValue);
        }

        if (kind == QueryKind)
        {
            return IriGrammar.IsQueryCharacter(scalarValue);
        }

        return IriGrammar.IsFragmentCharacter(scalarValue);
    }

    internal static UriDiagnostic CreateComponentFailure(
        UriComponent component,
        int offset,
        int length)
    {
        UriDiagnosticCode code = UriDiagnosticCode.InvalidFragment;
        string message = DiagnosticFactory.InvalidFragmentMessage;
        if (component == UriComponent.UserInfo)
        {
            code = UriDiagnosticCode.InvalidUserInfo;
            message = DiagnosticFactory.InvalidUserInfoMessage;
        }
        else if (component == UriComponent.Host)
        {
            code = UriDiagnosticCode.InvalidHost;
            message = DiagnosticFactory.InvalidHostMessage;
        }
        else if (component == UriComponent.Path)
        {
            code = UriDiagnosticCode.InvalidPath;
            message = DiagnosticFactory.InvalidPathMessage;
        }
        else if (component == UriComponent.Query)
        {
            code = UriDiagnosticCode.InvalidQuery;
            message = DiagnosticFactory.InvalidQueryMessage;
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            code,
            component,
            offset,
            length,
            message);
    }

    internal static UriDiagnostic CreatePercentFailure(
        PercentTripletResult triplet,
        UriComponent component,
        int baseOffset,
        int relativeOffset)
    {
        UriDiagnostic? diagnostic = triplet.Diagnostic;
        if (diagnostic != null)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidPercentEncoding,
                component,
                diagnostic.Range.Offset,
                diagnostic.Range.Length,
                DiagnosticFactory.InvalidPercentEncodingMessage);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            UriParserCore.GetAbsoluteOffset(baseOffset, relativeOffset),
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    internal static UriDiagnostic MapDiagnostic(UriDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            diagnostic.Code,
            diagnostic.Component,
            diagnostic.Range.Offset,
            diagnostic.Range.Length,
            diagnostic.Message);
    }
}
