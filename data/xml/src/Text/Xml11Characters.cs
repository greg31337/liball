using System;
using System.Globalization;
using System.Text;

namespace xml;

internal enum Xml11NormalizationIssueKind
{
    None = 0,
    NotNormalizationFormC = 1,
    ComposingCharacterAtConstructStart = 2
}

internal class Xml11NormalizationResult
{
    internal bool IsFullyNormalized;
    internal Xml11NormalizationIssueKind IssueKind;
    internal int Utf16Offset;
    internal XmlDiagnostic? Diagnostic;

    internal Xml11NormalizationResult(bool isFullyNormalized, Xml11NormalizationIssueKind issueKind, int utf16Offset, XmlDiagnostic? diagnostic)
    {
        IsFullyNormalized = isFullyNormalized;
        IssueKind = issueKind;
        Utf16Offset = utf16Offset;
        Diagnostic = diagnostic;
    }
}

internal static class Xml11CharacterRules
{
    internal static bool IsLegalXml11Character(int scalar)
    {
        if (scalar >= 0x1 && scalar <= 0xD7FF)
        {
            return true;
        }

        if (scalar >= 0xE000 && scalar <= 0xFFFD)
        {
            return true;
        }

        return scalar >= 0x10000 && scalar <= 0x10FFFF;
    }

    internal static bool IsRestrictedXml11Character(int scalar)
    {
        if (scalar >= 0x1 && scalar <= 0x8 || scalar >= 0xB && scalar <= 0xC || scalar >= 0xE && scalar <= 0x1F)
        {
            return true;
        }

        return scalar >= 0x7F && scalar <= 0x84 || scalar >= 0x86 && scalar <= 0x9F;
    }

    internal static bool IsDirectXml11Character(int scalar)
    {
        return IsLegalXml11Character(scalar) && !IsRestrictedXml11Character(scalar);
    }

    internal static bool IsXml11NameStartCharacter(int scalar)
    {
        return Xml10CharacterRules.IsXml10NameStartCharacter(scalar);
    }

    internal static bool IsXml11NameCharacter(int scalar)
    {
        return Xml10CharacterRules.IsXml10NameCharacter(scalar);
    }

    internal static void ValidateDirectXml11Characters(ReadOnlyMemory<char> characters)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(characters);
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (!IsLegalXml11Character(scalar.Value))
            {
                throw new FormatException("Illegal XML 1.1 character U+" + scalar.Value.ToString("X") + " at UTF-16 offset " + scalar.Utf16Offset + ".");
            }

            if (IsRestrictedXml11Character(scalar.Value))
            {
                throw new FormatException("Restricted XML 1.1 character U+" + scalar.Value.ToString("X") + " must appear as a character reference at UTF-16 offset " + scalar.Utf16Offset + ".");
            }
        }
    }
}

internal static class Xml11CharacterReferenceParser
{
    internal static XmlCharacterReference ParseXml11CharacterReference(ReadOnlySpan<char> characters, int startOffset)
    {
        Guard.NonNegativeInt(startOffset, "startOffset");
        if (startOffset >= characters.Length || characters[startOffset] != '&' || startOffset + 1 >= characters.Length || characters[startOffset + 1] != '#')
        {
            throw CreateXml11CharacterReferenceError("A character reference must begin with '&#'", startOffset);
        }

        int position = startOffset + 2;
        int numberBase = 10;
        if (position < characters.Length && characters[position] == 'x')
        {
            numberBase = 16;
            position = position + 1;
        }

        int digitCount = 0;
        int scalar = 0;
        while (position < characters.Length && characters[position] != ';')
        {
            int digit = Xml10CharacterReferenceParser.CharacterReferenceDigit(characters[position], numberBase);
            if (digit < 0)
            {
                throw CreateXml11CharacterReferenceError("A character reference contains an invalid digit", position);
            }

            if (scalar > (0x10FFFF - digit) / numberBase)
            {
                throw CreateXml11CharacterReferenceError("A character reference exceeds the Unicode scalar range", position);
            }

            scalar = scalar * numberBase + digit;
            digitCount = digitCount + 1;
            position = position + 1;
        }

        if (digitCount == 0)
        {
            throw CreateXml11CharacterReferenceError("A character reference requires at least one digit", position);
        }

        if (position >= characters.Length || characters[position] != ';')
        {
            throw CreateXml11CharacterReferenceError("A character reference must end with ';'", position);
        }

        if (!Xml11CharacterRules.IsLegalXml11Character(scalar))
        {
            throw CreateXml11CharacterReferenceError("A character reference does not identify a legal XML 1.1 character", startOffset);
        }

        string replacement = char.ConvertFromUtf32(scalar);
        bool restricted = Xml11CharacterRules.IsRestrictedXml11Character(scalar);
        return new XmlCharacterReference(scalar, position - startOffset + 1, replacement, restricted);
    }

    internal static FormatException CreateXml11CharacterReferenceError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}

internal static class Xml11NormalizationChecker
{
    internal static Xml11NormalizationResult CheckXml11Normalization(ReadOnlyMemory<char> characters, bool beginsRelevantConstruct)
    {
        ValidateNormalizationInputCharacters(characters);
        string text = characters.ToString();
        if (!text.IsNormalized(NormalizationForm.FormC))
        {
            int offset = FindFirstNfcFailureOffset(text);
            XmlDiagnostic diagnostic = CreateNormalizationDiagnostic("XML 1.1 text is not in Unicode Normalization Form C at UTF-16 offset " + offset + ".");
            return new Xml11NormalizationResult(false, Xml11NormalizationIssueKind.NotNormalizationFormC, offset, diagnostic);
        }

        if (beginsRelevantConstruct && text.Length > 0)
        {
            UnicodeScalar first = new UnicodeScalarScanner(characters).ReadUnicodeScalar();
            if (IsXml11ComposingCharacter(first.Value))
            {
                XmlDiagnostic diagnostic = CreateNormalizationDiagnostic("An XML 1.1 relevant construct begins with a composing character at UTF-16 offset " + first.Utf16Offset + ".");
                return new Xml11NormalizationResult(false, Xml11NormalizationIssueKind.ComposingCharacterAtConstructStart, first.Utf16Offset, diagnostic);
            }
        }

        return new Xml11NormalizationResult(true, Xml11NormalizationIssueKind.None, -1, null);
    }

    internal static bool IsXml11ComposingCharacter(int scalar)
    {
        if (!Xml11CharacterRules.IsLegalXml11Character(scalar))
        {
            return false;
        }

        if (HasNonZeroCanonicalCombiningClass(scalar))
        {
            return true;
        }

        if (scalar >= 0x1161 && scalar <= 0x1175 || scalar >= 0x11A8 && scalar <= 0x11C2)
        {
            return true;
        }

        return IsClassZeroCanonicalCompositionSecond(scalar);
    }

    internal static bool HasNonZeroCanonicalCombiningClass(int scalar)
    {
        string candidate = char.ConvertFromUtf32(scalar);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(candidate, 0);
        if (category != UnicodeCategory.NonSpacingMark && category != UnicodeCategory.SpacingCombiningMark && category != UnicodeCategory.EnclosingMark)
        {
            return false;
        }

        string decomposed = candidate.Normalize(NormalizationForm.FormD);
        string starter = "\uE000";
        string lowClass = "\u0334";
        string highClass = "\u0345";
        string highBefore = (starter + highClass + candidate).Normalize(NormalizationForm.FormD);
        if (!string.Equals(highBefore, starter + highClass + decomposed, StringComparison.Ordinal))
        {
            return true;
        }

        string lowAfter = (starter + candidate + lowClass).Normalize(NormalizationForm.FormD);
        return !string.Equals(lowAfter, starter + decomposed + lowClass, StringComparison.Ordinal);
    }

    internal static bool IsClassZeroCanonicalCompositionSecond(int scalar)
    {
        if (scalar == 0x09BE || scalar == 0x09D7 || scalar == 0x0B3E || scalar == 0x0B56 || scalar == 0x0B57 || scalar == 0x0BBE || scalar == 0x0BD7)
        {
            return true;
        }

        if (scalar == 0x0CC2 || scalar == 0x0CD5 || scalar == 0x0CD6 || scalar == 0x0D3E || scalar == 0x0D57 || scalar == 0x0DCF || scalar == 0x0DDF)
        {
            return true;
        }

        if (scalar == 0x0FB5 || scalar == 0x0FB7 || scalar == 0x102E || scalar == 0x1B35)
        {
            return true;
        }

        if (scalar == 0x11127 || scalar == 0x1133E || scalar == 0x11357 || scalar == 0x114B0 || scalar == 0x114BA || scalar == 0x114BD || scalar == 0x115AF || scalar == 0x11930)
        {
            return true;
        }

        return false;
    }

    internal static void ValidateNormalizationInputCharacters(ReadOnlyMemory<char> characters)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(characters);
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (!Xml11CharacterRules.IsLegalXml11Character(scalar.Value))
            {
                throw new FormatException("Illegal XML 1.1 character U+" + scalar.Value.ToString("X") + " at UTF-16 offset " + scalar.Utf16Offset + ".");
            }
        }
    }

    internal static int FindFirstNfcFailureOffset(string text)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(text.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            string prefix = text.Substring(0, scanner.CurrentUtf16Offset());
            if (!prefix.IsNormalized(NormalizationForm.FormC))
            {
                return scalar.Utf16Offset;
            }
        }

        return 0;
    }

    internal static XmlDiagnostic CreateNormalizationDiagnostic(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.GeneralFailure;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Warning;
        diagnostic.Message = message;
        return diagnostic;
    }
}
