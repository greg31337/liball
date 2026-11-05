using uspto;

namespace uspto.Tests;

public class PtabDossierBuilderTests
{
    [Test]
    public void SelectedSectionsDeduplicateDocumentsAndCompleteBoundedDownloads()
    {
        FakePtabDossierSource source = CreateSource();
        PtabDossierBuilder builder = new PtabDossierBuilder(source);
        PtabDossierSelection selection = new PtabDossierSelection
        {
            Downloads = new[]
            {
                SelectDownload("DOC-1"),
                SelectDownload("DOC-3")
            },
            MaximumDownloads = 2
        };
        RequestContext context = new RequestContext { CorrelationIdentifier = "dossier-correlation" };

        PtabDossier result = builder.Build("IPR2025-00001", selection, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.TrialNumber, Is.EqualTo("IPR2025-00001"));
            Assert.That(result.Selection, Is.Not.SameAs(selection));
            Assert.That(result.Selection.Downloads, Is.Not.SameAs(selection.Downloads));
            Assert.That(result.Proceeding?.TrialNumber, Is.EqualTo("IPR2025-00001"));
            Assert.That(result.Decisions.Select(item => item.Document.DocumentIdentifier), Is.EqualTo(new[] { "DOC-1", "DOC-2" }));
            Assert.That(result.TrialDocuments.Select(item => item.Document.DocumentIdentifier), Is.EqualTo(new[] { "doc-1", "DOC-3" }));
            Assert.That(result.Documents.Select(item => item.Document.DocumentIdentifier), Is.EqualTo(new[] { "doc-1", "DOC-2", "DOC-3" }));
            Assert.That(result.Documents[0].InDecisionCollection, Is.True);
            Assert.That(result.Documents[0].InDocumentCollection, Is.True);
            Assert.That(result.Documents[0].DecisionRecord, Is.SameAs(result.Decisions[0]));
            Assert.That(result.Documents[0].TrialDocumentRecord, Is.SameAs(result.TrialDocuments[0]));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(5));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(5));
            Assert.That(result.FailedSectionCount, Is.Zero);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("dossier-correlation"));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Proceeding").Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Decisions").IsAuthoritative, Is.True);
            Assert.That(result.Provenance.FieldSources.Count(item => item.FieldPath == "Documents"), Is.EqualTo(2));
            Assert.That(result.Provenance.FieldSources.Count(item => item.FieldPath == "Downloads"), Is.EqualTo(2));
        });

        Assert.Multiple(() =>
        {
            Assert.That(source.ProceedingCalls, Is.EqualTo(1));
            Assert.That(source.DecisionCalls, Is.EqualTo(1));
            Assert.That(source.DocumentCalls, Is.EqualTo(1));
            Assert.That(source.DownloadCalls, Is.EqualTo(2));
            Assert.That(source.DownloadCollections, Is.EqualTo(new[] { PtabCollection.TrialDocuments, PtabCollection.TrialDocuments }));
            Assert.That(source.DownloadRequests[0].SourceUri?.AbsoluteUri, Is.EqualTo("https://ptab.uspto.gov/doc/doc-1"));
            Assert.That(source.DownloadRequests[0].ExpectedSize, Is.EqualTo(120L));
            Assert.That(source.DownloadRequests[0].HasExpectedSize, Is.True);
            Assert.That(source.DownloadRequests[0].Context, Is.SameAs(context));
            Assert.That(result.Downloads.All(item => item.IsComplete), Is.True);
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[] { "proceeding", "decisions", "documents", "download:DOC-1", "download:DOC-3" }));
        });
    }

    [Test]
    public void IndependentSectionAndDownloadFailuresRetainSuccessfulData()
    {
        FakePtabDossierSource source = CreateSource();
        source.Failures[PtabDossierBuilder.ProceedingSection] = new ApiException("Proceeding unavailable.", ApiService.Odp, 503, true, "TRANSIENT", null, "proceeding-request", true, null);
        source.Failures[PtabDossierBuilder.DocumentsSection] = new NotFoundException("Documents missing.", ApiService.Odp, "IPR2025-00001", 404, true, "documents-request", null);
        source.TerminalDownloadIdentifiers.Add("DOC-1");
        PtabDossierBuilder builder = new PtabDossierBuilder(source);
        PtabDossierSelection selection = new PtabDossierSelection
        {
            Downloads = new[]
            {
                SelectDownload("DOC-1"),
                SelectDownload("MISSING")
            },
            MaximumDownloads = 2
        };

        PtabDossier result = builder.Build("IPR2025-00001", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Proceeding, Is.Null);
            Assert.That(result.Decisions.Length, Is.EqualTo(2));
            Assert.That(result.TrialDocuments, Is.Empty);
            Assert.That(result.Documents.Select(item => item.Document.DocumentIdentifier), Is.EqualTo(new[] { "DOC-1", "DOC-2" }));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(5));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(1));
            Assert.That(result.FailedSectionCount, Is.EqualTo(4));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Downloads[0].Download?.FailureCode, Is.EqualTo("TRANSFER_FAILED"));
            Assert.That(result.Downloads[0].Error?.ErrorCode, Is.EqualTo("TRANSFER_FAILED"));
            Assert.That(result.Downloads[1].Download, Is.Null);
            Assert.That(result.Downloads[1].Error?.ErrorCode, Is.EqualTo("DOCUMENT_NOT_FOUND"));
            Assert.That(source.DownloadCollections, Is.EqualTo(new[] { PtabCollection.TrialDecisions }));
        });

        SectionError proceeding = result.Errors.Single(item => item.Section == "proceeding");
        SectionError documents = result.Errors.Single(item => item.Section == "documents");
        Assert.Multiple(() =>
        {
            Assert.That(proceeding.ExceptionType, Is.EqualTo(nameof(ApiException)));
            Assert.That(proceeding.StatusCode, Is.EqualTo(503));
            Assert.That(proceeding.RequestIdentifier, Is.EqualTo("proceeding-request"));
            Assert.That(proceeding.IsRetryable, Is.True);
            Assert.That(documents.ExceptionType, Is.EqualTo(nameof(NotFoundException)));
            Assert.That(documents.ErrorCode, Is.EqualTo("NOT_FOUND"));
            Assert.That(documents.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public void DownloadOnlySelectionUsesHiddenDependenciesWithoutExposingThem()
    {
        FakePtabDossierSource source = CreateSource();
        PtabDossierBuilder builder = new PtabDossierBuilder(source);
        PtabDossierSelection selection = new PtabDossierSelection
        {
            IncludeProceeding = false,
            IncludeDecisions = false,
            IncludeDocuments = false,
            Downloads = new[] { SelectDownload("DOC-3") },
            MaximumDownloads = 1
        };

        PtabDossier result = builder.Build("IPR2025-00001", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Proceeding, Is.Null);
            Assert.That(result.Decisions, Is.Empty);
            Assert.That(result.TrialDocuments, Is.Empty);
            Assert.That(result.Documents, Is.Empty);
            Assert.That(result.Downloads.Single().IsComplete, Is.True);
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[] { "download:DOC-3" }));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(1));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(source.ProceedingCalls, Is.Zero);
            Assert.That(source.DecisionCalls, Is.EqualTo(1));
            Assert.That(source.DocumentCalls, Is.EqualTo(1));
            Assert.That(source.DownloadCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingIdentifiersRemainDistinctDuringDeduplication()
    {
        PtabTrialDecisionResponse decisions = new PtabTrialDecisionResponse
        {
            Decisions = new[]
            {
                new PtabTrialDecision { Document = new PtabDocument { Name = "first" } },
                new PtabTrialDecision { Document = new PtabDocument { Name = "second" } }
            }
        };

        PtabDossierDocument[] result = PtabDossierBuilder.BuildDeduplicatedDocuments(decisions, null, true, false);

        Assert.That(result.Select(item => item.Document.Name), Is.EqualTo(new[] { "first", "second" }));
    }

    [Test]
    public void InvalidSelectionsStopBeforeAnySourceCall()
    {
        FakePtabDossierSource source = CreateSource();
        PtabDossierBuilder builder = new PtabDossierBuilder(source);
        PtabDossierSelection empty = new PtabDossierSelection
        {
            IncludeProceeding = false,
            IncludeDecisions = false,
            IncludeDocuments = false
        };
        PtabDossierSelection overLimit = new PtabDossierSelection
        {
            Downloads = new[] { SelectDownload("DOC-1") },
            MaximumDownloads = 0
        };
        PtabDossierSelection duplicates = new PtabDossierSelection
        {
            Downloads = new[] { SelectDownload("DOC-1"), SelectDownload("doc-1") },
            MaximumDownloads = 2
        };

        ValidationException noSection = Assert.Throws<ValidationException>(() => builder.Build("IPR2025-00001", empty, new RequestContext()));
        ValidationException limit = Assert.Throws<ValidationException>(() => builder.Build("IPR2025-00001", overLimit, new RequestContext()));
        ValidationException duplicate = Assert.Throws<ValidationException>(() => builder.Build("IPR2025-00001", duplicates, new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(noSection.ErrorCode, Is.EqualTo("SECTION_REQUIRED"));
            Assert.That(limit.ErrorCode, Is.EqualTo("DOWNLOAD_LIMIT_EXCEEDED"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_DOWNLOAD"));
            Assert.That(source.ProceedingCalls, Is.Zero);
            Assert.That(source.DecisionCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.Zero);
            Assert.That(source.DownloadCalls, Is.Zero);
        });
    }

    [Test]
    public void CallerCancellationStopsBeforeSourceCalls()
    {
        FakePtabDossierSource source = CreateSource();
        PtabDossierBuilder builder = new PtabDossierBuilder(source);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => builder.Build("IPR2025-00001", new PtabDossierSelection(), new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(source.ProceedingCalls, Is.Zero);
            Assert.That(source.DecisionCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.Zero);
        });
    }

    [Test]
    public void CachedSectionsRetainAuthorityAndReportCacheDelivery()
    {
        FakePtabDossierSource source = CreateSource();
        source.Proceeding.Provenance.FromCache = true;
        source.Decisions.Provenance.FromCache = true;
        source.Documents.Provenance.FromCache = true;
        PtabDossierBuilder builder = new PtabDossierBuilder(source);

        PtabDossier result = builder.Build("IPR2025-00001", new PtabDossierSelection(), new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Sections, Has.Length.EqualTo(3));
            Assert.That(result.Sections.All(item => item.Provenance.FromCache), Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Proceeding").Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Proceeding").Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Decisions").Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "TrialDocuments").Delivery, Is.EqualTo(DataProvenance.Cache));
            Assert.That(result.Provenance.FieldSources.Where(item => item.FieldPath == "Documents").All(item => item.Delivery == DataProvenance.Cache), Is.True);
            Assert.That(result.Provenance.FieldSources.All(item => item.IsAuthoritative), Is.True);
        });
    }

    [Test]
    public void CancellationAfterASectionStopsRemainingDossierWork()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        FakePtabDossierSource source = CreateSource();
        source.Cancellation = cancellation;
        source.CancelAfterSection = PtabDossierBuilder.ProceedingSection;
        PtabDossierBuilder builder = new PtabDossierBuilder(source);

        Assert.Throws<OperationCanceledException>(() => builder.Build(
            "IPR2025-00001",
            new PtabDossierSelection(),
            new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(source.ProceedingCalls, Is.EqualTo(1));
            Assert.That(source.DecisionCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.Zero);
            Assert.That(source.DownloadCalls, Is.Zero);
        });
    }

    private static FakePtabDossierSource CreateSource()
    {
        FakePtabDossierSource source = new FakePtabDossierSource();
        source.Proceeding = new PtabTrialProceeding
        {
            TrialNumber = "IPR2025-00001",
            Provenance = Provenance(PtabCollection.TrialProceedings, "proceeding-request")
        };
        source.Decisions = new PtabTrialDecisionResponse
        {
            Decisions = new[]
            {
                Decision("DOC-1", "https://ptab.uspto.gov/decision/DOC-1", 100L),
                Decision("DOC-2", "https://ptab.uspto.gov/decision/DOC-2", 200L)
            },
            Provenance = Provenance(PtabCollection.TrialDecisions, "decision-request")
        };
        source.Documents = new PtabTrialDocumentResponse
        {
            Documents = new[]
            {
                Document("doc-1", "https://ptab.uspto.gov/doc/doc-1", 120L),
                Document("DOC-3", "https://ptab.uspto.gov/doc/DOC-3", 300L)
            },
            Provenance = Provenance(PtabCollection.TrialDocuments, "document-request")
        };
        return source;
    }

    private static PtabDossierDownloadRequest SelectDownload(string identifier)
    {
        return new PtabDossierDownloadRequest
        {
            DocumentIdentifier = identifier,
            Download = new DownloadRequest { Output = new MemoryStream() }
        };
    }

    private static PtabTrialDecision Decision(string identifier, string uri, long size)
    {
        return new PtabTrialDecision
        {
            TrialNumber = "IPR2025-00001",
            Document = PtabDocument(identifier, uri, size),
            Provenance = Provenance(PtabCollection.TrialDecisions, "decision-request")
        };
    }

    private static PtabTrialDocument Document(string identifier, string uri, long size)
    {
        return new PtabTrialDocument
        {
            TrialNumber = "IPR2025-00001",
            Document = PtabDocument(identifier, uri, size),
            Provenance = Provenance(PtabCollection.TrialDocuments, "document-request")
        };
    }

    private static PtabDocument PtabDocument(string identifier, string uri, long size)
    {
        return new PtabDocument
        {
            DocumentIdentifier = identifier,
            DownloadUri = new Uri(uri),
            Size = size,
            HasSize = true,
            Provenance = Provenance(PtabCollection.TrialDocuments, "document-request")
        };
    }

    private static PtabDataProvenance Provenance(PtabCollection collection, string requestIdentifier)
    {
        return new PtabDataProvenance
        {
            Source = DataProvenance.Odp,
            Collection = collection,
            HasCollection = true,
            RequestIdentifier = requestIdentifier,
            IsAuthoritative = true
        };
    }
}

internal class FakePtabDossierSource : IPtabDossierSource
{
    internal PtabTrialProceeding Proceeding = new PtabTrialProceeding();
    internal PtabTrialDecisionResponse Decisions = new PtabTrialDecisionResponse();
    internal PtabTrialDocumentResponse Documents = new PtabTrialDocumentResponse();
    internal Dictionary<int, Exception> Failures = new Dictionary<int, Exception>();
    internal HashSet<string> TerminalDownloadIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal List<PtabCollection> DownloadCollections = new List<PtabCollection>();
    internal List<DownloadRequest> DownloadRequests = new List<DownloadRequest>();
    internal CancellationTokenSource? Cancellation;
    internal int CancelAfterSection;
    internal int ProceedingCalls;
    internal int DecisionCalls;
    internal int DocumentCalls;
    internal int DownloadCalls;

    public PtabTrialProceeding GetDossierProceeding(string trialNumber, RequestContext context)
    {
        ProceedingCalls += 1;
        ThrowSectionFailure(PtabDossierBuilder.ProceedingSection);
        return Proceeding;
    }

    public PtabTrialDecisionResponse GetDossierDecisions(string trialNumber, RequestContext context)
    {
        DecisionCalls += 1;
        ThrowSectionFailure(PtabDossierBuilder.DecisionsSection);
        return Decisions;
    }

    public PtabTrialDocumentResponse GetDossierDocuments(string trialNumber, RequestContext context)
    {
        DocumentCalls += 1;
        ThrowSectionFailure(PtabDossierBuilder.DocumentsSection);
        return Documents;
    }

    public DownloadResult DownloadDossierDocument(PtabCollection collection, string documentIdentifier, DownloadRequest request)
    {
        DownloadCalls += 1;
        DownloadCollections.Add(collection);
        DownloadRequests.Add(request);
        if (TerminalDownloadIdentifiers.Contains(documentIdentifier))
        {
            return new DownloadResult
            {
                Request = request,
                State = DownloadState.Failed,
                FailureCode = "TRANSFER_FAILED",
                FailureMessage = "The transfer failed."
            };
        }

        return new DownloadResult
        {
            Request = request,
            State = DownloadState.Completed,
            Progress = new DownloadProgress { State = DownloadState.Completed }
        };
    }

    internal void ThrowSectionFailure(int section)
    {
        if (section == CancelAfterSection && Cancellation != null)
        {
            Cancellation.Cancel();
        }

        if (Failures.TryGetValue(section, out Exception? failure))
        {
            throw failure;
        }
    }
}
