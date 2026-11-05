using System;
using NUnit.Framework;

namespace htmlgui.Tests;

public class HtmlEscaperTests
{
    [Test]
    public void EscapesTextMetacharactersWithoutChangingValidUnicode()
    {
        string escaped = HtmlEscaper.EscapeText("Fish &amp; <chips> \"hot\" 'now' 😀", HtmlInvalidCharacterPolicy.Reject);

        Assert.That(escaped, Is.EqualTo("Fish &amp;amp; &lt;chips&gt; \"hot\" 'now' 😀"));
    }

    [Test]
    public void EscapesEveryQuotedAttributeDelimiterAndEntityLookalike()
    {
        string escaped = HtmlEscaper.EscapeAttributeValue("&quot; <tag> \"double\" 'single' 😀", HtmlInvalidCharacterPolicy.Reject);

        Assert.That(escaped, Is.EqualTo("&amp;quot; &lt;tag&gt; &quot;double&quot; &#39;single&#39; 😀"));
    }

    [Test]
    public void ReplacementPolicyReplacesMalformedAndForbiddenUnicodeScalars()
    {
        string input = "a\0b\ud800c\udc00d\ufffee\ud83f\udffef";

        string normalized = HtmlEscaper.NormalizeHtmlUnicode(input, HtmlInvalidCharacterPolicy.Replace);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo("a�b�c�d�e�f"));
            Assert.That(HtmlEscaper.EscapeText(input, HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("a�b�c�d�e�f"));
            Assert.That(HtmlEscaper.EscapeAttributeValue(input, HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("a�b�c�d�e�f"));
            Assert.That(HtmlEscaper.EscapeComment(input, HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("a�b�c�d�e�f"));
            Assert.That(HtmlEscaper.EscapeRawText(input, HtmlElements.Script, HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("a�b�c�d�e�f"));
        });
    }

    [Test]
    public void RejectionPolicyRejectsMalformedAndForbiddenUnicodeScalars()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeText("before\ud800after", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeAttributeValue("before\udc00after", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeComment("before\0after", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeRawText("before\uffffafter", HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.NormalizeHtmlUnicode("before\ud83f\udffeafter", HtmlInvalidCharacterPolicy.Reject));
        });
    }

    [Test]
    public void CommentContextPreservesSafeContentAndMakesDelimitersSafe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlEscaper.EscapeComment("first <tag> & second 😀", HtmlInvalidCharacterPolicy.Reject), Is.EqualTo("first <tag> & second 😀"));
            Assert.That(HtmlEscaper.EscapeComment("-start a--b end-", HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("�start a-�b end�"));
            Assert.That(HtmlEscaper.EscapeComment(">close", HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("�close"));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeComment("a--b", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeComment("-start", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeComment("end-", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeComment(">close", HtmlInvalidCharacterPolicy.Reject));
        });
    }

    [Test]
    public void RawTextContextDetectsOnlyMatchingHtmlEndTagDelimiters()
    {
        string input = "safe </scripture> &lt;/script> </ScRiPt > tail";

        string replaced = HtmlEscaper.EscapeRawText(input, HtmlElements.Script, HtmlInvalidCharacterPolicy.Replace);

        Assert.Multiple(() =>
        {
            Assert.That(replaced, Is.EqualTo("safe </scripture> &lt;/script> �/ScRiPt > tail"));
            Assert.That(HtmlEscaper.EscapeRawText("body::after{content:'</stylesheet>';}", HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject), Is.EqualTo("body::after{content:'</stylesheet>';}"));
            Assert.That(HtmlEscaper.EscapeRawText("let x = '</script';", HtmlElements.Script, HtmlInvalidCharacterPolicy.Reject), Is.EqualTo("let x = '</script';"));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeRawText("before</STYLE/ >after", HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject));
        });
    }

    [Test]
    public void RejectsNullValuesInvalidPoliciesAndMalformedRawTextElementNames()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => HtmlEscaper.EscapeText(null, HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentNullException>(() => HtmlEscaper.EscapeAttributeValue(null, HtmlInvalidCharacterPolicy.Replace));
            Assert.Throws<ArgumentNullException>(() => HtmlEscaper.EscapeComment(null, HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentNullException>(() => HtmlEscaper.EscapeRawText(null, HtmlElements.Script, HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeText("safe", (HtmlInvalidCharacterPolicy)99));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeRawText("safe", "script name", HtmlInvalidCharacterPolicy.Reject));
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeRawText("safe", string.Empty, HtmlInvalidCharacterPolicy.Reject));
        });
    }
}
