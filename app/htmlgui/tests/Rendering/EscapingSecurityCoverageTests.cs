using System;
using htmlgui;

namespace htmlgui.Tests;

public class EscapingSecurityCoverageTests
{
    [Test]
    public void RejectsOrReplacesMalformedUnicodeAndNoncharacterBoundaries()
    {
        string[] invalidValues = new string[]
        {
            "\0",
            "\ud800",
            "\udc00",
            "\ufdd0",
            "\ufdef",
            "\ufffe",
            "\uffff",
            "\ud83f\udffe",
            "\ud83f\udfff",
            "\udbff\udffe",
            "\udbff\udfff"
        };

        int index = 0;
        while (index < invalidValues.Length)
        {
            string invalid = invalidValues[index];
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeText(invalid, HtmlInvalidCharacterPolicy.Reject), index.ToString());
            Assert.That(HtmlEscaper.EscapeText(invalid, HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("\uFFFD"), index.ToString());
            Assert.That(HtmlSecurityPolicy.IsSafeResourceUrl("/" + invalid), Is.False, index.ToString());
            index++;
        }

        Assert.That(HtmlEscaper.EscapeText("\udbff\udffd", HtmlInvalidCharacterPolicy.Reject), Is.EqualTo("\udbff\udffd"));
    }

    [Test]
    public void EscapesMarkupMetacharactersAndEntityLookalikesAsLiteralText()
    {
        string value = "&amp; &#38; &#x26; &notit; <tag> \"double\" 'single'";

        Assert.Multiple(() =>
        {
            Assert.That(
                HtmlEscaper.EscapeText(value, HtmlInvalidCharacterPolicy.Reject),
                Is.EqualTo("&amp;amp; &amp;#38; &amp;#x26; &amp;notit; &lt;tag&gt; \"double\" 'single'"));
            Assert.That(
                HtmlEscaper.EscapeAttributeValue(value, HtmlInvalidCharacterPolicy.Reject),
                Is.EqualTo("&amp;amp; &amp;#38; &amp;#x26; &amp;notit; &lt;tag&gt; &quot;double&quot; &#39;single&#39;"));
            Assert.That(
                HtmlEscaper.EscapeAttributeValue("javascript&#58;alert(1)", HtmlInvalidCharacterPolicy.Reject),
                Is.EqualTo("javascript&amp;#58;alert(1)"));
        });
    }

    [Test]
    public void ProtectsEveryScriptStyleAndJsonEndTagBoundary()
    {
        char[] boundaries = new char[] { '>', '/', ' ', '\t', '\n', '\r', '\f' };
        int index = 0;
        while (index < boundaries.Length)
        {
            string scriptToken = "</ScRiPt" + boundaries[index] + "payload";
            string styleToken = "</StYlE" + boundaries[index] + "payload";
            Assert.That(HtmlSecurityPolicy.ProtectScriptClosingTokens(scriptToken), Is.EqualTo("\\x3C" + scriptToken.Substring(1)), index.ToString());
            Assert.That(HtmlSecurityPolicy.ProtectJsonClosingTokens(scriptToken), Is.EqualTo("\\u003C" + scriptToken.Substring(1)), index.ToString());
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeRawText(styleToken, HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject), index.ToString());
            Assert.That(HtmlEscaper.EscapeRawText(styleToken, HtmlElements.Style, HtmlInvalidCharacterPolicy.Replace), Is.EqualTo("\uFFFD" + styleToken.Substring(1)), index.ToString());
            index++;
        }

        string lookalikes = "</scripture> < /script> &lt;/script>";
        Assert.Multiple(() =>
        {
            Assert.That(HtmlSecurityPolicy.ProtectScriptClosingTokens(lookalikes), Is.EqualTo(lookalikes));
            Assert.That(HtmlSecurityPolicy.ProtectJsonClosingTokens(lookalikes), Is.EqualTo(lookalikes));
            Assert.That(HtmlSecurityPolicy.ProtectScriptClosingTokens("</script"), Is.EqualTo("</script"));
            Assert.That(HtmlSecurityPolicy.ProtectJsonClosingTokens("</script"), Is.EqualTo("</script"));
            Assert.That(HtmlEscaper.EscapeRawText("</stylesheet> </style", HtmlElements.Style, HtmlInvalidCharacterPolicy.Reject), Is.EqualTo("</stylesheet> </style"));
        });
    }

    [Test]
    public void RejectsOrNeutralizesMaliciousCommentDelimiters()
    {
        string[] invalidValues = new string[] { "-start", "end-", "a--b", "-->", "<!--", ">close", "--!>", "--->" };
        int index = 0;
        while (index < invalidValues.Length)
        {
            string invalid = invalidValues[index];
            Assert.Throws<ArgumentException>(() => HtmlEscaper.EscapeComment(invalid, HtmlInvalidCharacterPolicy.Reject), invalid);
            string replaced = HtmlEscaper.EscapeComment(invalid, HtmlInvalidCharacterPolicy.Replace);
            Assert.That(replaced, Does.Not.Contain("--"), invalid);
            Assert.That(replaced.StartsWith(">", StringComparison.Ordinal), Is.False, invalid);
            Assert.That(replaced.StartsWith("-", StringComparison.Ordinal), Is.False, invalid);
            Assert.That(replaced.EndsWith("-", StringComparison.Ordinal), Is.False, invalid);
            index++;
        }

        Assert.That(HtmlEscaper.EscapeComment("safe <tag> & \"quoted\"", HtmlInvalidCharacterPolicy.Reject), Is.EqualTo("safe <tag> & \"quoted\""));
    }

    [Test]
    public void RejectsMaliciousAndAuthorityConfusedResourceUrls()
    {
        string[] rejected = new string[]
        {
            "javascript:alert(1)",
            "java\tscript:alert(1)",
            "data:text/html,<script>alert(1)</script>",
            "https:///missing-host",
            "http://?query",
            "https://#fragment",
            "blob:https:///missing-host",
            "blob:http://?query",
            "blob:javascript:alert(1)",
            "/asset%GG.css",
            "/asset\\name.css",
            "/asset\ufdd0.css"
        };

        int index = 0;
        while (index < rejected.Length)
        {
            string value = rejected[index];
            Assert.That(HtmlSecurityPolicy.IsSafeResourceUrl(value), Is.False, value);
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.NormalizeResourceUrl(value), value);
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeUrl(value), value);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSecurityPolicy.IsSafeResourceUrl("blob:https://example.test/id"), Is.True);
            Assert.That(CssEscaper.EscapeUrl("blob:https://example.test/id"), Is.EqualTo("url(\"blob:https://example.test/id\")"));
            Assert.That(HtmlSecurityPolicy.IsSafeResourceUrl("javascript&#58;alert(1)"), Is.True);
        });
    }

    [Test]
    public void RejectsCssObfuscationAndEscapesTrustedStyleTerminators()
    {
        string[] rejected = new string[]
        {
            "u/**/rl(javascript:alert(1))",
            "u\\72l(javascript:alert(1))",
            "expre\\73sion(alert(1))",
            "red/*comment*/",
            "red;position:fixed",
            "red\0blue",
            "red\ud800blue"
        };

        int index = 0;
        while (index < rejected.Length)
        {
            Assert.Throws<ArgumentException>(() => CssEscaper.EscapeDeclarationValue(CssProperties.Color, rejected[index], false), rejected[index]);
            index++;
        }

        string escaped = CssEscaper.EscapeDeclarationValue("content", "\"</StYlE >\"", true);
        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("\"\\3c /StYlE \\3e \""));
            Assert.That(escaped, Does.Not.Contain("</StYlE"));
        });
    }

    [Test]
    public void DeniesRawContentWithoutAnExplicitValidTrustPolicy()
    {
        UiRawContentPolicy[] deniedPolicies = new UiRawContentPolicy[]
        {
            new UiRawContentPolicy { IsTrusted = false, PolicyId = "reviewed" },
            new UiRawContentPolicy { IsTrusted = true, PolicyId = string.Empty },
            new UiRawContentPolicy { IsTrusted = true, PolicyId = "contains space" },
            new UiRawContentPolicy { IsTrusted = true, PolicyId = "réviewed" },
            new UiRawContentPolicy { IsTrusted = true, PolicyId = "$renderer:custom" },
            new UiRawContentPolicy { IsTrusted = true, PolicyId = new string('a', 129) }
        };

        Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.ValidateRawContentTrust(null, "<b>raw</b>"));
        int index = 0;
        while (index < deniedPolicies.Length)
        {
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.ValidateRawContentTrust(deniedPolicies[index], "<b>raw</b>"), index.ToString());
            index++;
        }

        UiRawContentPolicy trusted = new UiRawContentPolicy { IsTrusted = true, PolicyId = "reviewed-markup" };
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.ValidateRawContentTrust(trusted, "raw\ud800content"));
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.ValidateRawContentTrust(trusted, "raw\0content"));
            Assert.That(HtmlSecurityPolicy.ValidateRawContentTrust(trusted, "<b>raw</b>"), Is.EqualTo("reviewed-markup"));
        });
    }
}
