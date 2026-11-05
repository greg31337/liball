using System.Text;

namespace http.Tests;

public class CombinedNegotiationTests
{
    [Test]
    public void CombinedSelectionUsesProductAcrossAllSuppliedDimensions()
    {
        HttpRequest request = new HttpRequest();
        AddField(request, HttpHeaderNames.Accept, "application/json, text/plain;q=0.8");
        AddField(request, HttpHeaderNames.AcceptCharset, "utf-8;q=0.5, iso-8859-1;q=0.8");
        AddField(request, HttpHeaderNames.AcceptEncoding, "gzip, br");
        AddField(request, HttpHeaderNames.AcceptLanguage, "en, fr");
        HttpNegotiationCandidate strongestMedia = Candidate(
            "application",
            "json",
            "utf-8",
            "gzip",
            "en",
            100,
            0);
        HttpNegotiationCandidate balanced = Candidate(
            "text",
            "plain",
            "iso-8859-1",
            "br",
            "fr",
            0,
            1);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { strongestMedia, balanced });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.Selected));
            Assert.That(result.Candidate, Is.SameAs(balanced));
            Assert.That(result.Representation, Is.SameAs(balanced.Representation));
            Assert.That(result.HasMediaMatch, Is.True);
            Assert.That(result.MediaQuality, Is.EqualTo(800));
            Assert.That(result.HasCharsetMatch, Is.True);
            Assert.That(result.CharsetQuality, Is.EqualTo(800));
            Assert.That(result.HasEncodingMatch, Is.True);
            Assert.That(result.EncodingQuality, Is.EqualTo(1000));
            Assert.That(result.HasLanguageMatch, Is.True);
            Assert.That(result.LanguageQuality, Is.EqualTo(1000));
            Assert.That(result.HasOverallQuality, Is.True);
            Assert.That(result.OverallQuality, Is.EqualTo(640000000000));
            Assert.That(result.Reason, Does.Contain("media=800"));
            Assert.That(result.Reason, Does.Contain("charset=800"));
            Assert.That(result.Reason, Does.Contain("result=640000000000"));
        });
    }

    [Test]
    public void AbsentDimensionsAreNeutralAndOmittedFromMatchMetadata()
    {
        HttpRequest request = new HttpRequest();
        AddField(request, HttpHeaderNames.Accept, "text/plain;q=0.8");
        AddField(request, HttpHeaderNames.AcceptEncoding, "gzip;q=0.5");
        HttpNegotiationCandidate candidate = Candidate(
            "text",
            "plain",
            "utf-8",
            "gzip",
            "en",
            0,
            0);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { candidate });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(candidate));
            Assert.That(result.HasMediaMatch, Is.True);
            Assert.That(result.HasCharsetMatch, Is.False);
            Assert.That(result.HasEncodingMatch, Is.True);
            Assert.That(result.HasLanguageMatch, Is.False);
            Assert.That(result.OverallQuality, Is.EqualTo(400000));
            Assert.That(result.Reason, Does.Contain("charset=1"));
            Assert.That(result.Reason, Does.Contain("language=1"));
        });
    }

    [Test]
    public void CombinedQualityTieUsesTotalSpecificity()
    {
        HttpRequest request = new HttpRequest();
        AddField(request, HttpHeaderNames.Accept, "application/*;q=0.8, text/plain;q=0.8");
        HttpNegotiationCandidate wildcard = Candidate(
            "application",
            "json",
            null,
            null,
            null,
            100,
            0);
        HttpNegotiationCandidate exact = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            0,
            1);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { wildcard, exact });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(exact));
            Assert.That(result.MediaSpecificity, Is.EqualTo(2000));
        });
    }

    [Test]
    public void CombinedTieUsesClientOrderBeforeServerPreference()
    {
        HttpRequest request = new HttpRequest();
        AddField(
            request,
            HttpHeaderNames.Accept,
            "application/json;q=0.8, text/plain;q=0.8");
        HttpNegotiationCandidate earlier = Candidate(
            "application",
            "json",
            null,
            null,
            null,
            0,
            5);
        HttpNegotiationCandidate serverPreferred = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            100,
            0);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { serverPreferred, earlier });

        Assert.That(result.Candidate, Is.SameAs(earlier));
    }

    [Test]
    public void CombinedFinalTieUsesServerThenStableSourceOrdering()
    {
        HttpRequest request = new HttpRequest();
        AddField(request, HttpHeaderNames.Accept, "text/plain");
        HttpNegotiationCandidate low = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            1,
            0);
        HttpNegotiationCandidate later = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            5,
            9);
        HttpNegotiationCandidate earlier = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            5,
            2);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { low, later, earlier });

        Assert.That(result.Candidate, Is.SameAs(earlier));
    }

    [Test]
    public void ZeroQualityInAnyDimensionExcludesTheCandidate()
    {
        HttpRequest request = new HttpRequest();
        AddField(request, HttpHeaderNames.Accept, "text/plain");
        AddField(request, HttpHeaderNames.AcceptEncoding, "gzip;q=0, identity");
        HttpNegotiationCandidate gzip = Candidate(
            "text",
            "plain",
            null,
            "gzip",
            null,
            100,
            0);
        HttpNegotiationCandidate identity = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            0,
            1);

        HttpNegotiationResult selected = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { gzip, identity });
        HttpNegotiationResult rejected = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { gzip });

        Assert.Multiple(() =>
        {
            Assert.That(selected.Candidate, Is.SameAs(identity));
            Assert.That(rejected.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(rejected.Candidate, Is.Null);
            Assert.That(rejected.HasMediaMatch, Is.True);
            Assert.That(rejected.HasEncodingMatch, Is.True);
            Assert.That(rejected.HasOverallQuality, Is.True);
            Assert.That(rejected.OverallQuality, Is.Zero);
            Assert.That(rejected.Reason, Does.Contain("every supplied"));
        });
    }

    [Test]
    public void EmptyAcceptProducesExplicitNoAcceptableOutcome()
    {
        HttpRequest request = new HttpRequest();
        AddField(request, HttpHeaderNames.Accept, "");
        AddField(request, HttpHeaderNames.AcceptEncoding, "identity");
        HttpNegotiationCandidate candidate = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            0,
            0);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            request,
            new List<HttpNegotiationCandidate> { candidate });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(result.HasMediaMatch, Is.True);
            Assert.That(result.HasEncodingMatch, Is.True);
            Assert.That(result.HasOverallQuality, Is.True);
        });
    }

    [Test]
    public void MalformedCombinedPreferenceIdentifiesItsDimension()
    {
        HttpNegotiationCandidate candidate = Candidate(
            "text",
            "plain",
            null,
            null,
            "en",
            0,
            0);
        HttpRequest languageRequest = new HttpRequest();
        AddField(languageRequest, HttpHeaderNames.Accept, "text/plain");
        AddField(languageRequest, HttpHeaderNames.AcceptLanguage, "en-*");
        HttpRequest encodingRequest = new HttpRequest();
        AddField(encodingRequest, HttpHeaderNames.AcceptEncoding, "gzip;level=1");

        HttpNegotiationResult language = NegotiationMatcher.SelectRepresentation(
            languageRequest,
            new List<HttpNegotiationCandidate> { candidate });
        HttpNegotiationResult encoding = NegotiationMatcher.SelectRepresentation(
            encodingRequest,
            new List<HttpNegotiationCandidate> { candidate });

        Assert.Multiple(() =>
        {
            Assert.That(language.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
            Assert.That(language.HasLanguageMatch, Is.True);
            Assert.That(language.HasMediaMatch, Is.False);
            Assert.That(encoding.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
            Assert.That(encoding.HasEncodingMatch, Is.True);
        });
    }

    [Test]
    public void NoSelectingFieldsUseDeterministicServerPreference()
    {
        HttpNegotiationCandidate later = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            5,
            8);
        HttpNegotiationCandidate earlier = Candidate(
            "application",
            "json",
            null,
            null,
            null,
            5,
            2);

        HttpNegotiationResult result = NegotiationMatcher.SelectRepresentation(
            new HttpRequest(),
            new List<HttpNegotiationCandidate> { later, earlier });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(result.Candidate, Is.SameAs(earlier));
            Assert.That(result.HasOverallQuality, Is.False);
        });
    }

    [Test]
    public void CombinedSelectionRejectsMalformedCandidateMetadata()
    {
        HttpNegotiationCandidate invalidMedia = Candidate(
            "text",
            "plain",
            null,
            null,
            null,
            0,
            0);
        invalidMedia.Representation!.MediaType!.IsSubtypeWildcard = true;
        invalidMedia.Representation.MediaType.Subtype = "";
        HttpNegotiationCandidate invalidLanguage = Candidate(
            "text",
            "plain",
            null,
            null,
            "bad_tag",
            0,
            0);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectRepresentation(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { invalidMedia }));
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectRepresentation(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { invalidLanguage }));
        });
    }

    static HttpNegotiationCandidate Candidate(
        string type,
        string subtype,
        string? charset,
        string? coding,
        string? language,
        int serverPreference,
        int sourceOrder)
    {
        HttpMediaType mediaType = new HttpMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        HttpRepresentation representation = new HttpRepresentation();
        representation.MediaType = mediaType;
        representation.Charset = charset;
        if (coding != null)
        {
            representation.ContentCodings.Add(coding);
        }

        if (language != null)
        {
            representation.Languages.Add(language);
        }

        HttpNegotiationCandidate candidate = new HttpNegotiationCandidate();
        candidate.Representation = representation;
        candidate.ServerPreference = serverPreference;
        candidate.SourceOrder = sourceOrder;
        return candidate;
    }

    static void AddField(HttpRequest request, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        request.Headers.Fields.Add(field);
    }
}
