using System.Text;

namespace http.Tests;

public class HttpHeaderServiceTests
{
    static readonly string TokenCharacters =
        "!#$%&'*+-.^_`|~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    [Test]
    public void FieldNameValidationAcceptsTokensAndRejectsInvalidNames()
    {
        HttpHeaderService service = new HttpHeaderService();

        Assert.Multiple(() =>
        {
            Assert.That(service.ValidateFieldName("Content-Type"), Is.True);
            Assert.That(service.ValidateFieldName("X!#$%&'*+-.^_`|~09"), Is.True);
            Assert.That(service.ValidateFieldName(""), Is.False);
            Assert.That(service.ValidateFieldName("Bad Name"), Is.False);
            Assert.That(service.ValidateFieldName("Bad:Name"), Is.False);
            Assert.That(service.ValidateFieldName("caf\u00e9"), Is.False);
        });
    }

    [Test]
    public void FieldNameValidationCoversEveryAsciiCharacter()
    {
        IHttpHeaderService service = new HttpHeaderService();

        Assert.Multiple(() =>
        {
            for (int value = 0; value <= 0x7f; value++)
            {
                string name = new string((char)value, 1);
                bool expected = TokenCharacters.IndexOf((char)value) >= 0;
                Assert.That(service.ValidateFieldName(name),
                    Is.EqualTo(expected), $"ASCII 0x{value:x2}");
            }

            Assert.That(service.ValidateFieldName(null!), Is.False);
            Assert.That(service.ValidateFieldName("caf\u00e9"), Is.False);
            Assert.That(service.ValidateFieldName("\ud83d\ude00"), Is.False);
        });
    }

    [Test]
    public void FieldValueValidationPreservesVisibleAndObsTextButRejectsControls()
    {
        HttpHeaderService service = new HttpHeaderService();

        Assert.Multiple(() =>
        {
            Assert.That(service.ValidateFieldValue(Array.Empty<byte>()), Is.True);
            Assert.That(service.ValidateFieldValue(new byte[] { 0x09, 0x20, 0x21, 0x7e, 0x80, 0xff }), Is.True);
            Assert.That(service.ValidateFieldValue(new byte[] { 0x00 }), Is.False);
            Assert.That(service.ValidateFieldValue(new byte[] { 0x0a }), Is.False);
            Assert.That(service.ValidateFieldValue(new byte[] { 0x0d }), Is.False);
            Assert.That(service.ValidateFieldValue(new byte[] { 0x1f }), Is.False);
            Assert.That(service.ValidateFieldValue(new byte[] { 0x7f }), Is.False);
        });
    }

    [Test]
    public void FieldValueValidationCoversEveryOctet()
    {
        IHttpHeaderService service = new HttpHeaderService();

        Assert.Multiple(() =>
        {
            for (int value = 0; value <= 0xff; value++)
            {
                bool expected = value == 0x09 ||
                    value >= 0x20 && value != 0x7f;
                Assert.That(service.ValidateFieldValue(
                    new byte[] { (byte)value }), Is.EqualTo(expected),
                    $"Octet 0x{value:x2}");
            }

            Assert.That(service.ValidateFieldValue(null!), Is.False);
        });
    }

    [Test]
    public void AddRetainsOrderDuplicatesOriginalSpellingAndExactValueBytes()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField first = Field("X-Trace", new byte[] { 0x61, 0x80 });
        HttpHeaderField middle = Field("Other", "middle"u8.ToArray());
        HttpHeaderField duplicate = Field("x-trace", new byte[] { 0x62, 0xff });

        service.AddField(headers, first);
        service.AddField(headers, middle);
        service.AddField(headers, duplicate);

        Assert.Multiple(() =>
        {
            Assert.That(headers.Fields, Is.EqualTo(new[] { first, middle, duplicate }));
            Assert.That(headers.Fields[0].Name, Is.EqualTo("X-Trace"));
            Assert.That(headers.Fields[2].Name, Is.EqualTo("x-trace"));
            Assert.That(headers.Fields[0].Value, Is.SameAs(first.Value));
            Assert.That(headers.Fields[0].Value, Is.EqualTo(new byte[] { 0x61, 0x80 }));
            Assert.That(headers.Fields[2].Value, Is.EqualTo(new byte[] { 0x62, 0xff }));
        });
    }

    [Test]
    public void InvalidAddAndSetDoNotMutateTheCollection()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField original = Field("Original", "value"u8.ToArray());
        headers.Fields.Add(original);

        HttpFieldException nameError = Assert.Throws<HttpFieldException>(() =>
            service.AddField(headers, Field("Bad Name", "x"u8.ToArray())));
        HttpFieldException valueError = Assert.Throws<HttpFieldException>(() =>
            service.SetField(headers, Field("Original", new byte[] { 0x61, 0x0d, 0x0a })));

        Assert.Multiple(() =>
        {
            Assert.That(nameError.DiagnosticCode, Is.EqualTo("invalid-field-name"));
            Assert.That(valueError.DiagnosticCode, Is.EqualTo("invalid-field-value"));
            Assert.That(headers.Fields, Is.EqualTo(new[] { original }));
        });
    }

    [TestCase(0x00)]
    [TestCase(0x0a)]
    [TestCase(0x0d)]
    public void InjectionOctetsAreRejectedAtomicallyByEveryMutation(
        int injected)
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField original = Field("X-Test", "safe"u8.ToArray());
        headers.Fields.Add(original);
        byte[] value = new byte[] { (byte)'a', (byte)injected, (byte)'b' };

        HttpFieldException addError = Assert.Throws<HttpFieldException>(() =>
            service.AddField(headers, Field("Injected", value)))!;
        HttpFieldException setError = Assert.Throws<HttpFieldException>(() =>
            service.SetField(headers, Field("X-Test", value)))!;

        Assert.Multiple(() =>
        {
            Assert.That(addError.DiagnosticCode,
                Is.EqualTo("invalid-field-value"));
            Assert.That(setError.DiagnosticCode,
                Is.EqualTo("invalid-field-value"));
            Assert.That(headers.Fields, Is.EqualTo(new[] { original }));
            Assert.That(original.Value, Is.EqualTo("safe"u8.ToArray()));
        });
    }

    [Test]
    public void FindAndRemoveUseAsciiCaseInsensitiveMatching()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField first = Field("X-Test", "one"u8.ToArray());
        HttpHeaderField unrelated = Field("Other", "two"u8.ToArray());
        HttpHeaderField second = Field("x-test", "three"u8.ToArray());
        headers.Fields.Add(first);
        headers.Fields.Add(unrelated);
        headers.Fields.Add(second);

        List<HttpHeaderField> found = service.FindFields(headers, "X-TEST");
        int removed = service.RemoveFields(headers, "x-TeSt");

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.EqualTo(new[] { first, second }));
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(headers.Fields, Is.EqualTo(new[] { unrelated }));
        });
    }

    [Test]
    public void CaseInsensitiveMatchingCoversEveryAsciiLetter()
    {
        IHttpHeaderService service = new HttpHeaderService();

        Assert.Multiple(() =>
        {
            for (char upper = 'A'; upper <= 'Z'; upper++)
            {
                char lower = (char)(upper + ('a' - 'A'));
                HttpHeaders headers = new HttpHeaders();
                HttpHeaderField field = Field(
                    "X-" + upper, new byte[] { (byte)upper });
                headers.Fields.Add(field);

                Assert.That(service.FindFields(
                    headers, "x-" + lower).Single(), Is.SameAs(field),
                    upper.ToString());
                Assert.That(service.RemoveFields(headers, "x-" + lower),
                    Is.EqualTo(1), upper.ToString());
                Assert.That(headers.Fields, Is.Empty, upper.ToString());
            }
        });
    }

    [Test]
    public void FindReturnsAnIndependentOrderedSelection()
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField first = Field("X-Test", "one"u8.ToArray());
        HttpHeaderField second = Field("x-test", "two"u8.ToArray());
        headers.Fields.Add(first);
        headers.Fields.Add(second);

        List<HttpHeaderField> found = service.FindFields(headers, "X-TEST");
        found.Reverse();
        found.Clear();
        List<HttpHeaderField> foundAgain =
            service.FindFields(headers, "x-test");

        Assert.Multiple(() =>
        {
            Assert.That(headers.Fields, Is.EqualTo(new[] { first, second }));
            Assert.That(foundAgain, Is.EqualTo(new[] { first, second }));
            Assert.That(foundAgain, Is.Not.SameAs(headers.Fields));
            Assert.That(service.RemoveFields(headers, "Missing"), Is.Zero);
            Assert.That(headers.Fields, Is.EqualTo(new[] { first, second }));
        });
    }

    [TestCase("")]
    [TestCase("bad name")]
    [TestCase("bad:name")]
    [TestCase("caf\u00e9")]
    public void LookupOperationsRejectInvalidNamesWithoutMutation(
        string name)
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField original = Field("X-Test", "safe"u8.ToArray());
        headers.Fields.Add(original);

        HttpFieldException findError = Assert.Throws<HttpFieldException>(() =>
            service.FindFields(headers, name))!;
        HttpFieldException removeError = Assert.Throws<HttpFieldException>(() =>
            service.RemoveFields(headers, name))!;
        HttpFieldException combineError = Assert.Throws<HttpFieldException>(() =>
            service.CombineFieldValues(headers, name))!;

        Assert.Multiple(() =>
        {
            Assert.That(findError.DiagnosticCode,
                Is.EqualTo("invalid-field-name"));
            Assert.That(removeError.DiagnosticCode,
                Is.EqualTo("invalid-field-name"));
            Assert.That(combineError.DiagnosticCode,
                Is.EqualTo("invalid-field-name"));
            Assert.That(headers.Fields, Is.EqualTo(new[] { original }));
        });
    }

    [Test]
    public void SetReplacesDuplicatesAtTheirFirstPositionAndLeavesOtherOrderIntact()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField before = Field("Before", "a"u8.ToArray());
        HttpHeaderField firstMatch = Field("X-Test", "old-1"u8.ToArray());
        HttpHeaderField middle = Field("Middle", "b"u8.ToArray());
        HttpHeaderField secondMatch = Field("x-test", "old-2"u8.ToArray());
        HttpHeaderField after = Field("After", "c"u8.ToArray());
        HttpHeaderField replacement = Field("X-TEST", "new"u8.ToArray());
        headers.Fields.Add(before);
        headers.Fields.Add(firstMatch);
        headers.Fields.Add(middle);
        headers.Fields.Add(secondMatch);
        headers.Fields.Add(after);

        service.SetField(headers, replacement);

        Assert.Multiple(() =>
        {
            Assert.That(headers.Fields, Is.EqualTo(new[] { before, replacement, middle, after }));
            Assert.That(headers.Fields[1].Name, Is.EqualTo("X-TEST"));
            Assert.That(headers.Fields[1].Value, Is.EqualTo("new"u8.ToArray()));
        });
    }

    [Test]
    public void SetAppendsWhenNoExistingFieldMatches()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField existing = Field("Existing", "one"u8.ToArray());
        HttpHeaderField added = Field("Added", "two"u8.ToArray());
        headers.Fields.Add(existing);

        service.SetField(headers, added);

        Assert.That(headers.Fields, Is.EqualTo(new[] { existing, added }));
    }

    [Test]
    public void ListFieldCombinationPreservesLineOrderAndObsText()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(Field("Accept", new byte[] { 0x74, 0x65, 0x78, 0x74, 0x2f, 0x80 }));
        headers.Fields.Add(Field("Other", "ignored"u8.ToArray()));
        headers.Fields.Add(Field("accept", "application/json"u8.ToArray()));

        byte[] combined = service.CombineFieldValues(headers, "ACCEPT");

        Assert.That(combined, Is.EqualTo(new byte[]
        {
            0x74, 0x65, 0x78, 0x74, 0x2f, 0x80,
            0x2c, 0x20,
            0x61, 0x70, 0x70, 0x6c, 0x69, 0x63, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x2f,
            0x6a, 0x73, 0x6f, 0x6e
        }));
    }

    [TestCaseSource(nameof(CommaCombinableNames))]
    public void EverySupportedListFieldUsesCommaSpaceCombination(string name)
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(Field(name.ToUpperInvariant(),
            "one"u8.ToArray()));
        headers.Fields.Add(Field(name.ToLowerInvariant(),
            "two"u8.ToArray()));

        byte[] combined = service.CombineFieldValues(headers, name);

        Assert.That(combined, Is.EqualTo("one, two"u8.ToArray()), name);
    }

    [Test]
    public void CombinationPreservesEmptyMembersAndReturnsOwnedOutput()
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(Field("Accept", Array.Empty<byte>()));
        headers.Fields.Add(Field("accept", "two"u8.ToArray()));

        byte[] combined = service.CombineFieldValues(headers, "ACCEPT");
        combined[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(service.CombineFieldValues(headers, "accept"),
                Is.EqualTo(", two"u8.ToArray()));
            Assert.That(headers.Fields[0].Value, Is.Empty);
            Assert.That(headers.Fields[1].Value,
                Is.EqualTo("two"u8.ToArray()));
        });
    }

    [TestCase(0x00)]
    [TestCase(0x0a)]
    [TestCase(0x0d)]
    public void CombinationRejectsInjectedMatchingValues(int injected)
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.Add(Field("Accept", "safe"u8.ToArray()));
        headers.Fields.Add(Field("accept", new byte[] { (byte)injected }));

        HttpFieldException error = Assert.Throws<HttpFieldException>(() =>
            service.CombineFieldValues(headers, "Accept"))!;

        Assert.Multiple(() =>
        {
            Assert.That(error.DiagnosticCode,
                Is.EqualTo("invalid-field-value"));
            Assert.That(error.FieldName, Is.EqualTo("accept"));
            Assert.That(headers.Fields, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void CookieUsesItsDefinedSeparatorAndSetCookieIsNotCombined()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders cookies = new HttpHeaders();
        HttpHeaders setCookies = new HttpHeaders();
        cookies.Fields.Add(Field("Cookie", "a=1"u8.ToArray()));
        cookies.Fields.Add(Field("cookie", "b=2"u8.ToArray()));
        setCookies.Fields.Add(Field("Set-Cookie", "a=1"u8.ToArray()));
        setCookies.Fields.Add(Field("set-cookie", "b=2"u8.ToArray()));

        byte[] combined = service.CombineFieldValues(cookies, "COOKIE");
        HttpFieldException error = Assert.Throws<HttpFieldException>(() =>
            service.CombineFieldValues(setCookies, "Set-Cookie"));

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(combined), Is.EqualTo("a=1; b=2"));
            Assert.That(error.DiagnosticCode, Is.EqualTo("field-not-combinable"));
        });
    }

    [Test]
    public void SingletonAndUnknownFieldGrammarsAreNotCombined()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders contentLengths = new HttpHeaders();
        HttpHeaders extensions = new HttpHeaders();
        contentLengths.Fields.Add(Field("Content-Length", "5"u8.ToArray()));
        contentLengths.Fields.Add(Field("content-length", "5"u8.ToArray()));
        extensions.Fields.Add(Field("X-Unknown", "one"u8.ToArray()));
        extensions.Fields.Add(Field("x-unknown", "two"u8.ToArray()));

        HttpFieldException singletonError = Assert.Throws<HttpFieldException>(() =>
            service.CombineFieldValues(contentLengths, "Content-Length"));
        HttpFieldException extensionError = Assert.Throws<HttpFieldException>(() =>
            service.CombineFieldValues(extensions, "X-Unknown"));

        Assert.Multiple(() =>
        {
            Assert.That(singletonError.DiagnosticCode, Is.EqualTo("field-not-combinable"));
            Assert.That(extensionError.DiagnosticCode, Is.EqualTo("field-not-combinable"));
        });
    }

    [Test]
    public void SingleFieldCombinationReturnsAnIndependentExactCopy()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders headers = new HttpHeaders();
        HttpHeaderField field = Field("ETag", new byte[] { 0x22, 0x80, 0x22 });
        headers.Fields.Add(field);

        byte[] combined = service.CombineFieldValues(headers, "etag");
        combined[0] = 0xff;

        Assert.Multiple(() =>
        {
            Assert.That(field.Value, Is.EqualTo(new byte[] { 0x22, 0x80, 0x22 }));
            Assert.That(service.CombineFieldValues(headers, "ETag"), Is.EqualTo(field.Value));
            Assert.That(service.CombineFieldValues(headers, "Missing"), Is.Empty);
        });
    }

    [Test]
    public void TrailerValidationAllowsEndComputedMetadataAndRejectsControlFields()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders valid = new HttpHeaders();
        valid.Fields.Add(Field("Digest", "sha-256=:YWJj:"u8.ToArray()));
        valid.Fields.Add(Field("X-Checksum", new byte[] { 0x80 }));

        Assert.That(service.ValidateTrailers(valid), Is.True);
        foreach (string name in ForbiddenTrailerNames())
        {
            HttpHeaders invalid = new HttpHeaders();
            invalid.Fields.Add(Field(name, "value"u8.ToArray()));
            Assert.That(service.ValidateTrailers(invalid), Is.False, name);
        }
    }

    [Test]
    public void TrailerValidationRejectsMalformedNamesAndValues()
    {
        HttpHeaderService service = new HttpHeaderService();
        HttpHeaders badName = new HttpHeaders();
        HttpHeaders badValue = new HttpHeaders();
        badName.Fields.Add(Field("Bad Name", "value"u8.ToArray()));
        badValue.Fields.Add(Field("Digest", new byte[] { 0x61, 0x0a, 0x62 }));

        Assert.Multiple(() =>
        {
            Assert.That(service.ValidateTrailers(badName), Is.False);
            Assert.That(service.ValidateTrailers(badValue), Is.False);
        });
    }

    [Test]
    public void TrailerRestrictionsAreAsciiCaseInsensitiveAndAllowDefinedMetadata()
    {
        IHttpHeaderService service = new HttpHeaderService();
        HttpHeaders allowed = new HttpHeaders();
        allowed.Fields.Add(Field("Authentication-Info",
            "nextnonce=abc"u8.ToArray()));
        allowed.Fields.Add(Field("Proxy-Authentication-Info",
            "nextnonce=def"u8.ToArray()));
        allowed.Fields.Add(Field("ETag", "\"value\""u8.ToArray()));
        allowed.Fields.Add(Field("Content-Digest", "sha-256=:YWJj:"u8.ToArray()));
        allowed.Fields.Add(Field("Server-Timing", "total;dur=1"u8.ToArray()));
        allowed.Fields.Add(Field("X-Extension", new byte[] { 0x09, 0x80 }));
        HttpHeaders lower = new HttpHeaders();
        lower.Fields.Add(Field("content-length", "1"u8.ToArray()));
        HttpHeaders upper = new HttpHeaders();
        upper.Fields.Add(Field("AUTHORIZATION", "value"u8.ToArray()));

        Assert.Multiple(() =>
        {
            Assert.That(service.ValidateTrailers(new HttpHeaders()), Is.True);
            Assert.That(service.ValidateTrailers(allowed), Is.True);
            Assert.That(service.ValidateTrailers(lower), Is.False);
            Assert.That(service.ValidateTrailers(upper), Is.False);
        });
    }

    static IEnumerable<string> CommaCombinableNames()
    {
        return new[]
        {
            "A-IM",
            "Accept",
            "Accept-Charset",
            "Accept-Encoding",
            "Accept-Language",
            "Accept-Patch",
            "Accept-Post",
            "Accept-Ranges",
            "Allow",
            "Cache-Control",
            "Cache-Status",
            "Connection",
            "Content-Encoding",
            "Content-Language",
            "Forwarded",
            "If-Match",
            "If-None-Match",
            "Link",
            "Pragma",
            "Proxy-Authenticate",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
            "Via",
            "Vary",
            "Warning",
            "WWW-Authenticate"
        };
    }

    static IEnumerable<string> ForbiddenTrailerNames()
    {
        return new[]
        {
            "Age",
            "Authorization",
            "Cache-Control",
            "Connection",
            "Content-Encoding",
            "Content-Length",
            "Content-Range",
            "Content-Type",
            "Date",
            "Expect",
            "Expires",
            "Host",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Keep-Alive",
            "Location",
            "Max-Forwards",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "Proxy-Connection",
            "Range",
            "Retry-After",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
            "Vary",
            "Warning",
            "WWW-Authenticate"
        };
    }

    static HttpHeaderField Field(string name, byte[] value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        return field;
    }
}
