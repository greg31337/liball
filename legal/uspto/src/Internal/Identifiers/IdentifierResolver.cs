namespace uspto;

internal class IdentifierResolutionLookupResult
{
    internal int MatchCount = 0;
    internal string[] ApplicationNumbers = new string[0];
}

internal interface IIdentifierResolutionLookup
{
    IdentifierResolutionLookupResult LookupExact(string fieldName, string fieldValue, RequestContext context);
}

internal class IdentifierResolver
{
    internal IIdentifierResolutionLookup Lookup;
    internal global::System.Collections.Generic.Dictionary<string, string> ResolutionCache = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal object CacheLock = new object();

    internal IdentifierResolver(IIdentifierResolutionLookup lookup)
    {
        if (lookup == null)
        {
            throw new ValidationException("An identifier resolution lookup is required.", "lookup", "REQUIRED", null);
        }

        Lookup = lookup;
    }

    internal string Resolve(string identifier, RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        IdentifierNormalizationResult normalized = IdentifierNormalization.NormalizePatent(identifier);
        if (!normalized.IsValid && !normalized.IsAmbiguous)
        {
            throw new ValidationException("The patent identifier is invalid.", "identifier", normalized.FailureCode, null);
        }

        if (normalized.IsValid && normalized.PatentKind == IdentifierKind.PatentApplication)
        {
            return normalized.CanonicalNumber;
        }

        string cacheKey = CreateCacheKey(normalized);
        string? cached = ReadCache(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        string? resolved;
        if (normalized.IsAmbiguous)
        {
            resolved = ResolveCandidates(normalized.Candidates, normalized.CandidateCount, context);
        }
        else
        {
            resolved = ResolveOne(normalized.PatentKind, normalized.CanonicalNumber, context);
        }

        if (resolved == null)
        {
            throw new NotFoundException("No patent application matched the identifier.", ApiService.Odp, cacheKey, 0, false, null, null);
        }

        WriteCache(cacheKey, resolved);
        return resolved;
    }

    internal string? ResolveCandidates(PatentIdentifierCandidateResult[] candidates, int candidateCount, RequestContext context)
    {
        string? resolved = null;
        int index;
        for (index = 0; index < candidateCount; index += 1)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            PatentIdentifierCandidateResult candidate = candidates[index];
            string? candidateResolution = ResolveOne(candidate.Kind, candidate.CanonicalNumber, context);
            if (candidateResolution != null)
            {
                if (resolved != null && !global::System.String.Equals(resolved, candidateResolution, global::System.StringComparison.Ordinal))
                {
                    throw new ValidationException("The patent identifier matched multiple application candidates.", "identifier", "AMBIGUOUS_IDENTIFIER_MATCH", null);
                }

                resolved = candidateResolution;
            }
        }

        return resolved;
    }

    internal string? ResolveOne(IdentifierKind kind, string canonicalNumber, RequestContext context)
    {
        string fieldName = ExactFieldName(kind);
        string fieldValue = ExactFieldValue(kind, canonicalNumber);
        IdentifierResolutionLookupResult result = Lookup.LookupExact(fieldName, fieldValue, context);
        if (result == null)
        {
            throw new ParseException("The identifier lookup returned no result.", DocumentFormat.Json, "ODP patent application search", null, 0L, false, 0, false, null);
        }

        if (result.MatchCount < 0 || result.ApplicationNumbers == null)
        {
            throw new ParseException("The identifier lookup returned an invalid match count.", DocumentFormat.Json, "ODP patent application search", "$.count", 0L, false, 0, false, null);
        }

        if (result.MatchCount == 0)
        {
            return null;
        }

        if (result.MatchCount != 1 || result.ApplicationNumbers.Length != 1)
        {
            throw new ValidationException("The patent identifier matched multiple applications.", "identifier", "MULTIPLE_IDENTIFIER_MATCHES", null);
        }

        PatentApplicationNumberResult application = PatentIdentifierParser.ParseApplicationNumber(result.ApplicationNumbers[0]);
        if (!application.IsValid)
        {
            throw new ParseException("The identifier lookup returned an invalid application number.", DocumentFormat.Json, "ODP patent application search", "$.patentFileWrapperDataBag[0].applicationNumberText", 0L, false, 0, false, null);
        }

        return application.CanonicalNumber;
    }

    internal static string ExactFieldName(IdentifierKind kind)
    {
        switch (kind)
        {
            case IdentifierKind.PatentApplication:
            case IdentifierKind.PctApplication:
                return "applicationNumberText";
            case IdentifierKind.PatentGrant:
                return "applicationMetaData.patentNumber";
            case IdentifierKind.PatentPublication:
                return "applicationMetaData.earliestPublicationNumber";
            default:
                throw new ValidationException("The identifier kind cannot be resolved online.", "identifier", "UNSUPPORTED_IDENTIFIER_KIND", null);
        }
    }

    internal static string ExactFieldValue(IdentifierKind kind, string canonicalNumber)
    {
        switch (kind)
        {
            case IdentifierKind.PatentApplication:
                PatentApplicationNumberResult application = PatentIdentifierParser.ParseApplicationNumber(canonicalNumber);
                if (!application.IsValid)
                {
                    throw new ValidationException("The application identifier is invalid.", "identifier", application.FailureCode, null);
                }

                return application.CanonicalNumber;
            case IdentifierKind.PctApplication:
                PctApplicationNumberResult pct = PatentIdentifierParser.ParsePctApplicationNumber(canonicalNumber);
                if (!pct.IsValid)
                {
                    throw new ValidationException("The PCT identifier is invalid.", "identifier", pct.FailureCode, null);
                }

                return pct.CanonicalNumber;
            case IdentifierKind.PatentGrant:
                PatentGrantNumberResult grant = PatentIdentifierParser.ParseGrantNumber(canonicalNumber);
                if (!grant.IsValid)
                {
                    throw new ValidationException("The grant identifier is invalid.", "identifier", grant.FailureCode, null);
                }

                return grant.DocumentPrefix + TrimLeadingZeroes(grant.NumberDigits);
            case IdentifierKind.PatentPublication:
                PatentPublicationNumberResult publication = PatentIdentifierParser.ParsePublicationNumber(canonicalNumber);
                if (!publication.IsValid)
                {
                    throw new ValidationException("The publication identifier is invalid.", "identifier", publication.FailureCode, null);
                }

                return publication.DisplayNumber;
            default:
                throw new ValidationException("The identifier kind cannot be resolved online.", "identifier", "UNSUPPORTED_IDENTIFIER_KIND", null);
        }
    }

    internal static string TrimLeadingZeroes(string digits)
    {
        int index = 0;
        while (index + 1 < digits.Length && digits[index] == '0')
        {
            index += 1;
        }

        return digits.Substring(index);
    }

    internal static string CreateCacheKey(IdentifierNormalizationResult normalized)
    {
        if (!normalized.IsAmbiguous)
        {
            return ((int)normalized.PatentKind).ToString(global::System.Globalization.CultureInfo.InvariantCulture) + ":" + normalized.CanonicalNumber;
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder("AMBIGUOUS");
        int index;
        for (index = 0; index < normalized.CandidateCount; index += 1)
        {
            builder.Append(':');
            builder.Append((int)normalized.Candidates[index].Kind);
            builder.Append(':');
            builder.Append(normalized.Candidates[index].CanonicalNumber);
        }

        return builder.ToString();
    }

    internal string? ReadCache(string key)
    {
        lock (CacheLock)
        {
            if (ResolutionCache.ContainsKey(key))
            {
                return ResolutionCache[key];
            }
        }

        return null;
    }

    internal void WriteCache(string key, string applicationNumber)
    {
        lock (CacheLock)
        {
            ResolutionCache[key] = applicationNumber;
        }
    }
}
