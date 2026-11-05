using System.Collections.Generic;
using System.Text;

namespace http;

internal static class NegotiationMatcher
{
    internal static HttpNegotiationResult SelectMedia(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        ValidateContext(request, candidates);
        List<HttpHeaderField> acceptFields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.Accept);
        if (acceptFields.Count == 0)
        {
            ValidateMediaCandidates(candidates);
            return SelectWithoutMediaPreference(candidates);
        }

        byte[] combined = HeaderStore.CombineMatchingValues(
            request.Headers,
            HttpHeaderNames.Accept);
        MediaTypeParseResult parsed = FieldValueParser.ParseWeightedMediaRanges(
            combined,
            combined.Length,
            int.MaxValue,
            int.MaxValue);
        if (parsed.Status != HttpOperationStatus.Done || HasDuplicateParameters(parsed.Values))
        {
            return CreateMalformed();
        }

        if (parsed.Values.Count == 0)
        {
            return CreateNoAcceptable(true, "The Accept field contains no media ranges.");
        }

        HttpNegotiationCandidate? bestCandidate = null;
        MediaNegotiationMatch? bestMatch = null;
        int bestCandidateIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            if (representation.MediaType != null)
            {
                MediaNegotiationMatch match = MatchMediaCandidate(
                    representation,
                    parsed.Values);
                if (match.Matches && match.Quality > 0 && IsBetterCandidate(
                    candidate,
                    match,
                    index,
                    bestCandidate,
                    bestMatch,
                    bestCandidateIndex))
                {
                    bestCandidate = candidate;
                    bestMatch = match;
                    bestCandidateIndex = index;
                }
            }

            index = index + 1;
        }

        if (bestCandidate == null || bestMatch == null)
        {
            return CreateNoAcceptable(
                true,
                "No available representation matches an acceptable media range.");
        }

        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.Selected;
        result.Candidate = bestCandidate;
        result.Representation = GetCandidateRepresentation(bestCandidate);
        result.HasMediaMatch = true;
        result.MediaQuality = bestMatch.Quality;
        result.MediaSpecificity = bestMatch.Specificity;
        result.Reason = "Selected by Accept media-range precedence.";
        return result;
    }

    internal static HttpNegotiationResult SelectCharset(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        ValidateContext(request, candidates);
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.AcceptCharset);
        if (fields.Count == 0)
        {
            ValidateCharsetCandidates(candidates);
            return SelectWithoutPreference(
                candidates,
                "Accept-Charset is absent, so server preference selected the representation.");
        }

        MediaTypeParseResult parsed = ParseTokenPreferences(
            request.Headers,
            HttpHeaderNames.AcceptCharset);
        if (!AreValidTokenPreferences(parsed))
        {
            return CreateMalformedCharset();
        }

        HttpNegotiationCandidate? bestCandidate = null;
        TokenNegotiationMatch? bestMatch = null;
        int bestCandidateIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            TokenNegotiationMatch match = MatchCharsetCandidate(
                representation,
                parsed.Values);
            if (match.Matches && match.Quality > 0 && IsBetterTokenCandidate(
                candidate,
                match,
                index,
                bestCandidate,
                bestMatch,
                bestCandidateIndex))
            {
                bestCandidate = candidate;
                bestMatch = match;
                bestCandidateIndex = index;
            }

            index = index + 1;
        }

        if (bestCandidate == null || bestMatch == null)
        {
            return CreateNoCharset();
        }

        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.Selected;
        result.Candidate = bestCandidate;
        result.Representation = GetCandidateRepresentation(bestCandidate);
        result.HasCharsetMatch = true;
        result.CharsetQuality = bestMatch.Quality;
        result.CharsetSpecificity = bestMatch.Specificity;
        result.Reason = "Selected by Accept-Charset preference.";
        return result;
    }

    internal static HttpNegotiationResult SelectEncoding(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        ValidateContext(request, candidates);
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.AcceptEncoding);
        if (fields.Count == 0)
        {
            ValidateEncodingCandidates(candidates);
            return SelectWithoutPreference(
                candidates,
                "Accept-Encoding is absent, so server preference selected the representation.");
        }

        MediaTypeParseResult parsed = ParseTokenPreferences(
            request.Headers,
            HttpHeaderNames.AcceptEncoding);
        if (!AreValidTokenPreferences(parsed))
        {
            return CreateMalformedEncoding();
        }

        HttpNegotiationCandidate? bestCandidate = null;
        TokenNegotiationMatch? bestMatch = null;
        int bestCandidateIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            ValidateContentCodings(representation.ContentCodings);
            TokenNegotiationMatch match = MatchEncodingCandidate(
                representation.ContentCodings,
                parsed.Values);
            if (match.Matches && match.Quality > 0 && IsBetterTokenCandidate(
                candidate,
                match,
                index,
                bestCandidate,
                bestMatch,
                bestCandidateIndex))
            {
                bestCandidate = candidate;
                bestMatch = match;
                bestCandidateIndex = index;
            }

            index = index + 1;
        }

        if (bestCandidate == null || bestMatch == null)
        {
            return CreateNoEncoding();
        }

        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.Selected;
        result.Candidate = bestCandidate;
        result.Representation = GetCandidateRepresentation(bestCandidate);
        result.HasEncodingMatch = true;
        result.EncodingQuality = bestMatch.Quality;
        result.EncodingSpecificity = bestMatch.Specificity;
        result.Reason = "Selected by Accept-Encoding preference.";
        return result;
    }

    internal static HttpNegotiationResult SelectLanguage(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return SelectLanguageFiltering(request, candidates, false);
    }

    internal static HttpNegotiationResult SelectLanguageExtended(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        return SelectLanguageFiltering(request, candidates, true);
    }

    internal static HttpNegotiationResult SelectLanguageLookup(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        ValidateContext(request, candidates);
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.AcceptLanguage);
        if (fields.Count == 0)
        {
            ValidateLanguageCandidates(candidates);
            return SelectWithoutPreference(
                candidates,
                "Accept-Language is absent, so server preference selected the representation.");
        }

        LanguageRangeParseResult parsed = ParseLanguagePreferences(
            request.Headers,
            false);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedLanguage();
        }

        ValidateLanguageCandidates(candidates);
        bool[] processed = new bool[parsed.Values.Count];
        int processedCount = 0;
        while (processedCount < parsed.Values.Count)
        {
            int preferenceIndex = FindNextLookupPreference(parsed.Values, processed);
            if (preferenceIndex < 0)
            {
                break;
            }

            processed[preferenceIndex] = true;
            processedCount = processedCount + 1;
            HttpLanguageRange preference = parsed.Values[preferenceIndex];
            if (preference.Quality > 0)
            {
                string current = preference.Range;
                while (current.Length != 0)
                {
                    HttpNegotiationCandidate? candidate = FindLookupCandidate(
                        current,
                        candidates,
                        parsed.Values,
                        preference);
                    if (candidate != null)
                    {
                        return CreateSelectedLanguage(
                            candidate,
                            preference.Quality,
                            CountLanguageSubtags(current),
                            "Selected by RFC 4647 language lookup.");
                    }

                    current = TruncateLookupRange(current);
                }
            }
        }

        HttpLanguageRange? wildcard = FindLookupWildcard(parsed.Values);
        if (wildcard != null && wildcard.Quality > 0)
        {
            HttpNegotiationCandidate? fallback = FindLookupDefaultCandidate(
                candidates,
                parsed.Values);
            if (fallback != null)
            {
                return CreateSelectedLanguage(
                    fallback,
                    wildcard.Quality,
                    0,
                    "Selected the deterministic wildcard default for language lookup.");
            }
        }

        return CreateNoLanguage();
    }

    internal static HttpNegotiationResult SelectRepresentation(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        ValidateContext(request, candidates);
        CombinedNegotiationContext context = ParseCombinedContext(request.Headers);
        HttpNegotiationResult? malformed = GetCombinedMalformedResult(context);
        if (malformed != null)
        {
            return malformed;
        }

        if (!context.HasMedia && !context.HasCharset &&
            !context.HasEncoding && !context.HasLanguage)
        {
            ValidateCombinedCandidates(candidates);
            return SelectWithoutPreference(
                candidates,
                "No proactive negotiation field is present, so server preference selected " +
                "the representation.");
        }

        HttpNegotiationCandidate? bestCandidate = null;
        CombinedNegotiationMatch? bestMatch = null;
        int bestCandidateIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            ValidateCombinedRepresentation(representation);
            CombinedNegotiationMatch match = MatchCombinedRepresentation(
                representation,
                context);
            if (match.Acceptable && IsBetterCombinedCandidate(
                candidate,
                match,
                index,
                bestCandidate,
                bestMatch,
                bestCandidateIndex))
            {
                bestCandidate = candidate;
                bestMatch = match;
                bestCandidateIndex = index;
            }

            index = index + 1;
        }

        if (bestCandidate == null || bestMatch == null)
        {
            return CreateNoCombinedRepresentation(context);
        }

        return CreateSelectedCombinedRepresentation(bestCandidate, bestMatch, context);
    }

    static HttpNegotiationResult SelectLanguageFiltering(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates,
        bool extended)
    {
        ValidateContext(request, candidates);
        List<HttpHeaderField> fields = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.AcceptLanguage);
        if (fields.Count == 0)
        {
            ValidateLanguageCandidates(candidates);
            return SelectWithoutPreference(
                candidates,
                "Accept-Language is absent, so server preference selected the representation.");
        }

        LanguageRangeParseResult parsed = ParseLanguagePreferences(
            request.Headers,
            extended);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedLanguage();
        }

        HttpNegotiationCandidate? bestCandidate = null;
        LanguageNegotiationMatch? bestMatch = null;
        int bestCandidateIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            ValidateLanguages(representation.Languages);
            LanguageNegotiationMatch match = MatchLanguageCandidate(
                representation.Languages,
                parsed.Values,
                extended);
            if (match.Matches && match.Quality > 0 && IsBetterLanguageCandidate(
                candidate,
                match,
                index,
                bestCandidate,
                bestMatch,
                bestCandidateIndex))
            {
                bestCandidate = candidate;
                bestMatch = match;
                bestCandidateIndex = index;
            }

            index = index + 1;
        }

        if (bestCandidate == null || bestMatch == null)
        {
            return CreateNoLanguage();
        }

        return CreateSelectedLanguage(
            bestCandidate,
            bestMatch.Quality,
            bestMatch.Specificity,
            extended ?
                "Selected by RFC 4647 extended language filtering." :
                "Selected by RFC 4647 basic language filtering.");
    }

    static CombinedNegotiationContext ParseCombinedContext(HttpHeaders headers)
    {
        CombinedNegotiationContext context = new CombinedNegotiationContext();
        context.HasMedia = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.Accept).Count != 0;
        context.HasCharset = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.AcceptCharset).Count != 0;
        context.HasEncoding = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.AcceptEncoding).Count != 0;
        context.HasLanguage = HeaderStore.FindMatchingFields(
            headers,
            HttpHeaderNames.AcceptLanguage).Count != 0;

        if (context.HasMedia)
        {
            byte[] combined = HeaderStore.CombineMatchingValues(
                headers,
                HttpHeaderNames.Accept);
            context.Media = FieldValueParser.ParseWeightedMediaRanges(
                combined,
                combined.Length,
                int.MaxValue,
                int.MaxValue);
        }

        if (context.HasCharset)
        {
            context.Charsets = ParseTokenPreferences(
                headers,
                HttpHeaderNames.AcceptCharset);
        }

        if (context.HasEncoding)
        {
            context.Encodings = ParseTokenPreferences(
                headers,
                HttpHeaderNames.AcceptEncoding);
        }

        if (context.HasLanguage)
        {
            context.Languages = ParseLanguagePreferences(headers, false);
        }

        return context;
    }

    static HttpNegotiationResult? GetCombinedMalformedResult(
        CombinedNegotiationContext context)
    {
        if (context.HasMedia &&
            (context.Media.Status != HttpOperationStatus.Done ||
            HasDuplicateParameters(context.Media.Values)))
        {
            return CreateMalformed();
        }

        if (context.HasCharset && !AreValidTokenPreferences(context.Charsets))
        {
            return CreateMalformedCharset();
        }

        if (context.HasEncoding && !AreValidTokenPreferences(context.Encodings))
        {
            return CreateMalformedEncoding();
        }

        if (context.HasLanguage && context.Languages.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedLanguage();
        }

        return null;
    }

    static CombinedNegotiationMatch MatchCombinedRepresentation(
        HttpRepresentation representation,
        CombinedNegotiationContext context)
    {
        CombinedNegotiationMatch result = new CombinedNegotiationMatch();
        result.Acceptable = true;
        result.OverallQuality = 1;

        if (context.HasMedia)
        {
            if (representation.MediaType == null)
            {
                result.Acceptable = false;
                return result;
            }

            result.Media = MatchMediaCandidate(representation, context.Media.Values);
            if (!result.Media.Matches || result.Media.Quality == 0)
            {
                result.Acceptable = false;
                return result;
            }

            result.OverallQuality = result.OverallQuality * result.Media.Quality;
            result.TotalSpecificity = result.TotalSpecificity + result.Media.Specificity;
            result.TotalClientOrder = result.TotalClientOrder + result.Media.ClientOrder;
        }

        if (context.HasCharset)
        {
            result.Charset = MatchCharsetCandidate(
                representation,
                context.Charsets.Values);
            if (!result.Charset.Matches || result.Charset.Quality == 0)
            {
                result.Acceptable = false;
                return result;
            }

            result.OverallQuality = result.OverallQuality * result.Charset.Quality;
            result.TotalSpecificity = result.TotalSpecificity + result.Charset.Specificity;
            result.TotalClientOrder = result.TotalClientOrder + result.Charset.ClientOrder;
        }

        if (context.HasEncoding)
        {
            result.Encoding = MatchEncodingCandidate(
                representation.ContentCodings,
                context.Encodings.Values);
            if (!result.Encoding.Matches || result.Encoding.Quality == 0)
            {
                result.Acceptable = false;
                return result;
            }

            result.OverallQuality = result.OverallQuality * result.Encoding.Quality;
            result.TotalSpecificity = result.TotalSpecificity + result.Encoding.Specificity;
            result.TotalClientOrder = result.TotalClientOrder + result.Encoding.ClientOrder;
        }

        if (context.HasLanguage)
        {
            result.Language = MatchLanguageCandidate(
                representation.Languages,
                context.Languages.Values,
                false);
            if (!result.Language.Matches || result.Language.Quality == 0)
            {
                result.Acceptable = false;
                return result;
            }

            result.OverallQuality = result.OverallQuality * result.Language.Quality;
            result.TotalSpecificity = result.TotalSpecificity + result.Language.Specificity;
            result.TotalClientOrder = result.TotalClientOrder + result.Language.ClientOrder;
        }

        return result;
    }

    static bool IsBetterCombinedCandidate(
        HttpNegotiationCandidate candidate,
        CombinedNegotiationMatch match,
        int candidateIndex,
        HttpNegotiationCandidate? bestCandidate,
        CombinedNegotiationMatch? bestMatch,
        int bestCandidateIndex)
    {
        if (bestCandidate == null || bestMatch == null)
        {
            return true;
        }

        if (match.OverallQuality != bestMatch.OverallQuality)
        {
            return match.OverallQuality > bestMatch.OverallQuality;
        }

        if (match.TotalSpecificity != bestMatch.TotalSpecificity)
        {
            return match.TotalSpecificity > bestMatch.TotalSpecificity;
        }

        if (match.TotalClientOrder != bestMatch.TotalClientOrder)
        {
            return match.TotalClientOrder < bestMatch.TotalClientOrder;
        }

        if (candidate.ServerPreference != bestCandidate.ServerPreference)
        {
            return candidate.ServerPreference > bestCandidate.ServerPreference;
        }

        if (candidate.SourceOrder != bestCandidate.SourceOrder)
        {
            return candidate.SourceOrder < bestCandidate.SourceOrder;
        }

        return candidateIndex < bestCandidateIndex;
    }

    static void ValidateCombinedCandidates(List<HttpNegotiationCandidate> candidates)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            HttpRepresentation representation = GetCandidateRepresentation(candidates[index]);
            ValidateCombinedRepresentation(representation);
            index = index + 1;
        }
    }

    static void ValidateCombinedRepresentation(HttpRepresentation representation)
    {
        if (representation.MediaType != null)
        {
            ValidateMediaType(representation.MediaType);
        }

        if (representation.Charset != null &&
            (!HeaderStore.IsValidName(representation.Charset) ||
            representation.Charset == "*"))
        {
            throw CreateProtocolError(
                "invalid-negotiation-candidate",
                "A candidate character set is not a valid token.");
        }

        ValidateContentCodings(representation.ContentCodings);
        ValidateLanguages(representation.Languages);
    }

    static HttpNegotiationResult SelectWithoutMediaPreference(
        List<HttpNegotiationCandidate> candidates)
    {
        return SelectWithoutPreference(
            candidates,
            "Accept is absent, so server preference selected the representation.");
    }

    static HttpNegotiationResult SelectWithoutPreference(
        List<HttpNegotiationCandidate> candidates,
        string reason)
    {
        if (candidates.Count == 0)
        {
            return CreateNoAcceptable(false, "No representations are available.");
        }

        HttpNegotiationCandidate best = candidates[0];
        GetCandidateRepresentation(best);
        int bestIndex = 0;
        int index = 1;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            GetCandidateRepresentation(candidate);
            if (IsBetterServerChoice(candidate, index, best, bestIndex))
            {
                best = candidate;
                bestIndex = index;
            }

            index = index + 1;
        }

        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.AbsentDimension;
        result.Candidate = best;
        result.Representation = GetCandidateRepresentation(best);
        result.Reason = reason;
        return result;
    }

    static MediaTypeParseResult ParseTokenPreferences(HttpHeaders headers, string name)
    {
        byte[] combined = HeaderStore.CombineMatchingValues(headers, name);
        return FieldValueParser.ParseWeightedValues(
            combined,
            combined.Length,
            int.MaxValue,
            int.MaxValue);
    }

    static bool AreValidTokenPreferences(MediaTypeParseResult parsed)
    {
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return false;
        }

        int index = 0;
        while (index < parsed.Values.Count)
        {
            if (parsed.Values[index].Parameters.Count != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static TokenNegotiationMatch MatchCharsetCandidate(
        HttpRepresentation representation,
        List<HttpWeightedValue> preferences)
    {
        if (representation.Charset == null)
        {
            TokenNegotiationMatch unspecified = new TokenNegotiationMatch();
            unspecified.Matches = true;
            unspecified.Quality = 1000;
            unspecified.ClientOrder = int.MaxValue;
            return unspecified;
        }

        if (!HeaderStore.IsValidName(representation.Charset) ||
            representation.Charset == "*")
        {
            throw CreateProtocolError(
                "invalid-negotiation-candidate",
                "A candidate character set is not a valid token.");
        }

        return MatchOrdinaryToken(representation.Charset, preferences);
    }

    static TokenNegotiationMatch MatchEncodingCandidate(
        List<string> codings,
        List<HttpWeightedValue> preferences)
    {
        if (codings.Count == 0)
        {
            return MatchIdentityEncoding(preferences);
        }

        TokenNegotiationMatch result = new TokenNegotiationMatch();
        result.Matches = true;
        result.Quality = 1000;
        result.Specificity = 1000;
        int index = 0;
        while (index < codings.Count)
        {
            TokenNegotiationMatch coding;
            if (Ascii.EqualsStringsIgnoreCase(codings[index], HttpContentCodings.Identity))
            {
                coding = MatchIdentityEncoding(preferences);
            }
            else
            {
                coding = MatchOrdinaryToken(codings[index], preferences);
            }

            if (!coding.Matches || coding.Quality == 0)
            {
                return coding;
            }

            if (coding.Quality < result.Quality)
            {
                result.Quality = coding.Quality;
            }

            if (coding.Specificity < result.Specificity)
            {
                result.Specificity = coding.Specificity;
            }

            if (coding.ClientOrder > result.ClientOrder)
            {
                result.ClientOrder = coding.ClientOrder;
            }

            index = index + 1;
        }

        return result;
    }

    static TokenNegotiationMatch MatchOrdinaryToken(
        string value,
        List<HttpWeightedValue> preferences)
    {
        TokenNegotiationMatch? wildcard = null;
        int index = 0;
        while (index < preferences.Count)
        {
            HttpWeightedValue preference = preferences[index];
            if (Ascii.EqualsStringsIgnoreCase(preference.Value, value))
            {
                return CreateTokenMatch(preference, 1000);
            }

            if (preference.Value == "*" && wildcard == null)
            {
                wildcard = CreateTokenMatch(preference, 0);
            }

            index = index + 1;
        }

        if (wildcard != null)
        {
            return wildcard;
        }

        return new TokenNegotiationMatch();
    }

    static TokenNegotiationMatch MatchIdentityEncoding(
        List<HttpWeightedValue> preferences)
    {
        TokenNegotiationMatch? wildcard = null;
        int index = 0;
        while (index < preferences.Count)
        {
            HttpWeightedValue preference = preferences[index];
            if (Ascii.EqualsStringsIgnoreCase(
                preference.Value,
                HttpContentCodings.Identity))
            {
                return CreateTokenMatch(preference, 1000);
            }

            if (preference.Value == "*" && wildcard == null)
            {
                wildcard = CreateTokenMatch(preference, 0);
            }

            index = index + 1;
        }

        if (wildcard != null && wildcard.Quality == 0)
        {
            return wildcard;
        }

        TokenNegotiationMatch result = new TokenNegotiationMatch();
        result.Matches = true;
        result.Quality = 1000;
        result.ClientOrder = int.MaxValue;
        return result;
    }

    static TokenNegotiationMatch CreateTokenMatch(
        HttpWeightedValue preference,
        int specificity)
    {
        TokenNegotiationMatch result = new TokenNegotiationMatch();
        result.Matches = true;
        result.Quality = preference.Quality;
        result.Specificity = specificity;
        result.ClientOrder = preference.SourceOrder;
        return result;
    }

    static LanguageRangeParseResult ParseLanguagePreferences(
        HttpHeaders headers,
        bool extended)
    {
        byte[] combined = HeaderStore.CombineMatchingValues(
            headers,
            HttpHeaderNames.AcceptLanguage);
        return FieldValueParser.ParseLanguageRanges(
            combined,
            combined.Length,
            int.MaxValue,
            extended);
    }

    static LanguageNegotiationMatch MatchLanguageCandidate(
        List<string> languages,
        List<HttpLanguageRange> preferences,
        bool extended)
    {
        if (languages.Count == 0)
        {
            LanguageNegotiationMatch unspecified = new LanguageNegotiationMatch();
            unspecified.Matches = true;
            unspecified.Quality = 1000;
            unspecified.ClientOrder = int.MaxValue;
            unspecified.LanguageOrder = int.MaxValue;
            return unspecified;
        }

        LanguageNegotiationMatch? best = null;
        int index = 0;
        while (index < languages.Count)
        {
            LanguageNegotiationMatch match = extended ?
                MatchExtendedLanguageTag(languages[index], preferences) :
                MatchBasicLanguageTag(languages[index], preferences);
            match.LanguageOrder = index;
            if (match.Matches && match.Quality > 0 &&
                IsBetterLanguageWithinCandidate(match, best))
            {
                best = match;
            }

            index = index + 1;
        }

        if (best == null)
        {
            return new LanguageNegotiationMatch();
        }

        return best;
    }

    static LanguageNegotiationMatch MatchBasicLanguageTag(
        string language,
        List<HttpLanguageRange> preferences)
    {
        LanguageNegotiationMatch result = new LanguageNegotiationMatch();
        int index = 0;
        while (index < preferences.Count)
        {
            HttpLanguageRange preference = preferences[index];
            if ((preference.IsWildcard ||
                MatchesBasicLanguageRange(preference.Range, language)) &&
                IsBetterLanguageRange(preference, result))
            {
                result.Matches = true;
                result.Quality = preference.Quality;
                result.Specificity = preference.Specificity;
                result.ClientOrder = preference.SourceOrder;
            }

            index = index + 1;
        }

        return result;
    }

    static LanguageNegotiationMatch MatchExtendedLanguageTag(
        string language,
        List<HttpLanguageRange> preferences)
    {
        LanguageNegotiationMatch result = new LanguageNegotiationMatch();
        int index = 0;
        while (index < preferences.Count)
        {
            HttpLanguageRange preference = preferences[index];
            if (MatchesExtendedLanguageRange(preference.Range, language) &&
                IsBetterLanguageRange(preference, result))
            {
                result.Matches = true;
                result.Quality = preference.Quality;
                result.Specificity = preference.Specificity;
                result.ClientOrder = preference.SourceOrder;
            }

            index = index + 1;
        }

        return result;
    }

    static bool MatchesBasicLanguageRange(string range, string language)
    {
        if (range.Length > language.Length)
        {
            return false;
        }

        if (!Ascii.EqualsStringsIgnoreCase(
            range,
            language.Substring(0, range.Length)))
        {
            return false;
        }

        return range.Length == language.Length || language[range.Length] == '-';
    }

    static bool MatchesExtendedLanguageRange(string range, string language)
    {
        if (range == "*")
        {
            return true;
        }

        string[] rangeSubtags = range.Split('-');
        string[] languageSubtags = language.Split('-');
        if (rangeSubtags[0] != "*" && !Ascii.EqualsStringsIgnoreCase(
            rangeSubtags[0],
            languageSubtags[0]))
        {
            return false;
        }

        int rangeIndex = 1;
        int languageIndex = 1;
        while (rangeIndex < rangeSubtags.Length)
        {
            if (rangeSubtags[rangeIndex] == "*")
            {
                rangeIndex = rangeIndex + 1;
            }
            else if (languageIndex >= languageSubtags.Length)
            {
                return false;
            }
            else if (Ascii.EqualsStringsIgnoreCase(
                rangeSubtags[rangeIndex],
                languageSubtags[languageIndex]))
            {
                rangeIndex = rangeIndex + 1;
                languageIndex = languageIndex + 1;
            }
            else if (languageSubtags[languageIndex].Length == 1)
            {
                return false;
            }
            else
            {
                languageIndex = languageIndex + 1;
            }
        }

        return true;
    }

    static bool IsBetterLanguageRange(
        HttpLanguageRange preference,
        LanguageNegotiationMatch current)
    {
        if (!current.Matches)
        {
            return true;
        }

        if (preference.Specificity != current.Specificity)
        {
            return preference.Specificity > current.Specificity;
        }

        return preference.SourceOrder < current.ClientOrder;
    }

    static bool IsBetterLanguageWithinCandidate(
        LanguageNegotiationMatch match,
        LanguageNegotiationMatch? best)
    {
        if (best == null)
        {
            return true;
        }

        if (match.Quality != best.Quality)
        {
            return match.Quality > best.Quality;
        }

        if (match.Specificity != best.Specificity)
        {
            return match.Specificity > best.Specificity;
        }

        if (match.ClientOrder != best.ClientOrder)
        {
            return match.ClientOrder < best.ClientOrder;
        }

        return match.LanguageOrder < best.LanguageOrder;
    }

    static bool IsBetterLanguageCandidate(
        HttpNegotiationCandidate candidate,
        LanguageNegotiationMatch match,
        int candidateIndex,
        HttpNegotiationCandidate? bestCandidate,
        LanguageNegotiationMatch? bestMatch,
        int bestCandidateIndex)
    {
        if (bestCandidate == null || bestMatch == null)
        {
            return true;
        }

        if (match.Quality != bestMatch.Quality)
        {
            return match.Quality > bestMatch.Quality;
        }

        if (match.Specificity != bestMatch.Specificity)
        {
            return match.Specificity > bestMatch.Specificity;
        }

        if (match.ClientOrder != bestMatch.ClientOrder)
        {
            return match.ClientOrder < bestMatch.ClientOrder;
        }

        if (candidate.ServerPreference != bestCandidate.ServerPreference)
        {
            return candidate.ServerPreference > bestCandidate.ServerPreference;
        }

        if (candidate.SourceOrder != bestCandidate.SourceOrder)
        {
            return candidate.SourceOrder < bestCandidate.SourceOrder;
        }

        return candidateIndex < bestCandidateIndex;
    }

    static int FindNextLookupPreference(
        List<HttpLanguageRange> preferences,
        bool[] processed)
    {
        int best = -1;
        int index = 0;
        while (index < preferences.Count)
        {
            HttpLanguageRange preference = preferences[index];
            if (!processed[index] && !preference.IsWildcard &&
                (best < 0 || preference.Quality > preferences[best].Quality ||
                preference.Quality == preferences[best].Quality &&
                preference.SourceOrder < preferences[best].SourceOrder))
            {
                best = index;
            }

            index = index + 1;
        }

        return best;
    }

    static HttpNegotiationCandidate? FindLookupCandidate(
        string language,
        List<HttpNegotiationCandidate> candidates,
        List<HttpLanguageRange> preferences,
        HttpLanguageRange selectingPreference)
    {
        HttpNegotiationCandidate? best = null;
        int bestIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            if (HasLookupLanguage(
                representation.Languages,
                language,
                preferences,
                selectingPreference) &&
                (best == null || IsBetterServerChoice(
                    candidate,
                    index,
                    best,
                    bestIndex)))
            {
                best = candidate;
                bestIndex = index;
            }

            index = index + 1;
        }

        return best;
    }

    static bool HasLookupLanguage(
        List<string> languages,
        string language,
        List<HttpLanguageRange> preferences,
        HttpLanguageRange selectingPreference)
    {
        int index = 0;
        while (index < languages.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(languages[index], language) &&
                !IsLookupLanguageExcluded(
                    languages[index],
                    preferences,
                    selectingPreference))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsLookupLanguageExcluded(
        string language,
        List<HttpLanguageRange> preferences,
        HttpLanguageRange? selectingPreference)
    {
        LanguageNegotiationMatch match = MatchBasicLanguageTag(language, preferences);
        if (!match.Matches || match.Quality != 0)
        {
            return false;
        }

        return match.Specificity != 0 || selectingPreference == null;
    }

    static HttpLanguageRange? FindLookupWildcard(List<HttpLanguageRange> preferences)
    {
        HttpLanguageRange? best = null;
        int index = 0;
        while (index < preferences.Count)
        {
            HttpLanguageRange preference = preferences[index];
            if (preference.IsWildcard &&
                (best == null || preference.Quality > best.Quality ||
                preference.Quality == best.Quality &&
                preference.SourceOrder < best.SourceOrder))
            {
                best = preference;
            }

            index = index + 1;
        }

        return best;
    }

    static HttpNegotiationCandidate? FindLookupDefaultCandidate(
        List<HttpNegotiationCandidate> candidates,
        List<HttpLanguageRange> preferences)
    {
        HttpNegotiationCandidate? best = null;
        int bestIndex = -1;
        int index = 0;
        while (index < candidates.Count)
        {
            HttpNegotiationCandidate candidate = candidates[index];
            HttpRepresentation representation = GetCandidateRepresentation(candidate);
            if (IsLookupDefaultAllowed(representation.Languages, preferences) &&
                (best == null || IsBetterServerChoice(
                    candidate,
                    index,
                    best,
                    bestIndex)))
            {
                best = candidate;
                bestIndex = index;
            }

            index = index + 1;
        }

        return best;
    }

    static bool IsLookupDefaultAllowed(
        List<string> languages,
        List<HttpLanguageRange> preferences)
    {
        if (languages.Count == 0)
        {
            return true;
        }

        int index = 0;
        while (index < languages.Count)
        {
            if (!IsLookupLanguageExcluded(languages[index], preferences, null))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string TruncateLookupRange(string range)
    {
        int separator = range.LastIndexOf('-');
        if (separator < 0)
        {
            return "";
        }

        string result = range.Substring(0, separator);
        int preceding = result.LastIndexOf('-');
        int lastLength = result.Length - preceding - 1;
        if (lastLength == 1)
        {
            if (preceding < 0)
            {
                return "";
            }

            return result.Substring(0, preceding);
        }

        return result;
    }

    static int CountLanguageSubtags(string language)
    {
        int count = 1;
        int index = 0;
        while (index < language.Length)
        {
            if (language[index] == '-')
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static MediaNegotiationMatch MatchMediaCandidate(
        HttpRepresentation representation,
        List<HttpWeightedValue> preferences)
    {
        HttpMediaType? selectedMediaType = representation.MediaType;
        if (selectedMediaType == null)
        {
            return new MediaNegotiationMatch();
        }

        HttpMediaType mediaType = selectedMediaType;
        ValidateMediaType(mediaType);
        MediaNegotiationMatch result = new MediaNegotiationMatch();
        int index = 0;
        while (index < preferences.Count)
        {
            HttpWeightedValue preference = preferences[index];
            if (MatchesMediaRange(representation, mediaType, preference) &&
                (!result.Matches || preference.Specificity > result.Specificity ||
                preference.Specificity == result.Specificity &&
                preference.SourceOrder < result.ClientOrder))
            {
                result.Matches = true;
                result.Quality = preference.Quality;
                result.Specificity = preference.Specificity;
                result.ClientOrder = preference.SourceOrder;
            }

            index = index + 1;
        }

        return result;
    }

    static bool MatchesMediaRange(
        HttpRepresentation representation,
        HttpMediaType mediaType,
        HttpWeightedValue preference)
    {
        int slash = FindSlash(preference.Value);
        if (slash < 0)
        {
            return false;
        }

        string type = preference.Value.Substring(0, slash);
        string subtype = preference.Value.Substring(slash + 1);
        if (type != "*" && !Ascii.EqualsStringsIgnoreCase(type, mediaType.Type))
        {
            return false;
        }

        if (subtype != "*" && !Ascii.EqualsStringsIgnoreCase(subtype, mediaType.Subtype))
        {
            return false;
        }

        int index = 0;
        while (index < preference.Parameters.Count)
        {
            if (!HasMatchingParameter(
                representation,
                mediaType,
                preference.Parameters[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool HasMatchingParameter(
        HttpRepresentation representation,
        HttpMediaType mediaType,
        HttpParameter preference)
    {
        if (Ascii.EqualsStringsIgnoreCase(preference.Name, "charset") &&
            representation.Charset != null)
        {
            return IsAsciiString(representation.Charset) &&
                Ascii.EqualsBytesIgnoreCase(
                    preference.Value,
                    Encoding.ASCII.GetBytes(representation.Charset));
        }

        int index = 0;
        while (index < mediaType.Parameters.Count)
        {
            HttpParameter parameter = mediaType.Parameters[index];
            if (Ascii.EqualsStringsIgnoreCase(preference.Name, parameter.Name))
            {
                if (Ascii.EqualsStringsIgnoreCase(preference.Name, "charset"))
                {
                    return Ascii.EqualsBytesIgnoreCase(preference.Value, parameter.Value);
                }

                return EqualBytes(preference.Value, parameter.Value);
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsBetterCandidate(
        HttpNegotiationCandidate candidate,
        MediaNegotiationMatch match,
        int candidateIndex,
        HttpNegotiationCandidate? bestCandidate,
        MediaNegotiationMatch? bestMatch,
        int bestCandidateIndex)
    {
        if (bestCandidate == null || bestMatch == null)
        {
            return true;
        }

        if (match.Quality != bestMatch.Quality)
        {
            return match.Quality > bestMatch.Quality;
        }

        if (match.Specificity != bestMatch.Specificity)
        {
            return match.Specificity > bestMatch.Specificity;
        }

        if (match.ClientOrder != bestMatch.ClientOrder)
        {
            return match.ClientOrder < bestMatch.ClientOrder;
        }

        if (candidate.ServerPreference != bestCandidate.ServerPreference)
        {
            return candidate.ServerPreference > bestCandidate.ServerPreference;
        }

        if (candidate.SourceOrder != bestCandidate.SourceOrder)
        {
            return candidate.SourceOrder < bestCandidate.SourceOrder;
        }

        return candidateIndex < bestCandidateIndex;
    }

    static bool IsBetterTokenCandidate(
        HttpNegotiationCandidate candidate,
        TokenNegotiationMatch match,
        int candidateIndex,
        HttpNegotiationCandidate? bestCandidate,
        TokenNegotiationMatch? bestMatch,
        int bestCandidateIndex)
    {
        if (bestCandidate == null || bestMatch == null)
        {
            return true;
        }

        if (match.Quality != bestMatch.Quality)
        {
            return match.Quality > bestMatch.Quality;
        }

        if (match.Specificity != bestMatch.Specificity)
        {
            return match.Specificity > bestMatch.Specificity;
        }

        if (match.ClientOrder != bestMatch.ClientOrder)
        {
            return match.ClientOrder < bestMatch.ClientOrder;
        }

        if (candidate.ServerPreference != bestCandidate.ServerPreference)
        {
            return candidate.ServerPreference > bestCandidate.ServerPreference;
        }

        if (candidate.SourceOrder != bestCandidate.SourceOrder)
        {
            return candidate.SourceOrder < bestCandidate.SourceOrder;
        }

        return candidateIndex < bestCandidateIndex;
    }

    static bool IsBetterServerChoice(
        HttpNegotiationCandidate candidate,
        int candidateIndex,
        HttpNegotiationCandidate best,
        int bestIndex)
    {
        if (candidate.ServerPreference != best.ServerPreference)
        {
            return candidate.ServerPreference > best.ServerPreference;
        }

        if (candidate.SourceOrder != best.SourceOrder)
        {
            return candidate.SourceOrder < best.SourceOrder;
        }

        return candidateIndex < bestIndex;
    }

    static bool HasDuplicateParameters(List<HttpWeightedValue> values)
    {
        int valueIndex = 0;
        while (valueIndex < values.Count)
        {
            List<HttpParameter> parameters = values[valueIndex].Parameters;
            int index = 0;
            while (index < parameters.Count)
            {
                int compare = index + 1;
                while (compare < parameters.Count)
                {
                    if (Ascii.EqualsStringsIgnoreCase(
                        parameters[index].Name,
                        parameters[compare].Name))
                    {
                        return true;
                    }

                    compare = compare + 1;
                }

                index = index + 1;
            }

            valueIndex = valueIndex + 1;
        }

        return false;
    }

    static void ValidateContext(
        HttpRequest request,
        List<HttpNegotiationCandidate> candidates)
    {
        if (request == null || request.Headers == null ||
            request.Headers.Fields == null || candidates == null)
        {
            throw CreateProtocolError(
                "invalid-negotiation-context",
                "The media negotiation context is incomplete.");
        }

        int index = 0;
        while (index < request.Headers.Fields.Count)
        {
            HttpHeaderField field = request.Headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw CreateProtocolError(
                    "invalid-negotiation-context",
                    "The selecting request contains a malformed field.");
            }

            index = index + 1;
        }
    }

    static HttpRepresentation GetCandidateRepresentation(HttpNegotiationCandidate candidate)
    {
        if (candidate == null || candidate.Representation == null)
        {
            throw CreateProtocolError(
                "invalid-negotiation-candidate",
                "An available negotiation candidate is incomplete.");
        }

        return candidate.Representation;
    }

    static void ValidateMediaType(HttpMediaType mediaType)
    {
        FieldValueWriteResult serialized = FieldValueWriter.SerializeMediaType(
            mediaType,
            int.MaxValue,
            int.MaxValue);
        if (serialized.Status != HttpOperationStatus.Done ||
            HasDuplicateCandidateParameters(mediaType.Parameters))
        {
            throw CreateProtocolError(
                "invalid-negotiation-candidate",
                "A candidate media type is malformed or ambiguous.");
        }
    }

    static void ValidateContentCodings(List<string> codings)
    {
        if (codings == null)
        {
            throw CreateProtocolError(
                "invalid-negotiation-candidate",
                "A candidate content-coding collection is incomplete.");
        }

        int index = 0;
        while (index < codings.Count)
        {
            if (!HeaderStore.IsValidName(codings[index]) || codings[index] == "*")
            {
                throw CreateProtocolError(
                    "invalid-negotiation-candidate",
                    "A candidate content coding is not a valid token.");
            }

            index = index + 1;
        }
    }

    static void ValidateCharsetCandidates(List<HttpNegotiationCandidate> candidates)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            HttpRepresentation representation = GetCandidateRepresentation(candidates[index]);
            if (representation.Charset != null &&
                (!HeaderStore.IsValidName(representation.Charset) ||
                representation.Charset == "*"))
            {
                throw CreateProtocolError(
                    "invalid-negotiation-candidate",
                    "A candidate character set is not a valid token.");
            }

            index = index + 1;
        }
    }

    static void ValidateMediaCandidates(List<HttpNegotiationCandidate> candidates)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            HttpRepresentation representation = GetCandidateRepresentation(candidates[index]);
            if (representation.MediaType != null)
            {
                ValidateMediaType(representation.MediaType);
            }

            index = index + 1;
        }
    }

    static void ValidateEncodingCandidates(List<HttpNegotiationCandidate> candidates)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            HttpRepresentation representation = GetCandidateRepresentation(candidates[index]);
            ValidateContentCodings(representation.ContentCodings);
            index = index + 1;
        }
    }

    static void ValidateLanguageCandidates(List<HttpNegotiationCandidate> candidates)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            HttpRepresentation representation = GetCandidateRepresentation(candidates[index]);
            ValidateLanguages(representation.Languages);
            index = index + 1;
        }
    }

    static void ValidateLanguages(List<string> languages)
    {
        if (languages == null)
        {
            throw CreateProtocolError(
                "invalid-negotiation-candidate",
                "A candidate language collection is incomplete.");
        }

        int index = 0;
        while (index < languages.Count)
        {
            if (!RepresentationSemantics.IsLanguageTag(languages[index]))
            {
                throw CreateProtocolError(
                    "invalid-negotiation-candidate",
                    "A candidate language tag is malformed.");
            }

            index = index + 1;
        }
    }

    static bool HasDuplicateCandidateParameters(List<HttpParameter> parameters)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            int compare = index + 1;
            while (compare < parameters.Count)
            {
                if (Ascii.EqualsStringsIgnoreCase(
                    parameters[index].Name,
                    parameters[compare].Name))
                {
                    return true;
                }

                compare = compare + 1;
            }

            index = index + 1;
        }

        return false;
    }

    static bool EqualBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsAsciiString(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int FindSlash(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '/')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static HttpNegotiationResult CreateMalformed()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.MalformedPreference;
        result.HasMediaMatch = true;
        result.Reason = "The Accept field is malformed or contains duplicate parameters.";
        return result;
    }

    static HttpNegotiationResult CreateNoAcceptable(bool evaluated, string reason)
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.NoAcceptableRepresentation;
        result.HasMediaMatch = evaluated;
        result.Reason = reason;
        return result;
    }

    static HttpNegotiationResult CreateMalformedCharset()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.MalformedPreference;
        result.HasCharsetMatch = true;
        result.Reason = "The Accept-Charset field is malformed or ambiguous.";
        return result;
    }

    static HttpNegotiationResult CreateMalformedEncoding()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.MalformedPreference;
        result.HasEncodingMatch = true;
        result.Reason = "The Accept-Encoding field is malformed or ambiguous.";
        return result;
    }

    static HttpNegotiationResult CreateNoCharset()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.NoAcceptableRepresentation;
        result.HasCharsetMatch = true;
        result.Reason = "No available representation has an acceptable character set.";
        return result;
    }

    static HttpNegotiationResult CreateNoEncoding()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.NoAcceptableRepresentation;
        result.HasEncodingMatch = true;
        result.Reason = "No available representation has an acceptable content coding.";
        return result;
    }

    static HttpNegotiationResult CreateMalformedLanguage()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.MalformedPreference;
        result.HasLanguageMatch = true;
        result.Reason = "The Accept-Language field is malformed.";
        return result;
    }

    static HttpNegotiationResult CreateNoLanguage()
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.NoAcceptableRepresentation;
        result.HasLanguageMatch = true;
        result.Reason = "No available representation has an acceptable language.";
        return result;
    }

    static HttpNegotiationResult CreateSelectedLanguage(
        HttpNegotiationCandidate candidate,
        int quality,
        int specificity,
        string reason)
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.Selected;
        result.Candidate = candidate;
        result.Representation = GetCandidateRepresentation(candidate);
        result.HasLanguageMatch = true;
        result.LanguageQuality = quality;
        result.LanguageSpecificity = specificity;
        result.Reason = reason;
        return result;
    }

    static HttpNegotiationResult CreateNoCombinedRepresentation(
        CombinedNegotiationContext context)
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.NoAcceptableRepresentation;
        result.HasMediaMatch = context.HasMedia;
        result.HasCharsetMatch = context.HasCharset;
        result.HasEncodingMatch = context.HasEncoding;
        result.HasLanguageMatch = context.HasLanguage;
        result.HasOverallQuality = true;
        result.Reason = "No available representation is acceptable across every supplied " +
            "proactive negotiation field.";
        return result;
    }

    static HttpNegotiationResult CreateSelectedCombinedRepresentation(
        HttpNegotiationCandidate candidate,
        CombinedNegotiationMatch match,
        CombinedNegotiationContext context)
    {
        HttpNegotiationResult result = new HttpNegotiationResult();
        result.Outcome = HttpNegotiationOutcome.Selected;
        result.Candidate = candidate;
        result.Representation = GetCandidateRepresentation(candidate);
        result.HasMediaMatch = context.HasMedia;
        result.MediaQuality = match.Media.Quality;
        result.MediaSpecificity = match.Media.Specificity;
        result.HasCharsetMatch = context.HasCharset;
        result.CharsetQuality = match.Charset.Quality;
        result.CharsetSpecificity = match.Charset.Specificity;
        result.HasEncodingMatch = context.HasEncoding;
        result.EncodingQuality = match.Encoding.Quality;
        result.EncodingSpecificity = match.Encoding.Specificity;
        result.HasLanguageMatch = context.HasLanguage;
        result.LanguageQuality = match.Language.Quality;
        result.LanguageSpecificity = match.Language.Specificity;
        result.HasOverallQuality = true;
        result.OverallQuality = match.OverallQuality;
        result.Reason = $"Combined quality product: media={GetCombinedFactor(
            context.HasMedia,
            match.Media.Quality)}, charset={GetCombinedFactor(
            context.HasCharset,
            match.Charset.Quality)}, encoding={GetCombinedFactor(
            context.HasEncoding,
            match.Encoding.Quality)}, language={GetCombinedFactor(
            context.HasLanguage,
            match.Language.Quality)}; absent dimensions use 1; result={
            match.OverallQuality}. Ties use total specificity, client order, server " +
            "preference, and source order.";
        return result;
    }

    static int GetCombinedFactor(bool present, int quality)
    {
        if (present)
        {
            return quality;
        }

        return 1;
    }

    static HttpProtocolException CreateProtocolError(string code, string message)
    {
        return new HttpProtocolException(message, code);
    }
}
