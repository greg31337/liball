using uspto;

namespace uspto.Tests;

public class ProsecutionTimelineBuilderTests
{
    [Test]
    public void TimelineJoinsByExplicitConfidenceAndKeepsSourceProvenanceSeparate()
    {
        FakeProsecutionTimelineSource source = new FakeProsecutionTimelineSource
        {
            OfficeActions = new OfficeActionTextResponse
            {
                Records = new[]
                {
                    OfficeAction("oa-exact", Date(2024, 1, 10), true, "EXACT-CODE", "doc-exact"),
                    OfficeAction("oa-date", Date(2024, 2, 20), true, "OA-CODE", null),
                    OfficeAction("oa-code", default(DateTimeOffset), false, "LOW-CODE", null)
                },
                IsComplete = true,
                Provenance = Provenance("office-actions")
            },
            Transactions = new PatentTransactionHistory
            {
                Events = new[]
                {
                    Transaction(9, Date(2024, 1, 1), "FILE"),
                    Transaction(1, Date(2024, 1, 10), "EXACT-CODE"),
                    Transaction(2, Date(2024, 2, 20), "OTHER-CODE"),
                    Transaction(3, Date(2024, 3, 10), "LOW-CODE")
                },
                Provenance = Provenance("transactions")
            },
            Documents = new PatentDocumentList
            {
                Documents = new[]
                {
                    Document("doc-exact", Date(2024, 1, 11), "OTHER-CODE"),
                    Document("doc-date", Date(2024, 2, 20), "OTHER-CODE"),
                    Document("doc-code", Date(2024, 3, 10), "LOW-CODE"),
                    Document("doc-unmatched", Date(2024, 4, 1), "NOTICE")
                },
                Provenance = Provenance("documents")
            }
        };
        RequestContext context = new RequestContext { CorrelationIdentifier = "timeline-correlation" };

        ProsecutionTimeline result = new ProsecutionTimelineBuilder(source).Build("16/123,456", context);

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("16123456"));
            Assert.That(result.Events.Select(item => item.EventIdentifier), Is.EqualTo(new[] { "FILE:9", "oa-exact", "oa-date", "oa-code", "doc-unmatched" }));
            Assert.That(result.Count, Is.EqualTo(5));
            Assert.That(result.MatchedOfficeActionCount, Is.EqualTo(3));
            Assert.That(result.UnmatchedOfficeActionCount, Is.Zero);
            Assert.That(result.UnmatchedTransactionCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedDocumentCount, Is.EqualTo(1));
            Assert.That(result.LoadedSectionCount, Is.EqualTo(3));
            Assert.That(result.FailedSectionCount, Is.Zero);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.HasPartialSuccess, Is.False);
            Assert.That(result.Errors, Is.Empty);
        });

        ProsecutionTimelineEvent exact = result.Events.Single(item => item.EventIdentifier == "oa-exact");
        ProsecutionTimelineEvent date = result.Events.Single(item => item.EventIdentifier == "oa-date");
        ProsecutionTimelineEvent code = result.Events.Single(item => item.EventIdentifier == "oa-code");
        Assert.Multiple(() =>
        {
            Assert.That(exact.TransactionMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.High));
            Assert.That(exact.TransactionMatchBasis, Is.EqualTo("dateAndCode"));
            Assert.That(exact.DocumentMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.Exact));
            Assert.That(exact.DocumentMatchBasis, Is.EqualTo("documentIdentifier"));
            Assert.That(date.TransactionMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.Medium));
            Assert.That(date.DocumentMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.Medium));
            Assert.That(code.TransactionMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.Low));
            Assert.That(code.DocumentMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.Low));
            Assert.That(code.HasEventDate, Is.True);
            Assert.That(code.EventDate, Is.EqualTo(Date(2024, 3, 10)));
        });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(exact.Provenance.DatasetIdentifier, Is.EqualTo("office-actions"));
            Assert.That(exact.Provenance, Is.Not.SameAs(exact.OfficeAction?.Provenance));
            Assert.That(exact.DerivationProvenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(exact.DerivationProvenance.IsAuthoritative, Is.False);
            Assert.That(exact.DerivationProvenance.RequestIdentifier, Is.EqualTo("timeline-correlation"));
            Assert.That(result.OfficeActionProvenance.DatasetIdentifier, Is.EqualTo("office-actions"));
            Assert.That(result.TransactionProvenance.DatasetIdentifier, Is.EqualTo("transactions"));
            Assert.That(result.DocumentProvenance.DatasetIdentifier, Is.EqualTo("documents"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.Provenance.HasRetrievedAt, Is.True);
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Events.OfficeAction").DatasetIdentifier, Is.EqualTo("office-actions"));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Events.Transaction").DatasetIdentifier, Is.EqualTo("transactions"));
            Assert.That(result.Provenance.FieldSources.Single(item => item.FieldPath == "Events.Document").DatasetIdentifier, Is.EqualTo("documents"));
            Assert.That(result.Provenance.FieldSources.All(item => item.IsAuthoritative), Is.True);
        });
    }

    [Test]
    public void EqualCandidatesUseStableIdentifiersAndAreMatchedOnlyOnce()
    {
        OfficeActionTextRecord first = OfficeAction("oa-1", Date(2024, 5, 1), true, "CTNF", null);
        OfficeActionTextRecord second = OfficeAction("oa-2", Date(2024, 5, 1), true, "CTNF", null);
        FakeProsecutionTimelineSource source = new FakeProsecutionTimelineSource
        {
            OfficeActions = new OfficeActionTextResponse { Records = new[] { first, second }, IsComplete = true },
            Transactions = new PatentTransactionHistory
            {
                Events = new[]
                {
                    Transaction(2, Date(2024, 5, 1), "CTNF"),
                    Transaction(1, Date(2024, 5, 1), "CTNF")
                }
            },
            Documents = new PatentDocumentList
            {
                Documents = new[]
                {
                    Document("z-document", Date(2024, 5, 1), "CTNF"),
                    Document("a-document", Date(2024, 5, 1), "CTNF")
                }
            }
        };

        ProsecutionTimeline result = new ProsecutionTimelineBuilder(source).Build("16123456", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Events.Length, Is.EqualTo(2));
            Assert.That(result.Events[0].EventIdentifier, Is.EqualTo("oa-1"));
            Assert.That(result.Events[0].Transaction?.EventSequenceNumber, Is.EqualTo(1));
            Assert.That(result.Events[0].Document?.DocumentIdentifier, Is.EqualTo("a-document"));
            Assert.That(result.Events[1].EventIdentifier, Is.EqualTo("oa-2"));
            Assert.That(result.Events[1].Transaction?.EventSequenceNumber, Is.EqualTo(2));
            Assert.That(result.Events[1].Document?.DocumentIdentifier, Is.EqualTo("z-document"));
            Assert.That(result.UnmatchedTransactionCount, Is.Zero);
            Assert.That(result.UnmatchedDocumentCount, Is.Zero);
        });
    }

    [Test]
    public void SameDateSourceRecordsHaveDeterministicOrderAcrossInputPermutations()
    {
        DateTimeOffset eventDate = Date(2024, 5, 2);
        FakeProsecutionTimelineSource forwardSource = new FakeProsecutionTimelineSource
        {
            OfficeActions = new OfficeActionTextResponse { IsComplete = true },
            Transactions = new PatentTransactionHistory
            {
                Events = new[]
                {
                    Transaction(2, eventDate, "STATUS"),
                    Transaction(1, eventDate, "STATUS")
                }
            },
            Documents = new PatentDocumentList
            {
                Documents = new[]
                {
                    Document("z-document", eventDate, "NOTICE"),
                    Document("a-document", eventDate, "NOTICE")
                }
            }
        };
        FakeProsecutionTimelineSource reverseSource = new FakeProsecutionTimelineSource
        {
            OfficeActions = new OfficeActionTextResponse { IsComplete = true },
            Transactions = new PatentTransactionHistory
            {
                Events = forwardSource.Transactions.Events.Reverse().ToArray()
            },
            Documents = new PatentDocumentList
            {
                Documents = forwardSource.Documents.Documents.Reverse().ToArray()
            }
        };

        ProsecutionTimeline forward = new ProsecutionTimelineBuilder(forwardSource).Build("16123456", new RequestContext());
        ProsecutionTimeline reverse = new ProsecutionTimelineBuilder(reverseSource).Build("16123456", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(forward.Events.Select(item => item.EventIdentifier), Is.EqualTo(new[] { "STATUS:1", "STATUS:2", "a-document", "z-document" }));
            Assert.That(reverse.Events.Select(item => item.EventIdentifier), Is.EqualTo(forward.Events.Select(item => item.EventIdentifier)));
            Assert.That(forward.Events.Select(item => item.SourceCategory), Is.EqualTo(new[] { "transaction", "transaction", "document", "document" }));
            Assert.That(forward.UnmatchedTransactionCount, Is.EqualTo(2));
            Assert.That(forward.UnmatchedDocumentCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void IndependentSourceFailureRetainsSuccessfulTimelineRecords()
    {
        FakeProsecutionTimelineSource source = new FakeProsecutionTimelineSource
        {
            FailedSection = ProsecutionTimelineBuilder.TransactionSection,
            OfficeActions = new OfficeActionTextResponse
            {
                Records = new[] { OfficeAction("oa-1", Date(2024, 6, 1), true, "CTNF", "doc-1") },
                IsComplete = true,
                Provenance = Provenance("office-actions")
            },
            Documents = new PatentDocumentList
            {
                Documents = new[] { Document("doc-1", Date(2024, 6, 1), "CTNF") },
                Provenance = Provenance("documents")
            }
        };

        ProsecutionTimeline result = new ProsecutionTimelineBuilder(source).Build("16123456", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Events.Length, Is.EqualTo(1));
            Assert.That(result.Events[0].Document?.DocumentIdentifier, Is.EqualTo("doc-1"));
            Assert.That(result.Events[0].Transaction, Is.Null);
            Assert.That(result.Events[0].DocumentMatchConfidence, Is.EqualTo(ProsecutionMatchConfidence.Exact));
            Assert.That(result.LoadedSectionCount, Is.EqualTo(2));
            Assert.That(result.FailedSectionCount, Is.EqualTo(1));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Errors.Single().Section, Is.EqualTo("transactions"));
            Assert.That(result.Errors.Single().ExceptionType, Is.EqualTo(nameof(ApiException)));
            Assert.That(result.Errors.Single().StatusCode, Is.EqualTo(503));
            Assert.That(result.Errors.Single().RequestIdentifier, Is.EqualTo("transaction-request"));
            Assert.That(result.TransactionProvenance.Source, Is.EqualTo(DataProvenance.Unknown));
        });
    }

    [Test]
    public void UnmatchedAndUndatedRecordsAreRetainedWithOfficialProvenance()
    {
        FakeProsecutionTimelineSource source = new FakeProsecutionTimelineSource
        {
            OfficeActions = new OfficeActionTextResponse
            {
                Records = new[] { OfficeAction("oa-unmatched", default(DateTimeOffset), false, "OA", null) },
                IsComplete = true
            },
            Transactions = new PatentTransactionHistory
            {
                Events = new[] { Transaction(1, Date(2024, 7, 1), "TX") },
                Provenance = Provenance("transactions")
            },
            Documents = new PatentDocumentList
            {
                Documents = new[] { new PatentDocument { DocumentIdentifier = "doc-undated", Code = "DOC" } },
                Provenance = Provenance("documents")
            }
        };

        ProsecutionTimeline result = new ProsecutionTimelineBuilder(source).Build("16123456", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Events.Select(item => item.EventIdentifier), Is.EqualTo(new[] { "TX:1", "oa-unmatched", "doc-undated" }));
            Assert.That(result.UnmatchedOfficeActionCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedTransactionCount, Is.EqualTo(1));
            Assert.That(result.UnmatchedDocumentCount, Is.EqualTo(1));
            Assert.That(result.Events[0].Provenance.DatasetIdentifier, Is.EqualTo("transactions"));
            Assert.That(result.Events[2].Provenance.DatasetIdentifier, Is.EqualTo("documents"));
            Assert.That(result.Events[1].IsDerivedMatch, Is.False);
            Assert.That(result.Events[1].DerivationProvenance.Source, Is.EqualTo(DataProvenance.Unknown));
            Assert.That(result.Events[2].HasEventDate, Is.False);
        });
    }

    [Test]
    public void CallerCancellationStopsBeforeIdentifierResolution()
    {
        FakeProsecutionTimelineSource source = new FakeProsecutionTimelineSource();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => new ProsecutionTimelineBuilder(source).Build("16123456", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(source.ResolveCalls, Is.Zero);
    }

    private static OfficeActionTextRecord OfficeAction(string identifier, DateTimeOffset date, bool hasDate, string code, string? documentIdentifier)
    {
        return new OfficeActionTextRecord
        {
            Identifier = identifier,
            SubmissionDate = date,
            HasSubmissionDate = hasDate,
            LegacyDocumentCodeIdentifiers = new[] { code },
            ObsoleteDocumentIdentifiers = documentIdentifier == null ? Array.Empty<string>() : new[] { documentIdentifier },
            Provenance = Provenance("office-actions")
        };
    }

    private static PatentTransaction Transaction(int sequence, DateTimeOffset date, string code)
    {
        return new PatentTransaction
        {
            EventSequenceNumber = sequence,
            HasEventSequenceNumber = true,
            EventDate = date,
            HasEventDate = true,
            EventCode = code
        };
    }

    private static PatentDocument Document(string identifier, DateTimeOffset date, string code)
    {
        return new PatentDocument
        {
            DocumentIdentifier = identifier,
            OfficialDate = date,
            HasOfficialDate = true,
            Code = code
        };
    }

    private static DateTimeOffset Date(int year, int month, int day)
    {
        return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
    }

    private static PatentDataProvenance Provenance(string datasetIdentifier)
    {
        return new PatentDataProvenance
        {
            Source = DataProvenance.Odp,
            DatasetIdentifier = datasetIdentifier,
            IsAuthoritative = true
        };
    }
}

internal class FakeProsecutionTimelineSource : IProsecutionTimelineSource
{
    internal int ResolveCalls = 0;
    internal int OfficeActionCalls = 0;
    internal int TransactionCalls = 0;
    internal int DocumentCalls = 0;
    internal int FailedSection = 0;
    internal OfficeActionTextResponse OfficeActions = new OfficeActionTextResponse { IsComplete = true };
    internal PatentTransactionHistory Transactions = new PatentTransactionHistory();
    internal PatentDocumentList Documents = new PatentDocumentList();

    public string ResolveTimelineIdentifier(string identifier, RequestContext context)
    {
        ResolveCalls += 1;
        return "16123456";
    }

    public OfficeActionTextResponse GetTimelineOfficeActions(string applicationNumber, RequestContext context)
    {
        OfficeActionCalls += 1;
        ThrowFailure(ProsecutionTimelineBuilder.OfficeActionSection);
        return OfficeActions;
    }

    public PatentTransactionHistory GetTimelineTransactions(string applicationNumber, RequestContext context)
    {
        TransactionCalls += 1;
        ThrowFailure(ProsecutionTimelineBuilder.TransactionSection);
        return Transactions;
    }

    public PatentDocumentList GetTimelineDocuments(string applicationNumber, RequestContext context)
    {
        DocumentCalls += 1;
        ThrowFailure(ProsecutionTimelineBuilder.DocumentSection);
        return Documents;
    }

    internal void ThrowFailure(int section)
    {
        if (FailedSection == section)
        {
            throw new ApiException("Timeline source unavailable.", ApiService.Odp, 503, true, "TRANSIENT", null, "transaction-request", true, null);
        }
    }
}
