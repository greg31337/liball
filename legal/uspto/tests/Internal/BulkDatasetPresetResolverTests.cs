using System.Net;
using uspto;

namespace uspto.Tests;

public class BulkDatasetPresetResolverTests
{
    [Test]
    public void ResolverGroupsEverySupportedFamilyFromTheCurrentCatalog()
    {
        BulkProduct fileWrapper = Product("PTFWPRE", "Patent File Wrapper Weekly");
        fileWrapper.Files = new[]
        {
            new BulkProductFile { FileName = "patent-file-wrapper-full-archive.zip" },
            new BulkProductFile { FileName = "patent-file-wrapper-weekly-update.zip" }
        };
        BulkProduct[] products = new[]
        {
            fileWrapper,
            Product("PTBLXML", "Patent Grant Full Text Data/XML"),
            Product("APPDT", "Published Patent Application Data/XML"),
            Product("PASDL", "Patent Assignments"),
            Product("OACT", "Office Actions Weekly Archives"),
            Product("PTAB-TRIALS", "Patent Trial and Appeal Board Bulk Data"),
            Product("TRTDXFAP", "Trademark Daily XML Files Applications and Status"),
            Product("TRTDXFAG", "Trademark Assignment Daily XML"),
            Product("TTABTDXF", "Trademark Trial and Appeal Board Daily XML"),
            Product("PV-GRANT", "PatentsView Grant Bibliographic Data"),
            Product("PV-PREGRANT", "PatentsView Pre-grant Publication Tables"),
            Product("PV-ANNUAL", "PatentsView Annualized Patent Data"),
            Product("PV-SORTED", "PatentsView Sorted Patent Tables")
        };
        DatasetFamily[] families = SupportedFamilies();
        PatentDataProvenance catalogProvenance = CatalogProvenance("catalog-request");

        BulkDatasetPreset[] results = BulkDatasetPresetResolver.ResolvePresets(families, products, catalogProvenance);

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(item => item.Family), Is.EqualTo(families));
            Assert.That(results.All(item => item.IsAvailable), Is.True);
            Assert.That(FindPreset(results, DatasetFamily.PatentFileWrapperComplete).ProductIdentifiers, Does.Contain("PTFWPRE"));
            Assert.That(FindPreset(results, DatasetFamily.PatentFileWrapperDelta).ProductIdentifiers, Does.Contain("PTFWPRE"));
            Assert.That(FindPreset(results, DatasetFamily.PatentGrantFullText).ProductIdentifiers, Does.Contain("PTBLXML"));
            Assert.That(FindPreset(results, DatasetFamily.PatentApplicationFullText).ProductIdentifiers, Does.Contain("APPDT"));
            Assert.That(FindPreset(results, DatasetFamily.PatentAssignment).ProductIdentifiers, Is.EqualTo(new[] { "PASDL" }));
            Assert.That(FindPreset(results, DatasetFamily.OfficeAction).ProductIdentifiers, Is.EqualTo(new[] { "OACT" }));
            Assert.That(FindPreset(results, DatasetFamily.Ptab).ProductIdentifiers, Is.EqualTo(new[] { "PTAB-TRIALS" }));
            Assert.That(FindPreset(results, DatasetFamily.TrademarkApplicationAndStatus).ProductIdentifiers, Is.EqualTo(new[] { "TRTDXFAP" }));
            Assert.That(FindPreset(results, DatasetFamily.TrademarkAssignment).ProductIdentifiers, Is.EqualTo(new[] { "TRTDXFAG" }));
            Assert.That(FindPreset(results, DatasetFamily.Ttab).ProductIdentifiers, Is.EqualTo(new[] { "TTABTDXF" }));
            Assert.That(FindPreset(results, DatasetFamily.PatentsViewGrant).ProductIdentifiers, Does.Contain("PV-GRANT"));
            Assert.That(FindPreset(results, DatasetFamily.PatentsViewPreGrant).ProductIdentifiers, Is.EqualTo(new[] { "PV-PREGRANT" }));
            Assert.That(FindPreset(results, DatasetFamily.PatentsViewAnnualized).ProductIdentifiers, Is.EqualTo(new[] { "PV-ANNUAL" }));
            Assert.That(FindPreset(results, DatasetFamily.PatentsViewSorted).ProductIdentifiers, Is.EqualTo(new[] { "PV-SORTED" }));
            Assert.That(results[0].Products.Single(), Is.SameAs(fileWrapper));
            Assert.That(results[0].Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(results[0].Provenance.SourceUri, Is.EqualTo(catalogProvenance.SourceUri));
            Assert.That(results[0].Provenance.RequestIdentifier, Is.EqualTo("catalog-request"));
            Assert.That(results[0].Provenance.IsAuthoritative, Is.False);
        });
    }

    [Test]
    public void ResolverUsesMetadataAndFilesForNewProductIdentifiers()
    {
        BulkProduct nextWrapper = Product("NEXT-WRAPPER", "Patent File Wrapper Data");
        nextWrapper.Files = new[]
        {
            new BulkProductFile { FileName = "complete-snapshot.zip" },
            new BulkProductFile { FileName = "weekly-update.zip" }
        };
        BulkProduct[] products = new[]
        {
            nextWrapper,
            Product("NEXT-GRANT", "Patent Grant Full Text XML"),
            Product("NEXT-APP", "Published Patent Application Full Text"),
            Product("NEXT-ASSIGN", "Patent Assignment Research Dataset"),
            Product("NEXT-OA", "Office Action Archive"),
            Product("NEXT-PTAB", "Patent Trial and Appeal Board Records"),
            Product("NEXT-TRADEMARK", "Trademark Daily XML Files (TDXF) - Applications"),
            Product("NEXT-TM-ASSIGN", "Trademark Assignment Records"),
            Product("NEXT-TTAB", "Trademark Trial and Appeal Board Records"),
            Product("NEXT-PV-GRANT", "PatentsView Grant Tables"),
            Product("NEXT-PV-PRE", "Patents View Pregrant Publication Data"),
            Product("NEXT-PV-ANNUAL", "PatentsView Annualized Tables"),
            Product("NEXT-PV-SORT", "PatentsView Sorted Tables")
        };

        BulkDatasetPreset[] results = BulkDatasetPresetResolver.ResolvePresets(SupportedFamilies(), products, CatalogProvenance(null));

        Assert.Multiple(() =>
        {
            Assert.That(results.All(item => item.IsAvailable), Is.True);
            Assert.That(FindPreset(results, DatasetFamily.PatentFileWrapperComplete).ProductIdentifiers, Is.EqualTo(new[] { "NEXT-WRAPPER" }));
            Assert.That(FindPreset(results, DatasetFamily.PatentFileWrapperDelta).ProductIdentifiers, Is.EqualTo(new[] { "NEXT-WRAPPER" }));
            Assert.That(FindPreset(results, DatasetFamily.PatentsViewPreGrant).ProductIdentifiers, Is.EqualTo(new[] { "NEXT-PV-PRE" }));
        });
    }

    [Test]
    public void MissingFamiliesRemainExplicitlyUnavailableWithoutCandidateProducts()
    {
        DatasetFamily[] requested = new[] { DatasetFamily.Ttab, DatasetFamily.OfficeAction, DatasetFamily.PatentsViewSorted };
        BulkProduct unrelated = Product("OTHER", "Unrelated economic research data");

        BulkDatasetPreset[] results = BulkDatasetPresetResolver.ResolvePresets(requested, new[] { unrelated }, CatalogProvenance("empty-request"));

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(item => item.Family), Is.EqualTo(requested));
            Assert.That(results.All(item => !item.IsAvailable), Is.True);
            Assert.That(results.All(item => item.ProductIdentifiers.Length == 0), Is.True);
            Assert.That(results.All(item => item.Products.Length == 0), Is.True);
            Assert.That(results[0].Provenance.DatasetIdentifier, Is.EqualTo("bulk-dataset-preset/Ttab"));
            Assert.That(results[0].Provenance.HasRetrievedAt, Is.True);
        });
    }

    [Test]
    public void PresetOperationRefreshesAvailabilityFromTheCatalogOnEveryCall()
    {
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(new[]
        {
            CatalogResponse("OACT", "Office Actions Weekly Archives", "first-request"),
            EmptyCatalogResponse(),
            CatalogResponse("OTHER", "Unrelated Product", "second-request"),
            EmptyCatalogResponse()
        });
        using HttpClient client = new HttpClient(handler);
        BulkDatasetPresetOperation operation = CreateOperation(client);
        IBulkDatasetPresetSource source = operation;
        DatasetFamily[] families = new[] { DatasetFamily.OfficeAction };

        BulkDatasetPreset first = source.GetBulkDatasetPresets(families, new RequestContext()).Single();
        BulkDatasetPreset second = operation.GetPresets(families, new RequestContext()).Single();

        Assert.Multiple(() =>
        {
            Assert.That(first.IsAvailable, Is.True);
            Assert.That(first.ProductIdentifiers, Is.EqualTo(new[] { "OACT" }));
            Assert.That(first.Provenance.RequestIdentifier, Is.EqualTo("first-request"));
            Assert.That(second.IsAvailable, Is.False);
            Assert.That(second.ProductIdentifiers, Is.Empty);
            Assert.That(second.Provenance.RequestIdentifier, Is.EqualTo("second-request"));
            Assert.That(handler.CallCount, Is.EqualTo(4));
            Assert.That(handler.Queries, Has.All.ContainKey("sort"));
            Assert.That(handler.Queries[0]["sort"], Is.EqualTo("productIdentifier asc"));
            Assert.That(handler.Queries[0].ContainsKey("q"), Is.False);
        });
    }

    [Test]
    public void EmptySelectionAndInvalidInputsDoNotCallTheCatalog()
    {
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(new string[0]);
        using HttpClient client = new HttpClient(handler);
        BulkDatasetPresetOperation operation = CreateOperation(client);

        BulkDatasetPreset[] empty = operation.GetPresets(new DatasetFamily[0], new RequestContext());
        ValidationException unknown = Assert.Throws<ValidationException>(() => operation.GetPresets(new[] { DatasetFamily.Unknown }, new RequestContext()));
        ValidationException duplicate = Assert.Throws<ValidationException>(() => operation.GetPresets(new[] { DatasetFamily.Ptab, DatasetFamily.Ptab }, new RequestContext()));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => operation.GetPresets(new[] { DatasetFamily.Ptab }, new RequestContext { CancellationToken = cancellation.Token }));

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(unknown.ErrorCode, Is.EqualTo("UNSUPPORTED_DATASET_FAMILY"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_DATASET_FAMILY"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static BulkDatasetPresetOperation CreateOperation(HttpClient client)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
        return new BulkDatasetPresetOperation(new BulkCatalogSearchOperation(client, options));
    }

    private static BulkProduct Product(string identifier, string title)
    {
        return new BulkProduct
        {
            ProductIdentifier = identifier,
            Title = title
        };
    }

    private static PatentDataProvenance CatalogProvenance(string? requestIdentifier)
    {
        return new PatentDataProvenance
        {
            Source = DataProvenance.Odp,
            SourceUri = new Uri("https://api.uspto.gov/api/v1/datasets/products/search"),
            RequestIdentifier = requestIdentifier,
            RetrievedAt = new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero),
            HasRetrievedAt = true,
            IsAuthoritative = true
        };
    }

    private static DatasetFamily[] SupportedFamilies()
    {
        return new[]
        {
            DatasetFamily.PatentFileWrapperComplete,
            DatasetFamily.PatentFileWrapperDelta,
            DatasetFamily.PatentGrantFullText,
            DatasetFamily.PatentApplicationFullText,
            DatasetFamily.PatentAssignment,
            DatasetFamily.OfficeAction,
            DatasetFamily.Ptab,
            DatasetFamily.TrademarkApplicationAndStatus,
            DatasetFamily.TrademarkAssignment,
            DatasetFamily.Ttab,
            DatasetFamily.PatentsViewGrant,
            DatasetFamily.PatentsViewPreGrant,
            DatasetFamily.PatentsViewAnnualized,
            DatasetFamily.PatentsViewSorted
        };
    }

    private static BulkDatasetPreset FindPreset(BulkDatasetPreset[] presets, DatasetFamily family)
    {
        return presets.Single(item => item.Family == family);
    }

    private static string CatalogResponse(string productIdentifier, string title, string requestIdentifier)
    {
        return "{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"" + productIdentifier + "\",\"productTitleText\":\"" + title + "\"}],\"requestIdentifier\":\"" + requestIdentifier + "\"}";
    }

    private static string EmptyCatalogResponse()
    {
        return "{\"count\":0,\"bulkDataProductBag\":[]}";
    }
}
