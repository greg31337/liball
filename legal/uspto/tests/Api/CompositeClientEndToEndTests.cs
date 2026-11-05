using System.Net;
using System.Numerics;
using System.Text;
using uspto;

namespace uspto.Tests;

public class CompositeClientEndToEndTests
{
    [TestCase(DossierBuilder.ApplicationSection, "application")]
    [TestCase(DossierBuilder.TransactionsSection, "transactions")]
    [TestCase(DossierBuilder.DocumentsSection, "documents")]
    [TestCase(DossierBuilder.AssociatedDocumentsSection, "associatedDocuments")]
    [TestCase(DossierBuilder.ContinuitySection, "continuity")]
    [TestCase(DossierBuilder.ForeignPrioritiesSection, "foreignPriorities")]
    [TestCase(DossierBuilder.AssignmentsSection, "assignments")]
    [TestCase(DossierBuilder.AttorneysSection, "attorneys")]
    [TestCase(DossierBuilder.TermAdjustmentSection, "termAdjustment")]
    [TestCase(DossierBuilder.FullTextSection, "fullText")]
    [TestCase(DossierBuilder.ResearchSection, "research")]
    public void ConfiguredPatentClientRetainsDossierResultsForEveryIndependentSectionFailure(int failedSection, string expectedSection)
    {
        using HttpClient transport = new HttpClient(new EmptyCatalogHandler());
        UsptoClients suite = CreateSuite(transport);
        FakePatentDossierSectionSource source = new FakePatentDossierSectionSource();
        source.Failures[failedSection] = Failure("dossier-" + expectedSection);
        ((ConfiguredPatentClient)suite.PatentClient!).Dossier = new DossierBuilder(source);

        PatentDossier result = suite.PatentClient.GetPatentDossier(
            "18/123,456",
            SelectAllDossierSections(),
            new RequestContext { CorrelationIdentifier = "public-dossier" });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.CompletedSectionCount, Is.GreaterThan(0));
            Assert.That(result.FailedSectionCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Errors.Select(item => item.Section), Does.Contain(expectedSection));
            Assert.That(result.Errors.Single(item => item.Section == expectedSection).RequestIdentifier, Is.EqualTo("dossier-" + expectedSection));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("public-dossier"));
        });

        suite.Lifetime!.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void ConfiguredOfficeActionClientRetainsTimelineRecordsForEverySourceFailureCombination(int failureMask)
    {
        using HttpClient transport = new HttpClient(new EmptyCatalogHandler());
        UsptoClients suite = CreateSuite(transport);
        EndToEndTimelineSource source = new EndToEndTimelineSource { FailureMask = failureMask };
        ((ConfiguredOfficeActionClient)suite.OfficeActionClient!).Timeline = new ProsecutionTimelineBuilder(source);

        ProsecutionTimeline result = suite.OfficeActionClient.GetProsecutionTimeline(
            "16/123,456",
            new RequestContext { CorrelationIdentifier = "public-timeline" });

        int failedCount = BitOperations.PopCount((uint)failureMask);
        int loadedCount = 3 - failedCount;
        Assert.Multiple(() =>
        {
            Assert.That(result.LoadedSectionCount, Is.EqualTo(loadedCount));
            Assert.That(result.FailedSectionCount, Is.EqualTo(failedCount));
            Assert.That(result.Events, Has.Length.EqualTo(loadedCount));
            Assert.That(result.IsComplete, Is.EqualTo(failureMask == 0));
            Assert.That(result.HasPartialSuccess, Is.EqualTo(failedCount > 0 && loadedCount > 0));
            Assert.That(result.Errors, Has.Length.EqualTo(failedCount));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("public-timeline"));
            Assert.That(result.Provenance.FieldSources, Has.Length.EqualTo(loadedCount));
        });

        suite.Lifetime!.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void ConfiguredPatentClientRetainsPortfolioEntriesForEveryHydrationFailureCombination(int failureMask)
    {
        using HttpClient transport = new HttpClient(new EmptyCatalogHandler());
        UsptoClients suite = CreateSuite(transport);
        FakePatentPortfolioSource source = PortfolioSource(failureMask);
        ((ConfiguredPatentClient)suite.PatentClient!).Portfolio = new PatentPortfolioLoader(source);

        PatentPortfolio result = suite.PatentClient.GetPatentPortfolio(new PatentPortfolioRequest
        {
            ApplicantName = "Example",
            MaximumResults = 3,
            Context = new RequestContext { CorrelationIdentifier = "public-portfolio" }
        });

        int failedCount = BitOperations.PopCount((uint)failureMask);
        int loadedCount = 3 - failedCount;
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.HydratedCount, Is.EqualTo(loadedCount));
            Assert.That(result.FailedHydrationCount, Is.EqualTo(failedCount));
            Assert.That(result.Entries.Count(item => item.IsHydrated), Is.EqualTo(loadedCount));
            Assert.That(result.Errors, Has.Length.EqualTo(failedCount));
            Assert.That(result.IsComplete, Is.EqualTo(failureMask == 0));
            Assert.That(result.HasPartialSuccess, Is.EqualTo(failureMask != 0));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("portfolio-search"));
            Assert.That(result.Provenance.FieldSources.Count(item => item.FieldPath == "Entries.Dossier"), Is.EqualTo(loadedCount));
        });

        suite.Lifetime!.Dispose();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void FactoryCitationBuilderAggregatesEverySourceOutcomeCombination(int failureMask)
    {
        using HttpClient transport = new HttpClient(new EmptyCatalogHandler());
        UsptoClients suite = CreateSuite(transport);
        ICitationGraphInputBuilder builder = suite.CitationGraphInputBuilder!;
        OfficeActionCitationResponse official = OfficialCitations();
        PatentsViewCitationResponse research = ResearchCitations();

        CitationGraphInputSet direct = builder.BuildCitationGraphInputs(official, research);
        CitationGraphAggregationRequest request = new CitationGraphAggregationRequest();
        if ((failureMask & 1) == 0)
        {
            request.OfficeActionCitations = official;
        }
        else
        {
            request.OfficeActionError = SourceError("office-action", ApiService.OfficeAction);
        }

        if ((failureMask & 2) == 0)
        {
            request.PatentsViewCitations = research;
        }
        else
        {
            request.PatentsViewError = SourceError("patents-view", ApiService.PatentsView);
        }

        CitationGraphInputSet aggregate = builder.AggregateCitationGraphInputs(request);
        int failedCount = BitOperations.PopCount((uint)failureMask);
        int loadedCount = 2 - failedCount;

        Assert.Multiple(() =>
        {
            Assert.That(direct.Inputs, Has.Length.EqualTo(2));
            Assert.That(direct.LoadedSourceCount, Is.EqualTo(2));
            Assert.That(direct.IsComplete, Is.True);
            Assert.That(aggregate.LoadedSourceCount, Is.EqualTo(loadedCount));
            Assert.That(aggregate.FailedSourceCount, Is.EqualTo(failedCount));
            Assert.That(aggregate.Inputs, Has.Length.EqualTo(loadedCount));
            Assert.That(aggregate.Errors, Has.Length.EqualTo(failedCount));
            Assert.That(aggregate.IsComplete, Is.EqualTo(failureMask == 0));
            Assert.That(aggregate.HasPartialSuccess, Is.EqualTo(failedCount == 1));
            Assert.That(aggregate.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(aggregate.Provenance.FieldSources, Has.Length.EqualTo(loadedCount));
        });

        suite.Lifetime!.Dispose();
    }

    [TestCase(FallbackAccess.Online)]
    [TestCase(FallbackAccess.Cache)]
    [TestCase(FallbackAccess.Local)]
    public void FactoryFallbackClientExercisesEveryAccessPathWithProvenance(FallbackAccess access)
    {
        using HttpClient transport = new HttpClient(new EmptyCatalogHandler());
        UsptoClients suite = CreateSuite(transport);
        RecordingDataFallbackSource source = FallbackSource();
        RequestContext context = new RequestContext
        {
            Fallback = new DataFallbackPolicy
            {
                Authority = FallbackAuthority.Either,
                AccessOrder = new[] { access },
                MaximumCacheAgeSeconds = 300,
                HasMaximumCacheAge = true,
                MaximumLocalAgeSeconds = 3600,
                HasMaximumLocalAge = true
            }
        };

        DataFallbackResult result = suite.DataFallbackClient!.ResolveDataFallback(source, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.SelectedAccess, Is.EqualTo(access));
            Assert.That(result.Value, Is.EqualTo(access.ToString()));
            Assert.That(result.Attempts, Has.Length.EqualTo(1));
            Assert.That(result.Attempts[0].IsSelected, Is.True);
            Assert.That(result.Provenance.FromCache, Is.EqualTo(access == FallbackAccess.Cache));
            Assert.That(result.Provenance.Source, Is.EqualTo(access == FallbackAccess.Local ? DataProvenance.LocalMirror : DataProvenance.Odp));
        });

        suite.Lifetime!.Dispose();
    }

    [Test]
    public void ConfiguredDatasetMirrorExercisesEveryPublicMethodWithAnUnavailableCatalogFamily()
    {
        EmptyCatalogHandler handler = new EmptyCatalogHandler();
        using HttpClient transport = new HttpClient(handler);
        using TemporaryTestRoots roots = new TemporaryTestRoots();
        UsptoClients suite = CreateSuite(transport, roots);
        MirrorPolicy policy = new MirrorPolicy
        {
            Directory = roots.MirrorRoot,
            DatasetFamilies = new[] { DatasetFamily.OfficeAction },
            MaximumParallelTransfers = 1,
            VerifyChecksums = true
        };
        RequestContext context = new RequestContext { CorrelationIdentifier = "public-mirror" };
        IDatasetMirror mirror = suite.DatasetMirror!;

        MirrorAction[] plan = mirror.PlanDatasetMirror(policy, context);
        DownloadResult[] synchronization = mirror.SynchronizeDatasetMirror(policy, context);
        MirrorWorkflowResult refresh = mirror.RefreshDatasetMirror(policy, context);
        DatasetManifest manifest = mirror.GetDatasetManifest(roots.MirrorRoot, context);
        DownloadResult[] verification = mirror.VerifyDatasetMirror(policy, context);
        DownloadResult[] repair = mirror.RepairDatasetMirror(policy, context);

        Assert.Multiple(() =>
        {
            Assert.That(plan, Is.Empty);
            Assert.That(synchronization, Is.Empty);
            Assert.That(refresh.Files, Is.Empty);
            Assert.That(refresh.UnavailableFamilyCount, Is.EqualTo(1));
            Assert.That(refresh.IsComplete, Is.False);
            Assert.That(refresh.HasPartialSuccess, Is.False);
            Assert.That(refresh.Errors.Single().ErrorCode, Is.EqualTo("MIRROR_FAMILY_UNAVAILABLE"));
            Assert.That(refresh.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(refresh.Provenance.RequestIdentifier, Is.EqualTo("public-mirror"));
            Assert.That(manifest.Products, Is.Empty);
            Assert.That(verification, Is.Empty);
            Assert.That(repair, Is.Empty);
            Assert.That(handler.CallCount, Is.EqualTo(4));
        });

        suite.Lifetime!.Dispose();
    }

    [TestCase(0, null)]
    [TestCase(1, "INTERRUPTED")]
    [TestCase(2, "CHECKSUM_FAILED")]
    [TestCase(3, "PARSE_FAILED")]
    [TestCase(4, "PARSE_FAILED")]
    public void ConfiguredDatasetMirrorAggregatesEveryPipelineFailureOutcome(int failureStage, string? expectedFailureCode)
    {
        const string catalog = "{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"OACT\",\"productTitleText\":\"Office Action Weekly Archive\",\"productFileBag\":{\"count\":1,\"fileDataBag\":[{\"fileName\":\"office.bin\",\"fileSize\":6,\"fileDownloadURI\":\"https://example.test/files/office.bin\"}]}}]}";
        EmptyCatalogHandler handler = new EmptyCatalogHandler { ResponseBody = catalog };
        using HttpClient transport = new HttpClient(handler);
        using TemporaryTestRoots roots = new TemporaryTestRoots();
        UsptoClients suite = CreateSuite(transport, roots);
        ConfiguredDatasetMirror configured = (ConfiguredDatasetMirror)suite.DatasetMirror!;
        RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
        transfer.Bodies["office.bin"] = Encoding.UTF8.GetBytes("office");
        if (failureStage == 1)
        {
            transfer.FailOnceFile = "office.bin";
        }

        RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
        if (failureStage >= 2)
        {
            stages.ConfigureFailure((MirrorPipelineStage)(failureStage - 1), 1, "deterministic stage failure");
        }

        configured.Synchronizer = new MirrorSynchronizer(transfer);
        configured.Pipeline = new MirrorStagePipeline(stages);
        MirrorPolicy policy = new MirrorPolicy
        {
            Directory = roots.MirrorRoot,
            DatasetFamilies = new[] { DatasetFamily.OfficeAction },
            MaximumParallelTransfers = 1,
            VerifyChecksums = true
        };

        MirrorWorkflowResult result = suite.DatasetMirror!.RefreshDatasetMirror(
            policy,
            new RequestContext { CorrelationIdentifier = "public-bulk-to-local" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Files, Has.Length.EqualTo(1));
            Assert.That(result.ProcessedFileCount, Is.EqualTo(1));
            Assert.That(result.IsComplete, Is.EqualTo(failureStage == 0));
            Assert.That(result.CompletedFileCount, Is.EqualTo(failureStage == 0 ? 1 : 0));
            Assert.That(result.FailedFileCount, Is.EqualTo(failureStage == 0 ? 0 : 1));
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(result.Errors, Has.Length.EqualTo(failureStage == 0 ? 0 : 1));
            Assert.That(result.Files[0].Error?.ErrorCode, Is.EqualTo(expectedFailureCode));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("public-bulk-to-local"));
            Assert.That(handler.CallCount, Is.EqualTo(2));
        });

        suite.Lifetime!.Dispose();
    }

    private static UsptoClients CreateSuite(HttpClient transport, TemporaryTestRoots? roots = null)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = transport,
            OdpApiKey = "deterministic-test-key",
            OdpBaseUri = new Uri("https://example.test/root/"),
            OfficeActionBaseUri = new Uri("https://example.test/root/"),
            TsdrBaseUri = new Uri("https://example.test/tsdr/"),
            PatentsViewBaseUri = new Uri("https://example.test/patentsview/")
        };
        if (roots != null)
        {
            roots.ApplyTo(options);
        }

        return new UsptoClientFactory().CreateUsptoClients(options);
    }

    private static PatentDossierSelection SelectAllDossierSections()
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

    private static FakePatentPortfolioSource PortfolioSource(int failureMask)
    {
        FakePatentPortfolioSource source = new FakePatentPortfolioSource();
        source.SearchResult.Summaries = new[]
        {
            new PatentApplicationSummary { ApplicationNumber = "18111111" },
            new PatentApplicationSummary { ApplicationNumber = "18222222" },
            new PatentApplicationSummary { ApplicationNumber = "18333333" }
        };
        source.SearchResult.Count = 3;
        source.SearchResult.IsComplete = true;
        source.SearchResult.RequestIdentifier = "portfolio-search";
        source.SearchResult.Provenance = Provenance(DataProvenance.Odp, "portfolio-search", true, false);
        int index;
        for (index = 0; index < source.SearchResult.Summaries.Length; index += 1)
        {
            if ((failureMask & (1 << index)) != 0)
            {
                string applicationNumber = source.SearchResult.Summaries[index].ApplicationNumber!;
                source.Failures[applicationNumber] = Failure("portfolio-" + applicationNumber);
            }
        }

        return source;
    }

    private static OfficeActionCitationResponse OfficialCitations()
    {
        return new OfficeActionCitationResponse
        {
            Citations = new[]
            {
                new OfficeActionCitation
                {
                    ReferenceIdentifier = "US7654321B2",
                    ParsedReferenceIdentifier = "US7654321B2",
                    CitedIdentifier = new CitedReferenceIdentifier
                    {
                        VerbatimText = "US7654321B2",
                        CanonicalPatentIdentifier = "US7654321B2",
                        PatentIdentifierKind = IdentifierKind.PatentGrant,
                        IsPatentDocument = true,
                        IsClassified = true
                    },
                    Provenance = Provenance(DataProvenance.Odp, "office-action", true, false)
                }
            },
            Provenance = Provenance(DataProvenance.Odp, "office-action", true, false)
        };
    }

    private static PatentsViewCitationResponse ResearchCitations()
    {
        return new PatentsViewCitationResponse
        {
            Citations = new[]
            {
                new PatentsViewCitation
                {
                    PatentIdentifier = "source",
                    CitedPatentIdentifier = "9999999",
                    CitationType = "us_patent",
                    Provenance = Provenance(DataProvenance.PatentsView, "patents-view", false, false)
                }
            },
            Metadata = new PatentsViewResponseMetadata
            {
                Provenance = Provenance(DataProvenance.PatentsView, "patents-view", false, false)
            }
        };
    }

    private static RecordingDataFallbackSource FallbackSource()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.OnlineCapability = Capability(FallbackAccess.Online, Provenance(DataProvenance.Odp, "online", true, false, now));
        source.CacheCapability = Capability(FallbackAccess.Cache, Provenance(DataProvenance.Odp, "cache", true, true, now));
        source.LocalCapability = Capability(FallbackAccess.Local, Provenance(DataProvenance.LocalMirror, "local", true, false, now));
        source.OnlineValue = Value(FallbackAccess.Online.ToString(), source.OnlineCapability.Provenance);
        source.CacheValue = Value(FallbackAccess.Cache.ToString(), source.CacheCapability.Provenance);
        source.LocalValue = Value(FallbackAccess.Local.ToString(), source.LocalCapability.Provenance);
        return source;
    }

    private static DataFallbackCapability Capability(FallbackAccess access, PatentDataProvenance provenance)
    {
        return new DataFallbackCapability
        {
            Access = access,
            IsAvailable = true,
            SupportsOperation = true,
            Provenance = provenance
        };
    }

    private static DataFallbackValue Value(object value, PatentDataProvenance provenance)
    {
        return new DataFallbackValue { Value = value, Provenance = provenance };
    }

    private static PatentDataProvenance Provenance(DataProvenance source, string dataset, bool authoritative, bool fromCache, DateTimeOffset timestamp = default)
    {
        PatentDataProvenance result = new PatentDataProvenance
        {
            Source = source,
            DatasetIdentifier = dataset,
            FromCache = fromCache,
            IsAuthoritative = authoritative
        };
        if (timestamp != default)
        {
            result.RetrievedAt = timestamp;
            result.HasRetrievedAt = true;
        }

        if (source == DataProvenance.LocalMirror)
        {
            result.FieldSources = new[]
            {
                new FieldDataProvenance
                {
                    FieldPath = "*",
                    Source = DataProvenance.Odp,
                    Delivery = DataProvenance.LocalMirror,
                    IsAuthoritative = true
                }
            };
        }

        return result;
    }

    private static SectionError SourceError(string requestIdentifier, ApiService service)
    {
        return new SectionError
        {
            ExceptionType = nameof(ApiException),
            Message = "The deterministic source failed.",
            ErrorCode = "DETERMINISTIC_FAILURE",
            Service = service,
            StatusCode = 503,
            HasStatusCode = true,
            RequestIdentifier = requestIdentifier,
            IsRetryable = true
        };
    }

    private static ApiException Failure(string requestIdentifier)
    {
        return new ApiException("The deterministic source failed.", ApiService.Odp, 503, true, "DETERMINISTIC_FAILURE", null, requestIdentifier, true, null);
    }
}

internal class EndToEndTimelineSource : IProsecutionTimelineSource
{
    internal int FailureMask;

    public string ResolveTimelineIdentifier(string identifier, RequestContext context)
    {
        return "16123456";
    }

    public OfficeActionTextResponse GetTimelineOfficeActions(string applicationNumber, RequestContext context)
    {
        ThrowWhenFailed(ProsecutionTimelineBuilder.OfficeActionSection);
        return new OfficeActionTextResponse
        {
            Records = new[]
            {
                new OfficeActionTextRecord
                {
                    Identifier = "office-action",
                    Provenance = Provenance("office-action")
                }
            },
            IsComplete = true,
            Provenance = Provenance("office-action")
        };
    }

    public PatentTransactionHistory GetTimelineTransactions(string applicationNumber, RequestContext context)
    {
        ThrowWhenFailed(ProsecutionTimelineBuilder.TransactionSection);
        return new PatentTransactionHistory
        {
            Events = new[]
            {
                new PatentTransaction
                {
                    EventSequenceNumber = 1,
                    HasEventSequenceNumber = true,
                    EventCode = "TRANSACTION"
                }
            },
            Provenance = Provenance("transactions")
        };
    }

    public PatentDocumentList GetTimelineDocuments(string applicationNumber, RequestContext context)
    {
        ThrowWhenFailed(ProsecutionTimelineBuilder.DocumentSection);
        return new PatentDocumentList
        {
            Documents = new[]
            {
                new PatentDocument
                {
                    DocumentIdentifier = "document",
                    Code = "DOCUMENT"
                }
            },
            Provenance = Provenance("documents")
        };
    }

    private void ThrowWhenFailed(int section)
    {
        if ((FailureMask & (1 << (section - 1))) != 0)
        {
            throw new ApiException("The timeline source failed.", ApiService.Odp, 503, true, "DETERMINISTIC_FAILURE", null, "timeline-" + section, true, null);
        }
    }

    private static PatentDataProvenance Provenance(string dataset)
    {
        return new PatentDataProvenance
        {
            Source = DataProvenance.Odp,
            DatasetIdentifier = dataset,
            IsAuthoritative = true
        };
    }
}

internal class EmptyCatalogHandler : HttpMessageHandler
{
    internal int CallCount;
    internal string ResponseBody = "{\"count\":0,\"bulkDataProductBag\":[]}";

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        string body = ResponseBody;
        if (request.RequestUri != null && request.RequestUri.Query.Contains("offset=1", StringComparison.Ordinal))
        {
            body = "{\"count\":0,\"bulkDataProductBag\":[]}";
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, UsptoMediaTypes.Json)
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Send(request, cancellationToken));
    }
}
