namespace cookies;

internal static class CookieGraphCloner
{
    internal static CookieAttribute CloneAttribute(CookieAttribute source)
    {
        if (source == null)
        {
            return new CookieAttribute();
        }

        return new CookieAttribute
        {
            Name = source.Name,
            HasValue = source.HasValue,
            Value = source.Value
        };
    }

    internal static CookieAttribute[] CloneAttributes(CookieAttribute[] source)
    {
        if (source == null)
        {
            return [];
        }

        CookieAttribute[] copy = new CookieAttribute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CloneAttribute(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static CookieIssue CloneIssue(CookieIssue source)
    {
        if (source == null)
        {
            return new CookieIssue();
        }

        return new CookieIssue
        {
            ErrorCode = source.ErrorCode,
            Severity = source.Severity,
            HasOffset = source.HasOffset,
            Offset = source.Offset,
            Length = source.Length,
            SafeFragment = source.SafeFragment
        };
    }

    internal static CookieIssue[] CloneIssues(CookieIssue[] source)
    {
        if (source == null)
        {
            return [];
        }

        CookieIssue[] copy = new CookieIssue[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CloneIssue(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static CookiePair ClonePair(CookiePair source)
    {
        if (source == null)
        {
            return new CookiePair();
        }

        return new CookiePair
        {
            Name = source.Name,
            Value = source.Value,
            IsQuoted = source.IsQuoted
        };
    }

    internal static CookiePair[] ClonePairs(CookiePair[] source)
    {
        if (source == null)
        {
            return [];
        }

        CookiePair[] copy = new CookiePair[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = ClonePair(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static CookieLimits CloneLimits(CookieLimits source)
    {
        if (source == null)
        {
            CookieLimits invalid = new CookieLimits();
            invalid.MaximumNameValueOctets = -1;
            return invalid;
        }

        return new CookieLimits
        {
            MaximumNameValueOctets = source.MaximumNameValueOctets,
            MaximumAttributeValueOctets = source.MaximumAttributeValueOctets,
            MaximumInputLineOctets = source.MaximumInputLineOctets,
            MaximumCookiesPerDomain = source.MaximumCookiesPerDomain,
            MaximumCookies = source.MaximumCookies,
            MaximumRequestHeaderOctets = source.MaximumRequestHeaderOctets,
            MaximumLifetime = source.MaximumLifetime
        };
    }

    internal static CookieParseOptions CloneParseOptions(CookieParseOptions source)
    {
        if (source == null)
        {
            CookieParseOptions invalid = new CookieParseOptions();
            invalid.ComplianceMode = (CookieComplianceMode)(-1);
            return invalid;
        }

        return new CookieParseOptions
        {
            ComplianceMode = source.ComplianceMode,
            Limits = CloneLimits(source.Limits),
            PreserveUnknownAttributes = source.PreserveUnknownAttributes,
            ReportUnknownAttributes = source.ReportUnknownAttributes,
            ReportDuplicateAttributes = source.ReportDuplicateAttributes,
            ReportCompatibilityRecoveries = source.ReportCompatibilityRecoveries
        };
    }

    internal static CookieSerializationOptions CloneSerializationOptions(
        CookieSerializationOptions source)
    {
        if (source == null)
        {
            CookieSerializationOptions invalid = new CookieSerializationOptions();
            invalid.ComplianceMode = (CookieComplianceMode)(-1);
            return invalid;
        }

        return new CookieSerializationOptions
        {
            ComplianceMode = source.ComplianceMode,
            Limits = CloneLimits(source.Limits),
            PreserveQuotedValues = source.PreserveQuotedValues,
            IncludeExtensions = source.IncludeExtensions,
            SplitCookieFields = source.SplitCookieFields
        };
    }

    internal static NetscapeCookieFileOptions CloneNetscapeOptions(
        NetscapeCookieFileOptions source)
    {
        if (source == null)
        {
            NetscapeCookieFileOptions invalid = new NetscapeCookieFileOptions();
            invalid.Limits.MaximumNameValueOctets = -1;
            return invalid;
        }

        return new NetscapeCookieFileOptions
        {
            Limits = CloneLimits(source.Limits),
            IncludeSessionCookies = source.IncludeSessionCookies,
            IncludeExpiredCookies = source.IncludeExpiredCookies,
            ImportSessionCookies = source.ImportSessionCookies,
            ImportExpiredCookies = source.ImportExpiredCookies,
            RejectPartitionedCookies = source.RejectPartitionedCookies
        };
    }

    internal static string[] CloneStrings(string[] source)
    {
        if (source == null)
        {
            return [];
        }

        string[] copy = new string[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CookieJarOptions CloneJarOptions(CookieJarOptions source)
    {
        if (source == null)
        {
            CookieJarOptions invalid = new CookieJarOptions();
            invalid.ComplianceMode = (CookieComplianceMode)(-1);
            return invalid;
        }

        CookieJarOptions copy = new CookieJarOptions
        {
            ComplianceMode = source.ComplianceMode,
            Limits = CloneLimits(source.Limits),
            ParseOptions = CloneParseOptions(source.ParseOptions),
            SerializationOptions = CloneSerializationOptions(source.SerializationOptions),
            NetscapeFileOptions = CloneNetscapeOptions(source.NetscapeFileOptions),
            EnforceSameSite = source.EnforceSameSite,
            DefaultSameSiteMode = source.DefaultSameSiteMode,
            EnableLaxAllowingUnsafe = source.EnableLaxAllowingUnsafe,
            LaxAllowingUnsafeGracePeriod = source.LaxAllowingUnsafeGracePeriod,
            RequireSecureForSameSiteNone = source.RequireSecureForSameSiteNone,
            PrefixPolicy = source.PrefixPolicy,
            UseCaseInsensitivePrefixes = source.UseCaseInsensitivePrefixes,
            RequirePublicSuffixList = source.RequirePublicSuffixList,
            EnablePartitionedCookies = source.EnablePartitionedCookies,
            AllowedDomains = CloneStrings(source.AllowedDomains),
            BlockedDomains = CloneStrings(source.BlockedDomains),
            TreatLoopbackAsSecure = source.TreatLoopbackAsSecure,
            ProtectSecureCookieOverlay = source.ProtectSecureCookieOverlay,
            EvictNonSecureCookiesFirst = source.EvictNonSecureCookiesFirst
        };
        if (source.AllowedDomains == null || source.BlockedDomains == null)
        {
            copy.ComplianceMode = (CookieComplianceMode)(-1);
        }

        return copy;
    }

    internal static CookieIdentity CloneIdentity(CookieIdentity source)
    {
        if (source == null)
        {
            return new CookieIdentity();
        }

        return new CookieIdentity
        {
            Name = source.Name,
            Domain = source.Domain,
            Path = source.Path,
            HasPartitionKey = source.HasPartitionKey,
            PartitionKey = source.PartitionKey
        };
    }

    internal static SetCookie CloneSetCookie(SetCookie source)
    {
        if (source == null)
        {
            return new SetCookie();
        }

        return new SetCookie
        {
            Name = source.Name,
            Value = source.Value,
            IsValueQuoted = source.IsValueQuoted,
            HasExpires = source.HasExpires,
            Expires = source.Expires,
            HasMaxAge = source.HasMaxAge,
            MaxAgeSeconds = source.MaxAgeSeconds,
            HasDomain = source.HasDomain,
            Domain = source.Domain,
            HasPath = source.HasPath,
            Path = source.Path,
            Secure = source.Secure,
            HttpOnly = source.HttpOnly,
            HasSameSite = source.HasSameSite,
            SameSiteMode = source.SameSiteMode,
            Partitioned = source.Partitioned,
            Extensions = CloneAttributes(source.Extensions)
        };
    }

    internal static StoredCookie CloneStoredCookie(StoredCookie source)
    {
        if (source == null)
        {
            return new StoredCookie();
        }

        return new StoredCookie
        {
            Name = source.Name,
            Value = source.Value,
            Domain = source.Domain,
            Path = source.Path,
            HasExpires = source.HasExpires,
            Expires = source.Expires,
            CreationTime = source.CreationTime,
            LastAccessTime = source.LastAccessTime,
            Persistent = source.Persistent,
            HostOnly = source.HostOnly,
            Secure = source.Secure,
            HttpOnly = source.HttpOnly,
            HasSameSite = source.HasSameSite,
            SameSiteMode = source.SameSiteMode,
            HasPartitionKey = source.HasPartitionKey,
            PartitionKey = source.PartitionKey,
            IsValueQuoted = source.IsValueQuoted,
            Extensions = CloneAttributes(source.Extensions),
            SequenceNumber = source.SequenceNumber
        };
    }

    internal static StoredCookie[] CloneStoredCookies(StoredCookie[] source)
    {
        if (source == null)
        {
            return [];
        }

        StoredCookie[] copy = new StoredCookie[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CloneStoredCookie(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static CookieRequestContext CloneRequestContext(CookieRequestContext source)
    {
        if (source == null)
        {
            return new CookieRequestContext();
        }

        return new CookieRequestContext
        {
            RequestUri = source.RequestUri,
            RequestMethod = source.RequestMethod,
            SameSiteContext = source.SameSiteContext,
            AccessKind = source.AccessKind,
            IsTopLevelNavigation = source.IsTopLevelNavigation,
            IsReload = source.IsReload,
            RedirectChainIsCrossSite = source.RedirectChainIsCrossSite,
            HasUserActivation = source.HasUserActivation,
            HasTopLevelSite = source.HasTopLevelSite,
            TopLevelSite = source.TopLevelSite
        };
    }

    internal static CookieSetContext CloneSetContext(CookieSetContext source)
    {
        if (source == null)
        {
            return new CookieSetContext();
        }

        CookieSetContext copy = new CookieSetContext
        {
            Origin = source.Origin,
            Options = CloneJarOptions(source.Options),
            SameSiteContext = source.SameSiteContext,
            AccessKind = source.AccessKind,
            ExistingCookies = CloneStoredCookies(source.ExistingCookies),
            IsTopLevelNavigation = source.IsTopLevelNavigation,
            HasTopLevelSite = source.HasTopLevelSite,
            TopLevelSite = source.TopLevelSite,
            HasImportMode = source.HasImportMode,
            ImportMode = source.ImportMode
        };
        if (source.ExistingCookie != null)
        {
            copy.ExistingCookie = CloneStoredCookie(source.ExistingCookie);
        }

        return copy;
    }

    internal static CookieStoreQuery CloneStoreQuery(CookieStoreQuery source)
    {
        if (source == null)
        {
            return new CookieStoreQuery();
        }

        return new CookieStoreQuery
        {
            HasIdentity = source.HasIdentity,
            Identity = CloneIdentity(source.Identity),
            HasDomain = source.HasDomain,
            Domain = source.Domain,
            IncludeSubdomains = source.IncludeSubdomains,
            HasPath = source.HasPath,
            Path = source.Path,
            HasName = source.HasName,
            Name = source.Name,
            FilterByPartition = source.FilterByPartition,
            HasPartitionKey = source.HasPartitionKey,
            PartitionKey = source.PartitionKey,
            SessionOnly = source.SessionOnly,
            ExpiredOnly = source.ExpiredOnly,
            HasEvaluationTime = source.HasEvaluationTime,
            EvaluationTime = source.EvaluationTime
        };
    }

    internal static CookieRemovalQuery CloneRemovalQuery(CookieRemovalQuery source)
    {
        if (source == null)
        {
            return new CookieRemovalQuery();
        }

        return new CookieRemovalQuery
        {
            HasIdentity = source.HasIdentity,
            Identity = CloneIdentity(source.Identity),
            HasDomain = source.HasDomain,
            Domain = source.Domain,
            IncludeSubdomains = source.IncludeSubdomains,
            HasPath = source.HasPath,
            Path = source.Path,
            HasName = source.HasName,
            Name = source.Name,
            FilterByPartition = source.FilterByPartition,
            HasPartitionKey = source.HasPartitionKey,
            PartitionKey = source.PartitionKey,
            SessionOnly = source.SessionOnly,
            ExpiredOnly = source.ExpiredOnly,
            HasEvaluationTime = source.HasEvaluationTime,
            EvaluationTime = source.EvaluationTime
        };
    }

    internal static SetCookieParseResult CloneSetCookieParseResult(
        SetCookieParseResult source)
    {
        if (source == null)
        {
            return new SetCookieParseResult();
        }

        SetCookieParseResult copy = new SetCookieParseResult
        {
            Success = source.Success,
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
        if (source.Cookie != null)
        {
            copy.Cookie = CloneSetCookie(source.Cookie);
        }

        return copy;
    }

    internal static CookieHeaderParseResult CloneHeaderParseResult(
        CookieHeaderParseResult source)
    {
        if (source == null)
        {
            return new CookieHeaderParseResult();
        }

        return new CookieHeaderParseResult
        {
            Success = source.Success,
            Pairs = ClonePairs(source.Pairs),
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
    }

    internal static CookieMatchResult CloneMatchResult(CookieMatchResult source)
    {
        if (source == null)
        {
            return new CookieMatchResult();
        }

        CookieMatchResult copy = new CookieMatchResult
        {
            Status = source.Status,
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
        if (source.Cookie != null)
        {
            copy.Cookie = CloneStoredCookie(source.Cookie);
        }

        if (source.Pair != null)
        {
            copy.Pair = ClonePair(source.Pair);
        }

        return copy;
    }

    internal static CookieMatchResult[] CloneMatchResults(CookieMatchResult[] source)
    {
        if (source == null)
        {
            return [];
        }

        CookieMatchResult[] copy = new CookieMatchResult[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CloneMatchResult(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static CookieSetDecision CloneSetDecision(CookieSetDecision source)
    {
        if (source == null)
        {
            return new CookieSetDecision();
        }

        CookieSetDecision copy = new CookieSetDecision
        {
            Status = source.Status,
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
        if (source.NormalizedCookie != null)
        {
            copy.NormalizedCookie = CloneStoredCookie(source.NormalizedCookie);
        }

        return copy;
    }

    internal static CookieSetResult CloneSetResult(CookieSetResult source)
    {
        if (source == null)
        {
            return new CookieSetResult();
        }

        CookieSetResult copy = new CookieSetResult
        {
            Status = source.Status,
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
        if (source.Cookie != null)
        {
            copy.Cookie = CloneStoredCookie(source.Cookie);
        }

        if (source.ParseResult != null)
        {
            copy.ParseResult = CloneSetCookieParseResult(source.ParseResult);
        }

        return copy;
    }

    internal static CookieSetResult[] CloneSetResults(CookieSetResult[] source)
    {
        if (source == null)
        {
            return [];
        }

        CookieSetResult[] copy = new CookieSetResult[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = CloneSetResult(source[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static CookieBatchSetResult CloneBatchSetResult(CookieBatchSetResult source)
    {
        if (source == null)
        {
            return new CookieBatchSetResult();
        }

        return new CookieBatchSetResult
        {
            Results = CloneSetResults(source.Results),
            AcceptedCount = source.AcceptedCount,
            ReplacedCount = source.ReplacedCount,
            DeletedCount = source.DeletedCount,
            IgnoredCount = source.IgnoredCount,
            RejectedCount = source.RejectedCount,
            Issues = CloneIssues(source.Issues)
        };
    }

    internal static CookieImportResult CloneImportResult(CookieImportResult source)
    {
        if (source == null)
        {
            return new CookieImportResult();
        }

        return new CookieImportResult
        {
            Success = source.Success,
            Mode = source.Mode,
            ErrorCode = source.ErrorCode,
            Results = CloneSetResults(source.Results),
            AcceptedCount = source.AcceptedCount,
            ReplacedCount = source.ReplacedCount,
            DeletedCount = source.DeletedCount,
            IgnoredCount = source.IgnoredCount,
            RejectedCount = source.RejectedCount,
            RemovedCount = source.RemovedCount,
            Issues = CloneIssues(source.Issues)
        };
    }

    internal static CookieJarSnapshot CloneJarSnapshot(CookieJarSnapshot source)
    {
        if (source == null)
        {
            return new CookieJarSnapshot();
        }

        return new CookieJarSnapshot
        {
            Version = source.Version,
            Cookies = CloneStoredCookies(source.Cookies),
            NextSequenceNumber = source.NextSequenceNumber
        };
    }

    internal static CookieSnapshotParseResult CloneSnapshotParseResult(
        CookieSnapshotParseResult source)
    {
        if (source == null)
        {
            return new CookieSnapshotParseResult();
        }

        CookieSnapshotParseResult copy = new CookieSnapshotParseResult
        {
            Success = source.Success,
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
        if (source.Snapshot != null)
        {
            copy.Snapshot = CloneJarSnapshot(source.Snapshot);
        }

        return copy;
    }

    internal static NetscapeCookieFileParseResult CloneNetscapeParseResult(
        NetscapeCookieFileParseResult source)
    {
        if (source == null)
        {
            return new NetscapeCookieFileParseResult();
        }

        return new NetscapeCookieFileParseResult
        {
            Success = source.Success,
            Cookies = CloneStoredCookies(source.Cookies),
            ErrorCode = source.ErrorCode,
            Issues = CloneIssues(source.Issues)
        };
    }

    internal static CookieSelectionResult CloneSelectionResult(
        CookieSelectionResult source)
    {
        if (source == null)
        {
            return new CookieSelectionResult();
        }

        return new CookieSelectionResult
        {
            Success = source.Success,
            ErrorCode = source.ErrorCode,
            Cookies = CloneStoredCookies(source.Cookies),
            Pairs = ClonePairs(source.Pairs),
            FieldValues = CloneStrings(source.FieldValues),
            Matches = CloneMatchResults(source.Matches),
            Issues = CloneIssues(source.Issues)
        };
    }
}
