namespace uspto;

internal interface IPatentPortfolioSource
{
    PatentApplicationResponse SearchPatentPortfolio(SearchRequest request);

    PatentDossier HydratePatentPortfolioEntry(string applicationNumber, PatentDossierSelection selection, RequestContext context);
}

internal class PatentPortfolioSource : IPatentPortfolioSource
{
    internal PatentApplicationSearchOperation SearchOperation;
    internal DossierBuilder Dossier;

    internal PatentPortfolioSource(global::System.Net.Http.HttpClient client, UsptoClientOptions options, IPatentsViewClient? researchClient)
    {
        SearchOperation = new PatentApplicationSearchOperation(client, options);
        Dossier = new DossierBuilder(new PatentDossierSectionSource(client, options, researchClient));
    }

    public PatentApplicationResponse SearchPatentPortfolio(SearchRequest request)
    {
        return SearchOperation.SearchAll(request);
    }

    public PatentDossier HydratePatentPortfolioEntry(string applicationNumber, PatentDossierSelection selection, RequestContext context)
    {
        return Dossier.Build(applicationNumber, selection, context);
    }
}

internal class PatentPortfolioHydrationResult
{
    internal PatentDossier? Dossier = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class PatentPortfolioLoader
{
    internal IPatentPortfolioSource Source;

    internal PatentPortfolioLoader(IPatentPortfolioSource source)
    {
        if (source == null)
        {
            throw new ValidationException("A patent portfolio source is required.", "source", "REQUIRED", null);
        }

        Source = source;
    }

    internal PatentPortfolio Load(PatentPortfolioRequest request)
    {
        Validate(request);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        SearchRequest searchRequest = CreateSearchRequest(request);
        PatentApplicationResponse search = Source.SearchPatentPortfolio(searchRequest);
        request.Context.CancellationToken.ThrowIfCancellationRequested();
        PatentPortfolio result = CreateResult(request, searchRequest, search);
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();
        bool allDossiersComplete = true;
        int index;
        for (index = 0; index < result.Entries.Length; index += 1)
        {
            request.Context.CancellationToken.ThrowIfCancellationRequested();
            PatentPortfolioEntry entry = new PatentPortfolioEntry();
            entry.Summary = search.Summaries[index];
            result.Entries[index] = entry;
            if (!request.HydrateDossiers)
            {
                continue;
            }

            string applicationNumber = entry.Summary.ApplicationNumber ?? string.Empty;
            PatentPortfolioHydrationResult hydration = LoadDossier(applicationNumber, request.Selection, request.Context);
            if (hydration.IsSuccessful && hydration.Dossier != null)
            {
                entry.Dossier = hydration.Dossier;
                entry.IsHydrated = true;
                ProvenanceLabeler.AddPatentField(result.Provenance, "Entries.Dossier", hydration.Dossier.Provenance);
                result.HydratedCount += 1;
                if (!hydration.Dossier.IsComplete)
                {
                    allDossiersComplete = false;
                    AddNestedDossierErrors(errors, applicationNumber, hydration.Dossier, result);
                }
            }
            else
            {
                allDossiersComplete = false;
                SectionError error = CreateHydrationError(applicationNumber, hydration.Failure);
                entry.Error = error;
                errors.Add(error);
                result.FailedHydrationCount += 1;
                result.FailedSectionCount += 1;
            }
        }

        result.Errors = errors.ToArray();
        result.IsComplete = search.IsComplete && search.Summaries.Length <= request.MaximumResults && (!request.HydrateDossiers || allDossiersComplete);
        result.HasPartialSuccess = result.Count > 0 && !result.IsComplete;
        return result;
    }

    internal static void AddNestedDossierErrors(global::System.Collections.Generic.List<SectionError> errors, string applicationNumber, PatentDossier dossier, PatentPortfolio result)
    {
        SectionError[] dossierErrors = dossier.Errors ?? new SectionError[0];
        int index;
        for (index = 0; index < dossierErrors.Length; index += 1)
        {
            SectionError source = dossierErrors[index] ?? new SectionError();
            SectionError error = new SectionError();
            string section = source.Section ?? "section";
            error.Section = applicationNumber + "." + section;
            error.ExceptionType = source.ExceptionType;
            error.Message = source.Message;
            error.ErrorCode = source.ErrorCode;
            error.Service = source.Service;
            error.StatusCode = source.StatusCode;
            error.HasStatusCode = source.HasStatusCode;
            error.RequestIdentifier = source.RequestIdentifier;
            error.IsRetryable = source.IsRetryable;
            errors.Add(error);
            result.FailedSectionCount += 1;
        }

        if (dossierErrors.Length == 0)
        {
            SectionError error = new SectionError();
            error.Section = applicationNumber;
            error.ExceptionType = "IncompleteOperation";
            error.Message = "The patent dossier did not complete every requested section.";
            error.ErrorCode = "DOSSIER_INCOMPLETE";
            errors.Add(error);
            result.FailedSectionCount += 1;
        }
    }

    internal static SearchRequest CreateSearchRequest(PatentPortfolioRequest request)
    {
        global::System.Collections.Generic.List<SearchFilter> filters = new global::System.Collections.Generic.List<SearchFilter>();
        AddTextFilter(filters, "applicationMetaData.attorneyBag.lastName", request.AttorneyLastName);
        AddExactFilter(filters, "applicationMetaData.attorneyBag.registrationNumber", request.AttorneyRegistrationNumber);
        AddExactFilter(filters, "applicationMetaData.customerNumber", request.CustomerNumber);
        AddTextFilter(filters, "applicationMetaData.firstApplicantName", request.ApplicantName);
        AddTextFilter(filters, "applicationMetaData.inventorBag.inventorNameText", request.InventorName);
        AddTextFilter(filters, "assignmentBag.assigneeBag.assigneeNameText", request.AssigneeName);
        AddTextFilter(filters, "applicationMetaData.applicationStatusDescriptionText", request.Status);
        if (request.HasStatusCode)
        {
            AddExactFilter(filters, "applicationMetaData.applicationStatusCode", request.StatusCode.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        AddExactFilter(filters, "applicationMetaData.groupArtUnitNumber", request.ArtUnit);
        global::System.Collections.Generic.List<SearchRange> ranges = new global::System.Collections.Generic.List<SearchRange>();
        if (request.HasFilingDateFrom || request.HasFilingDateTo)
        {
            SearchRange range = new SearchRange();
            range.Field = "applicationMetaData.filingDate";
            if (request.HasFilingDateFrom)
            {
                range.LowerValue = request.FilingDateFrom.ToString("yyyy-MM-dd", global::System.Globalization.CultureInfo.InvariantCulture);
            }

            if (request.HasFilingDateTo)
            {
                range.UpperValue = request.FilingDateTo.ToString("yyyy-MM-dd", global::System.Globalization.CultureInfo.InvariantCulture);
            }

            ranges.Add(range);
        }

        SearchRequest result = new SearchRequest();
        result.Filters = filters.ToArray();
        result.Ranges = ranges.ToArray();
        result.Sorts = new SearchSort[]
        {
            new SearchSort { Field = "applicationMetaData.filingDate", Direction = SortDirection.Descending },
            new SearchSort { Field = "applicationNumberText", Direction = SortDirection.Ascending }
        };
        result.FieldSelection.Fields = new string[]
        {
            "applicationNumberText",
            "applicationMetaData.patentNumber",
            "applicationMetaData.earliestPublicationNumber",
            "applicationMetaData.inventionTitle",
            "applicationMetaData.applicationTypeCode",
            "applicationMetaData.applicationTypeLabelName",
            "applicationMetaData.filingDate",
            "applicationMetaData.earliestPublicationDate",
            "applicationMetaData.grantDate",
            "applicationMetaData.applicationStatusCode",
            "applicationMetaData.applicationStatusDescriptionText",
            "applicationMetaData.firstApplicantName",
            "applicationMetaData.firstInventorName",
            "applicationMetaData.groupArtUnitNumber"
        };
        result.OffsetPage.Limit = request.PageSize;
        result.MaximumPages = request.MaximumPages;
        result.MaximumResults = request.MaximumResults;
        result.Context = request.Context;
        return result;
    }

    internal static void AddTextFilter(global::System.Collections.Generic.List<SearchFilter> filters, string field, string? value)
    {
        if (value == null)
        {
            return;
        }

        SearchFilter filter = new SearchFilter();
        filter.Field = field;
        filter.Operator = SearchOperator.Contains;
        filter.Value = value;
        filters.Add(filter);
    }

    internal static void AddExactFilter(global::System.Collections.Generic.List<SearchFilter> filters, string field, string? value)
    {
        if (value == null)
        {
            return;
        }

        SearchFilter filter = new SearchFilter();
        filter.Field = field;
        filter.Operator = SearchOperator.Equal;
        filter.Value = value;
        filters.Add(filter);
    }

    internal static void Validate(PatentPortfolioRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A patent portfolio request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A patent portfolio request context is required.", "Context", "REQUIRED", null);
        }

        if (request.Selection == null)
        {
            throw new ValidationException("A patent portfolio dossier selection is required.", "Selection", "REQUIRED", null);
        }

        ValidateText(request.AttorneyLastName, "AttorneyLastName");
        ValidateText(request.AttorneyRegistrationNumber, "AttorneyRegistrationNumber");
        ValidateText(request.CustomerNumber, "CustomerNumber");
        ValidateText(request.ApplicantName, "ApplicantName");
        ValidateText(request.InventorName, "InventorName");
        ValidateText(request.AssigneeName, "AssigneeName");
        ValidateText(request.Status, "Status");
        ValidateText(request.ArtUnit, "ArtUnit");
        if (!HasCriteria(request))
        {
            throw new ValidationException("At least one patent portfolio criterion is required.", "request", "PORTFOLIO_CRITERION_REQUIRED", null);
        }

        if (request.PageSize <= 0)
        {
            throw new ValidationException("The patent portfolio page size must be positive.", "PageSize", "INVALID_LIMIT", null);
        }

        if (request.MaximumPages <= 0)
        {
            throw new ValidationException("The patent portfolio page limit must be positive.", "MaximumPages", "PAGE_LIMIT_OUT_OF_RANGE", null);
        }

        if (request.MaximumResults <= 0)
        {
            throw new ValidationException("The patent portfolio result limit must be positive.", "MaximumResults", "RESULT_LIMIT_OUT_OF_RANGE", null);
        }

        if (request.HasFilingDateFrom && request.HasFilingDateTo && request.FilingDateFrom.Date > request.FilingDateTo.Date)
        {
            throw new ValidationException("The patent portfolio filing-date lower bound cannot exceed its upper bound.", "FilingDateFrom", "INVALID_RANGE", null);
        }

        if (request.HydrateDossiers && DossierBuilder.CountSelectedSections(request.Selection) == 0)
        {
            throw new ValidationException("At least one patent dossier section must be selected for portfolio hydration.", "Selection", "SECTION_REQUIRED", null);
        }
    }

    internal static bool HasCriteria(PatentPortfolioRequest request)
    {
        return request.AttorneyLastName != null || request.AttorneyRegistrationNumber != null || request.CustomerNumber != null || request.ApplicantName != null || request.InventorName != null || request.AssigneeName != null || request.Status != null || request.HasStatusCode || request.ArtUnit != null || request.HasFilingDateFrom || request.HasFilingDateTo;
    }

    internal static void ValidateText(string? value, string field)
    {
        if (value != null && global::System.String.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException("Patent portfolio text criteria cannot be blank.", field, "INVALID_QUERY_LITERAL", null);
        }
    }

    internal PatentPortfolioHydrationResult LoadDossier(string applicationNumber, PatentDossierSelection selection, RequestContext context)
    {
        global::System.Threading.Tasks.Task<PatentDossier> task = global::System.Threading.Tasks.Task.Run(() => Source.HydratePatentPortfolioEntry(applicationNumber, selection, context));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentPortfolioHydrationResult result = new PatentPortfolioHydrationResult();
        if (task.IsCompletedSuccessfully)
        {
            result.Dossier = task.Result;
            result.IsSuccessful = true;
        }
        else if (task.Exception != null)
        {
            result.Failure = task.Exception.GetBaseException();
        }
        else
        {
            result.Failure = new ValidationException("The patent portfolio dossier did not complete.", "applicationNumber", "DOSSIER_INCOMPLETE", null);
        }

        return result;
    }

    internal static SectionError CreateHydrationError(string applicationNumber, global::System.Exception? failure)
    {
        global::System.Exception actual = failure ?? new ValidationException("The patent portfolio dossier failed.", "applicationNumber", "DOSSIER_FAILED", null);
        SectionError result = DossierBuilder.CreateSectionError(DossierBuilder.ApplicationSection, actual);
        result.Section = applicationNumber;
        return result;
    }

    internal static PatentPortfolio CreateResult(PatentPortfolioRequest request, SearchRequest searchRequest, PatentApplicationResponse search)
    {
        PatentPortfolio result = new PatentPortfolio();
        result.Request = request;
        result.SearchRequest = searchRequest;
        int count = search.Summaries.Length;
        if (request.MaximumResults < count)
        {
            count = request.MaximumResults;
        }

        result.Entries = new PatentPortfolioEntry[count];
        result.Count = count;
        result.Total = search.Total;
        result.HasTotal = search.HasTotal;
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.RequestIdentifier = search.RequestIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        ProvenanceLabeler.AddPatentField(result.Provenance, "Entries.Summary", search.Provenance);
        return result;
    }
}
