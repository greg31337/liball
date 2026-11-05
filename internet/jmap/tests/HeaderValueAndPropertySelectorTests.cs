using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class HeaderValueAndPropertySelectorTests
{
    [Test]
    public void TextFormDecodesQBase64AndLegacyCharsets()
    {
        HeaderValueParser parser = new HeaderValueParser();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parser.DecodeText(" =?UTF-8?Q?caf=C3=A9?="), Is.EqualTo("café"));
            Assert.That(parser.DecodeText("=?UTF-8?B?5pel5pys6Kqe?="), Is.EqualTo("日本語"));
            Assert.That(parser.DecodeText("=?ISO-8859-1?Q?Andr=E9?="), Is.EqualTo("André"));
            Assert.That(parser.DecodeText("=?ISO-8859-2?Q?=A3=F3d=BC?="), Is.EqualTo("Łódź"));
            Assert.That(parser.DecodeText("=?windows-1252?Q?=93quoted=94?="), Is.EqualTo("“quoted”"));
        }
    }

    [Test]
    public void TextFormUnfoldsAndSuppressesWhitespaceOnlyBetweenEncodedWords()
    {
        HeaderValueParser parser = new HeaderValueParser();
        string adjacent = " =?UTF-8?Q?Hello?=\r\n  =?UTF-8?Q?_world?=";
        string mixed = " plain  =?UTF-8?Q?text?= tail";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parser.DecodeText(adjacent), Is.EqualTo("Hello world"));
            Assert.That(parser.DecodeText(mixed), Is.EqualTo("plain  text tail"));
            Assert.That(parser.DecodeText("  folded\r\n\tvalue"), Is.EqualTo("folded\tvalue"));
        }
    }

    [Test]
    public void TextFormHonorsPlacementAndRecoversFromDecodeFailures()
    {
        HeaderValueParser parser = new HeaderValueParser();
        string misplaced = "prefix=?UTF-8?Q?decoded?=";
        string unsupportedEncoding = "=?UTF-8?X?abc?=";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parser.DecodeText(misplaced), Is.EqualTo(misplaced));
            Assert.That(parser.DecodeText(unsupportedEncoding), Is.EqualTo(unsupportedEncoding));
            Assert.That(parser.DecodeText("=?UTF-8?Q?bad=GG?="), Is.EqualTo("�"));
            Assert.That(parser.DecodeText("=?unknown-charset?Q?abc?="), Is.EqualTo("�"));
            Assert.That(parser.DecodeText("=?US-ASCII?Q?A=00=09B?="), Is.EqualTo("AB"));
            Assert.That(parser.DecodeText("Cafe\u0301"), Is.EqualTo("Café"));
        }
    }

    [Test]
    public void SelectorReturnsLastOrAllRepeatedFieldsWithCaseInsensitiveNames()
    {
        HeaderParser parser = new HeaderParser();
        HeaderParseResult parsed = parser.ParseHeaders(Encoding.UTF8.GetBytes(
            "X-Title: =?UTF-8?Q?First?=\r\nx-title: second\r\n folded\r\nX-Title: =?UTF-8?B?VGhpcmQ=?=\r\n\r\n"));
        HeaderPropertySelector selector = new HeaderPropertySelector();

        JsonElement lastRaw = selector.SelectProperty(parsed.Headers, "header:X-TITLE");
        JsonElement allRaw = selector.SelectProperty(parsed.Headers, "header:x-title:asRaw:all");
        JsonElement allText = selector.SelectProperty(parsed.Headers, "header:X-Title:asText:all");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lastRaw.GetString(), Is.EqualTo(" =?UTF-8?B?VGhpcmQ=?="));
            Assert.That(allRaw.EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[]
            {
                " =?UTF-8?Q?First?=",
                " second\r\n folded",
                " =?UTF-8?B?VGhpcmQ=?="
            }));
            Assert.That(allText.EnumerateArray().Select(value => value.GetString()),
                Is.EqualTo(new[] { "First", "second folded", "Third" }));
        }
    }

    [Test]
    public void SelectorUsesNullAndEmptyArrayForMissingFields()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        List<EmailHeader> headers = new List<EmailHeader>();

        JsonElement single = selector.SelectProperty(headers, "header:X-Missing:asText");
        JsonElement all = selector.SelectProperty(headers, "header:X-Missing:asText:all");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(single.ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(all.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(all.GetArrayLength(), Is.Zero);
        }
    }

    [Test]
    public void SelectorParsesSuffixesAndDefinesNaturalKnownHeaderForms()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        HeaderSelection raw = selector.ParseProperty("header:Subject");
        HeaderSelection textAll = selector.ParseProperty("header:Subject:asText:all");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(raw.Form, Is.EqualTo(JmapHeaderForm.Raw));
            Assert.That(raw.HasExplicitForm, Is.False);
            Assert.That(textAll.Form, Is.EqualTo(JmapHeaderForm.Text));
            Assert.That(textAll.IsAll, Is.True);
            Assert.That(selector.DefaultForm("Subject"), Is.EqualTo(JmapHeaderForm.Text));
            Assert.That(selector.DefaultForm("From"), Is.EqualTo(JmapHeaderForm.Addresses));
            Assert.That(selector.DefaultForm("Message-ID"), Is.EqualTo(JmapHeaderForm.MessageIds));
            Assert.That(selector.DefaultForm("Date"), Is.EqualTo(JmapHeaderForm.Date));
            Assert.That(selector.DefaultForm("List-Post"), Is.EqualTo(JmapHeaderForm.Urls));
            Assert.That(selector.DefaultForm("Received"), Is.EqualTo(JmapHeaderForm.Raw));
            Assert.That(selector.DefaultForm("X-Custom"), Is.EqualTo(JmapHeaderForm.Raw));
        }
    }

    [Test]
    public void SelectorRejectsInvalidNamesSuffixOrderAndForbiddenTextForms()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();

        Assert.Multiple(() =>
        {
            Assert.That(() => selector.ParseProperty("header:"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => selector.ParseProperty("header:Bad Name"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => selector.ParseProperty("header:Subject:all:asText"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => selector.ParseProperty("header:Subject:asFuture"), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => selector.ParseProperty("header:From:asText"), Throws.TypeOf<JmapValidationException>());
            Assert.That(selector.SelectProperty(new List<EmailHeader>(), "header:From:asAddresses").ValueKind,
                Is.EqualTo(JsonValueKind.Null));
            Assert.That(selector.ParseProperty("header:X-Custom:asText").Form, Is.EqualTo(JmapHeaderForm.Text));
        });
    }
}
