using System.Globalization;

namespace UniversalHtml.Tests;

[TestFixture]
public class AsciiTests
{
    [Test]
    public void ClassificationIsExhaustiveAcrossUtf16CodeUnits()
    {
        for (int code = 0; code <= char.MaxValue; code++)
        {
            char value = (char)code;
            bool upper = code >= 'A' && code <= 'Z';
            bool lower = code >= 'a' && code <= 'z';
            bool digit = code >= '0' && code <= '9';

            Assert.That(Ascii.IsUpper(value), Is.EqualTo(upper), $"upper U+{code:X4}");
            Assert.That(Ascii.IsLower(value), Is.EqualTo(lower), $"lower U+{code:X4}");
            Assert.That(Ascii.IsAlpha(value), Is.EqualTo(upper || lower), $"alpha U+{code:X4}");
            Assert.That(Ascii.IsDigit(value), Is.EqualTo(digit), $"digit U+{code:X4}");
            Assert.That(Ascii.IsAlphanumeric(value), Is.EqualTo(upper || lower || digit), $"alphanumeric U+{code:X4}");
            Assert.That(Ascii.IsControl(value), Is.EqualTo(code <= 31 || code == 127), $"control U+{code:X4}");
        }

        Assert.That(Ascii.IsAscii(-1), Is.False);
        Assert.That(Ascii.IsAscii(0), Is.True);
        Assert.That(Ascii.IsAscii(127), Is.True);
        Assert.That(Ascii.IsAscii(128), Is.False);
    }

    [Test]
    public void HexClassificationAndValuesAreExhaustive()
    {
        for (int code = 0; code <= char.MaxValue; code++)
        {
            char value = (char)code;
            int expected = -1;
            if (code >= '0' && code <= '9')
            {
                expected = code - '0';
            }
            else if (code >= 'A' && code <= 'F')
            {
                expected = code - 'A' + 10;
            }
            else if (code >= 'a' && code <= 'f')
            {
                expected = code - 'a' + 10;
            }

            Assert.That(Ascii.IsHexDigit(value), Is.EqualTo(expected >= 0), $"U+{code:X4}");
            Assert.That(Ascii.GetHexValue(value), Is.EqualTo(expected), $"U+{code:X4}");
        }
    }

    [Test]
    public void HtmlWhitespaceAndNameDelimitersAreExhaustive()
    {
        for (int code = 0; code <= char.MaxValue; code++)
        {
            char value = (char)code;
            bool whitespace = value == '\t' || value == '\n' || value == '\f' || value == '\r' || value == ' ';
            bool tagDelimiter = whitespace || value == '/' || value == '>';
            bool attributeDelimiter = tagDelimiter || value == '=';

            Assert.That(Ascii.IsHtmlWhitespace(value), Is.EqualTo(whitespace), $"whitespace U+{code:X4}");
            Assert.That(UnicodeTools.IsHtmlWhitespace(value), Is.EqualTo(whitespace), $"UnicodeTools whitespace U+{code:X4}");
            Assert.That(Ascii.IsTagNameDelimiter(value), Is.EqualTo(tagDelimiter), $"tag U+{code:X4}");
            Assert.That(Ascii.IsAttributeNameDelimiter(value), Is.EqualTo(attributeDelimiter), $"attribute U+{code:X4}");
        }
    }

    [Test]
    [NonParallelizable]
    public void CaseFoldingIsAsciiOnlyAndCultureInvariant()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

            for (int code = 0; code <= char.MaxValue; code++)
            {
                char value = (char)code;
                char expectedLower = code >= 'A' && code <= 'Z' ? (char)(code + 32) : value;
                char expectedUpper = code >= 'a' && code <= 'z' ? (char)(code - 32) : value;
                Assert.That(Ascii.ToLower(value), Is.EqualTo(expectedLower), $"lower U+{code:X4}");
                Assert.That(Ascii.ToUpper(value), Is.EqualTo(expectedUpper), $"upper U+{code:X4}");
            }

            Assert.That(Ascii.EqualsIgnoreCase("TITLE".AsSpan(), "title".AsSpan()), Is.True);
            Assert.That(Ascii.EqualsIgnoreCase("TITLE".AsSpan(), "tıtle".AsSpan()), Is.False);
            Assert.That(Ascii.StartsWithIgnoreCase("DoCtYpE html".AsSpan(), "doctype".AsSpan()), Is.True);
            Assert.That(Ascii.EndsWithIgnoreCase("image.SvG".AsSpan(), ".svg".AsSpan()), Is.True);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void SpanCaseComparisonsHandleLengthsAndNonAsciiLiterally()
    {
        Assert.That(Ascii.EqualsIgnoreCase(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty), Is.True);
        Assert.That(Ascii.EqualsIgnoreCase("a".AsSpan(), "aa".AsSpan()), Is.False);
        Assert.That(Ascii.EqualsIgnoreCase("Ä".AsSpan(), "ä".AsSpan()), Is.False);
        Assert.That(Ascii.StartsWithIgnoreCase("ab".AsSpan(), "abc".AsSpan()), Is.False);
        Assert.That(Ascii.EndsWithIgnoreCase("ab".AsSpan(), "abc".AsSpan()), Is.False);
    }
}
