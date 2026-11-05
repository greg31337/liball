using uspto;

namespace uspto.Tests;

public class PatentPortfolioLoaderTests
{
    [Test]
    public void CriteriaTranslateToDeterministicBoundedOdpSearch()
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.IsComplete = true;
        PatentPortfolioLoader loader = new PatentPortfolioLoader(source);
        RequestContext context = new RequestContext { CorrelationIdentifier = "portfolio-request" };
        PatentPortfolioRequest request = new PatentPortfolioRequest
        {
            AttorneyLastName = "O'Brien",
            AttorneyRegistrationNumber = "65545",
            CustomerNumber = "38106",
            ApplicantName = "Example Semiconductor",
            InventorName = "Pascal Chevalier",
            AssigneeName = "Example Holdings",
            Status = "Patented Case",
            StatusCode = 150,
            HasStatusCode = true,
            ArtUnit = "2814",
            FilingDateFrom = new DateTimeOffset(2020, 1, 2, 18, 30, 0, TimeSpan.FromHours(-8)),
            HasFilingDateFrom = true,
            FilingDateTo = new DateTimeOffset(2024, 12, 31, 7, 0, 0, TimeSpan.FromHours(2)),
            HasFilingDateTo = true,
            HydrateDossiers = false,
            PageSize = 17,
            MaximumPages = 4,
            MaximumResults = 53,
            Context = context
        };

        PatentPortfolio result = loader.Load(request);
        SearchRequest search = source.LastSearchRequest ?? new SearchRequest();

        Assert.Multiple(() =>
        {
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.SearchRequest, Is.SameAs(search));
            Assert.That(search.Context, Is.SameAs(context));
            Assert.That(search.OffsetPage.Limit, Is.EqualTo(17));
            Assert.That(search.MaximumPages, Is.EqualTo(4));
            Assert.That(search.MaximumResults, Is.EqualTo(53));
            Assert.That(search.Sorts.Select(item => item.Field), Is.EqualTo(new[] { "applicationMetaData.filingDate", "applicationNumberText" }));
            Assert.That(search.Sorts.Select(item => item.Direction), Is.EqualTo(new[] { SortDirection.Descending, SortDirection.Ascending }));
            Assert.That(search.FieldSelection.Fields, Does.Contain("applicationNumberText"));
            Assert.That(search.FieldSelection.Fields, Does.Contain("applicationMetaData.groupArtUnitNumber"));
        });

        AssertFilter(search, "applicationMetaData.attorneyBag.lastName", SearchOperator.Contains, "O'Brien");
        AssertFilter(search, "applicationMetaData.attorneyBag.registrationNumber", SearchOperator.Equal, "65545");
        AssertFilter(search, "applicationMetaData.customerNumber", SearchOperator.Equal, "38106");
        AssertFilter(search, "applicationMetaData.firstApplicantName", SearchOperator.Contains, "Example Semiconductor");
        AssertFilter(search, "applicationMetaData.inventorBag.inventorNameText", SearchOperator.Contains, "Pascal Chevalier");
        AssertFilter(search, "assignmentBag.assigneeBag.assigneeNameText", SearchOperator.Contains, "Example Holdings");
        AssertFilter(search, "applicationMetaData.applicationStatusDescriptionText", SearchOperator.Contains, "Patented Case");
        AssertFilter(search, "applicationMetaData.applicationStatusCode", SearchOperator.Equal, "150");
        AssertFilter(search, "applicationMetaData.groupArtUnitNumber", SearchOperator.Equal, "2814");
        Assert.Multiple(() =>
        {
            Assert.That(search.Filters, Has.Length.EqualTo(9));
            Assert.That(search.Ranges, Has.Length.EqualTo(1));
            Assert.That(search.Ranges[0].Field, Is.EqualTo("applicationMetaData.filingDate"));
            Assert.That(search.Ranges[0].LowerValue, Is.EqualTo("2020-01-02"));
            Assert.That(search.Ranges[0].UpperValue, Is.EqualTo("2024-12-31"));
            Assert.That(search.Ranges[0].IncludeLower, Is.True);
            Assert.That(search.Ranges[0].IncludeUpper, Is.True);
            Assert.That(source.HydratedIdentifiers, Is.Empty);
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void BoundedMatchesHydrateInOrderWithSharedSelectionAndContext()
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.Summaries = new PatentApplicationSummary[]
        {
            new PatentApplicationSummary { ApplicationNumber = "18111111" },
            new PatentApplicationSummary { ApplicationNumber = "18222222" },
            new PatentApplicationSummary { ApplicationNumber = "18333333" }
        };
        source.SearchResult.Count = 3;
        source.SearchResult.Total = 27L;
        source.SearchResult.HasTotal = true;
        source.SearchResult.IsComplete = false;
        source.SearchResult.RequestIdentifier = "odp-search";
        source.SearchResult.Provenance = new PatentDataProvenance { Source = DataProvenance.Odp, DatasetIdentifier = "patent-file-wrapper", FromCache = true, IsAuthoritative = true };
        PatentPortfolioLoader loader = new PatentPortfolioLoader(source);
        PatentDossierSelection selection = new PatentDossierSelection { IncludeApplication = true, IncludeTransactions = true };
        RequestContext context = new RequestContext { CorrelationIdentifier = "portfolio-context" };
        PatentPortfolioRequest request = new PatentPortfolioRequest
        {
            CustomerNumber = "38106",
            Selection = selection,
            MaximumResults = 2,
            Context = context
        };

        PatentPortfolio result = loader.Load(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Entries.Select(item => item.Summary.ApplicationNumber), Is.EqualTo(new[] { "18111111", "18222222" }));
            Assert.That(result.Entries.All(item => item.IsHydrated), Is.True);
            Assert.That(result.Entries.All(item => item.Dossier != null), Is.True);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.HydratedCount, Is.EqualTo(2));
            Assert.That(result.FailedHydrationCount, Is.Zero);
            Assert.That(result.Total, Is.EqualTo(27L));
            Assert.That(result.HasTotal, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("odp-search"));
            Assert.That(result.Provenance.HasRetrievedAt, Is.True);
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Entries.Summary").Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Entries.Summary").Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(result.Provenance.FieldSources.Count(item => item.FieldPath == "Entries.Dossier"), Is.EqualTo(2));
            Assert.That(source.HydratedIdentifiers, Is.EqualTo(new[] { "18111111", "18222222" }));
            Assert.That(source.Selections, Has.All.SameAs(selection));
            Assert.That(source.Contexts, Has.All.SameAs(context));
        });
    }

    [Test]
    public void HydrationFailureIsCapturedWithoutDiscardingOtherApplications()
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.Summaries = new PatentApplicationSummary[]
        {
            new PatentApplicationSummary { ApplicationNumber = "18111111" },
            new PatentApplicationSummary { ApplicationNumber = "18222222" },
            new PatentApplicationSummary { ApplicationNumber = "18333333" }
        };
        source.SearchResult.Count = 3;
        source.SearchResult.IsComplete = true;
        source.Failures["18222222"] = new ApiException("Dossier unavailable.", ApiService.Odp, 503, true, "TRANSIENT", null, "dossier-request", true, null);
        PatentPortfolioLoader loader = new PatentPortfolioLoader(source);
        PatentPortfolioRequest request = new PatentPortfolioRequest { ApplicantName = "Example", MaximumResults = 3 };

        PatentPortfolio result = loader.Load(request);

        Assert.Multiple(() =>
        {
            Assert.That(source.HydratedIdentifiers, Is.EqualTo(new[] { "18111111", "18222222", "18333333" }));
            Assert.That(result.Entries[0].IsHydrated, Is.True);
            Assert.That(result.Entries[1].IsHydrated, Is.False);
            Assert.That(result.Entries[1].Dossier, Is.Null);
            Assert.That(result.Entries[2].IsHydrated, Is.True);
            Assert.That(result.HydratedCount, Is.EqualTo(2));
            Assert.That(result.FailedHydrationCount, Is.EqualTo(1));
            Assert.That(result.Errors, Has.Length.EqualTo(1));
            Assert.That(result.Errors[0], Is.SameAs(result.Entries[1].Error));
            Assert.That(result.Errors[0].Section, Is.EqualTo("18222222"));
            Assert.That(result.Errors[0].ExceptionType, Is.EqualTo(nameof(ApiException)));
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("TRANSIENT"));
            Assert.That(result.Errors[0].StatusCode, Is.EqualTo(503));
            Assert.That(result.Errors[0].RequestIdentifier, Is.EqualTo("dossier-request"));
            Assert.That(result.Errors[0].IsRetryable, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
        });
    }

    [Test]
    public void PartialDossierFailuresAreAggregatedWithoutDiscardingTheHydratedDossier()
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.Summaries = new PatentApplicationSummary[]
        {
            new PatentApplicationSummary { ApplicationNumber = "18111111" },
            new PatentApplicationSummary { ApplicationNumber = "18222222" }
        };
        source.SearchResult.IsComplete = true;
        SectionError nested = new SectionError
        {
            Section = "transactions",
            ExceptionType = nameof(ApiException),
            Message = "Transactions unavailable.",
            ErrorCode = "TRANSIENT",
            Service = ApiService.Odp,
            StatusCode = 503,
            HasStatusCode = true,
            RequestIdentifier = "nested-request",
            IsRetryable = true
        };
        source.Dossiers["18111111"] = new PatentDossier
        {
            ApplicationNumber = "18111111",
            Official = new PatentDossierOfficialData { Application = new PatentFileWrapper { ApplicationNumber = "18111111" } },
            Errors = new SectionError[] { nested },
            CompletedSectionCount = 1,
            FailedSectionCount = 1,
            HasPartialSuccess = true,
            IsComplete = false,
            Provenance = new PatentDataProvenance { Source = DataProvenance.ClientDerived }
        };

        PatentPortfolio result = new PatentPortfolioLoader(source).Load(new PatentPortfolioRequest { ApplicantName = "Example", MaximumResults = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Entries.All(item => item.IsHydrated), Is.True);
            Assert.That(result.Entries[0].Dossier, Is.SameAs(source.Dossiers["18111111"]));
            Assert.That(result.Entries[0].Dossier?.Official.Application?.ApplicationNumber, Is.EqualTo("18111111"));
            Assert.That(result.HydratedCount, Is.EqualTo(2));
            Assert.That(result.FailedHydrationCount, Is.Zero);
            Assert.That(result.FailedSectionCount, Is.EqualTo(1));
            Assert.That(result.Errors, Has.Length.EqualTo(1));
            Assert.That(result.Errors[0].Section, Is.EqualTo("18111111.transactions"));
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("TRANSIENT"));
            Assert.That(result.Errors[0].StatusCode, Is.EqualTo(503));
            Assert.That(result.Errors[0].RequestIdentifier, Is.EqualTo("nested-request"));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
        });
    }

    [Test]
    public void CancellationDuringHydrationStopsBeforeTheNextPortfolioEntry()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.Summaries = new PatentApplicationSummary[]
        {
            new PatentApplicationSummary { ApplicationNumber = "18111111" },
            new PatentApplicationSummary { ApplicationNumber = "18222222" },
            new PatentApplicationSummary { ApplicationNumber = "18333333" }
        };
        source.SearchResult.IsComplete = true;
        source.Cancellation = cancellation;
        source.CancelAfterHydrationCount = 1;
        PatentPortfolioRequest request = new PatentPortfolioRequest
        {
            ApplicantName = "Example",
            MaximumResults = 3,
            Context = new RequestContext { CancellationToken = cancellation.Token }
        };

        Assert.Throws<OperationCanceledException>(() => new PatentPortfolioLoader(source).Load(request));

        Assert.Multiple(() =>
        {
            Assert.That(source.SearchCalls, Is.EqualTo(1));
            Assert.That(source.HydratedIdentifiers, Is.EqualTo(new[] { "18111111" }));
        });
    }

    [Test]
    public void PortfolioValidationRejectsUnsafeOrInconsistentRequestsBeforeSearch()
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        PatentPortfolioLoader loader = new PatentPortfolioLoader(source);
        ValidationException noCriteria = Assert.Throws<ValidationException>(() => loader.Load(new PatentPortfolioRequest()));
        ValidationException blank = Assert.Throws<ValidationException>(() => loader.Load(new PatentPortfolioRequest { InventorName = " " }));
        ValidationException reversedDates = Assert.Throws<ValidationException>(() => loader.Load(new PatentPortfolioRequest
        {
            ArtUnit = "2814",
            FilingDateFrom = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            HasFilingDateFrom = true,
            FilingDateTo = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            HasFilingDateTo = true
        }));
        ValidationException emptySelection = Assert.Throws<ValidationException>(() => loader.Load(new PatentPortfolioRequest
        {
            ArtUnit = "2814",
            Selection = new PatentDossierSelection { IncludeApplication = false }
        }));
        ValidationException invalidBound = Assert.Throws<ValidationException>(() => loader.Load(new PatentPortfolioRequest
        {
            CustomerNumber = "38106",
            MaximumResults = 0
        }));

        Assert.Multiple(() =>
        {
            Assert.That(noCriteria.ErrorCode, Is.EqualTo("PORTFOLIO_CRITERION_REQUIRED"));
            Assert.That(blank.ErrorCode, Is.EqualTo("INVALID_QUERY_LITERAL"));
            Assert.That(reversedDates.ErrorCode, Is.EqualTo("INVALID_RANGE"));
            Assert.That(emptySelection.ErrorCode, Is.EqualTo("SECTION_REQUIRED"));
            Assert.That(invalidBound.ErrorCode, Is.EqualTo("RESULT_LIMIT_OUT_OF_RANGE"));
            Assert.That(source.SearchCalls, Is.Zero);
        });
    }

    [Test]
    public void DisabledHydrationAllowsAnEmptySectionSelection()
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.Summaries = new PatentApplicationSummary[] { new PatentApplicationSummary { ApplicationNumber = "18111111" } };
        source.SearchResult.Count = 1;
        source.SearchResult.IsComplete = true;
        PatentPortfolioLoader loader = new PatentPortfolioLoader(source);
        PatentPortfolioRequest request = new PatentPortfolioRequest
        {
            StatusCode = 150,
            HasStatusCode = true,
            HydrateDossiers = false,
            Selection = new PatentDossierSelection { IncludeApplication = false }
        };

        PatentPortfolio result = loader.Load(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.HydratedCount, Is.Zero);
            Assert.That(result.FailedHydrationCount, Is.Zero);
            Assert.That(result.Entries[0].Dossier, Is.Null);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(source.HydratedIdentifiers, Is.Empty);
        });
    }

    private static void AssertFilter(SearchRequest search, string field, SearchOperator searchOperator, string value)
    {
        SearchFilter filter = search.Filters.Single(item => item.Field == field);
        Assert.Multiple(() =>
        {
            Assert.That(filter.Operator, Is.EqualTo(searchOperator));
            Assert.That(filter.Value, Is.EqualTo(value));
        });
    }
}

internal class FakePatentPortfolioSource : IPatentPortfolioSource
{
    internal PatentApplicationResponse SearchResult = new PatentApplicationResponse();
    internal SearchRequest? LastSearchRequest;
    internal int SearchCalls;
    internal List<string> HydratedIdentifiers = new List<string>();
    internal List<PatentDossierSelection> Selections = new List<PatentDossierSelection>();
    internal List<RequestContext> Contexts = new List<RequestContext>();
    internal Dictionary<string, Exception> Failures = new Dictionary<string, Exception>(StringComparer.Ordinal);
    internal Dictionary<string, PatentDossier> Dossiers = new Dictionary<string, PatentDossier>(StringComparer.Ordinal);
    internal CancellationTokenSource? Cancellation;
    internal int CancelAfterHydrationCount;

    public PatentApplicationResponse SearchPatentPortfolio(SearchRequest request)
    {
        SearchCalls += 1;
        LastSearchRequest = request;
        return SearchResult;
    }

    public PatentDossier HydratePatentPortfolioEntry(string applicationNumber, PatentDossierSelection selection, RequestContext context)
    {
        HydratedIdentifiers.Add(applicationNumber);
        Selections.Add(selection);
        Contexts.Add(context);
        if (Cancellation != null && HydratedIdentifiers.Count == CancelAfterHydrationCount)
        {
            Cancellation.Cancel();
        }

        if (Failures.ContainsKey(applicationNumber))
        {
            throw Failures[applicationNumber];
        }

        if (Dossiers.ContainsKey(applicationNumber))
        {
            return Dossiers[applicationNumber];
        }

        return new PatentDossier
        {
            RequestedIdentifier = applicationNumber,
            ApplicationNumber = applicationNumber,
            Selection = DossierBuilder.CopySelection(selection),
            RequestedSectionCount = DossierBuilder.CountSelectedSections(selection),
            CompletedSectionCount = DossierBuilder.CountSelectedSections(selection),
            IsComplete = true,
            Provenance = new PatentDataProvenance { Source = DataProvenance.ClientDerived, DatasetIdentifier = "patent-dossier", IsAuthoritative = false }
        };
    }
}
