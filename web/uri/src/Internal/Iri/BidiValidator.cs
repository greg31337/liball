using System;
using System.Globalization;

namespace uri;

internal static class BidiValidator
{
    internal const int NeutralDirection = 0;
    internal const int LeftToRightDirection = 1;
    internal const int RightToLeftDirection = 2;
    internal const string ComponentMessage = "The IRI component does not satisfy the RFC 3987 bidirectional-text recommendations.";
    internal const string FormattingMessage = "The IRI component contains a forbidden bidirectional formatting character.";
    internal const string IdnLabelMessage = "The IDN label does not satisfy bidirectional-label requirements.";

    internal static UriDiagnostic? ValidateIri(ReadOnlySpan<char> text)
    {
        IriParseResult parsed = IriParserCore.ParseReference(text);
        if (!parsed.Success)
        {
            return IriComponentScanner.MapDiagnostic(parsed.Diagnostic);
        }

        return ValidateReference(parsed.Reference);
    }

    internal static UriDiagnostic? ValidateReference(IriReference reference)
    {
        int offset = 0;
        if (reference.HasScheme)
        {
            offset = reference.Scheme.Length + 1;
        }

        if (reference.HasAuthority)
        {
            offset = offset + 2;
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                UriDiagnostic? userInfo = ValidateComponent(
                    authority.UserInfo,
                    UriComponent.UserInfo,
                    offset);
                if (userInfo != null)
                {
                    return userInfo;
                }

                offset = offset + authority.UserInfo.Length + 1;
            }

            bool ipLiteral = UriComposerCore.IsIpLiteral(authority.HostKind);
            if (ipLiteral)
            {
                offset = offset + 1;
            }

            if (authority.HostKind == UriHostKind.RegisteredName)
            {
                UriDiagnostic? host = ValidateComponent(
                    authority.Host,
                    UriComponent.Host,
                    offset);
                if (host != null)
                {
                    return host;
                }
            }

            offset = offset + authority.Host.Length;
            if (ipLiteral)
            {
                offset = offset + 1;
            }

            if (authority.HasPort)
            {
                offset = offset + authority.Port.Length + 1;
            }
        }

        UriDiagnostic? path = ValidatePathSegments(reference.Path, offset);
        if (path != null)
        {
            return path;
        }

        offset = offset + reference.Path.Length;
        if (reference.HasQuery)
        {
            offset = offset + 1;
            UriDiagnostic? query = ValidateComponent(
                reference.Query,
                UriComponent.Query,
                offset);
            if (query != null)
            {
                return query;
            }

            offset = offset + reference.Query.Length;
        }

        if (reference.HasFragment)
        {
            offset = offset + 1;
            return ValidateComponent(
                reference.Fragment,
                UriComponent.Fragment,
                offset);
        }

        return null;
    }

    internal static UriDiagnostic? ValidateIdnHost(
        ReadOnlySpan<char> host,
        IriIdnMode mode)
    {
        if (mode == IriIdnMode.None)
        {
            return null;
        }

        if (mode != IriIdnMode.Platform)
        {
            return DiagnosticFactory.CreateError(
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Host,
                0,
                host.Length,
                IdnHostCore.MappingModeMessage);
        }

        string normalized = IdnHostCore.NormalizeSeparators(host);
        int labelOffset = 0;
        while (labelOffset < normalized.Length)
        {
            int separator = IdnHostCore.FindSeparator(
                normalized.AsSpan(),
                labelOffset);
            int end = separator;
            if (end < 0)
            {
                end = normalized.Length;
            }

            if (end > labelOffset)
            {
                UriDiagnostic? diagnostic = ValidateIdnLabelText(
                    normalized.AsSpan(labelOffset, end - labelOffset),
                    labelOffset);
                if (diagnostic != null)
                {
                    return diagnostic;
                }
            }

            if (separator < 0 || separator == normalized.Length - 1)
            {
                labelOffset = normalized.Length;
            }
            else
            {
                labelOffset = separator + 1;
            }
        }

        return null;
    }

    internal static UriDiagnostic? ValidateIdnLabelText(
        ReadOnlySpan<char> label,
        int baseOffset)
    {
        int firstRtlOffset = -1;
        int firstLtrOffset = -1;
        bool hasEuropeanDigit = false;
        bool hasArabicDigit = false;
        int offset = 0;
        while (offset < label.Length)
        {
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(label, offset);
            if (!scalar.Success)
            {
                return DiagnosticFactory.CreateInvalidUnicode(
                    DiagnosticDomain.Iri,
                    UriComponent.Host,
                    baseOffset + scalar.ErrorOffset,
                    scalar.ErrorLength);
            }

            if (IriConverterCore.IsBidiFormattingCharacter(scalar.ScalarValue))
            {
                return CreateIdnBidiDiagnostic(
                    baseOffset + offset,
                    scalar.CodeUnitLength);
            }

            int direction = GetStrongDirection(scalar.ScalarValue);
            if (direction == RightToLeftDirection && firstRtlOffset < 0)
            {
                firstRtlOffset = offset;
            }
            else if (direction == LeftToRightDirection && firstLtrOffset < 0)
            {
                firstLtrOffset = offset;
            }

            if (IsEuropeanDigit(scalar.ScalarValue))
            {
                hasEuropeanDigit = true;
            }

            if (IsArabicDigit(scalar.ScalarValue))
            {
                hasArabicDigit = true;
            }

            offset = offset + scalar.CodeUnitLength;
        }

        if (firstRtlOffset < 0)
        {
            return null;
        }

        if (firstLtrOffset >= 0)
        {
            return CreateIdnBidiDiagnostic(
                baseOffset + firstLtrOffset,
                GetScalarLength(label, firstLtrOffset));
        }

        UnicodeScalarResult first = UnicodeScalar.ReadAt(label, 0);
        if (GetStrongDirection(first.ScalarValue) != RightToLeftDirection)
        {
            return CreateIdnBidiDiagnostic(baseOffset, first.CodeUnitLength);
        }

        int lastOffset = GetLastScalarOffset(label);
        UnicodeScalarResult last = UnicodeScalar.ReadAt(label, lastOffset);
        if (!IsIdnEndCharacter(last.ScalarValue, label, lastOffset))
        {
            return CreateIdnBidiDiagnostic(
                baseOffset + lastOffset,
                last.CodeUnitLength);
        }

        if (hasEuropeanDigit && hasArabicDigit)
        {
            offset = 0;
            while (offset < label.Length)
            {
                UnicodeScalarResult scalar = UnicodeScalar.ReadAt(label, offset);
                if (IsArabicDigit(scalar.ScalarValue))
                {
                    return CreateIdnBidiDiagnostic(
                        baseOffset + offset,
                        scalar.CodeUnitLength);
                }

                offset = offset + scalar.CodeUnitLength;
            }
        }

        return null;
    }

    internal static UriDiagnostic? ValidateComponent(
        string text,
        UriComponent component,
        int baseOffset)
    {
        int firstRtlOffset = -1;
        int firstLtrOffset = -1;
        int offset = 0;
        while (offset < text.Length)
        {
            if (text[offset] == '%')
            {
                offset = offset + 3;
                continue;
            }

            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text.AsSpan(), offset);
            if (IriConverterCore.IsBidiFormattingCharacter(scalar.ScalarValue))
            {
                return CreateFormattingDiagnostic(
                    component,
                    baseOffset + offset,
                    scalar.CodeUnitLength);
            }

            int direction = GetStrongDirection(scalar.ScalarValue);
            if (direction == RightToLeftDirection && firstRtlOffset < 0)
            {
                firstRtlOffset = offset;
            }
            else if (direction == LeftToRightDirection && firstLtrOffset < 0)
            {
                firstLtrOffset = offset;
            }

            offset = offset + scalar.CodeUnitLength;
        }

        if (firstRtlOffset < 0)
        {
            return null;
        }

        if (firstLtrOffset >= 0)
        {
            return CreateComponentBidiDiagnostic(
                component,
                baseOffset + firstLtrOffset,
                GetScalarLength(text.AsSpan(), firstLtrOffset));
        }

        UnicodeScalarResult first = UnicodeScalar.ReadAt(text.AsSpan(), 0);
        if (GetStrongDirection(first.ScalarValue) != RightToLeftDirection)
        {
            return CreateComponentBidiDiagnostic(
                component,
                baseOffset,
                GetComponentTokenLength(text.AsSpan(), 0));
        }

        int lastOffset = GetLastComponentTokenOffset(text.AsSpan());
        UnicodeScalarResult last = UnicodeScalar.ReadAt(text.AsSpan(), lastOffset);
        if (GetStrongDirection(last.ScalarValue) != RightToLeftDirection)
        {
            return CreateComponentBidiDiagnostic(
                component,
                baseOffset + lastOffset,
                GetComponentTokenLength(text.AsSpan(), lastOffset));
        }

        return null;
    }

    internal static UriDiagnostic? ValidatePathSegments(string path, int baseOffset)
    {
        int segmentOffset = 0;
        while (segmentOffset <= path.Length)
        {
            int separator;
            int slash = -1;
            int offset = segmentOffset;
            while (offset < path.Length)
            {
                if (path[offset] == '/')
                {
                    slash = offset;
                    break;
                }

                offset = offset + 1;
            }

            separator = slash;
            int end = separator;
            if (end < 0)
            {
                end = path.Length;
            }

            if (end > segmentOffset)
            {
                UriDiagnostic? diagnostic = ValidateComponent(
                    path.Substring(segmentOffset, end - segmentOffset),
                    UriComponent.Path,
                    baseOffset + segmentOffset);
                if (diagnostic != null)
                {
                    return diagnostic;
                }
            }

            if (separator < 0)
            {
                return null;
            }

            segmentOffset = separator + 1;
        }

        return null;
    }

    internal static int GetLastComponentTokenOffset(ReadOnlySpan<char> text)
    {
        int offset = 0;
        int lastOffset = 0;
        while (offset < text.Length)
        {
            lastOffset = offset;
            if (text[offset] == '%')
            {
                offset = offset + 3;
            }
            else
            {
                UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
                offset = offset + scalar.CodeUnitLength;
            }
        }

        return lastOffset;
    }

    internal static int GetComponentTokenLength(ReadOnlySpan<char> text, int offset)
    {
        if (text[offset] == '%')
        {
            return 3;
        }

        return UnicodeScalar.ReadAt(text, offset).CodeUnitLength;
    }

    internal static int GetStrongDirection(int scalarValue)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(
            char.ConvertFromUtf32(scalarValue),
            0);
        if (!IsLetterCategory(category))
        {
            return NeutralDirection;
        }

        if (IsRightToLeftLetter(scalarValue))
        {
            return RightToLeftDirection;
        }

        return LeftToRightDirection;
    }

    internal static bool IsLetterCategory(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter ||
            category == UnicodeCategory.LowercaseLetter ||
            category == UnicodeCategory.TitlecaseLetter ||
            category == UnicodeCategory.ModifierLetter ||
            category == UnicodeCategory.OtherLetter;
    }

    internal static bool IsRightToLeftLetter(int scalarValue)
    {
        if (scalarValue >= 1424 && scalarValue <= 2303)
        {
            return true;
        }

        if (scalarValue >= 64285 && scalarValue <= 65279)
        {
            return true;
        }

        if (scalarValue >= 67584 && scalarValue <= 69631)
        {
            return true;
        }

        if (scalarValue >= 124928 && scalarValue <= 126143)
        {
            return true;
        }

        return scalarValue >= 126464 && scalarValue <= 126719;
    }

    internal static bool IsIdnEndCharacter(
        int scalarValue,
        ReadOnlySpan<char> label,
        int offset)
    {
        if (GetStrongDirection(scalarValue) == RightToLeftDirection ||
            IsEuropeanDigit(scalarValue) ||
            IsArabicDigit(scalarValue))
        {
            return true;
        }

        string scalarText = label.Slice(offset).ToString();
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(scalarText, 0);
        return category == UnicodeCategory.NonSpacingMark;
    }

    internal static bool IsEuropeanDigit(int scalarValue)
    {
        return scalarValue >= '0' && scalarValue <= '9';
    }

    internal static bool IsArabicDigit(int scalarValue)
    {
        return scalarValue >= 1632 && scalarValue <= 1641 ||
            scalarValue >= 1776 && scalarValue <= 1785;
    }

    internal static int GetLastScalarOffset(ReadOnlySpan<char> text)
    {
        int offset = 0;
        int lastOffset = 0;
        while (offset < text.Length)
        {
            lastOffset = offset;
            UnicodeScalarResult scalar = UnicodeScalar.ReadAt(text, offset);
            offset = offset + scalar.CodeUnitLength;
        }

        return lastOffset;
    }

    internal static int GetScalarLength(ReadOnlySpan<char> text, int offset)
    {
        return UnicodeScalar.ReadAt(text, offset).CodeUnitLength;
    }

    internal static UriDiagnostic CreateComponentBidiDiagnostic(
        UriComponent component,
        int offset,
        int length)
    {
        return DiagnosticFactory.CreateWarning(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidBidiText,
            component,
            offset,
            length,
            ComponentMessage);
    }

    internal static UriDiagnostic CreateIdnBidiDiagnostic(int offset, int length)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidBidiText,
            UriComponent.Host,
            offset,
            length,
            IdnLabelMessage);
    }

    internal static UriDiagnostic CreateFormattingDiagnostic(
        UriComponent component,
        int offset,
        int length)
    {
        return DiagnosticFactory.CreateError(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidBidiText,
            component,
            offset,
            length,
            FormattingMessage);
    }
}
