using System.Text;

namespace http.Tests;

public class HttpContentNegotiatorTests
{
    readonly IHttpContentNegotiator service = new HttpContentNegotiator();

    [Test]
    public void MediaQualityPrecedesSpecificity()
    {
        HttpNegotiationCandidate exact = Candidate("text", "plain", null, null, null, 100, 0);
        HttpNegotiationCandidate wildcard = Candidate("application", "json", null, null, null, 0, 1);

        HttpNegotiationResult result = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "text/plain;q=0.4, application/*;q=0.8"),
            [exact, wildcard]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.Selected));
            Assert.That(result.Candidate, Is.SameAs(wildcard));
            Assert.That(result.Representation, Is.SameAs(wildcard.Representation));
            Assert.That(result.HasMediaMatch, Is.True);
            Assert.That(result.MediaQuality, Is.EqualTo(800));
            Assert.That(result.MediaSpecificity, Is.EqualTo(1000));
        });
    }

    [Test]
    public void MediaSpecificityParametersAndExclusionsOverrideBroaderRanges()
    {
        HttpNegotiationCandidate plain = Candidate("text", "plain", null, null, null, 100, 0);
        HttpNegotiationCandidate flowed = Candidate("text", "plain", null, null, null, 0, 1);
        AddMediaParameter(flowed, "format", "flowed");
        HttpNegotiationCandidate image = Candidate("image", "png", null, null, null, 0, 2);

        HttpNegotiationResult selected = service.SelectMediaType(
            Request(HttpHeaderNames.Accept,
                "text/*;q=0.9, text/plain;q=0.5, text/plain;format=flowed;q=0.8"),
            [plain, flowed]);
        HttpNegotiationResult excluded = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "text/plain;q=0, */*;q=0.5"), [plain, image]);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Candidate, Is.SameAs(flowed));
            Assert.That(selected.MediaQuality, Is.EqualTo(800));
            Assert.That(selected.MediaSpecificity, Is.EqualTo(2001));
            Assert.That(excluded.Candidate, Is.SameAs(image));
            Assert.That(excluded.MediaQuality, Is.EqualTo(500));
        });
    }

    [Test]
    public void MediaTiesUseClientServerSourceAndStableListOrder()
    {
        HttpNegotiationCandidate clientFirst = Candidate("application", "json", null, null, null, 0, 9);
        HttpNegotiationCandidate clientSecond = Candidate("text", "plain", null, null, null, 100, 0);
        HttpNegotiationResult client = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "application/json;q=0.8, text/plain;q=0.8"),
            [clientSecond, clientFirst]);
        HttpNegotiationCandidate later = Candidate("text", "css", null, null, null, 5, 8);
        HttpNegotiationCandidate earlier = Candidate("text", "csv", null, null, null, 5, 2);
        HttpNegotiationResult source = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "text/*"), [later, earlier]);
        HttpNegotiationCandidate listFirst = Candidate("text", "calendar", null, null, null, 5, 2);
        HttpNegotiationCandidate listSecond = Candidate("text", "vcard", null, null, null, 5, 2);
        HttpNegotiationResult stable = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "text/*"), [listFirst, listSecond]);

        Assert.Multiple(() =>
        {
            Assert.That(client.Candidate, Is.SameAs(clientFirst));
            Assert.That(source.Candidate, Is.SameAs(earlier));
            Assert.That(stable.Candidate, Is.SameAs(listFirst));
        });
    }

    [Test]
    public void MediaAbsenceEmptyMalformedAndNoMatchRemainDistinct()
    {
        HttpNegotiationCandidate first = Candidate("text", "plain", null, null, null, 1, 0);
        HttpNegotiationCandidate preferred = Candidate("application", "json", null, null, null, 5, 1);
        HttpNegotiationResult absent = service.SelectMediaType(new HttpRequest(), [first, preferred]);
        HttpNegotiationResult empty = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, ""), [first]);
        HttpNegotiationResult malformed = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "text/plain;q=1.1"), [first]);
        HttpNegotiationResult noMatch = service.SelectMediaType(
            Request(HttpHeaderNames.Accept, "image/*"), [first]);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(preferred));
            Assert.That(absent.HasMediaMatch, Is.False);
            Assert.That(empty.Outcome, Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(malformed.Outcome, Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
            Assert.That(noMatch.Outcome, Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(noMatch.HasMediaMatch, Is.True);
        });
    }

    [Test]
    public void CharsetCoversExactWildcardExclusionDuplicatesAndClientOrder()
    {
        HttpNegotiationCandidate utf8 = Candidate(null, null, "utf-8", null, null, 100, 0);
        HttpNegotiationCandidate latin = Candidate(null, null, "iso-8859-1", null, null, 0, 1);
        HttpNegotiationResult wildcard = service.SelectCharset(
            Request(HttpHeaderNames.AcceptCharset, "utf-8;q=0, *;q=0.7"), [utf8, latin]);
        HttpNegotiationResult duplicate = service.SelectCharset(
            Request(HttpHeaderNames.AcceptCharset, "utf-8;q=0.2, UTF-8;q=0.9"), [utf8]);
        HttpNegotiationResult client = service.SelectCharset(
            Request(HttpHeaderNames.AcceptCharset, "iso-8859-1;q=0.8, utf-8;q=0.8"),
            [utf8, latin]);

        Assert.Multiple(() =>
        {
            Assert.That(wildcard.Candidate, Is.SameAs(latin));
            Assert.That(wildcard.CharsetQuality, Is.EqualTo(700));
            Assert.That(wildcard.CharsetSpecificity, Is.Zero);
            Assert.That(duplicate.CharsetQuality, Is.EqualTo(200));
            Assert.That(client.Candidate, Is.SameAs(latin));
        });
    }

    [Test]
    public void CharsetCoversUnspecifiedAbsentEmptyMalformedAndServerTie()
    {
        HttpNegotiationCandidate specified = Candidate(null, null, "utf-8", null, null, 5, 4);
        HttpNegotiationCandidate earlier = Candidate(null, null, "utf-8", null, null, 5, 1);
        HttpNegotiationCandidate unspecified = Candidate(null, null, null, null, null, 0, 2);
        HttpNegotiationResult absent = service.SelectCharset(new HttpRequest(), [specified, earlier]);
        HttpNegotiationResult empty = service.SelectCharset(
            Request(HttpHeaderNames.AcceptCharset, ""), [specified, unspecified]);
        HttpNegotiationResult tie = service.SelectCharset(
            Request(HttpHeaderNames.AcceptCharset, "utf-8"), [specified, earlier]);
        HttpNegotiationResult malformed = service.SelectCharset(
            Request(HttpHeaderNames.AcceptCharset, "utf-8;level=1"), [specified]);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(earlier));
            Assert.That(empty.Candidate, Is.SameAs(unspecified));
            Assert.That(empty.CharsetQuality, Is.EqualTo(1000));
            Assert.That(tie.Candidate, Is.SameAs(earlier));
            Assert.That(malformed.Outcome, Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
        });
    }

    [Test]
    public void EmptyEncodingSelectsIdentityOnly()
    {
        HttpNegotiationCandidate gzip = Candidate(null, null, null, HttpContentCodings.Gzip, null, 100, 0);
        HttpNegotiationCandidate identity = Candidate(null, null, null, null, null, 0, 1);
        HttpNegotiationResult result = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, ""), [gzip, identity]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(identity));
            Assert.That(result.HasEncodingMatch, Is.True);
            Assert.That(result.EncodingQuality, Is.EqualTo(1000));
            Assert.That(result.EncodingSpecificity, Is.Zero);
        });
    }

    [Test]
    public void EncodingCoversIdentityWildcardExactExclusionAndStacks()
    {
        HttpNegotiationCandidate identity = Candidate(null, null, null, null, null, 0, 0);
        HttpNegotiationCandidate gzip = Candidate(null, null, null, HttpContentCodings.Gzip, null, 100, 1);
        HttpNegotiationCandidate br = Candidate(null, null, null, HttpContentCodings.Br, null, 50, 2);
        HttpNegotiationResult implicitIdentity = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, "gzip;q=0, *;q=0.5"), [gzip, br, identity]);
        HttpNegotiationResult explicitExclusion = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, "identity;q=0, gzip;q=0.4"), [identity, gzip]);
        HttpNegotiationResult wildcardExclusion = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, "*;q=0"), [identity]);
        HttpNegotiationCandidate stacked = Candidate(null, null, null, null, null, 0, 0);
        stacked.Representation!.ContentCodings.Add(HttpContentCodings.Gzip);
        stacked.Representation.ContentCodings.Add(HttpContentCodings.Br);
        HttpNegotiationResult stack = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, "gzip;q=0.8, br;q=0.6"), [stacked]);

        Assert.Multiple(() =>
        {
            Assert.That(implicitIdentity.Candidate, Is.SameAs(identity));
            Assert.That(explicitExclusion.Candidate, Is.SameAs(gzip));
            Assert.That(explicitExclusion.EncodingQuality, Is.EqualTo(400));
            Assert.That(wildcardExclusion.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(stack.EncodingQuality, Is.EqualTo(600));
            Assert.That(stack.EncodingSpecificity, Is.EqualTo(1000));
        });
    }

    [Test]
    public void EncodingAbsenceMalformedAndTiesAreDeterministic()
    {
        HttpNegotiationCandidate gzip = Candidate(null, null, null, HttpContentCodings.Gzip, null, 5, 4);
        HttpNegotiationCandidate br = Candidate(null, null, null, HttpContentCodings.Br, null, 5, 1);
        HttpNegotiationResult absent = service.SelectContentCoding(new HttpRequest(), [gzip, br]);
        HttpNegotiationResult client = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, "gzip;q=0.8, br;q=0.8"), [br, gzip]);
        HttpNegotiationResult malformed = service.SelectContentCoding(
            Request(HttpHeaderNames.AcceptEncoding, "gzip;level=1"), [gzip]);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(br));
            Assert.That(client.Candidate, Is.SameAs(gzip));
            Assert.That(malformed.Outcome, Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
        });
    }

    [Test]
    public void BasicLanguageFilteringCoversPrefixWildcardSpecificityAndExclusion()
    {
        HttpNegotiationCandidate british = Candidate(null, null, null, null, "en-GB", 100, 0);
        HttpNegotiationCandidate american = Candidate(null, null, null, null, "en-US", 0, 1);
        HttpNegotiationCandidate french = Candidate(null, null, null, null, "fr", 50, 2);
        HttpNegotiationResult result = service.SelectLanguage(
            Request(HttpHeaderNames.AcceptLanguage, "en;q=0.8, en-GB;q=0, *;q=0.5"),
            [british, french, american]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(american));
            Assert.That(result.LanguageQuality, Is.EqualTo(800));
            Assert.That(result.LanguageSpecificity, Is.EqualTo(1));
        });
    }

    [Test]
    public void BasicLanguageFilteringDoesNotSkipSubtagsAndChoosesBestLanguage()
    {
        HttpNegotiationCandidate direct = Candidate(null, null, null, null, "de-DE", 0, 0);
        HttpNegotiationCandidate scripted = Candidate(null, null, null, null, "de-Latn-DE", 100, 1);
        HttpNegotiationCandidate multilingual = Candidate(null, null, null, null, "fr", 0, 2);
        multilingual.Representation!.Languages.Add("en-GB");
        HttpNegotiationResult noSkip = service.SelectLanguage(
            Request(HttpHeaderNames.AcceptLanguage, "de-DE"), [scripted, direct]);
        HttpNegotiationResult best = service.SelectLanguage(
            Request(HttpHeaderNames.AcceptLanguage, "en-GB;q=0.9, fr;q=0.4"), [multilingual]);

        Assert.Multiple(() =>
        {
            Assert.That(noSkip.Candidate, Is.SameAs(direct));
            Assert.That(best.Candidate, Is.SameAs(multilingual));
            Assert.That(best.LanguageQuality, Is.EqualTo(900));
            Assert.That(best.LanguageSpecificity, Is.EqualTo(2));
        });
    }

    [Test]
    public void LanguageAbsenceEmptyMalformedAndClientTiesHaveDefinedResults()
    {
        HttpNegotiationCandidate english = Candidate(null, null, null, null, "en", 10, 5);
        HttpNegotiationCandidate french = Candidate(null, null, null, null, "fr", 20, 9);
        HttpNegotiationCandidate untagged = Candidate(null, null, null, null, null, 0, 1);
        HttpNegotiationResult absent = service.SelectLanguage(new HttpRequest(), [english, french]);
        HttpNegotiationResult empty = service.SelectLanguage(
            Request(HttpHeaderNames.AcceptLanguage, ""), [english, untagged]);
        HttpNegotiationResult tie = service.SelectLanguage(
            Request(HttpHeaderNames.AcceptLanguage, "fr;q=0.8, en;q=0.8"), [english, french]);
        HttpNegotiationResult malformed = service.SelectLanguage(
            Request(HttpHeaderNames.AcceptLanguage, "en-*"), [english]);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(french));
            Assert.That(empty.Candidate, Is.SameAs(untagged));
            Assert.That(tie.Candidate, Is.SameAs(french));
            Assert.That(malformed.Outcome, Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
        });
    }

    [Test]
    public void CombinedSelectionUsesQualityProductAndNeutralAbsentDimensions()
    {
        HttpRequest all = Request(HttpHeaderNames.Accept, "application/json, text/plain;q=0.8");
        AddField(all.Headers, HttpHeaderNames.AcceptCharset, "utf-8;q=0.5, iso-8859-1;q=0.8");
        AddField(all.Headers, HttpHeaderNames.AcceptEncoding, "gzip, br");
        AddField(all.Headers, HttpHeaderNames.AcceptLanguage, "en, fr");
        HttpNegotiationCandidate media = Candidate("application", "json", "utf-8", "gzip", "en", 100, 0);
        HttpNegotiationCandidate product = Candidate("text", "plain", "iso-8859-1", "br", "fr", 0, 1);
        HttpNegotiationResult result = service.SelectRepresentation(all, [media, product]);
        HttpRequest partial = Request(HttpHeaderNames.Accept, "text/plain;q=0.8");
        AddField(partial.Headers, HttpHeaderNames.AcceptEncoding, "br;q=0.5");
        HttpNegotiationResult neutral = service.SelectRepresentation(partial, [product]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(product));
            Assert.That(result.MediaQuality, Is.EqualTo(800));
            Assert.That(result.CharsetQuality, Is.EqualTo(800));
            Assert.That(result.EncodingQuality, Is.EqualTo(1000));
            Assert.That(result.LanguageQuality, Is.EqualTo(1000));
            Assert.That(result.OverallQuality, Is.EqualTo(640000000000));
            Assert.That(neutral.HasCharsetMatch, Is.False);
            Assert.That(neutral.HasLanguageMatch, Is.False);
            Assert.That(neutral.OverallQuality, Is.EqualTo(400000));
        });
    }

    [Test]
    public void CombinedTieOrderIsSpecificityClientServerSourceThenList()
    {
        HttpNegotiationCandidate broad = Candidate("application", "json", null, null, null, 100, 0);
        HttpNegotiationCandidate exact = Candidate("text", "plain", null, null, null, 0, 1);
        HttpNegotiationResult specificity = service.SelectRepresentation(
            Request(HttpHeaderNames.Accept, "application/*;q=0.8, text/plain;q=0.8"),
            [broad, exact]);
        HttpNegotiationCandidate clientFirst = Candidate("application", "json", null, null, null, 0, 9);
        HttpNegotiationCandidate clientSecond = Candidate("text", "plain", null, null, null, 100, 0);
        HttpNegotiationResult client = service.SelectRepresentation(
            Request(HttpHeaderNames.Accept, "application/json;q=0.8, text/plain;q=0.8"),
            [clientSecond, clientFirst]);
        HttpNegotiationCandidate later = Candidate("text", "plain", null, null, null, 5, 8);
        HttpNegotiationCandidate earlier = Candidate("text", "plain", null, null, null, 5, 2);
        HttpNegotiationResult server = service.SelectRepresentation(
            Request(HttpHeaderNames.Accept, "text/plain"), [later, earlier]);

        Assert.Multiple(() =>
        {
            Assert.That(specificity.Candidate, Is.SameAs(exact));
            Assert.That(client.Candidate, Is.SameAs(clientFirst));
            Assert.That(server.Candidate, Is.SameAs(earlier));
        });
    }

    [Test]
    public void CombinedSelectionReportsAbsenceEmptyExclusionAndMalformedDimension()
    {
        HttpNegotiationCandidate candidate = Candidate("text", "plain", null, "gzip", "en", 0, 0);
        HttpNegotiationResult absent = service.SelectRepresentation(new HttpRequest(), [candidate]);
        HttpNegotiationResult empty = service.SelectRepresentation(
            Request(HttpHeaderNames.Accept, ""), [candidate]);
        HttpRequest excludedRequest = Request(HttpHeaderNames.Accept, "text/plain");
        AddField(excludedRequest.Headers, HttpHeaderNames.AcceptEncoding, "gzip;q=0");
        HttpNegotiationResult excluded = service.SelectRepresentation(excludedRequest, [candidate]);
        HttpRequest malformedRequest = Request(HttpHeaderNames.Accept, "text/plain");
        AddField(malformedRequest.Headers, HttpHeaderNames.AcceptLanguage, "en-*");
        HttpNegotiationResult malformed = service.SelectRepresentation(malformedRequest, [candidate]);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.HasOverallQuality, Is.False);
            Assert.That(empty.Outcome, Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(excluded.Outcome, Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(excluded.HasOverallQuality, Is.True);
            Assert.That(excluded.OverallQuality, Is.Zero);
            Assert.That(malformed.Outcome, Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
            Assert.That(malformed.HasLanguageMatch, Is.True);
        });
    }

    [Test]
    public void VaryKeysNormalizeNamesWhitespaceAndCombinableDuplicates()
    {
        HttpHeaders firstNames = ParsedNames("X-Mode", "Accept-Encoding", "x-mode");
        HttpHeaders secondNames = ParsedNames("accept-encoding", "X-MODE");
        HttpRequest split = new HttpRequest();
        AddField(split.Headers, HttpHeaderNames.AcceptEncoding, "gzip");
        AddField(split.Headers, "accept-encoding", "br");
        AddField(split.Headers, "x-mode", "fast");
        HttpRequest combined = new HttpRequest();
        AddField(combined.Headers, HttpHeaderNames.AcceptEncoding, " gzip ,\t br ");
        AddField(combined.Headers, "X-Mode", " fast ");

        Assert.That(service.CompareVaryKey(
            service.BuildVaryKey(split, firstNames),
            service.BuildVaryKey(combined, secondNames)), Is.True);
    }

    [Test]
    public void VaryKeysDistinguishAbsentEmptyLineOrderQuotedAndOpaqueValues()
    {
        HttpHeaders unknown = ParsedNames("X-Mode");
        HttpRequest ordered = new HttpRequest();
        AddField(ordered.Headers, "X-Mode", "a");
        AddField(ordered.Headers, "X-Mode", "b");
        HttpRequest reversed = new HttpRequest();
        AddField(reversed.Headers, "X-Mode", "b");
        AddField(reversed.Headers, "X-Mode", "a");
        HttpHeaders accept = ParsedNames(HttpHeaderNames.Accept);
        HttpRequest quotedWide = Request(HttpHeaderNames.Accept, "text/plain; profile=\"a  b\"");
        HttpRequest quotedNarrow = Request(HttpHeaderNames.Accept, "text/plain; profile=\"a b\"");
        HttpHeaders opaque = ParsedNames("X-Opaque");
        HttpRequest binary = new HttpRequest();
        AddField(binary.Headers, "X-Opaque", new byte[] { 0x80, 0xff, 0x3a });

        Assert.Multiple(() =>
        {
            Assert.That(service.CompareVaryKey(
                service.BuildVaryKey(new HttpRequest(), unknown),
                service.BuildVaryKey(Request("X-Mode", ""), unknown)), Is.False);
            Assert.That(service.CompareVaryKey(
                service.BuildVaryKey(ordered, unknown),
                service.BuildVaryKey(reversed, unknown)), Is.False);
            Assert.That(service.CompareVaryKey(
                service.BuildVaryKey(quotedWide, accept),
                service.BuildVaryKey(quotedNarrow, accept)), Is.False);
            Assert.That(service.BuildVaryKey(binary, opaque), Does.Contain("80ff3a"));
        });
    }

    [Test]
    public void VaryWildcardNeverMatches()
    {
        string wildcard = service.BuildVaryKey(new HttpRequest(), ParsedNames("*"));

        Assert.Multiple(() =>
        {
            Assert.That(wildcard, Is.EqualTo("V1;*"));
            Assert.That(service.CompareVaryKey(wildcard, wildcard), Is.False);
        });
    }

    [Test]
    public void PriorityMergeUsesServerOverridesAndPreservesExtensions()
    {
        HttpPriority client = FieldValueParser.ParsePriority(
            "u=5, i, shared=client, client-only=1"u8.ToArray());
        HttpPriority server = FieldValueParser.ParsePriority(
            "u=1, i=?0, shared=server, server-only=?0"u8.ToArray());
        HttpPriority merged = PriorityCodec.MergeClientAndServer(client, server);

        Assert.Multiple(() =>
        {
            Assert.That(merged.Urgency, Is.EqualTo(1));
            Assert.That(merged.Incremental, Is.False);
            Assert.That(merged.StructuredValue, Is.Null);
            Assert.That(merged.UnknownParameters, Has.Count.EqualTo(3));
            Assert.That(merged.UnknownParameters[0].DictionaryKey, Is.EqualTo("shared"));
            Assert.That(merged.UnknownParameters[0].TextValue, Is.EqualTo("server"));
            Assert.That(merged.UnknownParameters[1].DictionaryKey, Is.EqualTo("client-only"));
            Assert.That(merged.UnknownParameters[2].DictionaryKey, Is.EqualTo("server-only"));
        });
    }

    [Test]
    public void PublicOperationsRejectInvalidContextsAndCandidates()
    {
        HttpNegotiationCandidate missing = new HttpNegotiationCandidate();
        HttpNegotiationCandidate invalidMedia = Candidate("text", "plain", null, null, null, 0, 0);
        invalidMedia.Representation!.MediaType!.IsSubtypeWildcard = true;
        invalidMedia.Representation.MediaType.Subtype = "";
        HttpNegotiationCandidate invalidCharset = Candidate(null, null, "*", null, null, 0, 0);
        HttpNegotiationCandidate invalidEncoding = Candidate(null, null, null, "*", null, 0, 0);
        HttpNegotiationCandidate invalidLanguage = Candidate(null, null, null, null, "bad_tag", 0, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => service.SelectMediaType(
                new HttpRequest(), [invalidMedia]));
            Assert.Throws<HttpProtocolException>(() => service.SelectCharset(
                new HttpRequest(), [invalidCharset]));
            Assert.Throws<HttpProtocolException>(() => service.SelectContentCoding(
                new HttpRequest(), [invalidEncoding]));
            Assert.Throws<HttpProtocolException>(() => service.SelectLanguage(
                new HttpRequest(), [invalidLanguage]));
            Assert.Throws<HttpProtocolException>(() => service.SelectRepresentation(
                new HttpRequest(), [missing]));
            Assert.Throws<HttpProtocolException>(() => service.SelectMediaType(null!, []));
            Assert.Throws<HttpProtocolException>(() => service.BuildVaryKey(
                null!, new HttpHeaders()));
            Assert.Throws<HttpProtocolException>(() => service.CompareVaryKey(null!, "V1;0;"));
        });
    }

    static HttpNegotiationCandidate Candidate(
        string? type,
        string? subtype,
        string? charset,
        string? coding,
        string? language,
        int serverPreference,
        int sourceOrder)
    {
        HttpRepresentation representation = new HttpRepresentation();
        if (type != null && subtype != null)
        {
            HttpMediaType media = new HttpMediaType();
            media.Type = type;
            media.Subtype = subtype;
            representation.MediaType = media;
        }

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

    static void AddMediaParameter(HttpNegotiationCandidate candidate, string name, string value)
    {
        HttpParameter parameter = new HttpParameter();
        parameter.Name = name;
        parameter.HasValue = true;
        parameter.Value = Encoding.ASCII.GetBytes(value);
        candidate.Representation!.MediaType!.Parameters.Add(parameter);
    }

    static HttpRequest Request(string name, string value)
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, name, value);
        return request;
    }

    static HttpHeaders ParsedNames(params string[] names)
    {
        HttpHeaders result = new HttpHeaders();
        foreach (string name in names)
        {
            HttpHeaderField field = new HttpHeaderField();
            field.Name = name;
            result.Fields.Add(field);
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
