using System.Text;

namespace http.Tests;

public class VarySemanticsTests
{
    [Test]
    public void ParseCombinesLinesDeduplicatesNamesAndSortsCanonically()
    {
        HttpHeaders response = new HttpHeaders();
        AddField(response, HttpHeaderNames.Vary, "X-Mode, Accept-Encoding");
        AddField(response, HttpHeaderNames.Vary, "accept-language, x-mode");

        HttpHeaders result = VarySemantics.Parse(response);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fields, Has.Count.EqualTo(3));
            Assert.That(result.Fields[0].Name, Is.EqualTo("accept-encoding"));
            Assert.That(result.Fields[1].Name, Is.EqualTo("accept-language"));
            Assert.That(result.Fields[2].Name, Is.EqualTo("x-mode"));
            Assert.That(result.Fields[0].Value, Is.Empty);
        });
    }

    [Test]
    public void ParseDistinguishesAbsentEmptyAndWildcardVary()
    {
        HttpHeaders absent = VarySemantics.Parse(new HttpHeaders());
        HttpHeaders source = new HttpHeaders();
        AddField(source, HttpHeaderNames.Vary, "");
        HttpHeaders empty = VarySemantics.Parse(source);
        HttpHeaders wildcardSource = new HttpHeaders();
        AddField(wildcardSource, HttpHeaderNames.Vary, "*");
        HttpHeaders wildcard = VarySemantics.Parse(wildcardSource);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Fields, Is.Empty);
            Assert.That(empty.Fields, Is.Empty);
            Assert.That(wildcard.Fields, Has.Count.EqualTo(1));
            Assert.That(wildcard.Fields[0].Name, Is.EqualTo("*"));
        });
    }

    [Test]
    public void ParseRejectsMalformedAndMixedWildcardValues()
    {
        HttpHeaders malformed = new HttpHeaders();
        AddField(malformed, HttpHeaderNames.Vary, "Accept Encoding");
        HttpHeaders mixed = new HttpHeaders();
        AddField(mixed, HttpHeaderNames.Vary, "*, Accept-Encoding");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpFieldException>(() => VarySemantics.Parse(malformed));
            HttpFieldException? error = Assert.Throws<HttpFieldException>(
                () => VarySemantics.Parse(mixed));
            Assert.That(error!.DiagnosticCode, Is.EqualTo("ambiguous-vary-wildcard"));
        });
    }

    [Test]
    public void KeyNamesAreCaseOrderAndDuplicateInsensitive()
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, "Accept-Encoding", "gzip");
        AddField(request.Headers, "X-Mode", "fast");
        HttpHeaders first = ParsedNames("X-Mode", "accept-encoding", "x-mode");
        HttpHeaders second = ParsedNames("ACCEPT-ENCODING", "x-MODE");

        string firstKey = VarySemantics.BuildKey(request, first);
        string secondKey = VarySemantics.BuildKey(request, second);

        Assert.That(VarySemantics.CompareKeys(firstKey, secondKey), Is.True);
    }

    [Test]
    public void KeyDistinguishesAbsentFieldFromPresentEmptyField()
    {
        HttpHeaders names = ParsedNames("X-Mode");
        HttpRequest absent = new HttpRequest();
        HttpRequest empty = new HttpRequest();
        AddField(empty.Headers, "x-mode", "");

        string absentKey = VarySemantics.BuildKey(absent, names);
        string emptyKey = VarySemantics.BuildKey(empty, names);

        Assert.Multiple(() =>
        {
            Assert.That(absentKey, Does.Contain("=A;"));
            Assert.That(emptyKey, Does.Contain("=P1;0:;"));
            Assert.That(VarySemantics.CompareKeys(absentKey, emptyKey), Is.False);
        });
    }

    [Test]
    public void CombinableDuplicateLinesMatchOneCombinedNormalizedValue()
    {
        HttpHeaders names = ParsedNames(HttpHeaderNames.AcceptEncoding);
        HttpRequest duplicate = new HttpRequest();
        AddField(duplicate.Headers, HttpHeaderNames.AcceptEncoding, "gzip");
        AddField(duplicate.Headers, "accept-encoding", "br");
        HttpRequest combined = new HttpRequest();
        AddField(combined.Headers, HttpHeaderNames.AcceptEncoding, " gzip ,\t br ");

        string duplicateKey = VarySemantics.BuildKey(duplicate, names);
        string combinedKey = VarySemantics.BuildKey(combined, names);

        Assert.That(VarySemantics.CompareKeys(duplicateKey, combinedKey), Is.True);
    }

    [Test]
    public void UnknownDuplicateFieldsPreserveLineCountAndOrder()
    {
        HttpHeaders names = ParsedNames("X-Mode");
        HttpRequest duplicate = new HttpRequest();
        AddField(duplicate.Headers, "X-Mode", "a");
        AddField(duplicate.Headers, "X-Mode", "b");
        HttpRequest combined = new HttpRequest();
        AddField(combined.Headers, "X-Mode", "a, b");
        HttpRequest reversed = new HttpRequest();
        AddField(reversed.Headers, "X-Mode", "b");
        AddField(reversed.Headers, "X-Mode", "a");

        string duplicateKey = VarySemantics.BuildKey(duplicate, names);

        Assert.Multiple(() =>
        {
            Assert.That(VarySemantics.CompareKeys(
                duplicateKey,
                VarySemantics.BuildKey(combined, names)), Is.False);
            Assert.That(VarySemantics.CompareKeys(
                duplicateKey,
                VarySemantics.BuildKey(reversed, names)), Is.False);
        });
    }

    [Test]
    public void ValueNormalizationCollapsesOptionalWhitespaceButPreservesQuotedContent()
    {
        HttpHeaders names = ParsedNames(HttpHeaderNames.AcceptEncoding);
        HttpRequest first = new HttpRequest();
        AddField(first.Headers, HttpHeaderNames.AcceptEncoding, "  gzip ,\t br  ");
        HttpRequest second = new HttpRequest();
        AddField(second.Headers, HttpHeaderNames.AcceptEncoding, "gzip, br");
        HttpHeaders acceptNames = ParsedNames(HttpHeaderNames.Accept);
        HttpRequest quotedFirst = new HttpRequest();
        AddField(quotedFirst.Headers, HttpHeaderNames.Accept,
            "text/plain; profile=\"a  b\"");
        HttpRequest quotedSecond = new HttpRequest();
        AddField(quotedSecond.Headers, HttpHeaderNames.Accept,
            "text/plain; profile=\"a b\"");

        Assert.Multiple(() =>
        {
            Assert.That(VarySemantics.CompareKeys(
                VarySemantics.BuildKey(first, names),
                VarySemantics.BuildKey(second, names)), Is.True);
            Assert.That(VarySemantics.CompareKeys(
                VarySemantics.BuildKey(quotedFirst, acceptNames),
                VarySemantics.BuildKey(quotedSecond, acceptNames)), Is.False);
        });
    }

    [Test]
    public void HexEncodingKeepsOpaqueValuesUnambiguous()
    {
        HttpHeaders names = ParsedNames("X-Opaque");
        HttpRequest first = new HttpRequest();
        AddField(first.Headers, "X-Opaque", new byte[] { 0x80, 0xff, 0x3a });
        HttpRequest second = new HttpRequest();
        AddField(second.Headers, "X-Opaque", new byte[] { 0x80, 0xff, 0x3b });

        string firstKey = VarySemantics.BuildKey(first, names);
        string secondKey = VarySemantics.BuildKey(second, names);

        Assert.Multiple(() =>
        {
            Assert.That(firstKey, Does.Contain("80ff3a"));
            Assert.That(VarySemantics.CompareKeys(firstKey, secondKey), Is.False);
        });
    }

    [Test]
    public void WildcardKeysNeverMatch()
    {
        HttpHeaders wildcard = ParsedNames("*");
        string first = VarySemantics.BuildKey(new HttpRequest(), wildcard);
        string second = VarySemantics.BuildKey(new HttpRequest(), wildcard);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("V1;*"));
            Assert.That(VarySemantics.CompareKeys(first, second), Is.False);
        });
    }

    static HttpHeaders ParsedNames(params string[] names)
    {
        HttpHeaders result = new HttpHeaders();
        int index = 0;
        while (index < names.Length)
        {
            HttpHeaderField field = new HttpHeaderField();
            field.Name = names[index];
            result.Fields.Add(field);
            index = index + 1;
        }

        return result;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        AddField(headers, name, Encoding.ASCII.GetBytes(value));
    }

    static void AddField(HttpHeaders headers, string name, byte[] value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        headers.Fields.Add(field);
    }
}
