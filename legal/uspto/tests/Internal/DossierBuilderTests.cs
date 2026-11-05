using uspto;

namespace uspto.Tests;

public class DossierBuilderTests
{
    [Test]
    public void SelectedSectionsHydrateOnceAndKeepOfficialAndResearchDataSeparate()
    {
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        DossierBuilder builder = new DossierBuilder(source);
        PatentDossierSelection selection = SelectAllSections();
        RequestContext context = new RequestContext { CorrelationIdentifier = "dossier-correlation" };

        PatentDossier result = builder.Build("US 12,345,678 B2", selection, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.RequestedIdentifier, Is.EqualTo("US 12,345,678 B2"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Selection, Is.Not.SameAs(selection));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(11));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(11));
            Assert.That(result.FailedSectionCount, Is.Zero);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("dossier-correlation"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Official.Application?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.Transactions?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.Documents?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.AssociatedDocuments?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.Continuity?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.ForeignPriorities?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.Assignments?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.Attorneys?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.TermAdjustment?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Official.FullText?.BibliographicData.InventionTitle, Is.EqualTo("Full text"));
            Assert.That(result.Research.PatentRecord?.PatentIdentifier, Is.EqualTo("10905426"));
            Assert.That(result.Research.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
        });

        Assert.Multiple(() =>
        {
            Assert.That(source.ResolveCalls, Is.EqualTo(1));
            Assert.That(source.ApplicationCalls, Is.EqualTo(1));
            Assert.That(source.AssociatedDocumentCalls, Is.EqualTo(1));
            Assert.That(source.FullTextCalls, Is.EqualTo(1));
            Assert.That(source.ResearchCalls, Is.EqualTo(1));
            Assert.That(source.LastFullTextDocuments, Is.SameAs(result.Official.AssociatedDocuments));
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[]
            {
                "application",
                "transactions",
                "documents",
                "associatedDocuments",
                "continuity",
                "foreignPriorities",
                "assignments",
                "attorneys",
                "termAdjustment",
                "fullText",
                "research"
            }));
            Assert.That(result.Sections.Take(10).All(item => !item.IsResearch), Is.True);
            Assert.That(result.Sections[10].IsResearch, Is.True);
            Assert.That(result.Sections[10].Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Official.Application").Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Official.Application").IsAuthoritative, Is.True);
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Research").Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Research").IsAuthoritative, Is.False);
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Research.PatentRecord").Source, Is.EqualTo(DataProvenance.PatentsView));
        });
    }

    [Test]
    public void IndependentFailuresAreCapturedAndSuccessfulSectionsRemainAvailable()
    {
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        source.Failures[DossierBuilder.TransactionsSection] = new ApiException("Transactions unavailable.", ApiService.Odp, 503, true, "TRANSIENT", null, "transaction-request", true, null);
        source.Failures[DossierBuilder.AssociatedDocumentsSection] = new NotFoundException("Associated XML was not found.", ApiService.Odp, "18123456", 404, true, "associated-request", null);
        DossierBuilder builder = new DossierBuilder(source);
        PatentDossierSelection selection = new PatentDossierSelection
        {
            IncludeApplication = true,
            IncludeTransactions = true,
            IncludeDocuments = true,
            IncludeFullText = true,
            IncludeResearch = true
        };

        PatentDossier result = builder.Build("18/123,456", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.RequestedSectionCount, Is.EqualTo(5));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(3));
            Assert.That(result.FailedSectionCount, Is.EqualTo(2));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Official.Application, Is.Not.Null);
            Assert.That(result.Official.Documents, Is.Not.Null);
            Assert.That(result.Official.Transactions, Is.Null);
            Assert.That(result.Official.FullText, Is.Null);
            Assert.That(result.Research.PatentRecord, Is.Not.Null);
            Assert.That(source.FullTextCalls, Is.Zero);
            Assert.That(source.ResearchCalls, Is.EqualTo(1));
        });

        SectionError transaction = result.Errors.Single(error => error.Section == "transactions");
        SectionError fullText = result.Errors.Single(error => error.Section == "fullText");
        Assert.Multiple(() =>
        {
            Assert.That(transaction.ExceptionType, Is.EqualTo(nameof(ApiException)));
            Assert.That(transaction.ErrorCode, Is.EqualTo("TRANSIENT"));
            Assert.That(transaction.StatusCode, Is.EqualTo(503));
            Assert.That(transaction.HasStatusCode, Is.True);
            Assert.That(transaction.RequestIdentifier, Is.EqualTo("transaction-request"));
            Assert.That(transaction.IsRetryable, Is.True);
            Assert.That(fullText.ExceptionType, Is.EqualTo(nameof(NotFoundException)));
            Assert.That(fullText.ErrorCode, Is.EqualTo("NOT_FOUND"));
            Assert.That(fullText.StatusCode, Is.EqualTo(404));
            Assert.That(fullText.RequestIdentifier, Is.EqualTo("associated-request"));
            Assert.That(result.Sections.Single(item => item.Section == "fullText").Error, Is.SameAs(fullText));
        });
    }

    [Test]
    public void UnselectedDependenciesAreReusedWithoutBeingExposedAsSections()
    {
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        DossierBuilder builder = new DossierBuilder(source);
        PatentDossierSelection selection = new PatentDossierSelection
        {
            IncludeApplication = false,
            IncludeFullText = true,
            IncludeResearch = true
        };

        PatentDossier result = builder.Build("18/123,456", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.RequestedSectionCount, Is.EqualTo(2));
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[] { "fullText", "research" }));
            Assert.That(result.Official.Application, Is.Null);
            Assert.That(result.Official.AssociatedDocuments, Is.Null);
            Assert.That(result.Official.FullText, Is.Not.Null);
            Assert.That(result.Research.PatentRecord, Is.Not.Null);
            Assert.That(source.ApplicationCalls, Is.EqualTo(1));
            Assert.That(source.AssociatedDocumentCalls, Is.EqualTo(1));
            Assert.That(source.FullTextCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void CachedSectionDeliveryIsRetainedInCompositeProvenance()
    {
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        source.ReturnCachedProvenance = true;
        DossierBuilder builder = new DossierBuilder(source);
        PatentDossierSelection selection = new PatentDossierSelection
        {
            IncludeApplication = true,
            IncludeTransactions = true
        };

        PatentDossier result = builder.Build("18/123,456", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Sections, Has.Length.EqualTo(2));
            Assert.That(result.Sections.All(item => item.Provenance.FromCache), Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Official.Application").Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Official.Transactions").Delivery, Is.EqualTo(DataProvenance.Cache));
        });
    }

    [Test]
    public void CancellationAfterASectionStopsRemainingDossierHydration()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        source.Cancellation = cancellation;
        source.CancelAfterSection = DossierBuilder.ApplicationSection;
        DossierBuilder builder = new DossierBuilder(source);
        PatentDossierSelection selection = new PatentDossierSelection
        {
            IncludeApplication = true,
            IncludeTransactions = true,
            IncludeDocuments = true
        };

        Assert.Throws<OperationCanceledException>(() => builder.Build(
            "18/123,456",
            selection,
            new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(source.ResolveCalls, Is.EqualTo(1));
            Assert.That(source.ApplicationCalls, Is.EqualTo(1));
            Assert.That(source.TransactionCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.Zero);
        });
    }

    [Test]
    public void EmptySelectionAndCallerCancellationStopBeforeResolution()
    {
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        DossierBuilder builder = new DossierBuilder(source);
        PatentDossierSelection empty = new PatentDossierSelection { IncludeApplication = false };
        ValidationException noSections = Assert.Throws<ValidationException>(() => builder.Build("18/123,456", empty, new RequestContext()));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => builder.Build("18/123,456", new PatentDossierSelection(), new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(noSections.ErrorCode, Is.EqualTo("SECTION_REQUIRED"));
            Assert.That(source.ResolveCalls, Is.Zero);
        });
    }

    private static PatentDossierSelection SelectAllSections()
    {
        return new PatentDossierSelection
        {
            IncludeApplication = true,
            IncludeTransactions = true,
            IncludeDocuments = true,
            IncludeAssociatedDocuments = true,
            IncludeContinuity = true,
            IncludeForeignPriorities = true,
            IncludeAssignments = true,
            IncludeAttorneys = true,
            IncludeTermAdjustment = true,
            IncludeFullText = true,
            IncludeResearch = true
        };
    }
}

internal class FakePatentDossierSectionSource : IPatentDossierSectionSource
{
    internal Dictionary<int, Exception> Failures = new Dictionary<int, Exception>();
    internal int ResolveCalls;
    internal int ApplicationCalls;
    internal int TransactionCalls;
    internal int DocumentCalls;
    internal int AssociatedDocumentCalls;
    internal int ContinuityCalls;
    internal int ForeignPriorityCalls;
    internal int AssignmentCalls;
    internal int AttorneyCalls;
    internal int TermAdjustmentCalls;
    internal int FullTextCalls;
    internal int ResearchCalls;
    internal PatentAssociatedDocumentList? LastFullTextDocuments;
    internal bool ReturnCachedProvenance;
    internal CancellationTokenSource? Cancellation;
    internal int CancelAfterSection;

    public string ResolveDossierIdentifier(string identifier, RequestContext context)
    {
        ResolveCalls += 1;
        return "18123456";
    }

    public PatentFileWrapper GetDossierApplication(string applicationNumber, RequestContext context)
    {
        ApplicationCalls += 1;
        ThrowFailure(DossierBuilder.ApplicationSection);
        return new PatentFileWrapper
        {
            ApplicationNumber = applicationNumber,
            Metadata = new PatentApplicationMetadata
            {
                ApplicationNumber = applicationNumber,
                PatentNumber = "12345678",
                PublicationNumbers = new[] { "20240012345" }
            },
            Provenance = OfficialProvenance("application-request")
        };
    }

    public PatentTransactionHistory GetDossierTransactions(string applicationNumber, RequestContext context)
    {
        TransactionCalls += 1;
        ThrowFailure(DossierBuilder.TransactionsSection);
        return new PatentTransactionHistory { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("transaction-request") };
    }

    public PatentDocumentList GetDossierDocuments(string applicationNumber, RequestContext context)
    {
        DocumentCalls += 1;
        ThrowFailure(DossierBuilder.DocumentsSection);
        return new PatentDocumentList { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("document-request") };
    }

    public PatentAssociatedDocumentList GetDossierAssociatedDocuments(string applicationNumber, RequestContext context)
    {
        AssociatedDocumentCalls += 1;
        ThrowFailure(DossierBuilder.AssociatedDocumentsSection);
        return new PatentAssociatedDocumentList { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("associated-request") };
    }

    public PatentContinuity GetDossierContinuity(string applicationNumber, RequestContext context)
    {
        ContinuityCalls += 1;
        ThrowFailure(DossierBuilder.ContinuitySection);
        return new PatentContinuity { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("continuity-request") };
    }

    public PatentForeignPriorityList GetDossierForeignPriorities(string applicationNumber, RequestContext context)
    {
        ForeignPriorityCalls += 1;
        ThrowFailure(DossierBuilder.ForeignPrioritiesSection);
        return new PatentForeignPriorityList { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("priority-request") };
    }

    public PatentAssignmentHistory GetDossierAssignments(string applicationNumber, RequestContext context)
    {
        AssignmentCalls += 1;
        ThrowFailure(DossierBuilder.AssignmentsSection);
        return new PatentAssignmentHistory { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("assignment-request") };
    }

    public PatentAttorneyRecord GetDossierAttorneys(string applicationNumber, RequestContext context)
    {
        AttorneyCalls += 1;
        ThrowFailure(DossierBuilder.AttorneysSection);
        return new PatentAttorneyRecord { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("attorney-request") };
    }

    public PatentTermAdjustment GetDossierTermAdjustment(string applicationNumber, RequestContext context)
    {
        TermAdjustmentCalls += 1;
        ThrowFailure(DossierBuilder.TermAdjustmentSection);
        return new PatentTermAdjustment { ApplicationNumber = applicationNumber, Provenance = OfficialProvenance("term-request") };
    }

    public PatentFullText GetDossierFullText(PatentAssociatedDocumentList documents, RequestContext context)
    {
        FullTextCalls += 1;
        LastFullTextDocuments = documents;
        ThrowFailure(DossierBuilder.FullTextSection);
        return new PatentFullText
        {
            BibliographicData = new PatentBibliographicData { InventionTitle = "Full text" },
            Provenance = new PatentDataProvenance { Source = DataProvenance.Bulk, DatasetIdentifier = "PTGRXML" }
        };
    }

    public PatentDossierResearchData GetDossierResearch(PatentFileWrapper application, RequestContext context)
    {
        ResearchCalls += 1;
        ThrowFailure(DossierBuilder.ResearchSection);
        return new PatentDossierResearchData
        {
            PatentRecord = new PatentsViewPatent { PatentIdentifier = "10905426" },
            Provenance = new PatentDataProvenance { Source = DataProvenance.PatentsView, DatasetIdentifier = "PatentsView PatentSearch" }
        };
    }

    private void ThrowFailure(int section)
    {
        if (section == CancelAfterSection && Cancellation != null)
        {
            Cancellation.Cancel();
        }

        if (Failures.ContainsKey(section))
        {
            throw Failures[section];
        }
    }

    private PatentDataProvenance OfficialProvenance(string requestIdentifier)
    {
        return new PatentDataProvenance
        {
            Source = DataProvenance.Odp,
            RequestIdentifier = requestIdentifier,
            FromCache = ReturnCachedProvenance,
            IsAuthoritative = true
        };
    }
}
