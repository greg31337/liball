using uspto;

namespace uspto.Tests;

public class TrademarkDossierBuilderTests
{
    [Test]
    public void SelectedSectionsExposeStatusDerivedDataDocumentsAndContent()
    {
        FakeTrademarkDossierSource source = CreateSource();
        TrademarkDossierBuilder builder = new TrademarkDossierBuilder(source);
        TrademarkDossierSelection selection = new TrademarkDossierSelection
        {
            StatusFormat = DocumentFormat.Xml,
            SelectedContent = new[]
            {
                SelectContent("NOA-1", DocumentFormat.Pdf),
                SelectContent("REG-2", DocumentFormat.Zip)
            },
            MaximumSelectedContent = 2
        };
        RequestContext context = new RequestContext { CorrelationIdentifier = "dossier-correlation" };

        TrademarkDossier result = builder.Build("SN 90/123,456", selection, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.RequestedIdentifier, Is.EqualTo("SN 90/123,456"));
            Assert.That(result.CanonicalIdentifier, Is.EqualTo("sn90123456"));
            Assert.That(result.IdentifierKind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(result.Selection, Is.Not.SameAs(selection));
            Assert.That(result.Selection.SelectedContent, Is.Not.SameAs(selection.SelectedContent));
            Assert.That(result.Selection.StatusFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.CaseStatus, Is.SameAs(source.Status));
            Assert.That(result.Status?.ExternalDescription, Is.EqualTo("LIVE"));
            Assert.That(result.Mark?.VerbalElementText, Is.EqualTo("EXAMPLE MARK"));
            Assert.That(result.Events.Select(item => item.Code), Is.EqualTo(new[] { "NWOS", "PUBO" }));
            Assert.That(result.Parties?.CurrentOwners.Single().Name, Is.EqualTo("Current Owner"));
            Assert.That(result.Parties?.PriorOwners.Single().Name, Is.EqualTo("Prior Owner"));
            Assert.That(result.Parties?.Attorneys.Single().Name, Is.EqualTo("Counsel Name"));
            Assert.That(result.Parties?.Correspondence.Name, Is.EqualTo("Correspondent"));
            Assert.That(result.GoodsAndServices.Single().PrimeClassCode, Is.EqualTo("042"));
            Assert.That(result.DocumentCollection, Is.SameAs(source.Documents));
            Assert.That(result.Documents.Select(item => item.DocumentIdentifier), Is.EqualTo(new[] { "NOA-1", "REG-2" }));
            Assert.That(result.SelectedContent.All(item => item.IsComplete), Is.True);
            Assert.That(result.SelectedContent[0].Document, Is.SameAs(source.Documents.Documents[0]));
            Assert.That(result.SelectedContent[1].Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(4));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(4));
            Assert.That(result.FailedSectionCount, Is.Zero);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("dossier-correlation"));
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[] { "status", "documents", "content:NOA-1:pdf", "content:REG-2:zip" }));
            Assert.That(result.Sections[2].Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "CaseStatus").Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "GoodsAndServices").IsAuthoritative, Is.True);
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "DocumentCollection").Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.FieldSources.Count(item => item.FieldPath == "SelectedContent"), Is.EqualTo(2));
        });

        Assert.Multiple(() =>
        {
            Assert.That(source.StatusCalls, Is.EqualTo(1));
            Assert.That(source.StatusFormats, Is.EqualTo(new[] { DocumentFormat.Xml }));
            Assert.That(source.DocumentCalls, Is.EqualTo(1));
            Assert.That(source.ContentCalls, Is.EqualTo(2));
            Assert.That(source.ContentIdentifiers, Is.EqualTo(new[] { "NOA-1", "REG-2" }));
            Assert.That(source.ContentFormats, Is.EqualTo(new[] { DocumentFormat.Pdf, DocumentFormat.Zip }));
            Assert.That(source.ContentRequests.All(item => item.Context == context), Is.True);
            Assert.That(source.ContentRequests[0], Is.Not.SameAs(selection.SelectedContent[0].Download));
        });
    }

    [Test]
    public void IndependentFailuresRetainSuccessfulSectionsAndStructuredErrors()
    {
        FakeTrademarkDossierSource source = CreateSource();
        source.Failures[TrademarkDossierBuilder.StatusSection] = new ApiException("Status unavailable.", ApiService.Tsdr, 503, true, "TRANSIENT", null, "status-request", true, null);
        source.TerminalContentIdentifiers.Add("NOA-1");
        TrademarkDossierBuilder builder = new TrademarkDossierBuilder(source);
        TrademarkDossierSelection selection = new TrademarkDossierSelection
        {
            SelectedContent = new[]
            {
                SelectContent("NOA-1", DocumentFormat.Pdf),
                SelectContent("MISSING", DocumentFormat.Pdf)
            },
            MaximumSelectedContent = 2
        };

        TrademarkDossier result = builder.Build("sn90123456", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.CaseStatus, Is.Null);
            Assert.That(result.Status, Is.Null);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Parties, Is.Null);
            Assert.That(result.Documents.Length, Is.EqualTo(2));
            Assert.That(result.SelectedContent[0].Download?.FailureCode, Is.EqualTo("TRANSFER_FAILED"));
            Assert.That(result.SelectedContent[0].Error?.ErrorCode, Is.EqualTo("TRANSFER_FAILED"));
            Assert.That(result.SelectedContent[1].Download, Is.Null);
            Assert.That(result.SelectedContent[1].Error?.ErrorCode, Is.EqualTo("DOCUMENT_NOT_FOUND"));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(4));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(1));
            Assert.That(result.FailedSectionCount, Is.EqualTo(3));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(source.ContentCalls, Is.EqualTo(1));
        });

        SectionError status = result.Errors.Single(item => item.Section == "status");
        Assert.Multiple(() =>
        {
            Assert.That(status.ExceptionType, Is.EqualTo(nameof(ApiException)));
            Assert.That(status.StatusCode, Is.EqualTo(503));
            Assert.That(status.RequestIdentifier, Is.EqualTo("status-request"));
            Assert.That(status.IsRetryable, Is.True);
        });
    }

    [Test]
    public void ContentStillLoadsWhenRequestedDocumentListingFails()
    {
        FakeTrademarkDossierSource source = CreateSource();
        source.Failures[TrademarkDossierBuilder.DocumentsSection] = new NotFoundException("Documents unavailable.", ApiService.Tsdr, "sn90123456", 404, true, "documents-request", null);
        TrademarkDossierBuilder builder = new TrademarkDossierBuilder(source);
        TrademarkDossierSelection selection = new TrademarkDossierSelection
        {
            SelectedContent = new[] { SelectContent("NOA-1", DocumentFormat.Pdf) },
            MaximumSelectedContent = 1
        };

        TrademarkDossier result = builder.Build("sn90123456", selection, new RequestContext { CorrelationIdentifier = "correlation" });

        Assert.Multiple(() =>
        {
            Assert.That(result.CaseStatus, Is.Not.Null);
            Assert.That(result.DocumentCollection, Is.Null);
            Assert.That(result.Documents, Is.Empty);
            Assert.That(result.SelectedContent.Single().Document, Is.Null);
            Assert.That(result.SelectedContent.Single().IsComplete, Is.True);
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[] { "status", "documents", "content:NOA-1:pdf" }));
            Assert.That(result.Sections[2].Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Sections[2].Provenance.RequestIdentifier, Is.EqualTo("correlation"));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(3));
            Assert.That(result.CompletedSectionCount, Is.EqualTo(2));
            Assert.That(result.FailedSectionCount, Is.EqualTo(1));
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(source.ContentCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ContentOnlySelectionUsesDocumentListingAsHiddenMetadata()
    {
        FakeTrademarkDossierSource source = CreateSource();
        TrademarkDossierBuilder builder = new TrademarkDossierBuilder(source);
        TrademarkDossierSelection selection = new TrademarkDossierSelection
        {
            IncludeStatus = false,
            StatusFormat = DocumentFormat.Pdf,
            IncludeDocuments = false,
            SelectedContent = new[] { SelectContent("NOA-1", DocumentFormat.Pdf) },
            MaximumSelectedContent = 1
        };

        TrademarkDossier result = builder.Build("sn90123456", selection, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.CaseStatus, Is.Null);
            Assert.That(result.DocumentCollection, Is.Null);
            Assert.That(result.Documents, Is.Empty);
            Assert.That(result.SelectedContent.Single().Document, Is.SameAs(source.Documents.Documents[0]));
            Assert.That(result.Sections.Select(item => item.Section), Is.EqualTo(new[] { "content:NOA-1:pdf" }));
            Assert.That(result.RequestedSectionCount, Is.EqualTo(1));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(source.StatusCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.EqualTo(1));
            Assert.That(source.ContentCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidSelectionsStopBeforeAnySourceCall()
    {
        FakeTrademarkDossierSource source = CreateSource();
        TrademarkDossierBuilder builder = new TrademarkDossierBuilder(source);
        TrademarkDossierSelection empty = new TrademarkDossierSelection
        {
            IncludeStatus = false,
            IncludeDocuments = false
        };
        TrademarkDossierSelection overLimit = new TrademarkDossierSelection
        {
            SelectedContent = new[] { SelectContent("NOA-1", DocumentFormat.Pdf) },
            MaximumSelectedContent = 0
        };
        TrademarkDossierSelection duplicates = new TrademarkDossierSelection
        {
            SelectedContent = new[]
            {
                SelectContent("NOA-1", DocumentFormat.Pdf),
                SelectContent("noa-1", DocumentFormat.Pdf)
            },
            MaximumSelectedContent = 2
        };
        TrademarkDossierSelection invalidFormat = new TrademarkDossierSelection
        {
            SelectedContent = new[] { SelectContent("NOA-1", DocumentFormat.Html) },
            MaximumSelectedContent = 1
        };

        ValidationException noSection = Assert.Throws<ValidationException>(() => builder.Build("sn90123456", empty, new RequestContext()));
        ValidationException limit = Assert.Throws<ValidationException>(() => builder.Build("sn90123456", overLimit, new RequestContext()));
        ValidationException duplicate = Assert.Throws<ValidationException>(() => builder.Build("sn90123456", duplicates, new RequestContext()));
        ValidationException format = Assert.Throws<ValidationException>(() => builder.Build("sn90123456", invalidFormat, new RequestContext()));
        ValidationException identifier = Assert.Throws<ValidationException>(() => builder.Build("sn123", new TrademarkDossierSelection(), new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(noSection.ErrorCode, Is.EqualTo("SECTION_REQUIRED"));
            Assert.That(limit.ErrorCode, Is.EqualTo("CONTENT_LIMIT_EXCEEDED"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_CONTENT"));
            Assert.That(format.ErrorCode, Is.EqualTo("UNSUPPORTED_DOCUMENT_FORMAT"));
            Assert.That(identifier.ErrorCode, Is.EqualTo("INVALID_TRADEMARK_SERIAL"));
            Assert.That(source.StatusCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.Zero);
            Assert.That(source.ContentCalls, Is.Zero);
        });
    }

    [Test]
    public void CallerCancellationStopsBeforeSourceCalls()
    {
        FakeTrademarkDossierSource source = CreateSource();
        TrademarkDossierBuilder builder = new TrademarkDossierBuilder(source);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => builder.Build("sn90123456", new TrademarkDossierSelection(), new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(source.StatusCalls, Is.Zero);
            Assert.That(source.DocumentCalls, Is.Zero);
            Assert.That(source.ContentCalls, Is.Zero);
        });
    }

    private static FakeTrademarkDossierSource CreateSource()
    {
        FakeTrademarkDossierSource source = new FakeTrademarkDossierSource();
        source.Status = new TrademarkCaseStatus
        {
            RequestedIdentifier = "SN 90/123,456",
            CanonicalIdentifier = "sn90123456",
            IdentifierKind = TrademarkIdentifierKind.SerialNumber,
            Mark = new TrademarkMark { VerbalElementText = "EXAMPLE MARK" },
            Status = new TrademarkStatus { ExternalDescription = "LIVE" },
            CurrentOwners = new[] { new TrademarkOwner { Name = "Current Owner" } },
            PriorOwners = new[] { new TrademarkOwner { Name = "Prior Owner" } },
            Attorneys = new[] { new TrademarkAttorney { Name = "Counsel Name" } },
            Correspondence = new TrademarkCorrespondence { Name = "Correspondent" },
            ProsecutionEvents = new[]
            {
                new TrademarkProsecutionEvent { Code = "NWOS" },
                new TrademarkProsecutionEvent { Code = "PUBO" }
            },
            GoodsAndServices = new[] { new TrademarkGoodsAndService { PrimeClassCode = "042" } },
            Provenance = Provenance("TSDR case status", "status-source")
        };
        source.Documents = new TrademarkDocumentCollection
        {
            RequestedIdentifier = "SN 90/123,456",
            CanonicalIdentifier = "sn90123456",
            IdentifierKind = TrademarkIdentifierKind.SerialNumber,
            Documents = new[]
            {
                Document("NOA-1"),
                Document("REG-2")
            },
            Provenance = Provenance("TSDR case documents", "documents-source"),
            IsComplete = true
        };
        return source;
    }

    private static TrademarkDossierContentRequest SelectContent(string identifier, DocumentFormat format)
    {
        return new TrademarkDossierContentRequest
        {
            DocumentIdentifier = identifier,
            Format = format,
            Download = new DownloadRequest { Output = new MemoryStream() }
        };
    }

    private static TrademarkDocument Document(string identifier)
    {
        return new TrademarkDocument
        {
            CanonicalCaseIdentifier = "sn90123456",
            DocumentIdentifier = identifier,
            Provenance = Provenance("TSDR case documents", "document-source")
        };
    }

    private static PatentDataProvenance Provenance(string dataset, string requestIdentifier)
    {
        return new PatentDataProvenance
        {
            Source = DataProvenance.Tsdr,
            DatasetIdentifier = dataset,
            RequestIdentifier = requestIdentifier,
            IsAuthoritative = true
        };
    }
}

internal class FakeTrademarkDossierSource : ITrademarkDossierSource
{
    internal TrademarkCaseStatus Status = new TrademarkCaseStatus();
    internal TrademarkDocumentCollection Documents = new TrademarkDocumentCollection();
    internal Dictionary<int, Exception> Failures = new Dictionary<int, Exception>();
    internal HashSet<string> TerminalContentIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, Exception> ContentFailures = new Dictionary<string, Exception>(StringComparer.OrdinalIgnoreCase);
    internal List<DocumentFormat> StatusFormats = new List<DocumentFormat>();
    internal List<string> ContentIdentifiers = new List<string>();
    internal List<DocumentFormat> ContentFormats = new List<DocumentFormat>();
    internal List<DownloadRequest> ContentRequests = new List<DownloadRequest>();
    internal int StatusCalls;
    internal int DocumentCalls;
    internal int ContentCalls;

    public TrademarkCaseStatus GetTrademarkDossierStatus(string identifier, DocumentFormat format, RequestContext context)
    {
        StatusCalls += 1;
        StatusFormats.Add(format);
        ThrowSectionFailure(TrademarkDossierBuilder.StatusSection);
        return Status;
    }

    public TrademarkDocumentCollection GetTrademarkDossierDocuments(string identifier, RequestContext context)
    {
        DocumentCalls += 1;
        ThrowSectionFailure(TrademarkDossierBuilder.DocumentsSection);
        return Documents;
    }

    public DownloadResult GetTrademarkDossierContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request)
    {
        ContentCalls += 1;
        ContentIdentifiers.Add(documentIdentifier);
        ContentFormats.Add(format);
        ContentRequests.Add(request);
        if (ContentFailures.TryGetValue(documentIdentifier, out Exception? failure))
        {
            throw failure;
        }

        if (TerminalContentIdentifiers.Contains(documentIdentifier))
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
            Content = new DownloadContentMetadata
            {
                FinalUri = new Uri("https://tsdrapi.uspto.gov/ts/cd/casedoc/sn90123456/" + documentIdentifier + "/content.pdf"),
                Format = format
            },
            Progress = new DownloadProgress { State = DownloadState.Completed }
        };
    }

    internal void ThrowSectionFailure(int section)
    {
        if (Failures.TryGetValue(section, out Exception? failure))
        {
            throw failure;
        }
    }
}
