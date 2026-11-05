using System.Text;

namespace http.Tests;

public class NegotiationMatcherTests
{
    [Test]
    public void MostSpecificRangeDeterminesQualityAndExclusionsOverrideWildcards()
    {
        HttpRequest request = Request(
            "text/*;q=0.3, text/plain;q=0.7, " +
            "text/plain;format=flowed, text/plain;format=fixed;q=0, */*;q=0.5");
        HttpNegotiationCandidate plain = Candidate("text", "plain", null, 10, 1);
        HttpNegotiationCandidate flowed = Candidate("text", "plain", "flowed", 0, 2);
        HttpNegotiationCandidate fixedValue = Candidate("text", "plain", "fixed", 100, 0);
        HttpNegotiationCandidate image = Candidate("image", "png", null, 100, 3);

        HttpNegotiationResult selected = NegotiationMatcher.SelectMedia(
            request,
            new List<HttpNegotiationCandidate> { plain, fixedValue, image, flowed });
        HttpNegotiationResult excluded = NegotiationMatcher.SelectMedia(
            request,
            new List<HttpNegotiationCandidate> { fixedValue });

        Assert.Multiple(() =>
        {
            Assert.That(selected.Outcome, Is.EqualTo(HttpNegotiationOutcome.Selected));
            Assert.That(selected.Candidate, Is.SameAs(flowed));
            Assert.That(selected.Representation, Is.SameAs(flowed.Representation));
            Assert.That(selected.HasMediaMatch, Is.True);
            Assert.That(selected.MediaQuality, Is.EqualTo(1000));
            Assert.That(selected.MediaSpecificity, Is.EqualTo(2001));
            Assert.That(excluded.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(excluded.MediaQuality, Is.Zero);
        });
    }

    [Test]
    public void QualityRanksCandidatesBeforeSpecificity()
    {
        HttpRequest request = Request("text/plain;q=0.4, image/*;q=0.8");
        HttpNegotiationCandidate text = Candidate("text", "plain", null, 100, 0);
        HttpNegotiationCandidate image = Candidate("image", "png", null, 0, 1);

        HttpNegotiationResult result = NegotiationMatcher.SelectMedia(
            request,
            new List<HttpNegotiationCandidate> { text, image });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(image));
            Assert.That(result.MediaQuality, Is.EqualTo(800));
            Assert.That(result.MediaSpecificity, Is.EqualTo(1000));
        });
    }

    [Test]
    public void ClientOrderPrecedesServerPreferenceAndStableServerOrderBreaksLaterTies()
    {
        HttpNegotiationCandidate json = Candidate("application", "json", null, 0, 5);
        HttpNegotiationCandidate plain = Candidate("text", "plain", null, 100, 0);
        HttpNegotiationResult clientOrder = NegotiationMatcher.SelectMedia(
            Request("application/json;q=0.8, text/plain;q=0.8"),
            new List<HttpNegotiationCandidate> { plain, json });

        HttpNegotiationCandidate low = Candidate("text", "html", null, 1, 0);
        HttpNegotiationCandidate highLater = Candidate("text", "plain", null, 5, 10);
        HttpNegotiationCandidate highEarlier = Candidate("text", "css", null, 5, 2);
        HttpNegotiationResult serverOrder = NegotiationMatcher.SelectMedia(
            Request("text/*"),
            new List<HttpNegotiationCandidate> { low, highLater, highEarlier });

        Assert.Multiple(() =>
        {
            Assert.That(clientOrder.Candidate, Is.SameAs(json));
            Assert.That(serverOrder.Candidate, Is.SameAs(highEarlier));
        });
    }

    [Test]
    public void AbsentAndEmptyAcceptFieldsHaveDistinctOutcomes()
    {
        HttpNegotiationCandidate first = Candidate("text", "plain", null, 1, 0);
        HttpNegotiationCandidate preferred = Candidate("image", "png", null, 5, 1);
        List<HttpNegotiationCandidate> candidates = new List<HttpNegotiationCandidate>
        {
            first,
            preferred
        };

        HttpNegotiationResult absent = NegotiationMatcher.SelectMedia(
            new HttpRequest(),
            candidates);
        HttpNegotiationResult empty = NegotiationMatcher.SelectMedia(
            Request(""),
            candidates);
        HttpNegotiationResult unavailable = NegotiationMatcher.SelectMedia(
            new HttpRequest(),
            new List<HttpNegotiationCandidate>());

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(preferred));
            Assert.That(absent.HasMediaMatch, Is.False);
            Assert.That(empty.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(empty.HasMediaMatch, Is.True);
            Assert.That(empty.Candidate, Is.Null);
            Assert.That(unavailable.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
        });
    }

    [Test]
    public void MultipleAcceptLinesCombineAndTokensMatchCaseInsensitively()
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, HttpHeaderNames.Accept, "IMAGE/*;q=0.5");
        AddField(request.Headers, HttpHeaderNames.Accept, "Text/Plain;q=0.8");
        HttpNegotiationCandidate image = Candidate("image", "png", null, 0, 0);
        HttpNegotiationCandidate text = Candidate("text", "plain", null, 0, 1);

        HttpNegotiationResult result = NegotiationMatcher.SelectMedia(
            request,
            new List<HttpNegotiationCandidate> { image, text });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(text));
            Assert.That(result.MediaQuality, Is.EqualTo(800));
        });
    }

    [Test]
    public void MediaParametersMustAllMatchAndCharsetTokensIgnoreCase()
    {
        HttpRequest request = Request(
            "text/plain;charset=UTF-8;format=Flowed, text/plain;q=0.2");
        HttpNegotiationCandidate exact = Candidate("text", "plain", "Flowed", 0, 0);
        exact.Representation!.Charset = "utf-8";
        HttpNegotiationCandidate wrongCase = Candidate("text", "plain", "flowed", 100, 1);
        wrongCase.Representation!.Charset = "utf-8";

        HttpNegotiationResult result = NegotiationMatcher.SelectMedia(
            request,
            new List<HttpNegotiationCandidate> { wrongCase, exact });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(exact));
            Assert.That(result.MediaQuality, Is.EqualTo(1000));
            Assert.That(result.MediaSpecificity, Is.EqualTo(2002));
        });
    }

    [Test]
    public void MalformedAcceptSyntaxAndDuplicateParametersAreReported()
    {
        HttpNegotiationCandidate candidate = Candidate("text", "plain", null, 0, 0);
        string[] malformed =
        {
            "text/plain;q=1.1",
            "text/plain;q=0.5;q=0.5",
            "text/plain;level=1;LEVEL=1",
            "text",
            "*/plain"
        };

        Assert.Multiple(() =>
        {
            foreach (string value in malformed)
            {
                HttpNegotiationResult result = NegotiationMatcher.SelectMedia(
                    Request(value),
                    new List<HttpNegotiationCandidate> { candidate });
                Assert.That(result.Outcome,
                    Is.EqualTo(HttpNegotiationOutcome.MalformedPreference),
                    value);
                Assert.That(result.Candidate, Is.Null, value);
            }
        });
    }

    [Test]
    public void InvalidCandidatesAndRequestCollectionsAreRejected()
    {
        HttpRequest request = Request("text/plain");
        HttpNegotiationCandidate missing = new HttpNegotiationCandidate();
        HttpNegotiationCandidate wildcard = Candidate("text", "plain", null, 0, 0);
        wildcard.Representation!.MediaType!.IsSubtypeWildcard = true;
        wildcard.Representation.MediaType.Subtype = "";

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectMedia(
                request,
                new List<HttpNegotiationCandidate> { missing }));
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectMedia(
                request,
                new List<HttpNegotiationCandidate> { wildcard }));
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectMedia(
                null!,
                new List<HttpNegotiationCandidate>()));
        });
    }

    [Test]
    public void CharsetExactMatchesOverrideWildcardAndUseClientOrderForTies()
    {
        HttpRequest request = PreferenceRequest(
            HttpHeaderNames.AcceptCharset,
            "ISO-8859-1;q=0.8, UTF-8;q=0.8, *;q=0.9");
        HttpNegotiationCandidate utf8 = RepresentationCandidate("utf-8", 100, 0);
        HttpNegotiationCandidate latin = RepresentationCandidate("iso-8859-1", 0, 1);
        HttpNegotiationCandidate unknown = RepresentationCandidate("windows-1252", 0, 2);

        HttpNegotiationResult result = NegotiationMatcher.SelectCharset(
            request,
            new List<HttpNegotiationCandidate> { utf8, latin, unknown });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.Selected));
            Assert.That(result.Candidate, Is.SameAs(unknown));
            Assert.That(result.HasCharsetMatch, Is.True);
            Assert.That(result.CharsetQuality, Is.EqualTo(900));
            Assert.That(result.CharsetSpecificity, Is.Zero);
        });

        HttpNegotiationResult exactTie = NegotiationMatcher.SelectCharset(
            PreferenceRequest(
                HttpHeaderNames.AcceptCharset,
                "ISO-8859-1;q=0.8, UTF-8;q=0.8"),
            new List<HttpNegotiationCandidate> { utf8, latin });

        Assert.That(exactTie.Candidate, Is.SameAs(latin));
    }

    [Test]
    public void RepeatedTokenPreferencesUseTheFirstOccurrenceDeterministically()
    {
        HttpNegotiationCandidate charset = RepresentationCandidate("utf-8", 0, 0);
        HttpNegotiationCandidate encoding = RepresentationCandidate(
            null,
            0,
            0,
            HttpContentCodings.Gzip);

        HttpNegotiationResult charsetResult = NegotiationMatcher.SelectCharset(
            PreferenceRequest(
                HttpHeaderNames.AcceptCharset,
                "utf-8;q=0.2, UTF-8;q=0.9"),
            new List<HttpNegotiationCandidate> { charset });
        HttpNegotiationResult encodingResult = NegotiationMatcher.SelectEncoding(
            PreferenceRequest(
                HttpHeaderNames.AcceptEncoding,
                "gzip;q=0.3, GZIP;q=0.8"),
            new List<HttpNegotiationCandidate> { encoding });

        Assert.Multiple(() =>
        {
            Assert.That(charsetResult.CharsetQuality, Is.EqualTo(200));
            Assert.That(encodingResult.EncodingQuality, Is.EqualTo(300));
        });
    }

    [Test]
    public void CharsetZeroQualityExactMatchOverridesWildcard()
    {
        HttpRequest request = PreferenceRequest(
            HttpHeaderNames.AcceptCharset,
            "utf-8;q=0, *;q=0.7");
        HttpNegotiationCandidate utf8 = RepresentationCandidate("UTF-8", 100, 0);
        HttpNegotiationCandidate latin = RepresentationCandidate("iso-8859-1", 0, 1);

        HttpNegotiationResult selected = NegotiationMatcher.SelectCharset(
            request,
            new List<HttpNegotiationCandidate> { utf8, latin });
        HttpNegotiationResult excluded = NegotiationMatcher.SelectCharset(
            request,
            new List<HttpNegotiationCandidate> { utf8 });

        Assert.Multiple(() =>
        {
            Assert.That(selected.Candidate, Is.SameAs(latin));
            Assert.That(selected.CharsetQuality, Is.EqualTo(700));
            Assert.That(excluded.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(excluded.HasCharsetMatch, Is.True);
        });
    }

    [Test]
    public void AbsentAndEmptyAcceptCharsetFieldsHaveDistinctOutcomes()
    {
        HttpNegotiationCandidate first = RepresentationCandidate("utf-8", 1, 0);
        HttpNegotiationCandidate preferred = RepresentationCandidate("iso-8859-1", 5, 1);
        List<HttpNegotiationCandidate> candidates = new List<HttpNegotiationCandidate>
        {
            first,
            preferred
        };

        HttpNegotiationResult absent = NegotiationMatcher.SelectCharset(
            new HttpRequest(),
            candidates);
        HttpNegotiationResult empty = NegotiationMatcher.SelectCharset(
            PreferenceRequest(HttpHeaderNames.AcceptCharset, ""),
            candidates);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(preferred));
            Assert.That(absent.HasCharsetMatch, Is.False);
            Assert.That(empty.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(empty.HasCharsetMatch, Is.True);
        });
    }

    [Test]
    public void EmptyAcceptEncodingSelectsOnlyIdentity()
    {
        HttpNegotiationCandidate gzip = RepresentationCandidate(
            null,
            100,
            0,
            HttpContentCodings.Gzip);
        HttpNegotiationCandidate identity = RepresentationCandidate(null, 0, 1);

        HttpNegotiationResult result = NegotiationMatcher.SelectEncoding(
            PreferenceRequest(HttpHeaderNames.AcceptEncoding, ""),
            new List<HttpNegotiationCandidate> { gzip, identity });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.Selected));
            Assert.That(result.Candidate, Is.SameAs(identity));
            Assert.That(result.HasEncodingMatch, Is.True);
            Assert.That(result.EncodingQuality, Is.EqualTo(1000));
            Assert.That(result.EncodingSpecificity, Is.Zero);
        });
    }

    [Test]
    public void EncodingIdentityDefaultsToAcceptableAndExactExclusionOverridesWildcard()
    {
        HttpRequest request = PreferenceRequest(
            HttpHeaderNames.AcceptEncoding,
            "gzip;q=0, *;q=0.5");
        HttpNegotiationCandidate gzip = RepresentationCandidate(
            null,
            100,
            0,
            HttpContentCodings.Gzip);
        HttpNegotiationCandidate brotli = RepresentationCandidate(
            null,
            50,
            1,
            HttpContentCodings.Br);
        HttpNegotiationCandidate identity = RepresentationCandidate(null, 0, 2);

        HttpNegotiationResult result = NegotiationMatcher.SelectEncoding(
            request,
            new List<HttpNegotiationCandidate> { gzip, brotli, identity });
        HttpNegotiationResult excluded = NegotiationMatcher.SelectEncoding(
            request,
            new List<HttpNegotiationCandidate> { gzip });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(identity));
            Assert.That(result.EncodingQuality, Is.EqualTo(1000));
            Assert.That(excluded.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
        });
    }

    [Test]
    public void IdentityCanBeExcludedExplicitlyOrByZeroQualityWildcard()
    {
        HttpNegotiationCandidate identity = RepresentationCandidate(null, 0, 0);
        HttpNegotiationCandidate gzip = RepresentationCandidate(
            null,
            0,
            1,
            HttpContentCodings.Gzip);

        HttpNegotiationResult explicitIdentity = NegotiationMatcher.SelectEncoding(
            PreferenceRequest(
                HttpHeaderNames.AcceptEncoding,
                "identity;q=0, gzip;q=0.4"),
            new List<HttpNegotiationCandidate> { identity, gzip });
        HttpNegotiationResult wildcard = NegotiationMatcher.SelectEncoding(
            PreferenceRequest(HttpHeaderNames.AcceptEncoding, "*;q=0"),
            new List<HttpNegotiationCandidate> { identity });

        Assert.Multiple(() =>
        {
            Assert.That(explicitIdentity.Candidate, Is.SameAs(gzip));
            Assert.That(explicitIdentity.EncodingQuality, Is.EqualTo(400));
            Assert.That(wildcard.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
        });
    }

    [Test]
    public void StackedContentCodingsRequireEveryCodingAndUseWeakestQuality()
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, HttpHeaderNames.AcceptEncoding, "GZIP;q=0.8");
        AddField(request.Headers, HttpHeaderNames.AcceptEncoding, "br;q=0.6, identity;q=0");
        HttpNegotiationCandidate stacked = RepresentationCandidate(
            null,
            0,
            0,
            "gzip",
            "BR");

        HttpNegotiationResult accepted = NegotiationMatcher.SelectEncoding(
            request,
            new List<HttpNegotiationCandidate> { stacked });
        HttpNegotiationCandidate partlyUnknown = RepresentationCandidate(
            null,
            0,
            0,
            "gzip",
            "zstd");
        HttpNegotiationResult rejected = NegotiationMatcher.SelectEncoding(
            request,
            new List<HttpNegotiationCandidate> { partlyUnknown });

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Candidate, Is.SameAs(stacked));
            Assert.That(accepted.EncodingQuality, Is.EqualTo(600));
            Assert.That(accepted.EncodingSpecificity, Is.EqualTo(1000));
            Assert.That(rejected.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
        });
    }

    [Test]
    public void AbsentAcceptEncodingUsesDeterministicServerPreference()
    {
        HttpNegotiationCandidate earlier = RepresentationCandidate(null, 5, 1);
        HttpNegotiationCandidate later = RepresentationCandidate(
            null,
            5,
            4,
            HttpContentCodings.Gzip);

        HttpNegotiationResult result = NegotiationMatcher.SelectEncoding(
            new HttpRequest(),
            new List<HttpNegotiationCandidate> { later, earlier });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(result.Candidate, Is.SameAs(earlier));
            Assert.That(result.HasEncodingMatch, Is.False);
        });
    }

    [Test]
    public void MalformedTokenPreferencesAreReported()
    {
        HttpNegotiationCandidate candidate = RepresentationCandidate("utf-8", 0, 0);
        string[] malformed =
        {
            "gzip;q=1.1",
            "gzip;level=1",
            "gzip;q=\"0.5\"",
            "gzip/deflate"
        };

        Assert.Multiple(() =>
        {
            foreach (string value in malformed)
            {
                HttpNegotiationResult charset = NegotiationMatcher.SelectCharset(
                    PreferenceRequest(HttpHeaderNames.AcceptCharset, value),
                    new List<HttpNegotiationCandidate> { candidate });
                HttpNegotiationResult encoding = NegotiationMatcher.SelectEncoding(
                    PreferenceRequest(HttpHeaderNames.AcceptEncoding, value),
                    new List<HttpNegotiationCandidate> { candidate });
                Assert.That(charset.Outcome,
                    Is.EqualTo(HttpNegotiationOutcome.MalformedPreference),
                    value);
                Assert.That(charset.HasCharsetMatch, Is.True, value);
                Assert.That(encoding.Outcome,
                    Is.EqualTo(HttpNegotiationOutcome.MalformedPreference),
                    value);
                Assert.That(encoding.HasEncodingMatch, Is.True, value);
            }
        });
    }

    [Test]
    public void InvalidCharsetAndEncodingCandidatesAreRejectedEvenWhenFieldIsAbsent()
    {
        HttpNegotiationCandidate charset = RepresentationCandidate("*", 0, 0);
        HttpNegotiationCandidate encoding = RepresentationCandidate(null, 0, 0, "*");
        HttpNegotiationCandidate missingCodings = RepresentationCandidate(null, 0, 0);
        missingCodings.Representation!.ContentCodings = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectCharset(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { charset }));
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectEncoding(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { encoding }));
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectEncoding(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { missingCodings }));
        });
    }

    static HttpRequest Request(string accept)
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, HttpHeaderNames.Accept, accept);
        return request;
    }

    static HttpRequest PreferenceRequest(string name, string value)
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, name, value);
        return request;
    }

    static HttpNegotiationCandidate RepresentationCandidate(
        string? charset,
        int serverPreference,
        int sourceOrder,
        params string[] codings)
    {
        HttpRepresentation representation = new HttpRepresentation();
        representation.Charset = charset;
        representation.ContentCodings.AddRange(codings);
        HttpNegotiationCandidate candidate = new HttpNegotiationCandidate();
        candidate.Representation = representation;
        candidate.ServerPreference = serverPreference;
        candidate.SourceOrder = sourceOrder;
        return candidate;
    }

    static HttpNegotiationCandidate Candidate(
        string type,
        string subtype,
        string? format,
        int serverPreference,
        int sourceOrder)
    {
        HttpMediaType mediaType = new HttpMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        if (format != null)
        {
            HttpParameter parameter = new HttpParameter();
            parameter.Name = "format";
            parameter.HasValue = true;
            parameter.Value = Encoding.ASCII.GetBytes(format);
            mediaType.Parameters.Add(parameter);
        }

        HttpRepresentation representation = new HttpRepresentation();
        representation.MediaType = mediaType;
        HttpNegotiationCandidate candidate = new HttpNegotiationCandidate();
        candidate.Representation = representation;
        candidate.ServerPreference = serverPreference;
        candidate.SourceOrder = sourceOrder;
        return candidate;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }
}
